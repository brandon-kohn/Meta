using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Meta_UnitTests
{
    [TestClass]
    public class MultiMapTest
    {
        [TestMethod]
        public void TestLinearInsert()
        {
            Meta.MultiMap<int, int> map = new Meta.MultiMap<int, int>();
            for (int i = 0; i < 10000; ++i)
                for (int j = 0; j < 10; ++j)
                    map.Insert(i, j);

            //! Forward iteration
            {
                var it = map.Begin;
                while (it.MoveNext())
                {
                    int i = it.Current.Key;
                    int j = it.Current.Value;
                    System.Console.WriteLine("I: " + i + " J: " + j);
                }
            }

            //! Reverse iteration
            {
                var it = map.RBegin;
                while (it.MovePrev())
                {
                    int i = it.Current.Key;
                    int j = it.Current.Value;
                    System.Console.WriteLine("I: " + i + " J: " + j);
                }
            }
        }

        [TestMethod]
        public void TestRandomInsert()
        {
            Random rnd = new Random();
            Meta.MultiMap<int, int> map = new Meta.MultiMap<int, int>();
            for (int i = 0; i < 10000; ++i)
                for (int j = 0; j < 10; ++j)
                {
                    map.Insert(rnd.Next(100000), rnd.Next(10));
                }


            //! Forward iteration
            {
                var it = map.Begin;
                while (it.MoveNext())
                {
                    int i = it.Current.Key;
                    int j = it.Current.Value;
                    System.Console.WriteLine("I: " + i + " J: " + j);
                }
            }

            //! Reverse iteration
            {
                var it = map.RBegin;
                while (it.MovePrev())
                {
                    int i = it.Current.Key;
                    int j = it.Current.Value;
                    System.Console.WriteLine("I: " + i + " J: " + j);
                }
            }
        }
    }
}
