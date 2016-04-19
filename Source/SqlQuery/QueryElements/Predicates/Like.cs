namespace LinqToDB.SqlQuery.QueryElements.Predicates
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using LinqToDB.SqlQuery.QueryElements.Enums;
    using LinqToDB.SqlQuery.QueryElements.Interfaces;
    using LinqToDB.SqlQuery.QueryElements.Predicates.Interfaces;
    using LinqToDB.SqlQuery.QueryElements.SqlElements.Interfaces;

    public class Like : NotExpr,
                        ILike
    {
        public Like(IQueryExpression exp1, bool isNot, IQueryExpression exp2, IQueryExpression escape)
            : base(exp1, isNot, SqlQuery.Precedence.Comparison)
        {
            Expr2  = exp2;
            Escape = escape;
        }

        public IQueryExpression Expr2  { get; set; }

        public IQueryExpression Escape { get; set; }

        protected override void Walk(bool skipColumns, Func<IQueryExpression,IQueryExpression> func)
        {
            base.Walk(skipColumns, func);
            Expr2 = Expr2.Walk(skipColumns, func);

            Escape = Escape?.Walk(skipColumns, func);
        }

        protected override ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
        {
            if (!doClone(this))
                return this;

            ICloneableElement clone;

            if (!objectTree.TryGetValue(this, out clone))
                objectTree.Add(this, clone = new Like(
                                                 (IQueryExpression)Expr1.Clone(objectTree, doClone), IsNot, (IQueryExpression)Expr2.Clone(objectTree, doClone), Escape));

            return clone;
        }

        public override EQueryElementType ElementType => EQueryElementType.LikePredicate;

        protected override void ToStringInternal(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
        {
            Expr1.ToString(sb, dic);

            sb.Append(GetOperator());

            Expr2.ToString(sb, dic);

            if (Escape != null)
            {
                sb.Append(" ESCAPE ");
                Escape.ToString(sb, dic);
            }
        }

        public virtual string GetOperator()
        {
            if (IsNot) return " NOT LIKE ";
            return " LIKE ";
        }
    }
}