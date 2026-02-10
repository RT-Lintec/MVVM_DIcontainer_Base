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

        /// <summary>
        /// 指示秒ごとにcallbackを実行する
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="intervalMs"></param>
        /// <param name="token"></param>
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
                token.ThrowIfCancellationRequested();

                while (isRun)
                {
                    long now = sw.ElapsedTicks;

                    if (now >= next)
                    {
                        // インターバルごとの処理
                        await callback();

                        // 遅延があっても想定クロックを維持
                        next += intervalTicks;
                    }

                    token.ThrowIfCancellationRequested();

                    // CPU・精度バランスの良い待ち
                    Thread.Sleep(1);
                }
            }, token);
        }

        /// <summary>
        /// 毎秒　callbackPerSecも実行する
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="callbackPerSec"></param>
        /// <param name="intervalMs"></param>
        /// <param name="token"></param>
        public void StartWithNotice(Func<Task> callback, Func<Task> callbackPerSec, int intervalMs, CancellationToken token)
        {
            isRun = true;

            // 多重起動防止
            if (cts != null)
            {
                return;
            }

            long intervalTicks = (long)(Stopwatch.Frequency * (intervalMs / 1000.0));
            long oneTicks = (long)(Stopwatch.Frequency);

            timerTask = Task.Run(async () =>
            {
                var sw = Stopwatch.StartNew();
                long next = intervalTicks;
                long nextOneSec = oneTicks;

                while (isRun)
                {
                    long now = sw.ElapsedTicks;


                    if (now >= nextOneSec)
                    {
                        _ = Task.Run(async () =>
                        {
                            await callbackPerSec();
                        }, token);

                        nextOneSec += oneTicks;
                    }

                    if (now >= next)
                    {
                        _ = Task.Run(async () =>
                        {
                            await callback();
                        }, token);

                        next += intervalTicks;
                    }

                    // CPU・精度バランスの良い待ち
                    Thread.Sleep(1);
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
