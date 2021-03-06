﻿using System;
using System.Linq;
using System.Linq.Expressions;
using Bars2Db.Expressions;
using Bars2Db.SqlQuery.QueryElements;
using Bars2Db.SqlQuery.QueryElements.Interfaces;
using Bars2Db.SqlQuery.QueryElements.SqlElements.Interfaces;

namespace Bars2Db.Linq.Builder
{
    internal class SubQueryContext : PassThroughContext
    {
        public SubQueryContext(IBuildContext subQuery, ISelectQuery selectQuery, bool addToSql)
            : base(subQuery)
        {
            if (selectQuery == subQuery.Select)
                throw new ArgumentException("Wrong subQuery argument.", nameof(subQuery));

            SubQuery = subQuery;
            SubQuery.Parent = this;
            Select = selectQuery;

            if (addToSql)
                selectQuery.From.Table(SubQuery.Select);
        }

        public SubQueryContext(IBuildContext subQuery, bool addToSql = true)
            : this(subQuery, new SelectQuery {ParentSelect = subQuery.Select.ParentSelect}, addToSql)
        {
        }

        public IBuildContext SubQuery { get; }
        public override ISelectQuery Select { get; set; }
        public override IBuildContext Parent { get; set; }

        public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
        {
            if (Expression.NodeType == ExpressionType.Lambda)
            {
                var le = (LambdaExpression) Expression;

                if (le.Parameters.Count == 2 ||
                    le.Parameters.Count == 1 && null != Expression.Find(
                        e => e.NodeType == ExpressionType.Call && ((MethodCallExpression) e).IsQueryable()))
                {
                    if (le.Body.NodeType == ExpressionType.New)
                    {
                        var ne = (NewExpression) le.Body;
                        var p = Expression.Parameter(ne.Type, "p");

                        var seq = new SelectContext(
                            Parent,
                            Expression.Lambda(
                                Expression.New(
                                    ne.Constructor,
                                    ne.Members.Select(m => Expression.MakeMemberAccess(p, m)),
                                    ne.Members),
                                p),
                            this);

                        seq.BuildQuery(query, queryParameter);

                        return;
                    }

                    if (le.Body.NodeType == ExpressionType.MemberInit)
                    {
                        var mi = (MemberInitExpression) le.Body;

                        if (mi.NewExpression.Arguments.Count == 0 && mi.Bindings.All(b => b is MemberAssignment))
                        {
                            var p = Expression.Parameter(mi.Type, "p");

                            var seq = new SelectContext(
                                Parent,
                                Expression.Lambda(
                                    Expression.MemberInit(
                                        mi.NewExpression,
                                        mi.Bindings
                                            .OfType<MemberAssignment>()
                                            .Select(
                                                ma =>
                                                    Expression.Bind(ma.Member, Expression.MakeMemberAccess(p, ma.Member)))),
                                    p),
                                this);

                            seq.BuildQuery(query, queryParameter);

                            return;
                        }
                    }
                }
            }

            base.BuildQuery(query, queryParameter);
        }

        public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
        {
            return SubQuery
                .ConvertToIndex(expression, level, flags)
                .Select(idx => new SqlInfo(idx.Members) {Sql = SubQuery.Select.Select.Columns[idx.Index]})
                .ToArray();
        }

        // JoinContext has similar logic. Consider to review it.
        //
        public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
        {
            return ConvertToSql(expression, level, flags)
                .Select(idx =>
                {
                    idx.Query = Select;
                    idx.Index = GetIndex((IColumn) idx.Sql);

                    return idx;
                })
                .ToArray();
        }

        public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor testFlag)
        {
            switch (testFlag)
            {
                case RequestFor.SubQuery:
                    return IsExpressionResult.True;
            }

            return base.IsExpression(expression, level, testFlag);
        }

        private int GetIndex(IColumn column)
        {
            return Select.Select.Add(column);
        }

        public override int ConvertToParentIndex(int index, IBuildContext context)
        {
            var idx = GetIndex(context.Select.Select.Columns[index]);
            return Parent == null ? idx : Parent.ConvertToParentIndex(idx, this);
        }

        public override void SetAlias(string alias)
        {
#if NETFX_CORE
            if (alias.Contains("<"))
#else
            if (alias.Contains('<'))
#endif
                return;

            if (Select.From.Tables.First.Value.Alias == null)
                Select.From.Tables.First.Value.Alias = alias;
        }

        public override IQueryExpression GetSubQuery(IBuildContext context)
        {
            return null;
        }
    }
}