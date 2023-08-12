using System.Threading;
using System.Threading.Tasks;
using TradingPlatform.BusinessLayer;

namespace JPublicIndicators
{
    public abstract class JPublicIndicator : Indicator
    {
        public Task WaitingTask { get; set; }

        public CancellationTokenSource WaitingTaskCts { get; set; }

        public bool HistoricalDataLoaded => (this.Count + 1) >= this.HistoricalData.Count;

        public void Log(string message, LoggingLevel loggingLevel = LoggingLevel.System)
        {
            Core.Instance.Loggers.Log(this.Name, message, loggingLevel);
        }

        public void LogError(string message)
        {
            this.Log(message, LoggingLevel.Error);
        }

        public void LogTrading(string message)
        {
            this.Log(message, LoggingLevel.Trading);
        }
    }
}