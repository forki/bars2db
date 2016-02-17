namespace LinqToDB.SqlQuery.QueryElements
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;

    using LinqToDB.Extensions;
    using LinqToDB.SqlQuery.QueryElements.Enums;
    using LinqToDB.SqlQuery.QueryElements.Interfaces;

    [DebuggerDisplay("SQL = {SqlText}")]
    public abstract class BaseQueryElement : IQueryElement
    {
        protected void FillList<TElement>(IEnumerable<TElement> items, LinkedList<IQueryElement> list) where TElement: IQueryElement
        {
            foreach (var item in items)
            {
                list.AddLast(item);
            }
        }

        public LinkedList<TElementType> DeepFindParentLast<TElementType>() where TElementType : class, IQueryElement
        {
            var returnList = new LinkedList<TElementType>();
            var list = new LinkedList<IQueryElement>();

            list.AddFirst(this);

            while (list.First != null)
            {
                var current = list.First;
                var value = current.Value as TElementType;

                current.Value?.GetChildren(list);

                if (value != null)
                {
                    returnList.AddFirst(value);
                }

                list.RemoveFirst();
            }

            return returnList;
        }

        public LinkedList<TElementType> DeepFindParentFirst<TElementType>() where TElementType: class, IQueryElement
        {
            var returnList = new LinkedList<TElementType>();
            var processed = new LinkedList<IQueryElement>();

            processed.AddFirst(this);

            while (processed.First != null)
            {
                var current = processed.Last;
                var value = current.Value as TElementType;
                if (value != null)
                {
                    returnList.AddLast(value);
                }

                processed.RemoveLast();

                current.Value?.GetChildren(processed);
            }

            return returnList;
        }

        public LinkedList<TElementType> DeepFindDownTo<TElementType>() where TElementType : class, IQueryElement
        {
            var returnList = new LinkedList<TElementType>();
            var list = new LinkedList<IQueryElement>();

            list.AddFirst(this);

            while (list.First != null)
            {
                var current = list.First;

                if (current.Value != null)
                {
                    var value = current.Value as TElementType;

                    if (value != this && value != null)
                    {
                        returnList.AddLast(value);
                    }
                    else
                    {
                        current.Value.GetChildren(list);
                    }
                }

                list.RemoveFirst();
            }

            return returnList;
        }

        public abstract void GetChildren(LinkedList<IQueryElement> list);

        public abstract EQueryElementType ElementType { get; }

        public abstract StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic);

        public string SqlText => ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>()).ToString();
    }
}