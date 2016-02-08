namespace LinqToDB.SqlQuery.QueryElements.Clauses
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using LinqToDB.SqlQuery.QueryElements.Conditions;
    using LinqToDB.SqlQuery.QueryElements.Interfaces;
    using LinqToDB.SqlQuery.QueryElements.SqlElements;
    using LinqToDB.SqlQuery.QueryElements.SqlElements.Interfaces;

    public class FromClause : ClauseBase, IQueryElement, ISqlExpressionWalkable
    {
        internal FromClause(ISelectQuery selectQuery) : base(selectQuery)
        {
        }

        internal FromClause(
            ISelectQuery selectQuery,
            FromClause  clone,
            Dictionary<ICloneableElement,ICloneableElement> objectTree,
            Predicate<ICloneableElement> doClone)
            : base(selectQuery)
        {
            _tables.AddRange(clone._tables.Select(ts => (ITableSource)ts.Clone(objectTree, doClone)));
        }

        internal FromClause(IEnumerable<ITableSource> tables)
            : base(null)
        {
            _tables.AddRange(tables);
        }

        public FromClause Table(ISqlTableSource table, params Join[] joins)
        {
            return Table(table, null, joins);
        }

        public FromClause Table(ISqlTableSource table, string alias, params Join[] joins)
        {
            var ts = AddOrGetTable(table, alias);

            if (joins != null && joins.Length > 0)
                foreach (var join in joins)
                    ts.Joins.Add(@join.JoinedTable);

            return this;
        }

        ITableSource GetTable(ISqlTableSource table, string alias)
        {
            foreach (var ts in Tables)
                if (ts.Source == table)
                    if (alias == null || ts.Alias == alias)
                        return ts;
                    else
                        throw new ArgumentException("alias");

            return null;
        }

        ITableSource AddOrGetTable(ISqlTableSource table, string alias)
        {
            var ts = GetTable(table, alias);

            if (ts != null)
                return ts;

            var t = new TableSource(table, alias);

            Tables.Add(t);

            return t;
        }

        public ITableSource this[ISqlTableSource table] => this[table, null];

        public ITableSource this[ISqlTableSource table, string alias]
        {
            get
            {
                foreach (var ts in Tables)
                {
                    var t = QueryElements.SelectQuery.CheckTableSource(ts, table, alias);

                    if (t != null)
                        return t;
                }

                return null;
            }
        }

        public bool IsChild(ISqlTableSource table)
        {
            return Tables.Any(ts => ts.Source == table || CheckChild(ts.Joins, table));
        }

        static bool CheckChild(IEnumerable<JoinedTable> joins, ISqlTableSource table)
        {
            foreach (var j in joins)
                if (j.Table.Source == table || CheckChild(j.Table.Joins, table))
                    return true;
            return false;
        }

        readonly List<ITableSource> _tables = new List<ITableSource>();
        public   List<ITableSource>  Tables => _tables;

        static IEnumerable<ISqlTableSource> GetJoinTables(ITableSource source, QueryElementType elementType)
        {
            if (source.Source.ElementType == elementType)
                yield return source.Source;

            foreach (var join in source.Joins)
                foreach (var table in GetJoinTables(@join.Table, elementType))
                    yield return table;
        }

        internal IEnumerable<ISqlTableSource> GetFromTables()
        {
            return Tables.SelectMany(_ => GetJoinTables(_, QueryElementType.SqlTable));
        }

        internal IEnumerable<ISqlTableSource> GetFromQueries()
        {
            return Tables.SelectMany(_ => GetJoinTables(_, QueryElementType.SqlQuery));
        }

        static ITableSource FindTableSource(ITableSource source, SqlTable table)
        {
            if (source.Source == table)
                return source;

            foreach (var join in source.Joins)
            {
                var ts = FindTableSource(@join.Table, table);
                if (ts != null)
                    return ts;
            }

            return null;
        }

        public ISqlTableSource FindTableSource(SqlTable table)
        {
            foreach (var source in Tables)
            {
                var ts = FindTableSource(source, table);
                if (ts != null)
                    return ts;
            }

            return null;
        }

        #region ISqlExpressionWalkable Members

        ISqlExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
        {
            for (var i = 0; i <	Tables.Count; i++)
                Tables[i].Walk(skipColumns, func);

            return null;
        }

        #endregion

        #region IQueryElement Members

        protected override void GetChildrenInternal(List<IQueryElement> list)
        {
            list.AddRange(Tables);
        }

        public override QueryElementType ElementType => QueryElementType.FromClause;

        public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
        {
            sb.Append(" \nFROM \n");

            if (Tables.Count > 0)
            {
                foreach (IQueryElement ts in Tables)
                {
                    sb.Append('\t');
                    var len = sb.Length;
                    ts.ToString(sb, dic).Replace("\n", "\n\t", len, sb.Length - len);
                    sb.Append(", ");
                }

                sb.Length -= 2;
            }

            return sb;
        }

        #endregion
    }
}