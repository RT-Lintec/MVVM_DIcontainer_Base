using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM_Base.Model;
using System.Windows;

namespace MVVM_Base.ViewModel
{
    public partial class vmBalw : ObservableObject, IViewModel
    {
        [RelayCommand]
        private void Stop()
        {
            isStop = true;
        }

        [RelayCommand]
        private void Mark() 
        {
            MarkValue = TimeSpan.Zero;
        }

        /// <summary>
        /// Manual
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task Manual()
        {
            if (!commStatusService.IsBalanceConnected)
            {
                await messageService.ShowMessage(languageService.BalancePortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }

            ChangeState(ProcessState.Measurement);

            isStop = false;

            _calculateCts = new CancellationTokenSource();
            ResetMeasureResult();

            vmService.CanTransit = false;

            // 天秤との定期通信開始
            var res = await StartTimerWIthBalCom(_calculateCts.Token);

            ChangeState(ProcessState.Initial);

            vmService.CanTransit = true;
            return;
        }

        /// <summary>
        /// 定間隔での天秤通信を開始する Mabiki99がベース
        /// </summary>
        /// <returns></returns>
        private async Task<string> StartTimerWIthBalCom(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            bool isSucceed = true;
            // 比較初期値として天秤からの値を取得しておく
            lastUTC = DateTime.UtcNow;
            var firstVal = await CommBalanceAsyncCommand(token);
            if (firstVal == identifierService.Failed || firstVal == identifierService.Canceled)
            {
                return firstVal;
            }
            lastBalanceVal = ConvertBalanceResToG(firstVal);
            balNumList[0] = lastBalanceVal;
            dateList[0] = lastUTC;
            cntBalCom = 0;

            int interval =(int.Parse(intervalMinValue) * 60 + int.Parse(IntervalSecValue)) * 1000;

            // 比較値格納インデクス
            int index = 1;

            // 非同期精密タイマースレッド開始
            // →天秤とインターバル値間隔で通信
            // →結果をテキストボックスに表示
            precisionTimer.StartWithNotice(
            async () =>
            {
                cntBalCom++;
                LogQ();
                var res = await Gn5GnComm(index, token);

                if (index >= 10)
                {
                    index = 1;
                    Logging(res, true);
                    return;
                }
                else if (!commStatusService.IsBalanceConnected)
                {
                    isSucceed = false;
                    precisionTimer.Stop();
                }
                else
                {
                    Logging(res, false);
                }

                index++;
                return;
            }, 
            async () =>
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MarkValue += TimeSpan.FromSeconds(1);
                });
            },
            interval, token);

            while (true)
            {
                try
                {
                    token.ThrowIfCancellationRequested();

                    await Task.Delay(10, token);
                    if (!commStatusService.IsBalanceConnected)
                    {
                        break;
                    }

                    if (isStop)
                    {
                        // 非同期精密タイマースレッドを終了
                        if (precisionTimer.IsRun)
                        {
                            precisionTimer.Stop();
                        }
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    precisionTimer.Stop();
                    isSucceed = false;

                    return identifierService.Canceled;
                }
            }

            if (isSucceed) return "";
            else return identifierService.Failed;
        }

        private void LogQ()
        {
            if (languageService.CurrentLanguage == LanguageType.Japanese)
            {
                Logging("送信：Q", false);
            }
            else
            {
                Logging("Send : Q", false);
            }
        }

        /// <summary>
        /// ログ追加
        /// </summary>
        /// <param name="message"></param>
        private void Logging(string message, bool isNeedLinebreak)
        {
            // 改行不要
            if (!isNeedLinebreak)
            {
                // スレッドセーフのためUIスレッドで実行
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    Logs.Add($"{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff} {message}");
                });
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    Logs.Add($"{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff} {message}");

                    // TODO : ユニークな文字列しか反応してくれない
                    Logs.Add($"{DateTime.Now:yyyy/MM/dd HH:mm:ss.fff}");
                });
            }
        }
    }
}
