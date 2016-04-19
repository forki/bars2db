namespace LinqToDB.SqlQuery.QueryElements.Predicates
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using LinqToDB.SqlQuery.QueryElements.Enums;
    using LinqToDB.SqlQuery.QueryElements.Interfaces;
    using LinqToDB.SqlQuery.QueryElements.Predicates.Interfaces;
    using LinqToDB.SqlQuery.QueryElements.SqlElements.Interfaces;
    using LinqToDB.SqlQuery.Search;

    public class ExprExpr : Expr,
                            IExprExpr
    {
        public ExprExpr(IQueryExpression exp1, EOperator op, IQueryExpression exp2)
            : base(exp1, SqlQuery.Precedence.Comparison)
        {
            EOperator = op;
            Expr2    = exp2;
        }

        public EOperator   EOperator { get; private  set; }

        public IQueryExpression Expr2    { get; set; }

        protected override void Walk(bool skipColumns, Func<IQueryExpression,IQueryExpression> func)
        {
            base.Walk(skipColumns, func);
            Expr2 = Expr2.Walk(skipColumns, func);
        }

        public override bool CanBeNull()
        {
            return base.CanBeNull() || Expr2.CanBeNull();
        }

        protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
        {
            if (!doClone(this))
                return this;

            ICloneableElement clone;

            if (!objectTree.TryGetValue(this, out clone))
                objectTree.Add(this, clone = new ExprExpr(
                                                 (IQueryExpression)Expr1.Clone(objectTree, doClone), EOperator, (IQueryExpression)Expr2.Clone(objectTree, doClone)));

            return clone;
        }

        public override EQueryElementType ElementType => EQueryElementType.ExprExprPredicate;

        protected override void ToStringInternal(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
        {
            Expr1.ToString(sb, dic);

            string op;

            switch (EOperator)
            {
                case EOperator.Equal         : op = "=";  break;
                case EOperator.NotEqual      : op = "<>"; break;
                case EOperator.Greater       : op = ">";  break;
                case EOperator.GreaterOrEqual: op = ">="; break;
                case EOperator.NotGreater    : op = "!>"; break;
                case EOperator.Less          : op = "<";  break;
                case EOperator.LessOrEqual   : op = "<="; break;
                case EOperator.NotLess       : op = "!<"; break;
                default: throw new InvalidOperationException();
            }

            sb.Append(" ").Append(op).Append(" ");

            Expr2.ToString(sb, dic);
        }
    }
}