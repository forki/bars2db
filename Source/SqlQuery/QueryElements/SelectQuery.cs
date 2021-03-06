﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Bars2Db.Extensions;
using Bars2Db.Reflection;
using Bars2Db.SqlQuery.QueryElements.Clauses;
using Bars2Db.SqlQuery.QueryElements.Clauses.Interfaces;
using Bars2Db.SqlQuery.QueryElements.Conditions;
using Bars2Db.SqlQuery.QueryElements.Conditions.Interfaces;
using Bars2Db.SqlQuery.QueryElements.Enums;
using Bars2Db.SqlQuery.QueryElements.Interfaces;
using Bars2Db.SqlQuery.QueryElements.Predicates;
using Bars2Db.SqlQuery.QueryElements.Predicates.Interfaces;
using Bars2Db.SqlQuery.QueryElements.SqlElements;
using Bars2Db.SqlQuery.QueryElements.SqlElements.Enums;
using Bars2Db.SqlQuery.QueryElements.SqlElements.Interfaces;

namespace Bars2Db.SqlQuery.QueryElements
{
    public class SelectQuery : BaseQueryElement,
        ISelectQuery
    {
        public ISelectClause Select { get; private set; }

        public ICreateTableStatement CreateTable { get; private set; } = new CreateTableStatement();

        public IInsertClause Insert { get; private set; } = new InsertClause();

        public void ClearInsert()
        {
            Insert = null;
        }


        public IUpdateClause Update { get; private set; } = new UpdateClause();

        public void ClearUpdate()
        {
            Update = null;
        }

        public IDeleteClause Delete { get; private set; } = new DeleteClause();

        public IWhereClause Where { get; private set; }

        public IGroupByClause GroupBy { get; private set; }

        public IWhereClause Having { get; private set; }

        public IOrderByClause OrderBy { get; private set; }

        public LinkedList<IUnion> Unions { get; private set; } = new LinkedList<IUnion>();

        public bool HasUnion => Unions != null && Unions.Count > 0;

        #region ICloneableElement Members

        public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree,
            Predicate<ICloneableElement> doClone)
        {
            if (!doClone(this))
                return this;

            ICloneableElement clone;

            if (!objectTree.TryGetValue(this, out clone))
                clone = new SelectQuery(this, objectTree, doClone);

            return clone;
        }

        #endregion

        #region ISqlExpressionWalkable Members

        IQueryExpression ISqlExpressionWalkable.Walk(bool skipColumns, Func<IQueryExpression, IQueryExpression> func)
        {
            Insert?.Walk(skipColumns, func);
            Update?.Walk(skipColumns, func);
            Delete?.Walk(skipColumns, func);

            Select.Walk(skipColumns, func);
            From.Walk(skipColumns, func);
            Where.Walk(skipColumns, func);
            GroupBy.Walk(skipColumns, func);
            Having.Walk(skipColumns, func);
            OrderBy.Walk(skipColumns, func);

            if (HasUnion)
                foreach (var union in Unions)
                    union.SelectQuery.Walk(skipColumns, func);

            return func(this);
        }

        #endregion

        #region IEquatable<ISqlExpression> Members

        bool IEquatable<IQueryExpression>.Equals(IQueryExpression other)
        {
            return this == other;
        }

        #endregion

        #region Init

        private static readonly Dictionary<string, object> _reservedWords = new Dictionary<string, object>();

        static SelectQuery()
        {
#if NETFX_CORE
            using (var stream = typeof(SelectQuery).AssemblyEx().GetManifestResourceStream("ReservedWords.txt"))
#else
            using (
                var stream = typeof(ISelectQuery).AssemblyEx()
                    .GetManifestResourceStream(typeof(SelectQuery), "ReservedWords.txt"))
#endif
            using (var reader = new StreamReader(stream))
            {
                string s;
                while ((s = reader.ReadLine()) != null)
                    _reservedWords.Add(s, s);
            }
        }

        public SelectQuery()
        {
            SourceID = Interlocked.Increment(ref SourceIDCounter);

            Select = new SelectClause(this);
            From = new FromClause(this);
            Where = new WhereClause(this);
            GroupBy = new GroupByClause(this);
            Having = new WhereClause(this);
            OrderBy = new OrderByClause(this);
        }

        internal SelectQuery(int id)
        {
            SourceID = id;
        }

        public void Init(
            IInsertClause insert,
            IUpdateClause update,
            IDeleteClause delete,
            ISelectClause select,
            IFromClause from,
            IWhereClause where,
            IGroupByClause groupBy,
            IWhereClause having,
            IOrderByClause orderBy,
            LinkedList<IUnion> unions,
            ISelectQuery parentSelect,
            ICreateTableStatement createTable,
            bool parameterDependent,
            List<ISqlParameter> parameters)
        {
            Insert = insert;
            Update = update;
            Delete = delete;
            Select = select;
            From = from;
            Where = where;
            GroupBy = groupBy;
            Having = having;
            OrderBy = orderBy;
            Unions = unions;
            ParentSelect = parentSelect;
            CreateTable = createTable;
            IsParameterDependent = parameterDependent;

            Parameters.AddRange(parameters);

            foreach (var col in select.Columns)
                col.Parent = this;

            Select.SetSqlQuery(this);
            From.SetSqlQuery(this);
            Where.SetSqlQuery(this);
            GroupBy.SetSqlQuery(this);
            Having.SetSqlQuery(this);
            OrderBy.SetSqlQuery(this);
        }

        public List<ISqlParameter> Parameters { get; } = new List<ISqlParameter>();

        public List<object> Properties { get; } = new List<object>();

        public bool IsParameterDependent { get; set; }
        public ISelectQuery ParentSelect { get; set; }

        public bool IsSimple
            => !Select.HasModifier && Where.IsEmpty && GroupBy.IsEmpty && Having.IsEmpty && OrderBy.IsEmpty;

        public EQueryType EQueryType { get; set; } = EQueryType.Select;

        public bool IsCreateTable => EQueryType == EQueryType.CreateTable;

        public bool IsSelect => EQueryType == EQueryType.Select;

        public bool IsDelete => EQueryType == EQueryType.Delete;

        public bool IsInsert => EQueryType == EQueryType.Insert || EQueryType == EQueryType.InsertOrUpdate;

        public bool IsUpdate => EQueryType == EQueryType.Update || EQueryType == EQueryType.InsertOrUpdate;

        #endregion

        #region FromClause

        public static IJoin InnerJoin(ISqlTableSource table, params IJoin[] joins)
        {
            return new Join(EJoinType.Inner, table, null, false, joins);
        }

        public static IJoin LeftJoin(ISqlTableSource table, params IJoin[] joins)
        {
            return new Join(EJoinType.Left, table, null, false, joins);
        }

        public static IJoin CrossApply(ISqlTableSource table, params IJoin[] joins)
        {
            return new Join(EJoinType.CrossApply, table, null, false, joins);
        }

        public static IJoin OuterApply(ISqlTableSource table, params IJoin[] joins)
        {
            return new Join(EJoinType.OuterApply, table, null, false, joins);
        }

        public static IJoin WeakInnerJoin(ISqlTableSource table, params IJoin[] joins)
        {
            return new Join(EJoinType.Inner, table, null, true, joins);
        }

        public static IJoin WeakLeftJoin(ISqlTableSource table, params IJoin[] joins)
        {
            return new Join(EJoinType.Left, table, null, true, joins);
        }

        public IFromClause From { get; private set; }

        #endregion

        #region ProcessParameters

        public ISelectQuery ProcessParameters()
        {
            if (!IsParameterDependent)
            {
                return this;
            }

            var query = new QueryVisitor().Convert(
                this,
                e =>
                {
                    switch (e.ElementType)
                    {
                        case EQueryElementType.SqlParameter:
                        {
                            var p = (ISqlParameter) e;

                            if (p.Value == null)
                                return new SqlValue(null);
                        }

                            break;

                        case EQueryElementType.ExprExprPredicate:
                        {
                            var ee = (IExprExpr) e;

                            if (ee.EOperator == EOperator.Equal || ee.EOperator == EOperator.NotEqual)
                            {
                                object value1;
                                object value2;

                                var expr1 = ee.Expr1 as ISqlValue;
                                if (expr1 != null)
                                    value1 = expr1.Value;
                                else
                                {
                                    var parameter = ee.Expr1 as ISqlParameter;
                                    if (parameter != null)
                                        value1 = parameter.Value;
                                    else
                                        break;
                                }

                                var sqlValue = ee.Expr2 as ISqlValue;
                                if (sqlValue != null)
                                    value2 = sqlValue.Value;
                                else
                                {
                                    var parameter = ee.Expr2 as ISqlParameter;
                                    if (parameter != null)
                                        value2 = parameter.Value;
                                    else
                                        break;
                                }

                                var value = Equals(value1, value2);

                                if (ee.EOperator == EOperator.NotEqual)
                                    value = !value;

                                return new Expr(new SqlValue(value), SqlQuery.Precedence.Comparison);
                            }
                        }

                            break;

                        case EQueryElementType.InListPredicate:
                            return ConvertInListPredicate((IInList) e);
                    }

                    return null;
                });

            if (query == this)
            {
                return query;
            }

            query.Parameters.Clear();

            foreach (var parameter in QueryVisitor.FindOnce<ISqlParameter>(query).Where(p => p.IsQueryParameter))
            {
                query.Parameters.Add(parameter);
            }

            return query;
        }

        private static ISqlPredicate ConvertInListPredicate(IInList p)
        {
            if (p.Values == null || p.Values.Count == 0)
                return new Expr(new SqlValue(p.IsNot));

            var sqlParameter = p.Values[0] as ISqlParameter;
            if (p.Values.Count == 1 && sqlParameter != null)
            {
                if (sqlParameter.Value == null)
                    return new Expr(new SqlValue(p.IsNot));

                var enumerable = sqlParameter.Value as IEnumerable;
                if (enumerable != null)
                {
                    var sqlTableSource = p.Expr1 as ISqlTableSource;
                    if (sqlTableSource != null)
                    {
                        var table = sqlTableSource;
                        var keys = table.GetKeys(true);

                        if (keys == null || keys.Count == 0)
                            throw new SqlException("Cant create IN expression.");

                        if (keys.Count == 1)
                        {
                            var values = new List<IQueryExpression>();
                            var field = GetUnderlayingField(keys[0]);
                            var cd = field.ColumnDescriptor;

                            foreach (var item in enumerable)
                            {
                                var value = cd.MemberAccessor.GetValue(item);
                                values.Add(cd.MappingSchema.GetSqlValue(cd.MemberType, value));
                            }

                            if (values.Count == 0)
                                return new Expr(new SqlValue(p.IsNot));

                            return new InList(keys[0], p.IsNot, values);
                        }

                        {
                            var sc = new SearchCondition();

                            foreach (var item in enumerable)
                            {
                                var itemCond = new SearchCondition();

                                foreach (var key in keys)
                                {
                                    var field = GetUnderlayingField(key);
                                    var cd = field.ColumnDescriptor;
                                    var value = cd.MemberAccessor.GetValue(item);
                                    var cond = value == null
                                        ? new Condition(false, new IsNull(field, false))
                                        : new Condition(false,
                                            new ExprExpr(field, EOperator.Equal, cd.MappingSchema.GetSqlValue(value)));

                                    itemCond.Conditions.AddLast(cond);
                                }

                                sc.Conditions.AddLast(new Condition(false, new Expr(itemCond), true));
                            }

                            if (sc.Conditions.Count == 0)
                                return new Expr(new SqlValue(p.IsNot));

                            if (p.IsNot)
                                return new NotExpr(sc, true, SqlQuery.Precedence.LogicalNegation);

                            return new Expr(sc, SqlQuery.Precedence.LogicalDisjunction);
                        }
                    }

                    var expr = p.Expr1 as ISqlExpression;
                    if (expr?.Expr.Length > 1 && expr.Expr[0] == '\x1')
                    {
                        var type = enumerable.GetListItemType();
                        var ta = TypeAccessor.GetAccessor(type);
                        var names = expr.Expr.Substring(1).Split(',');

                        if (expr.Parameters.Length == 1)
                        {
                            var values = new List<IQueryExpression>();

                            foreach (var item in enumerable)
                            {
                                var ma = ta[names[0]];
                                var value = ma.GetValue(item);
                                values.Add(new SqlValue(value));
                            }

                            if (values.Count == 0)
                                return new Expr(new SqlValue(p.IsNot));

                            return new InList(expr.Parameters[0], p.IsNot, values);
                        }

                        {
                            var sc = new SearchCondition();

                            foreach (var item in enumerable)
                            {
                                var itemCond = new SearchCondition();

                                for (var i = 0; i < expr.Parameters.Length; i++)
                                {
                                    var sql = expr.Parameters[i];
                                    var value = ta[names[i]].GetValue(item);
                                    var cond = value == null
                                        ? new Condition(false, new IsNull(sql, false))
                                        : new Condition(false, new ExprExpr(sql, EOperator.Equal, new SqlValue(value)));

                                    itemCond.Conditions.AddLast(cond);
                                }

                                sc.Conditions.AddLast(new Condition(false, new Expr(itemCond), true));
                            }

                            if (sc.Conditions.Count == 0)
                                return new Expr(new SqlValue(p.IsNot));

                            if (p.IsNot)
                                return new NotExpr(sc, true, SqlQuery.Precedence.LogicalNegation);

                            return new Expr(sc, SqlQuery.Precedence.LogicalDisjunction);
                        }
                    }
                }
            }

            return null;
        }

        private static ISqlField GetUnderlayingField(IQueryExpression expr)
        {
            switch (expr.ElementType)
            {
                case EQueryElementType.SqlField:
                    return (ISqlField) expr;
                case EQueryElementType.Column:
                    return GetUnderlayingField(((IColumn) expr).Expression);
            }

            throw new InvalidOperationException();
        }

        #endregion

        #region Clone

        private SelectQuery(ISelectQuery clone, Dictionary<ICloneableElement, ICloneableElement> objectTree,
            Predicate<ICloneableElement> doClone)
        {
            objectTree.Add(clone, this);

            SourceID = Interlocked.Increment(ref SourceIDCounter);

            ICloneableElement parentClone;

            if (clone.ParentSelect != null)
                ParentSelect = objectTree.TryGetValue(clone.ParentSelect, out parentClone)
                    ? (ISelectQuery) parentClone
                    : clone.ParentSelect;

            EQueryType = clone.EQueryType;

            if (IsInsert) Insert = (IInsertClause) clone.Insert.Clone(objectTree, doClone);
            if (IsUpdate) Update = (IUpdateClause) clone.Update.Clone(objectTree, doClone);
            if (IsDelete) Delete = (IDeleteClause) clone.Delete.Clone(objectTree, doClone);

            Select = new SelectClause(this, clone.Select, objectTree, doClone);
            From = new FromClause(this, clone.From, objectTree, doClone);
            Where = new WhereClause(this, clone.Where, objectTree, doClone);
            GroupBy = new GroupByClause(this, clone.GroupBy, objectTree, doClone);
            Having = new WhereClause(this, clone.Having, objectTree, doClone);
            OrderBy = new OrderByClause(this, clone.OrderBy, objectTree, doClone);

            Parameters.AddRange(clone.Parameters.Select(p => (ISqlParameter) p.Clone(objectTree, doClone)));
            IsParameterDependent = clone.IsParameterDependent;

            foreach (var query in QueryVisitor.FindOnce<ISelectQuery>(this).Where(sq => sq.ParentSelect == clone))
            {
                query.ParentSelect = this;
            }
        }

        public ISelectQuery Clone()
        {
            return (ISelectQuery) Clone(new Dictionary<ICloneableElement, ICloneableElement>(), _ => true);
        }

        #endregion

        #region Aliases

        private IDictionary<string, object> _aliases;

        public void RemoveAlias(string alias)
        {
            if (_aliases != null)
            {
                alias = alias.ToUpper();
                if (_aliases.ContainsKey(alias))
                    _aliases.Remove(alias);
            }
        }

        public string GetAlias(string desiredAlias, string defaultAlias)
        {
            if (_aliases == null)
                _aliases = new Dictionary<string, object>();

            var alias = desiredAlias;

            if (string.IsNullOrEmpty(desiredAlias) || desiredAlias.Length > 25)
            {
                alias = defaultAlias + "1";
            }


            var strDigit = string.Empty;

            var index = alias.Length - 1;
            while (char.IsDigit(alias[index]))
            {
                strDigit = alias[index] + strDigit;
                index--;
            }


            var digit = !string.IsNullOrEmpty(strDigit)
                ? int.Parse(strDigit)
                : 1;

            var textAlias = alias.Substring(0, index + 1);

            for (var i = 1;; i++)
            {
                var s = alias.ToUpper();

                if (!_aliases.ContainsKey(s) && !_reservedWords.ContainsKey(s))
                {
                    _aliases.Add(s, s);
                    break;
                }

                alias = string.Concat(textAlias, ++digit);
            }

            return alias;
        }

        public string[] GetTempAliases(int n, string defaultAlias)
        {
            var aliases = new string[n];

            for (var i = 0; i < aliases.Length; i++)
                aliases[i] = GetAlias(defaultAlias, defaultAlias);

            foreach (var t in aliases)
                RemoveAlias(t);

            return aliases;
        }

        public void SetAliases()
        {
            _aliases = null;

            var objs = new HashSet<IQueryElement>();

            Parameters.Clear();

            foreach (var element in QueryVisitor.FindOnce<IQueryElement>(this))
            {
                switch (element.ElementType)
                {
                    case EQueryElementType.SqlParameter:
                    {
                        var p = (ISqlParameter) element;

                        if (p.IsQueryParameter)
                        {
                            if (!objs.Contains(element))
                            {
                                objs.Add(element);
                                p.Name = GetAlias(p.Name, "p");
                            }

                            Parameters.Add(p);
                        }
                        else
                            IsParameterDependent = true;
                    }

                        break;

                    case EQueryElementType.Column:
                    {
                        if (!objs.Contains(element))
                        {
                            objs.Add(element);

                            var c = (IColumn) element;

                            if (c.Alias != "*")
                                c.Alias = GetAlias(c.Alias, "c");
                        }
                    }

                        break;

                    case EQueryElementType.TableSource:
                    {
                        var table = (ITableSource) element;

                        if (!objs.Contains(table))
                        {
                            objs.Add(table);
                            table.Alias = GetAlias(table.Alias, "t");
                        }
                    }

                        break;

                    case EQueryElementType.SqlQuery:
                    {
                        var sql = (ISelectQuery) element;

                        if (sql.HasUnion)
                        {
                            for (var i = 0; i < sql.Select.Columns.Count; i++)
                            {
                                var col = sql.Select.Columns[i];

                                var index = i;
                                sql.Unions.ForEach(
                                    node =>
                                    {
                                        var union = node.Value.SelectQuery.Select;

                                        objs.Remove(union.Columns[index]);

                                        union.Columns[index].Alias = col.Alias;
                                    });
                            }
                        }

                        break;
                    }
                }
            }
        }

        public ISqlTableSource GetTableSource(ISqlTableSource table)
        {
            var ts = From[table];

//			if (ts == null && IsUpdate && Update.Table == table)
//				return Update.Table;

            return ts == null && ParentSelect != null ? ParentSelect.GetTableSource(table) : ts;
        }

        public static ITableSource CheckTableSource(ITableSource ts, ISqlTableSource table, string alias)
        {
            if (ts.Source == table && (alias == null || ts.Alias == alias))
                return ts;

            var jt = ts[table, alias];

            if (jt != null)
                return jt;

            var query = ts.Source as ISelectQuery;
            var s = query?.From[table, alias];

            return s;
        }

        #endregion

        #region ISqlExpression Members

        public bool CanBeNull()
        {
            return true;
        }

        public bool Equals(IQueryExpression other, Func<IQueryExpression, IQueryExpression, bool> comparer)
        {
            return this == other;
        }

        public int Precedence => SqlQuery.Precedence.Unknown;

        public Type SystemType
        {
            get
            {
                if (Select.Columns.Count == 1)
                    return Select.Columns[0].SystemType;

                if (From.Tables.Count == 1 && From.Tables.First.Value.Joins.Count == 0)
                    return From.Tables.First.Value.SystemType;

                return null;
            }
        }

        #endregion

        #region ISqlTableSource Members

        public static int SourceIDCounter;

        public int SourceID { get; }

        public ESqlTableType SqlTableType
        {
            get { return ESqlTableType.Table; }
            set { throw new NotSupportedException(); }
        }

        private List<IQueryExpression> _keys;

        public IList<IQueryExpression> GetKeys(bool allIfEmpty)
        {
            if (_keys == null && From.Tables.Count == 1 && From.Tables.First.Value.Joins.Count == 0)
            {
                _keys = new List<IQueryExpression>();

                var q =
                    from key in From.Tables.First.Value.GetKeys(allIfEmpty)
                    from col in Select.Columns
                    where col.Expression == key
                    select col as IQueryExpression;

                _keys = q.ToList();
            }

            return _keys;
        }

        #endregion

        #region IQueryElement Members

        public override EQueryElementType ElementType => EQueryElementType.SqlQuery;

        public sealed override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
        {
            if (dic.ContainsKey(this))
                return sb.Append("...");

            dic.Add(this, this);

            sb
                .Append("(")
                .Append(SourceID)
                .Append(") ");

            Select.ToString(sb, dic);
            From.ToString(sb, dic);
            Where.ToString(sb, dic);
            GroupBy.ToString(sb, dic);
            Having.ToString(sb, dic);
            OrderBy.ToString(sb, dic);

            if (HasUnion)
                foreach (var u in Unions)
                    u.ToString(sb, dic);

            dic.Remove(this);

            return sb;
        }

        #endregion
    }
}