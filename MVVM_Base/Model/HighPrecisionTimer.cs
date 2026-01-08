namespace MVVM_Base.Model
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;

    public class HighPrecisionTimer
    {
        private bool isRun = false;
        public bool IsRun
        {
            get { return isRun; }
        }

        private CancellationTokenSource cts;
        private Task timerTask;

        public void Start(Func<Task> callback, int intervalMs, CancellationToken token)
        {
            isRun = true;

            // 多重起動防止
            if (cts != null)
            {
                return;
            }

            //cts = new CancellationTokenSource();
            //CancellationToken token = cts.Token;

            long intervalTicks = (long)(Stopwatch.Frequency * (intervalMs / 1000.0));

            timerTask = Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                long next = intervalTicks;

                while (/*!token.IsCancellationRequested || */isRun)
                {
                    long now = sw.ElapsedTicks;

                    if (now >= next)
                    {
                        // インターバルごとの処理
                        await callback();

                        // 遅延があっても想定クロックを維持
                        next += intervalTicks;
                    }

                    // CPU・精度バランスの良い待ち
                    Thread.Sleep(50);
                }
            }, token);
        }

        private readonly object stopLock = new object();
        public async void Stop()
        {
            isRun = false;

            lock (stopLock)
            {
                if (cts == null) return;
            }

            cts.Cancel();
            try
            {
                await timerTask;
            }
            finally
            {
                lock (stopLock)
                {
                    cts?.Dispose();
                    cts = null;
                }
            }
        }
    }
}
