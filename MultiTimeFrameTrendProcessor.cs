using System;
using System.Drawing;
using System.Threading;
using TradingPlatform.BusinessLayer;

namespace JPublicIndicators
{
    public class MultiTimeFrameTrendProcessor : MultiThreadedBufferedProcessor<MTFUpdate>
    {
        public MultiTimeFrameTrend MainIndicator;

        public HistoricalData Min1HistoricalData;
        public MovingAverageCloud Min1MovingAverageCloud;

        public HistoricalData Min5HistoricalData;
        public MovingAverageCloud Min5MovingAverageCloud;

        protected override void Process(MTFUpdate subject)
        {
            
            Thread.Sleep(new Random().Next(123, 1234));
            int offsetIndex = (int) MainIndicator.HistoricalData.GetIndexByTime(subject.Time.Ticks, SeekOriginHistory.End);

            try
            {
                int min1CandleIndex = (int) Min1HistoricalData.GetIndexByTime(subject.Time.Ticks, SeekOriginHistory.Begin);
                bool min1CandleExists = min1CandleIndex >= 0;

                if (min1CandleExists)
                {
                    bool cloudIsBullish = IsCloudBullish(Min1MovingAverageCloud, min1CandleIndex);
                    bool cloudIsBearish = IsCloudBearish(Min1MovingAverageCloud, min1CandleIndex);

                    if (cloudIsBullish)
                    {
                        MainIndicator.LinesSeries[0].SetMarker(offsetIndex, Color.LawnGreen);
                    }

                    if (cloudIsBearish)
                    {
                        MainIndicator.LinesSeries[0].SetMarker(offsetIndex, Color.OrangeRed);
                    }
                }
            }
            catch (Exception)
            {
            }

            try
            {
                int min5CandleIndex = (int) Min5HistoricalData.GetIndexByTime(subject.Time.Ticks, SeekOriginHistory.Begin);
                bool min5CandleExists = min5CandleIndex >= 0;

                if (min5CandleExists)
                {
                    bool cloudIsBullish = IsCloudBullish(Min5MovingAverageCloud, min5CandleIndex);
                    bool cloudIsBearish = IsCloudBearish(Min5MovingAverageCloud, min5CandleIndex);

                    if (cloudIsBullish)
                    {
                        MainIndicator.LinesSeries[1].SetMarker(offsetIndex, Color.LawnGreen);
                    }

                    if (cloudIsBearish)
                    {
                        MainIndicator.LinesSeries[1].SetMarker(offsetIndex, Color.OrangeRed);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public static bool IsCloudBullish(MovingAverageCloud cloud, double index)
        {
            int candleIndex = (int) index;
            double currFastValue = cloud.GetValue(candleIndex, 0, SeekOriginHistory.Begin);
            double currSlowValue = cloud.GetValue(candleIndex, 1, SeekOriginHistory.Begin);

            return currFastValue >= currSlowValue;
        }

        public static bool IsCloudBearish(MovingAverageCloud cloud, double index)
        {
            int candleIndex = (int) index;
            double currFastValue = cloud.GetValue(candleIndex, 0, SeekOriginHistory.Begin);
            double currSlowValue = cloud.GetValue(candleIndex, 1, SeekOriginHistory.Begin);

            return currFastValue <= currSlowValue;
        }
    }
}