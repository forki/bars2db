﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using Bars2Db.Expressions;
using Bars2Db.Metadata;

namespace Bars2Db.Mapping
{
    public class FluentMappingBuilder
    {
        public EntityMappingBuilder<T> Entity<T>(string configuration = null)
        {
            return new EntityMappingBuilder<T>(this, configuration);
        }

        #region Init

        public FluentMappingBuilder([Properties.NotNull] MappingSchema mappingSchema)
        {
            if (mappingSchema == null) throw new ArgumentNullException(nameof(mappingSchema));

            MappingSchema = mappingSchema;
            MappingSchema.AddMetadataReader(_reader);
        }

        public MappingSchema MappingSchema { get; }

        private readonly FluentMetadataReader _reader = new FluentMetadataReader();

        #endregion

        #region GetAtributes

        public T[] GetAttributes<T>(Type type)
            where T : Attribute
        {
            return _reader.GetAttributes<T>(type);
        }

        public T[] GetAttributes<T>(MemberInfo memberInfo)
            where T : Attribute
        {
            return _reader.GetAttributes<T>(memberInfo);
        }

        #endregion

        #region HasAtribute

        public FluentMappingBuilder HasAttribute(Type type, Attribute attribute)
        {
            _reader.AddAttribute(type, attribute);
            return this;
        }

        public FluentMappingBuilder HasAttribute<T>(Attribute attribute)
        {
            _reader.AddAttribute(typeof(T), attribute);
            return this;
        }

        public FluentMappingBuilder HasAttribute(MemberInfo memberInfo, Attribute attribute)
        {
            _reader.AddAttribute(memberInfo, attribute);
            return this;
        }

        public FluentMappingBuilder HasAttribute(LambdaExpression func, Attribute attribute)
        {
            var memberInfo = MemberHelper.GetMemeberInfo(func);
            _reader.AddAttribute(memberInfo, attribute);
            return this;
        }

        public FluentMappingBuilder HasAttribute<T>(Expression<Func<T, object>> func, Attribute attribute)
        {
            var memberInfo = MemberHelper.MemberOf(func);
            _reader.AddAttribute(memberInfo, attribute);
            return this;
        }

        #endregion
    }
}