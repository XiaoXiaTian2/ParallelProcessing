using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ParallelProcessing
{
    internal static class ListExtensions
    {
        public static double Medium<T>(this IEnumerable<T> list, Func<T, double> selector)
        {
            var doubleList = list.Select(selector).OrderBy(x => x).ToList();

            int count = doubleList.Count();
            var middle = count / 2;
            if (count % 2 == 0)
            {
                double rent = doubleList.ElementAt(middle - 1) + doubleList.ElementAt(middle);
                return rent / 2;
            }

            return doubleList.ElementAt(middle);
        }
    }
}
