using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpNeat.Utility
{
    public static class Extensions
    {
        public static double StdDev<T>(this IEnumerable<T> list, Func<T,double> selector)
        {
            double ret = 0;
            int count = list.Count();
            if (count > 0)
            {
                //Compute the Average      
                double avg = list.Average(selector);
                //Perform the Sum of (value-avg)_2_2      
                double sum = list.Sum(d => Math.Pow(selector(d) - avg, 2));
                //Put it all together      
                ret = Math.Sqrt(sum / count);
            }
            return ret;
        }
    }
}
