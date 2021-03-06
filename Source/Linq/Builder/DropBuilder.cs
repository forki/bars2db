﻿using System;
using System.Linq.Expressions;
using Bars2Db.Expressions;
using Bars2Db.SqlQuery.QueryElements.Enums;

namespace Bars2Db.Linq.Builder
{
    internal class DropBuilder : MethodCallBuilder
    {
        #region DropContext

        private class DropContext : SequenceContextBase
        {
            public DropContext(IBuildContext parent, IBuildContext sequence)
                : base(parent, sequence, null)
            {
            }

            public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
            {
                query.SetNonQueryQuery();
            }

            public override Expression BuildExpression(Expression expression, int level)
            {
                throw new NotImplementedException();
            }

            public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
            {
                throw new NotImplementedException();
            }

            public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
            {
                throw new NotImplementedException();
            }

            public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
            {
                throw new NotImplementedException();
            }

            public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region DropBuilder

        protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall,
            BuildInfo buildInfo)
        {
            return methodCall.IsQueryable("Drop");
        }

        protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall,
            BuildInfo buildInfo)
        {
            var sequence =
                (TableBuilder.TableContext) builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));

            sequence.Select.EQueryType = EQueryType.CreateTable;
            sequence.Select.CreateTable.Table = sequence.SqlTable;
            sequence.Select.CreateTable.IsDrop = true;

            return new DropContext(buildInfo.Parent, sequence);
        }

        protected override SequenceConvertInfo Convert(
            ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
        {
            return null;
        }

        #endregion
    }
}