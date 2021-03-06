﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Bars2Db.Common;
using Bars2Db.Extensions;
using Bars2Db.SqlQuery.QueryElements.SqlElements;
using Bars2Db.SqlQuery.QueryElements.SqlElements.Interfaces;

namespace Bars2Db.SqlProvider
{
    using ConverterType = Action<StringBuilder, ISqlDataType, object>;

    public class ValueToSqlConverter
    {
        private static readonly NumberFormatInfo _numberFormatInfo = new NumberFormatInfo
        {
            CurrencyDecimalDigits = NumberFormatInfo.InvariantInfo.CurrencyDecimalDigits,
            CurrencyDecimalSeparator = NumberFormatInfo.InvariantInfo.CurrencyDecimalSeparator,
            CurrencyGroupSeparator = NumberFormatInfo.InvariantInfo.CurrencyGroupSeparator,
            CurrencyGroupSizes = NumberFormatInfo.InvariantInfo.CurrencyGroupSizes,
            CurrencyNegativePattern = NumberFormatInfo.InvariantInfo.CurrencyNegativePattern,
            CurrencyPositivePattern = NumberFormatInfo.InvariantInfo.CurrencyPositivePattern,
            CurrencySymbol = NumberFormatInfo.InvariantInfo.CurrencySymbol,
            NaNSymbol = NumberFormatInfo.InvariantInfo.NaNSymbol,
            NegativeInfinitySymbol = NumberFormatInfo.InvariantInfo.NegativeInfinitySymbol,
            NegativeSign = NumberFormatInfo.InvariantInfo.NegativeSign,
            NumberDecimalDigits = NumberFormatInfo.InvariantInfo.NumberDecimalDigits,
            NumberDecimalSeparator = ".",
            NumberGroupSeparator = NumberFormatInfo.InvariantInfo.NumberGroupSeparator,
            NumberGroupSizes = NumberFormatInfo.InvariantInfo.NumberGroupSizes,
            NumberNegativePattern = NumberFormatInfo.InvariantInfo.NumberNegativePattern,
            PercentDecimalDigits = NumberFormatInfo.InvariantInfo.PercentDecimalDigits,
            PercentDecimalSeparator = ".",
            PercentGroupSeparator = NumberFormatInfo.InvariantInfo.PercentGroupSeparator,
            PercentGroupSizes = NumberFormatInfo.InvariantInfo.PercentGroupSizes,
            PercentNegativePattern = NumberFormatInfo.InvariantInfo.PercentNegativePattern,
            PercentPositivePattern = NumberFormatInfo.InvariantInfo.PercentPositivePattern,
            PercentSymbol = NumberFormatInfo.InvariantInfo.PercentSymbol,
            PerMilleSymbol = NumberFormatInfo.InvariantInfo.PerMilleSymbol,
            PositiveInfinitySymbol = NumberFormatInfo.InvariantInfo.PositiveInfinitySymbol,
            PositiveSign = NumberFormatInfo.InvariantInfo.PositiveSign
        };

        private readonly ValueToSqlConverter[] _baseConverters;
        private readonly Dictionary<Type, ConverterType> _converters = new Dictionary<Type, ConverterType>();

        private ConverterType _booleanConverter;
        private ConverterType _byteConverter;
        private ConverterType _charConverter;
        private ConverterType _dateTimeConverter;
        private ConverterType _decimalConverter;
        private ConverterType _doubleConverter;
        private ConverterType _int16Converter;
        private ConverterType _int32Converter;
        private ConverterType _int64Converter;
        private ConverterType _sByteConverter;
        private ConverterType _singleConverter;
        private ConverterType _stringConverter;
        private ConverterType _uInt16Converter;
        private ConverterType _uInt32Converter;
        private ConverterType _uInt64Converter;

        public ValueToSqlConverter(params ValueToSqlConverter[] converters)
        {
            _baseConverters = converters ?? Array<ValueToSqlConverter>.Empty;
        }

        internal void SetDefauls()
        {
            SetConverter(typeof(bool), (sb, dt, v) => sb.Append((bool) v ? "1" : "0"));
            SetConverter(typeof(char), (sb, dt, v) => BuildChar(sb, (char) v));
            SetConverter(typeof(sbyte), (sb, dt, v) => sb.Append((sbyte) v));
            SetConverter(typeof(byte), (sb, dt, v) => sb.Append((byte) v));
            SetConverter(typeof(short), (sb, dt, v) => sb.Append((short) v));
            SetConverter(typeof(ushort), (sb, dt, v) => sb.Append((ushort) v));
            SetConverter(typeof(int), (sb, dt, v) => sb.Append((int) v));
            SetConverter(typeof(uint), (sb, dt, v) => sb.Append((uint) v));
            SetConverter(typeof(long), (sb, dt, v) => sb.Append((long) v));
            SetConverter(typeof(ulong), (sb, dt, v) => sb.Append((ulong) v));
            SetConverter(typeof(float), (sb, dt, v) => sb.Append(((float) v).ToString(_numberFormatInfo)));
            SetConverter(typeof(double), (sb, dt, v) => sb.Append(((double) v).ToString(_numberFormatInfo)));
            SetConverter(typeof(decimal), (sb, dt, v) => sb.Append(((decimal) v).ToString(_numberFormatInfo)));
            SetConverter(typeof(DateTime), (sb, dt, v) => BuildDateTime(sb, (DateTime) v));
            SetConverter(typeof(string), (sb, dt, v) => BuildString(sb, v.ToString()));
            SetConverter(typeof(Guid), (sb, dt, v) => sb.Append('\'').Append(v).Append('\''));
        }

        private static void BuildString(StringBuilder stringBuilder, string value)
        {
            stringBuilder
                .Append('\'')
                .Append(value.Replace("'", "''"))
                .Append('\'');
        }

        private static void BuildChar(StringBuilder stringBuilder, char value)
        {
            stringBuilder.Append('\'');

            if (value == '\'') stringBuilder.Append("''");
            else stringBuilder.Append(value);

            stringBuilder.Append('\'');
        }

        private static void BuildDateTime(StringBuilder stringBuilder, DateTime value)
        {
            var format = "'{0:yyyy-MM-dd HH:mm:ss.fff}'";

            if (value.Millisecond == 0)
            {
                format = value.Hour == 0 && value.Minute == 0 && value.Second == 0
                    ? "'{0:yyyy-MM-dd}'"
                    : "'{0:yyyy-MM-dd HH:mm:ss}'";
            }

            stringBuilder.AppendFormat(format, value);
        }

        public bool TryConvert(StringBuilder stringBuilder, object value)
        {
            if (value == null)
            {
                stringBuilder.Append("NULL");
                return true;
            }

            return TryConvert(stringBuilder, new SqlDataType(value.GetType()), value);
        }

        public bool TryConvert(StringBuilder stringBuilder, ISqlDataType dataType, object value)
        {
            if (value == null)
            {
                stringBuilder.Append("NULL");
                return true;
            }

            var type = value.GetType();

            ConverterType converter = null;

            if (_converters.Count > 0 && !type.IsEnumEx())
            {
                switch (type.GetTypeCodeEx())
                {
                    case TypeCode.DBNull:
                        stringBuilder.Append("NULL");
                        return true;
                    case TypeCode.Boolean:
                        converter = _booleanConverter;
                        break;
                    case TypeCode.Char:
                        converter = _charConverter;
                        break;
                    case TypeCode.SByte:
                        converter = _sByteConverter;
                        break;
                    case TypeCode.Byte:
                        converter = _byteConverter;
                        break;
                    case TypeCode.Int16:
                        converter = _int16Converter;
                        break;
                    case TypeCode.UInt16:
                        converter = _uInt16Converter;
                        break;
                    case TypeCode.Int32:
                        converter = _int32Converter;
                        break;
                    case TypeCode.UInt32:
                        converter = _uInt32Converter;
                        break;
                    case TypeCode.Int64:
                        converter = _int64Converter;
                        break;
                    case TypeCode.UInt64:
                        converter = _uInt64Converter;
                        break;
                    case TypeCode.Single:
                        converter = _singleConverter;
                        break;
                    case TypeCode.Double:
                        converter = _doubleConverter;
                        break;
                    case TypeCode.Decimal:
                        converter = _decimalConverter;
                        break;
                    case TypeCode.DateTime:
                        converter = _dateTimeConverter;
                        break;
                    case TypeCode.String:
                        converter = _stringConverter;
                        break;
                    default:
                        _converters.TryGetValue(type, out converter);
                        break;
                }
            }

            if (converter != null)
            {
                converter(stringBuilder, dataType, value);
                return true;
            }

            if (_baseConverters.Length > 0)
                foreach (var valueConverter in _baseConverters)
                    if (valueConverter.TryConvert(stringBuilder, dataType, value))
                        return true;

            return false;
        }

        public StringBuilder Convert(StringBuilder stringBuilder, object value)
        {
            if (!TryConvert(stringBuilder, value))
                stringBuilder.Append(value);
            return stringBuilder;
        }

        public StringBuilder Convert(StringBuilder stringBuilder, ISqlDataType dataType, object value)
        {
            if (!TryConvert(stringBuilder, dataType, value))
                stringBuilder.Append(value);
            return stringBuilder;
        }

        public void SetConverter(Type type, ConverterType converter)
        {
            if (converter == null)
            {
                if (_converters.ContainsKey(type))
                    _converters.Remove(type);
            }
            else
            {
                _converters[type] = converter;

                switch (type.GetTypeCodeEx())
                {
                    case TypeCode.Boolean:
                        _booleanConverter = converter;
                        return;
                    case TypeCode.Char:
                        _charConverter = converter;
                        return;
                    case TypeCode.SByte:
                        _sByteConverter = converter;
                        return;
                    case TypeCode.Byte:
                        _byteConverter = converter;
                        return;
                    case TypeCode.Int16:
                        _int16Converter = converter;
                        return;
                    case TypeCode.UInt16:
                        _uInt16Converter = converter;
                        return;
                    case TypeCode.Int32:
                        _int32Converter = converter;
                        return;
                    case TypeCode.UInt32:
                        _uInt32Converter = converter;
                        return;
                    case TypeCode.Int64:
                        _int64Converter = converter;
                        return;
                    case TypeCode.UInt64:
                        _uInt64Converter = converter;
                        return;
                    case TypeCode.Single:
                        _singleConverter = converter;
                        return;
                    case TypeCode.Double:
                        _doubleConverter = converter;
                        return;
                    case TypeCode.Decimal:
                        _decimalConverter = converter;
                        return;
                    case TypeCode.DateTime:
                        _dateTimeConverter = converter;
                        return;
                    case TypeCode.String:
                        _stringConverter = converter;
                        return;
                }
            }
        }
    }
}