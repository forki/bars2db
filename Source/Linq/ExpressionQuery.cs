﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Bars2Db.Extensions;
using Bars2Db.Properties;

namespace Bars2Db.Linq
{
    public abstract class ExpressionQuery<T> : IExpressionQuery<T>
    {
        #region Init

        protected void Init(IDataContextInfo dataContextInfo, Expression expression)
        {
#if SILVERLIGHT || NETFX_CORE
            if (dataContextInfo == null) throw new ArgumentNullException("dataContextInfo");

            DataContextInfo = dataContextInfo;
#else
            DataContextInfo = dataContextInfo ?? new DefaultDataContextInfo();
#endif
            Expression = expression ?? Expression.Constant(this);
        }

        [NotNull]
        public Expression Expression { get; set; }

        [NotNull]
        public IDataContextInfo DataContextInfo { get; set; }

        internal Query<T> Info;
        internal object[] Parameters;

        #endregion

        #region Public Members

        [DebuggerBrowsable(DebuggerBrowsableState.Never)] private string _sqlTextHolder;

// ReSharper disable InconsistentNaming
        [UsedImplicitly]
        private string _sqlText => SqlText;

        // ReSharper restore InconsistentNaming

        public string SqlText
        {
            get
            {
                if (_sqlTextHolder == null)
                {
                    var info = GetQuery(Expression, false);
                    var sqlText = info.GetSqlText(DataContextInfo.DataContext, Expression, Parameters, 0);

                    _sqlTextHolder = sqlText;
                }

                return _sqlTextHolder;
            }
        }

        #endregion

        #region Execute

        public IEnumerable Execute(Expression expression)
        {
            return Execute(DataContextInfo, expression);
        }

        private IEnumerable<T> Execute(IDataContextInfo dataContextInfo, Expression expression)
        {
            return GetQuery(expression, true).GetIEnumerable(null, dataContextInfo, expression, Parameters);
        }

        private Query<T> GetQuery(Expression expression, bool cache)
        {
            if (cache && Info != null)
                return Info;

            var info = Query<T>.GetQuery(DataContextInfo, expression, false);

            if (cache)
                Info = info;

            return info;
        }

        public Query GetQuery()
        {
            return Query<T>.GetQuery(DataContextInfo, Expression, true);
        }

        #endregion

        #region IQueryable Members

        Type IQueryable.ElementType => typeof(T);

        Expression IQueryable.Expression => Expression;

        IQueryProvider IQueryable.Provider => this;

        #endregion

        #region IQueryProvider Members

        IQueryable<TElement> IQueryProvider.CreateQuery<TElement>(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            return new ExpressionQueryImpl<TElement>(DataContextInfo, expression);
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            var elementType = expression.Type.GetItemType() ?? expression.Type;

            try
            {
                return
                    (IQueryable)
                        Activator.CreateInstance(typeof(ExpressionQueryImpl<>).MakeGenericType(elementType),
                            DataContextInfo, expression);
            }
            catch (TargetInvocationException ex)
            {
                throw ex.InnerException;
            }
        }

        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            return (TResult) GetQuery(expression, false).GetElement(null, DataContextInfo, expression, Parameters);
        }

        object IQueryProvider.Execute(Expression expression)
        {
            return GetQuery(expression, false).GetElement(null, DataContextInfo, expression, Parameters);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return Execute(DataContextInfo, Expression).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Execute(DataContextInfo, Expression).GetEnumerator();
        }

        #endregion
    }
}