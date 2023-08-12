using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace JPublicIndicators
{
    public class MovingAverageCloud : JPublicIndicator
    {
        [InputParameter("Fast MA", 0, 1, 999, 1)]
        public int FastMALength = 10;

        [InputParameter("Slow MA", 1, 1, 999, 1)]
        public int SlowMALength = 20;

        [InputParameter("Green Cloud", 2)] public Color GreenCloudColor = Color.FromArgb(150, 105, 160, 61);

        [InputParameter("Red Cloud", 2, 1, 999, 1)]
        public Color RedCloudColor = Color.FromArgb(150, 112, 53, 66);

        [InputParameter("Type of Moving Average", 1, variants: new object[]
            {
                "Simple", MaMode.SMA,
                "Exponential", MaMode.EMA,
                "Smoothed", MaMode.SMMA,
                "Linear Weighted", MaMode.LWMA
            }
        )]
        public MaMode MAType = MaMode.SMA;

        public Indicator FastMa;
        public Indicator SlowMa;
        public override string ShortName => $"Cloud ({MAType} {FastMALength}:{SlowMALength})";

        public MovingAverageCloud()
            : base()
        {
            Name = "Moving Average - Cloud";
            Description = "Moving Average Cloud";
            SeparateWindow = false;

            AddLineSeries("Fast MA", Color.FromArgb(150, 105, 160, 61), 3, LineStyle.Solid);
            AddLineSeries("Slow MA", Color.FromArgb(150, 112, 53, 66), 3, LineStyle.Solid);
        }

        protected override void OnInit()
        {
            FastMa = Core.Indicators.BuiltIn.MA(FastMALength, PriceType.Close, (MaMode) this.MAType, Indicator.DEFAULT_CALCULATION_TYPE);
            SlowMa = Core.Indicators.BuiltIn.MA(SlowMALength, PriceType.Close, (MaMode) this.MAType, Indicator.DEFAULT_CALCULATION_TYPE);
            AddIndicator(FastMa);
            AddIndicator(SlowMa);
        }

        protected override void OnUpdate(UpdateArgs args)
        {
            double currFastValue = this.FastMa.GetValue();
            double currSlowValue = this.SlowMa.GetValue();

            double prevFastValue = this.FastMa.GetValue(1);
            double prevSlowValue = this.SlowMa.GetValue(1);

            var isCrossing = currFastValue > currSlowValue && prevFastValue < prevSlowValue ||
                             currFastValue < currSlowValue && prevFastValue > prevSlowValue;

            SetValue(currFastValue, 0);
            SetValue(currSlowValue, 1);

            if (isCrossing)
            {
                this.EndCloud(0, 1, Color.Empty);
                if (currFastValue > currSlowValue)
                    this.BeginCloud(0, 1, GreenCloudColor);
                else if (currFastValue < currSlowValue)
                    this.BeginCloud(0, 1, RedCloudColor);
            }
        }
    }
}