﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Bars2Db.Common;
using Bars2Db.Expressions;
using Bars2Db.Extensions;
using Bars2Db.Metadata;
using Bars2Db.SqlQuery.QueryElements.SqlElements;
using Bars2Db.SqlQuery.QueryElements.SqlElements.Interfaces;

namespace Bars2Db.Mapping
{
    internal class MappingSchemaInfo
    {
        #region Options

        public StringComparison? ColumnComparisonOption;

        #endregion

        public string Configuration;
        public IMetadataReader MetadataReader;

        public MappingSchemaInfo(string configuration)
        {
            Configuration = configuration;
        }

        #region Default Values

        private volatile ConcurrentDictionary<Type, object> _defaultValues;

        public Option<object> GetDefaultValue(Type type)
        {
            if (_defaultValues == null)
                return Option<object>.None;

            object o;
            return _defaultValues.TryGetValue(type, out o) ? Option<object>.Some(o) : Option<object>.None;
        }

        public void SetDefaultValue(Type type, object value)
        {
            if (_defaultValues == null)
                lock (this)
                    if (_defaultValues == null)
                        _defaultValues = new ConcurrentDictionary<Type, object>();

            _defaultValues[type] = value;
        }

        #endregion

        #region CanBeNull

        private volatile ConcurrentDictionary<Type, bool> _canBeNull;

        public Option<bool> GetCanBeNull(Type type)
        {
            if (_canBeNull == null)
                return Option<bool>.None;

            bool o;
            return _canBeNull.TryGetValue(type, out o) ? Option<bool>.Some(o) : Option<bool>.None;
        }

        public void SetCanBeNull(Type type, bool value)
        {
            if (_canBeNull == null)
                lock (this)
                    if (_canBeNull == null)
                        _canBeNull = new ConcurrentDictionary<Type, bool>();

            _canBeNull[type] = value;
        }

        #endregion

        #region GenericConvertProvider

        private volatile Dictionary<Type, List<Type[]>> _genericConvertProviders;

        public bool InitGenericConvertProvider(Type[] types, MappingSchema mappingSchema)
        {
            var changed = false;

            if (_genericConvertProviders != null)
            {
                lock (_genericConvertProviders)
                {
                    foreach (var type in _genericConvertProviders)
                    {
                        var args = type.Key.GetGenericArgumentsEx();

                        if (args.Length == types.Length)
                        {
                            if (type.Value.Aggregate(false, (cur, ts) => cur || ts.SequenceEqual(types)))
                                continue;

                            var gtype = type.Key.MakeGenericType(types);
                            var provider = (IGenericInfoProvider) Activator.CreateInstance(gtype);

                            provider.SetInfo(new MappingSchema(this));

                            type.Value.Add(types);

                            changed = true;
                        }
                    }
                }
            }

            return changed;
        }

        public void SetGenericConvertProvider(Type type)
        {
            if (_genericConvertProviders == null)
                lock (this)
                    if (_genericConvertProviders == null)
                        _genericConvertProviders = new Dictionary<Type, List<Type[]>>();

            if (!_genericConvertProviders.ContainsKey(type))
                lock (_genericConvertProviders)
                    if (!_genericConvertProviders.ContainsKey(type))
                        _genericConvertProviders[type] = new List<Type[]>();
        }

        #endregion

        #region ConvertInfo

        private ConvertInfo _convertInfo;

        public void SetConvertInfo(Type from, Type to, ConvertInfo.LambdaInfo expr)
        {
            if (_convertInfo == null)
                _convertInfo = new ConvertInfo();
            _convertInfo.Set(from, to, expr);
        }

        public ConvertInfo.LambdaInfo GetConvertInfo(Type from, Type to)
        {
            return _convertInfo == null ? null : _convertInfo.Get(from, to);
        }

        private ConcurrentDictionary<object, Func<object, object>> _converters;

        public ConcurrentDictionary<object, Func<object, object>> Converters
            => _converters ?? (_converters = new ConcurrentDictionary<object, Func<object, object>>());

        #endregion

        #region Scalar Types

        private volatile ConcurrentDictionary<Type, bool> _scalarTypes;

        public Option<bool> GetScalarType(Type type)
        {
            if (_scalarTypes != null)
            {
                bool isScalarType;
                if (_scalarTypes.TryGetValue(type, out isScalarType))
                    return Option<bool>.Some(isScalarType);
            }

            return Option<bool>.None;
        }

        public void SetScalarType(Type type, bool isScalarType = true)
        {
            if (_scalarTypes == null)
                lock (this)
                    if (_scalarTypes == null)
                        _scalarTypes = new ConcurrentDictionary<Type, bool>();

            _scalarTypes[type] = isScalarType;
        }

        #endregion

        #region DataTypes

        private volatile ConcurrentDictionary<Type, ISqlDataType> _dataTypes;

        public Option<ISqlDataType> GetDataType(Type type)
        {
            if (_dataTypes == null)
            {
                return Option<ISqlDataType>.None;
            }

            ISqlDataType dataType;
            return _dataTypes.TryGetValue(type, out dataType)
                ? Option<ISqlDataType>.Some(dataType)
                : Option<ISqlDataType>.None;
        }

        public void SetDataType(Type type, DataType dataType)
        {
            SetDataType(type, new SqlDataType(dataType, type, null, null, null));
        }

        public void SetDataType(Type type, ISqlDataType dataType)
        {
            if (_dataTypes == null)
                lock (this)
                    if (_dataTypes == null)
                        _dataTypes = new ConcurrentDictionary<Type, ISqlDataType>();

            _dataTypes[type] = dataType;
        }

        #endregion
    }
}