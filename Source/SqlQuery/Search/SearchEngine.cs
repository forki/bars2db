﻿namespace LinqToDB.SqlQuery.Search
{
    using System;
    using System.Collections.Generic;

    using LinqToDB.SqlQuery.Search.PathBuilder;
    using LinqToDB.SqlQuery.Search.TypeGraph;

    public class SearchEngine<TBaseSearchInterface>
    {
        private readonly PathBuilder<TBaseSearchInterface> _pathBuilder;
        private readonly Dictionary<TypeKey, Delegate> _delegateCache = new Dictionary<TypeKey, Delegate>();

        private SearchEngine()
        {
            var types = typeof(TypeVertex).Assembly.GetTypes();
            var typeGraph = new TypeGraph<TBaseSearchInterface>(types);
            _pathBuilder = new PathBuilder<TBaseSearchInterface>(typeGraph);
        }

        public static SearchEngine<TBaseSearchInterface> Current { get; } = new SearchEngine<TBaseSearchInterface>();

        public void Find<TElement>(TBaseSearchInterface source, LinkedList<TElement> resultList, SearchStrategy<TElement> strategy, HashSet<object> visited) where TElement : class, TBaseSearchInterface
        {
            var deleg = GetOrCreateDelegate(source, strategy);
            deleg.Invoke(source, resultList, visited);

# if DEBUG
            ////var isEqualToReflection = ReflectionSearcher.FindAndCompare(source, stepIntoFound, result);
            ////
            ////if (!isEqualToReflection)
            ////{
            ////    throw new Exception("result not corresponding to reflection");
            ////}
#endif
        }

        public ResultDelegate<TElement> GetOrCreateDelegate<TElement>(TBaseSearchInterface source, SearchStrategy<TElement> strategy) where TElement : class
        {
            var key = new TypeKey(strategy.GetType(), source.GetType(), typeof(TElement));
            Delegate cachedDelegate;
            if (!_delegateCache.TryGetValue(key, out cachedDelegate))
            {
                var paths = _pathBuilder.Find<TElement>(source);

                var delegateConstructor = new DelegateConstructor<TElement>();
                cachedDelegate = delegateConstructor.CreateResultDelegate(paths, strategy);
                _delegateCache[key] = cachedDelegate;
            }

            return (ResultDelegate<TElement>)cachedDelegate;
        }
    }
}