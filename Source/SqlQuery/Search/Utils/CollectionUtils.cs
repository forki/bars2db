﻿namespace LinqToDB.SqlQuery.Search.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public static class CollectionUtils
    {
        public static bool IsCollection(Type sourceType)
        {
            return sourceType != GetElementType(sourceType);
        }

        public static Type GetElementType(Type sourceType)
        {
            if (sourceType.IsArray)
            {
                return sourceType.GetElementType();
            }

            if (!sourceType.IsGenericType)
            {
                return sourceType;
            }

            if (typeof(LinkedList<>).IsAssignableFrom(sourceType.GetGenericTypeDefinition()))
            {
                return sourceType.GetGenericArguments()[0];
            }

            if (typeof(Dictionary<,>).IsAssignableFrom(sourceType.GetGenericTypeDefinition()))
            {
                return sourceType.GetGenericArguments()[1];
            }

            if (typeof(List<>).IsAssignableFrom(sourceType.GetGenericTypeDefinition()))
            {
                return sourceType.GetGenericArguments()[0];
            }

            return sourceType;
        }

        public static object[] GetCollectionItem(object collection)
        {
            object[] resultArray;

            var dict = collection as IDictionary;
            if (dict != null)
            {
                resultArray = new object[dict.Values.Count];
                dict.Values.CopyTo(resultArray, 0);
                return resultArray;
            }

            var coll = collection as ICollection;
            if (coll != null)
            {

                resultArray = new object[coll.Count];
                coll.CopyTo(resultArray, 0);
                return resultArray;
            }

            throw new NotSupportedException();
        }
    }
}