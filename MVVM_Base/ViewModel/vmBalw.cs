using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM_Base.Model;
using MVVM_Base.Common;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Input;
using static MVVM_Base.ViewModel.vmLinear;

namespace MVVM_Base.ViewModel
{
    public partial class vmBalw : ObservableObject, IViewModel
    { 
        public vmBalw(ThemeService _themeService, CommStatusService _commStatusService, IMessageService _messageService,
                        ViewModelManagerService _vmService, ApplicationStatusService _appStatusService, HighPrecisionTimer _precisionTimer,
                        LanguageService _languageService, IdentifierService _identifierService)
        {
            mfcService = MfcSerialService.Instance;

            themeService = _themeService;
            themeService.PropertyChanged += ThemeService_PropertyChanged;

            commStatusService = _commStatusService;
            commStatusService.PropertyChanged += CommStatusService_PropertyChanged;

            messageService = _messageService;

            vmService = _vmService;
            vmService.Register(this);

            appStatusService = _appStatusService;
            appStatusService.PropertyChanged += AppStatusService_PropertyChanged;

            precisionTimer = _precisionTimer;

            MesurementItems = new ObservableCollection<MeasureResult>();
            MeasurementValues = new ObservableCollection<MeasureResult>();

            languageService = _languageService;
            identifierService = _identifierService;

            // 計測結果の表を形成
            ResetMeasureResult();

            ChangeState(ProcessState.Initial);
        }

        public void Dispose()
        {
            // 終了可否判断
            canQuit = true;

            // 終了可否チェック
            vmService.CheckCanQuit();

            // 
            vmService.CanTransit = true;
        }

        #region 状態変更通知に対応する処理
        /// <summary>
        /// カラーテーマ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThemeService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ThemeService.CurrentTheme))
            {
                // CurrentTheme変化を検知
                OnThemeChanged(themeService.CurrentTheme);
            }
        }

        private void OnThemeChanged(string newTheme)
        {
            // View に依存せず ViewModel 内で処理可能
            // 例：内部フラグ更新や別プロパティ更新など
            IsDarkTheme = newTheme == themeService.Dark;
            ColorTheme = newTheme;
            OnPropertyChanged(nameof(IsDarkTheme));
        }

        /// <summary>
        /// 通信状態
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommStatusService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CommStatusService.IsMfcConnected))
            {
                // ここで CurrentTheme 変化を検知可能
                OnMfcCommChanged(commStatusService.IsMfcConnected);
            }

            if (e.PropertyName == nameof(CommStatusService.IsBalanceConnected))
            {
                // ここで CurrentTheme 変化を検知可能
                OnBalanceCommChanged(commStatusService.IsBalanceConnected);
            }
        }

        private void OnMfcCommChanged(bool isConnected)
        {
            IsMfcConnected = isConnected;
        }
        private void OnBalanceCommChanged(bool isConnected)
        {
            IsBalanceConnected = isConnected;
        }

        /// <summary>
        /// アプリケーション終了の検知
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppStatusService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ApplicationStatusService.IsQuit))
            {
                if (appStatusService.IsQuit)
                {
                    Dispose();
                }
            }
        }

        #endregion

        /// <summary>
        /// UI拡縮
        /// </summary>
        public ICommand AdjustUICommand => new RelayCommand<object>(e =>
        {
            int delta = 0;

            if (e is KeyEventArgs k)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
                if (k.Key == Key.OemPlus || k.Key == Key.Add) delta = 1;
                else if (k.Key == Key.OemMinus || k.Key == Key.Subtract) delta = -1;
            }
            else if (e is MouseWheelEventArgs me)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
                delta = me.Delta > 0 ? 1 : -1;
            }
            else
            {
                return;
            }

            // deltaが決まったらサイズ調整
            AdjustFontSizeByDelta(delta);
        });


        private void AdjustFontSizeByDelta(int delta)
        {
            if (delta > 0 && tcnt > 4) return;
            if (delta < 0 && tcnt < -4) return;

            TitleFontSize += delta;
            UnitFontSize += delta * 0.5;
            IconSize += delta;
            ButtonFontSize += delta;
            LogFontSize += delta * 0.5;
            LabelSize += delta * 0.5;

            MesureBtnSize += delta * 4;
            OutputBtnSize += delta * 6;

            MarginMark += delta;
            MarginStartStop += delta;
            MarginReading += delta * 2;

            MarkWidth += delta * 2;
            ReadWidth += delta * 2;

            CelWidth += delta * 5;

            CommBoxWidth += delta * 6;
            CommBoxHeight += delta * 4;

            UnitTextboxWidth += delta * 5;
            TimerSettingBoxWidth += delta * 18;

            OutputBoxWidth += delta * 24;
            OutputBoxHeight += delta * 16;

            MeasureBoxWidth += delta * 12;
            MeasureBoxHeight += delta * 12;

            LogBoxWidth += delta * 12;
            LogBoxHeight += delta * 12;

            tcnt += delta;
        }

        /// <summary>
        /// 計測結果の表をリセットする
        /// </summary>
        private void ResetMeasureResult()
        {
            // 計測結果の表を新規形成
            if (MesurementItems.Count == 0)
            {
                for (int i = 0; i < 11; i++)
                {
                    if (i == 0)
                    {
                        MeasureResult temp1 = new MeasureResult();
                        temp1.Value = "gn5-gn";
                        MesurementItems.Add(temp1);

                        MeasureResult temp2 = new MeasureResult();
                        temp2.Value = ($"");
                        MeasurementValues.Add(temp2);

                        continue;
                    }

                    MeasureResult di = new MeasureResult();
                    di.Value = ($"d{i}");
                    MesurementItems.Add(di);

                    MeasureResult m = new MeasureResult();
                    m.Value = ($"");
                    MeasurementValues.Add(m);
                }
            }
            // 値を全て初期化
            else
            {
                for (int i = 0; i < 11; i++)
                {
                    MeasurementValues[i].Value = "";
                }
            }
        }


        #region 天秤通信処理

        /// <summary>
        /// Balanceの返信文字列をfloat型のx[g]に変換する
        /// </summary>
        /// <param name="_res"></param>
        /// <returns></returns>
        private float ConvertBalanceResToG(string _res)
        {
            var res = _res;
            res = res.Substring(3, res.Length - 3);
            res = res.Substring(0, res.Length - 2);
            var val = float.Parse(res);

            return val;
        }

        /// <summary>
        /// 天秤との通信
        /// </summary>
        /// <returns></returns>
        private async Task<string> CommBalanceAsyncCommand(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (BalanceSerialService.Instance.Port == null || !BalanceSerialService.Instance.Port.IsOpen)
                {
                    await messageService.ShowMessage(languageService.BalancePortError);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                    return identifierService.Failed;
                }

                var result = await BalanceSerialService.Instance.RequestWeightAsync(token);
                if (result != null)
                {
                    if (result.Status == OperationResultType.Success)
                    {
                        //debugList.Add(DateTime.UtcNow + "  :  " + result.Payload);
                        return result.Payload;
                    }
                    else
                    {
                        await messageService.ShowMessage(languageService.BalanceCommError);
                        await Task.Delay(messageFadeTime);
                        await messageService.CloseWithFade();
                        return identifierService.Failed;
                    }
                }

                return "";
            }
            catch (OperationCanceledException)
            {
                return identifierService.Canceled;
            }
        }

        /// <summary>
        /// Span合わせでの5個前データ比較計算
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private async Task<string> Gn5GnComm(int index, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var currentUTC = DateTime.UtcNow;
                var result = await CommBalanceAsyncCommand(token);
                if (result == identifierService.Failed)
                {
                    return result;
                }
                var val = ConvertBalanceResToG(result);

                // UI反映
                ReadValue = val.ToString();

                if (result != null && result != "")
                {
                    //計測結果の表に結果を格納
                    if (index < 11)
                    {
                        token.ThrowIfCancellationRequested();

                        var intervalUTC = currentUTC - lastUTC;

                        // 取得した値を分単位のmg変化量に変換して格納
                        MeasurementValues[index].Value = (60 / intervalUTC.TotalSeconds * (val - lastBalanceVal)).ToString("F3");

                        // 点滅処理
                        MarkUpdatedTemporarily(index, int.Parse(intervalSecValue) * 1000);
                        balNumList[index] = val;

                        // 前回の値を保持
                        lastBalanceVal = val;
                        lastUTC = DateTime.UtcNow;

                        // 計測時間の格納
                        dateList[index] = currentUTC;

                        // g(n+5) - g(n)を計算
                        if (cntBalCom >= 5)
                        {
                            if (index > 5)
                            {
                                MeasurementValues[0].Value = (60 / (dateList[index] - dateList[index - 5]).TotalSeconds * (balNumList[index] - balNumList[index - 5])).ToString("F3");
                            }
                            else
                            {
                                MeasurementValues[0].Value = (60 / (dateList[index] - dateList[index + 5]).TotalSeconds * (balNumList[index] - balNumList[index + 5])).ToString("F3");
                            }                            
                        }
                        return result;
                    }
                }

                return "";
            }
            catch (OperationCanceledException)
            {
                return identifierService.Canceled;
            }
        }

        /// <summary>
        /// インデクス毎のキャンセレーショントークンを管理
        /// </summary>
        private readonly Dictionary<int, CancellationTokenSource> _blinkCts = new();

        /// <summary>
        /// 手動測定での視認性向上のため、文字列を明滅させるためのフラグ管理を行う
        /// </summary>
        /// <param name="index"></param>
        /// <param name="delayMs"></param>
        public async void MarkUpdatedTemporarily(int index, int delayMs)
        {
            // 既存の明滅をキャンセル
            if (_blinkCts.TryGetValue(index, out var oldCts))
            {
                oldCts.Cancel();
            }

            var cts = new CancellationTokenSource();
            _blinkCts[index] = cts;
            var token = cts.Token;

            // 開始前に念のため止める
            MeasurementValues[index].IsUpdate = false;

            // 次のフレームで開始
            await Task.Yield();

            MeasurementValues[index].IsUpdate = true;

            try
            {
                await Task.Delay(delayMs, token);
            }
            catch (TaskCanceledException)
            {
                // キャンセルされたら即終了
                return;
            }
            finally
            {
                // 正常終了・キャンセルどちらでも必ず元に戻す
                MeasurementValues[index].IsUpdate = false;
            }
        }

        #endregion

        #region 状態遷移
        /// <summary>
        /// 操作状態に応じてUIを管理
        /// </summary>
        /// <param name="state"></param>
        private void ChangeState(ProcessState state)
        {
            curState = state;

            SwitchAllBtn(false);
            SwitchAllUI(false);

            switch (state)
            {
                case ProcessState.Initial:
                    {
                        SwitchAllBtn(true);
                        SwitchAllUI(true);
                        break;
                    }
                    ;
                case ProcessState.Measurement:
                    {
                        CanMeasure = true;
                        break;
                    }
                    ;
                case ProcessState.Exporting:
                    {
                        break;
                    }
                    ;
            }
        }

        private void SwitchAllBtn(bool enable)
        {
            CanExport = enable;
            CanMeasure = enable;
        }

        private void SwitchAllUI(bool enable)
        {
            SettingEnable = enable;
        }

        #endregion

        /// <summary>
        /// ログのcsv出力
        /// </summary>
        /// <param name="path"></param>
        [RelayCommand]
        private void ExportCsv()
        {
            if (Logs.Count == 0)
            {
                return;
            }

            vmService.CanTransit = false;
            ChangeState(ProcessState.Exporting);

            var sb = new StringBuilder();

            //sb.AppendLine("Reading Value");

            for (int i = 0; i < Logs.Count; i++)
            {
                if (i != 0 && i % 10 == 0 )
                {
                    sb.AppendLine($"=\"{Logs[i]}\"");               
                }
                else
                {
                    sb.AppendLine($"\"{Logs[i]}\"");
                }
                
            }

            string baseDir = AppContext.BaseDirectory;
            string csvDir = System.IO.Path.Combine(baseDir, "CSV\\Balw");

            if (Directory.Exists(csvDir))
            {
                // フォルダあり
            }
            else
            {
                Directory.CreateDirectory(csvDir);
            }

            string now = DateTime.Now.ToString("yyyyMMddHHmmss");
            var fileName = "\\" + now + "_Balw.csv";
            string path = csvDir + fileName;

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

            ChangeState(ProcessState.Initial);
            vmService.CanTransit = true;
        }

        /// <summary>
        /// ログクリア
        /// </summary>
        [RelayCommand]
        private void ClearLog()
        {
            Logs.Clear();
        }
    }
}
