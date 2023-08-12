using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;

namespace JPublicIndicators
{
    public class MTFUpdate
    {
        public int CandleIndex { get; set; }
        public UpdateReason UpdateReason { get; set; }

        public DateTime Time { get; set; }
    }

    public class MultiTimeFrameTrend : JPublicIndicator
    {
        public MultiTimeFrameTrendProcessor MultiTimeFrameTrendProcessor;

        public Font DefaultFont;
        private int _lastSecondUpdate = -1;

        public HistoricalData Min1HistoricalData;
        public MovingAverageCloud Min1MovingAverageCloud;

        public HistoricalData Min5HistoricalData;
        public MovingAverageCloud Min5MovingAverageCloud;

        public override string ShortName => "MTF - Trend";
        public bool OutOfSessionLoading = false;

        public MultiTimeFrameTrend() : base()
        {
            Name = "Multi-Time Frame Trend";
            Description = "Multi-Time Frame Trend";
            SeparateWindow = true;
            UpdateType = IndicatorUpdateType.OnTick;

            AddLineSeries("1 Min Trend", Color.Gray, 5, LineStyle.Points);
            AddLineSeries("5 Min Trend", Color.Gray, 5, LineStyle.Points);
            AddLineSeries("Dummy Upper", Color.Transparent, 0, LineStyle.Points);
            AddLineSeries("Dummy Lower", Color.Transparent, 0, LineStyle.Points);
        }

        protected override void OnInit()
        {
            OutOfSessionLoading = false;
            this.MultiTimeFrameTrendProcessor = new MultiTimeFrameTrendProcessor()
            {
                MainIndicator = this,
            };

            Task.Run(() =>
            {
                Min1HistoricalData = this.Symbol.GetHistory(Period.MIN1, this.HistoricalData.HistoryType, this.HistoricalData.FromTime.AddDays(-3));
                Min1HistoricalData.AddIndicator(Min1MovingAverageCloud = new MovingAverageCloud());
                this.MultiTimeFrameTrendProcessor.Min1MovingAverageCloud = Min1MovingAverageCloud;
                this.MultiTimeFrameTrendProcessor.Min1HistoricalData = Min1HistoricalData;

                this.Log("Min1HistoricalData loaded");
            });

            Task.Run(() =>
            {
                Min5HistoricalData = this.Symbol.GetHistory(Period.MIN5, this.HistoricalData.HistoryType, this.HistoricalData.FromTime.AddDays(-10));
                Min5HistoricalData.AddIndicator(Min5MovingAverageCloud = new MovingAverageCloud());
                this.MultiTimeFrameTrendProcessor.Min5MovingAverageCloud = Min5MovingAverageCloud;
                this.MultiTimeFrameTrendProcessor.Min5HistoricalData = Min5HistoricalData;

                this.Log("Min5HistoricalData loaded");
            });
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            SetValue(0, 0);
            SetValue(1, 1);

            SetValue(3, 2);
            SetValue(-2, 3);

            if (args.Reason == UpdateReason.NewTick)
            {
                int lastSecond = DateTime.Now.Second;

                if (lastSecond != 0)
                {
                    if (lastSecond % 5 != 0)
                        return;
                }

                if (lastSecond == _lastSecondUpdate)
                    return;

                _lastSecondUpdate = lastSecond;
            }

            this.MultiTimeFrameTrendProcessor.Push(new MTFUpdate()
            {
                CandleIndex = Count - 1,
                UpdateReason = args.Reason,
                Time = Time(),
            });

            if (this.HistoricalDataLoaded && OutOfSessionLoading == false)
            {
                OutOfSessionLoading = true;
                WaitingTaskCts = new CancellationTokenSource();
                WaitingTask = Task.Run(() =>
                {
                    while (this.MultiTimeFrameTrendProcessor.Min1HistoricalData == null)
                    {
                    }

                    while (this.MultiTimeFrameTrendProcessor.Min5HistoricalData == null)
                    {
                    }

                    this.MultiTimeFrameTrendProcessor.Start();
                    this.Log("Finished loading out of session data.");
                }, WaitingTaskCts.Token);
            }
        }

        public static Font GetDefaultFont(int fontSize = 12)
        {
            var fonts = new InstalledFontCollection();
            List<FontFamily> fontFamilies = fonts.Families.ToList();

            var jetBrainsMono = fontFamilies.FirstOrDefault(f => f.Name == "JetBrains Mono");

            if (jetBrainsMono != null)
                return new Font(jetBrainsMono, fontSize);

            var droidSansMono = fontFamilies.FirstOrDefault(f => f.Name == "Droid Sans Mono");

            if (droidSansMono != null)
                return new Font(droidSansMono, fontSize);

            var dejavuSansMono = fontFamilies.FirstOrDefault(f => f.Name == "DejaVu Sans Mono");

            if (dejavuSansMono != null)
                return new Font(dejavuSansMono, fontSize);

            var consolas = fontFamilies.FirstOrDefault(f => f.Name == "Consolas");

            if (consolas != null)
                return new Font(consolas, fontSize);

            var verdana = fontFamilies.FirstOrDefault(f => f.Name == "Verdana");

            return verdana != null ? new Font(verdana, fontSize) : new Font("Arial", fontSize);
        }

        public static Font GetDefaultFont(int fontSize, FontStyle fontStyle)
        {
            Font font = GetDefaultFont(fontSize);

            return new Font(font, fontStyle);
        }

        public override void OnPaintChart(PaintChartEventArgs args)
        {
            base.OnPaintChart(args);
            Graphics graphics = args.Graphics;
            graphics.SetClip(args.Rectangle);

            if (DefaultFont == null)
                DefaultFont = GetDefaultFont(9, FontStyle.Bold);

            SizeF textSize = graphics.MeasureString("5M", DefaultFont);
            double defaultYOffset = textSize.Height / 2;
            double defaultXOffset = textSize.Width;

            var currentWindow = CurrentChart.Windows[args.WindowIndex];
            float rightX = (float) (currentWindow.ClientRectangle.Right - defaultXOffset);

            double min1Y = currentWindow.CoordinatesConverter.GetChartY(0) - defaultYOffset;
            double min5Y = currentWindow.CoordinatesConverter.GetChartY(1) - defaultYOffset;

            graphics.DrawString("1M", DefaultFont, Brushes.White, rightX, (float) min1Y);
            graphics.DrawString("5M", DefaultFont, Brushes.White, rightX, (float) min5Y);
        }

        public override void Dispose()
        {
            if (this.WaitingTaskCts != null)
            {
                this.WaitingTaskCts.Cancel();
                this.WaitingTaskCts.Dispose();
            }

            if (WaitingTask != null)
            {
                this.WaitingTask.Dispose();
            }

            if (this.MultiTimeFrameTrendProcessor != null)
            {
                this.MultiTimeFrameTrendProcessor.Stop();
            }

            this.Min1HistoricalData.Dispose();
            this.Min1MovingAverageCloud.Dispose();
            this.Min5HistoricalData.Dispose();
            this.Min5MovingAverageCloud.Dispose();

            this.Clear();
        }
    }
}