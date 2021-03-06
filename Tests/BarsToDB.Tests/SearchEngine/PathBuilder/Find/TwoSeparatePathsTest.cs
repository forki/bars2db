﻿using Bars2Db.SqlQuery.Search;
using Bars2Db.SqlQuery.Search.PathBuilder;
using Bars2Db.SqlQuery.Search.TypeGraph;
using LinqToDB.Tests.SearchEngine.PathBuilder.Find.Base;
using Xunit;

namespace LinqToDB.Tests.SearchEngine.PathBuilder.Find
{
    public class TwoSeparatePathsTest : BaseFindTest
    {
        public interface IBase
        {
        }

        public interface IA : IBase
        {
            [SearchContainer]
            IB B { get; set; }

            [SearchContainer]
            IC C { get; set; }
        }

        public interface IB : IBase
        {
            [SearchContainer]
            IC C { get; set; }
        }

        public interface IC : IBase
        {
            [SearchContainer]
            ID D { get; set; }

            [SearchContainer]
            IE E { get; set; }

            [SearchContainer]
            IF F { get; set; }
        }

        public interface ID : IBase
        {
        }

        public interface IE : IBase
        {
        }

        public interface IF : IBase
        {
        }

        public class A : IA
        {
            public IB B { get; set; }

            public IC C { get; set; }
        }

        [Fact]
        public void Test()
        {
            var typeGraph = new TypeGraph<IBase>(GetType().Assembly.GetTypes());

            var pathBuilder = new PathBuilder<IBase>(typeGraph);

            var result = pathBuilder.Find(new A(), typeof(IF));

            var ab = typeof(IA).GetProperty("B");
            var ac = typeof(IA).GetProperty("C");
            var bc = typeof(IB).GetProperty("C");
            var cf = typeof(IC).GetProperty("F");

            var pathCommon = new CompositPropertyVertex();
            pathCommon.PropertyList.AddLast(cf);

            var path1 = new CompositPropertyVertex();
            path1.PropertyList.AddLast(ab);
            path1.PropertyList.AddLast(bc);
            path1.Children.AddLast(pathCommon);

            var path2 = new CompositPropertyVertex();
            path2.PropertyList.AddLast(ac);
            path2.Children.AddLast(pathCommon);

            var expected = new[] {path1, path2};

            Assert.True(IsEqual(expected, result));
        }
    }
}