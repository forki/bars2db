﻿namespace LinqToDB.SqlQuery.QueryElements.SqlElements
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using LinqToDB.Extensions;
    using LinqToDB.SqlQuery.QueryElements;
    using LinqToDB.SqlQuery.QueryElements.Enums;
    using LinqToDB.SqlQuery.QueryElements.Interfaces;
    using LinqToDB.SqlQuery.QueryElements.SqlElements.Interfaces;

    public interface ISqlParameter : IQueryExpression,
                                     IValueContainer
    {
        string Name { get; set; }

        bool IsQueryParameter { get; set; }

        DataType DataType { get; set; }

        int DbSize { get; set; }

        string LikeStart { get; set; }

        string LikeEnd { get; set; }

        bool ReplaceLike { get; set; }

        void SetTakeConverter(int take);

        Func<object, object> ValueConverter { get; set; }

        object RawValue { get; }
    }

    public class SqlParameter : BaseQueryElement,
                                ISqlParameter
    {
		public SqlParameter(Type systemType, string name, object value)
		{
			if (systemType.ToNullableUnderlying().IsEnumEx())
				throw new ArgumentException();

			IsQueryParameter = true;
			Name             = name;
			SystemType       = systemType;
			_value           = value;
			DataType         = DataType.Undefined;
		}

		public SqlParameter(Type systemType, string name, object value, Func<object,object> valueConverter)
			: this(systemType, name, value)
		{
			_valueConverter = valueConverter;
		}

		public string   Name             { get; set; }
		public Type     SystemType       { get; set; }
		public bool     IsQueryParameter { get; set; }
		public DataType DataType         { get; set; }
		public int      DbSize           { get; set; }
		public string   LikeStart        { get; set; }
		public string   LikeEnd          { get; set; }
		public bool     ReplaceLike      { get; set; }

		private object _value;
		public  object  Value
		{
			get
			{
				var value = _value;

				if (ReplaceLike)
				{
					value = value?.ToString().Replace("[", "[[]");
				}

				if (LikeStart != null)
				{
					if (value != null)
					{
						return value.ToString().IndexOfAny(new[] { '%', '_' }) < 0 ?
							LikeStart + value + LikeEnd :
							LikeStart + EscapeLikeText(value.ToString()) + LikeEnd;
					}
				}

				var valueConverter = ValueConverter;
				return valueConverter == null? value: valueConverter(value);
			}

			set { _value = value; }
		}

		public object RawValue => _value;

        #region Value Converter

		internal List<int>  TakeValues;

		private Func<object,object> _valueConverter;
		public  Func<object,object>  ValueConverter
		{
			get
			{
				if (_valueConverter == null && TakeValues != null)
					foreach (var take in TakeValues.ToArray())
						SetTakeConverter(take);

				return _valueConverter;
			}

			set { _valueConverter = value; }
		}

		public void SetTakeConverter(int take)
		{
			if (TakeValues == null)
				TakeValues = new List<int>();

			TakeValues.Add(take);

			SetTakeConverterInternal(take);
		}

		void SetTakeConverterInternal(int take)
		{
			var conv = _valueConverter;

			if (conv == null)
				_valueConverter = v => v == null ? null : (object) ((int) v + take);
			else
				_valueConverter = v => v == null ? null : (object) ((int) conv(v) + take);
		}

		static string EscapeLikeText(string text)
		{
			if (text.IndexOfAny(new[] { '%', '_' }) < 0)
				return text;

			var builder = new StringBuilder(text.Length);

			foreach (var ch in text)
			{
				switch (ch)
				{
					case '%':
					case '_':
					case '~':
						builder.Append('~');
						break;
				}

				builder.Append(ch);
			}

			return builder.ToString();
		}

		#endregion

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		#region ISqlExpression Members

		public int Precedence => SqlQuery.Precedence.Primary;

        #endregion

		#region ISqlExpressionWalkable Members

		IQueryExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<IQueryExpression,IQueryExpression> func)
		{
			return func(this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<IQueryExpression>.Equals(IQueryExpression other)
		{
			if (this == other)
				return true;

			var p = other as ISqlParameter;
			return p != null && Name != null && p.Name != null && Name == p.Name && SystemType == p.SystemType;
		}

		#endregion

		#region ISqlExpression Members

		public bool CanBeNull()
		{
			if (SystemType == null && _value == null)
				return true;

			return SqlDataType.CanBeNull(SystemType ?? _value.GetType());
		}

		public bool Equals(IQueryExpression other, Func<IQueryExpression,IQueryExpression,bool> comparer)
		{
			return ((IQueryExpression)this).Equals(other) && comparer(this, other);
		}

		#endregion

		#region ICloneableElement Members

		public ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			ICloneableElement clone;

			if (!objectTree.TryGetValue(this, out clone))
			{
				var p = new SqlParameter(SystemType, Name, _value, _valueConverter)
					{
						IsQueryParameter = IsQueryParameter,
						DataType         = DataType,
						DbSize           = DbSize,
						LikeStart        = LikeStart,
						LikeEnd          = LikeEnd,
						ReplaceLike      = ReplaceLike,
					};

				objectTree.Add(this, clone = p);
			}

			return clone;
		}

		#endregion

		#region IQueryElement Members

        public override void GetChildren(LinkedList<IQueryElement> list)
        {
        }

        public override EQueryElementType ElementType => EQueryElementType.SqlParameter;

        public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			return sb
				.Append('@')
				.Append(Name ?? "parameter")
				.Append('[')
				.Append(Value ?? "NULL")
				.Append(']');
		}

		#endregion
	}
}
