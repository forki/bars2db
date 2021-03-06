namespace Bars2Db.SqlQuery.QueryElements.Enums
{
    public enum EQueryElementType
    {
        SqlField,
        SqlFunction,
        SqlParameter,
        SqlExpression,
        SqlBinaryExpression,
        SqlValue,
        SqlDataType,
        SqlTable,

        ExprPredicate,
        NotExprPredicate,
        ExprExprPredicate,
        HierarhicalPredicate,
        LikePredicate,
        BetweenPredicate,
        IsNullPredicate,
        InSubQueryPredicate,
        InListPredicate,
        FuncLikePredicate,

        SqlQuery,
        Column,
        SearchCondition,
        Condition,
        TableSource,
        JoinedTable,

        SelectClause,
        InsertClause,
        UpdateClause,
        SetExpression,
        DeleteClause,
        FromClause,
        WhereClause,
        GroupByClause,
        OrderByClause,
        OrderByItem,
        Union,
        CreateTableStatement,

        None
    }
}