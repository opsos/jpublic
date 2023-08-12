using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Quantower.Utilities.BufferedProcessor;

namespace JPublicIndicators
{
    public abstract class MultiThreadedBufferedProcessor<T> : IBufferedProcessor
    {
        private readonly ConcurrentQueue<T> _concurrentBuffer;
        private readonly object _bufferLocker;
        private readonly ManualResetEvent _resetEvent;
        private CancellationTokenSource _cts;

        public bool Started = false;
        public bool Backtest = false;

        protected MultiThreadedBufferedProcessor()
        {
            this._concurrentBuffer = new ConcurrentQueue<T>();
            this._bufferLocker = new object();
            this._resetEvent = new ManualResetEvent(false);
            this.Started = false;
            this.Backtest = false;
        }

        public event Action<Exception> ExceptionOccurred;

        public int BufferDepth => this._concurrentBuffer.Count;

        public virtual void Start(bool isBacktest = false)
        {
            if (isBacktest)
            {
                this.StartBacktest();
            }
            else
            {
                this.Start();
            }
        }

        public virtual void Start()
        {
            this.Started = true;
            this._cts = new CancellationTokenSource();

            int numThreads = Environment.ProcessorCount; // Number of threads to use

            for (int i = 0; i < numThreads; i++)
            {
                Task.Factory.StartNew(this.ProcessLoop, this._cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }

        public virtual void Stop()
        {
            this._cts?.Cancel();

            this.Clear();

            this._resetEvent.Set();
        }

        public virtual void Clear()
        {
            lock (this._bufferLocker)
            {
                this._concurrentBuffer.Clear();
            }
        }


        public void StartBacktest()
        {
            this.Backtest = true;
            this.Started = true;
            this._cts = new CancellationTokenSource();

            try
            {
                if (this._cts?.IsCancellationRequested ?? false)
                    return;

                while (this._concurrentBuffer.Count > 0)
                {
                    if (this._cts?.IsCancellationRequested ?? false)
                        return;

                    try
                    {
                        while (_concurrentBuffer.TryDequeue(out T subject))
                        {
                            this.Process(subject);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.OnException(ex.InnerException ?? ex);
                    }
                }
            }
            catch (Exception ex)
            {
                this.OnException(ex.InnerException ?? ex);
            }
        }

        public void Push(T subject)
        {
            _concurrentBuffer.Enqueue(subject);
            _resetEvent.Set();
        }

        private void ProcessLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    _resetEvent.WaitOne();

                    while (_concurrentBuffer.TryDequeue(out T subject))
                    {
                        Process(subject);
                    }
                }
                catch (Exception ex)
                {
                    OnException(ex.InnerException ?? ex);
                }
                finally
                {
                    _resetEvent.Reset();
                }
            }
        }

        protected abstract void Process(T subject);

        private void OnException(Exception ex)
        {
            this.ExceptionOccurred?.Invoke(ex);
        }
    }
}