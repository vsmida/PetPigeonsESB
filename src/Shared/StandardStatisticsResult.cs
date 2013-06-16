namespace Shared
{
    public class StandardStatisticsResult
    {
        public double Median { get; set; }
        public double Mean { get; set; }
        public double StandardDeviation { get; set; }
        public double Quantile90Percent { get; set; }
        public double Quantile95Percent { get; set; }

        public override string ToString()
        {
            return string.Format("Median: {0}, Mean: {1}, StandardDeviation: {2}, Quantile90Percent: {3}, Quantile95Percent: {4}", Median, Mean, StandardDeviation, Quantile90Percent, Quantile95Percent);
        }
    }
}