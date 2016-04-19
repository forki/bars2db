namespace LinqToDB.SqlQuery.QueryElements
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using LinqToDB.SqlQuery.QueryElements.Enums;
    using LinqToDB.SqlQuery.QueryElements.Interfaces;
    using LinqToDB.SqlQuery.QueryElements.SqlElements;
    using LinqToDB.SqlQuery.QueryElements.SqlElements.Interfaces;
    using LinqToDB.SqlQuery.Search;

    public class CreateTableStatement : BaseQueryElement,
                                        ICreateTableStatement
    {
        public ISqlTable       Table           { get; set; }

        public bool           IsDrop          { get; set; }
        public string         StatementHeader { get; set; }
        public string         StatementFooter { get; set; }
        public EDefaulNullable EDefaulNullable  { get; set; }

        #region IQueryElement Members

        public override EQueryElementType ElementType => EQueryElementType.CreateTableStatement;

        public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
        {
            sb.Append(IsDrop ? "DROP TABLE " : "CREATE TABLE ");

            Table?.ToString(sb, dic);

            sb.AppendLine();

            return sb;
        }

        #endregion

        #region ISqlExpressionWalkable Members

        IQueryExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<IQueryExpression,IQueryExpression> func)
        {
            Table?.Walk(skipColumns, func);

            return null;
        }

        #endregion

        #region ICloneableElement Members

        public ICloneableElement Clone(Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
        {
            if (!doClone(this))
                return this;

            var clone = new CreateTableStatement { };

            if (Table != null)
                clone.Table = (ISqlTable)Table.Clone(objectTree, doClone);

            objectTree.Add(this, clone);

            return clone;
        }

        #endregion
    }
}