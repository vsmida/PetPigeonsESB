using System;
using System.Collections.Generic;
using MathNet.Numerics.Statistics;
using System.Linq;

namespace Shared
{
    public static class ExtendEnumerable
    {
        public static StandardStatisticsResult ComputeStatistics<T>(this IEnumerable<T> enumerable)
        {
            if (!typeof(IConvertible).IsAssignableFrom(typeof(T)))
            {
                throw new ArgumentException(string.Format("cannot calculate statitics on type {0}", typeof(T).FullName));
            }

            double[] doubles;
            if (typeof(T) == typeof(double))
                doubles = enumerable.Cast<double>().ToArray();
            else
                doubles = enumerable.Select(x => Convert.ToDouble(x)).ToArray();
            var mean = doubles.Mean();
            var median = doubles.Median();
            var standardDeviation = doubles.StandardDeviation();
            var quantile90 = doubles.Quantile(0.90);
            var quantile95 = doubles.Quantile(0.95);

            return new StandardStatisticsResult
                       {
                           Mean = mean,
                           Median = median,
                           Quantile90Percent = quantile90,
                           Quantile95Percent = quantile95,
                           StandardDeviation = standardDeviation
                       };
        }

    }
}