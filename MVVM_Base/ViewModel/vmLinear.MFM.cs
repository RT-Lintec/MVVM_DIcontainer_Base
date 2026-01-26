using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.IO;

namespace MVVM_Base.ViewModel
{
    public partial class vmLinear : ObservableObject, IViewModel
    {
        [RelayCommand]
        private void MFMStop()
        {
            if (_mfmCts != null)
            {
                _mfmCts.Cancel();
            }
        }

        [RelayCommand]
        private async Task MFMExecute(string mode)
        {
            if(!commStatusService.IsMfcConnected)
            {
                await messageService.ShowMessage("MFC isn't connected");
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }
            else if(!commStatusService.IsBalanceConnected)
            {
                await messageService.ShowMessage("Balance isn't connected");
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }

            vmService.CanTransit = false;
            await ChangeState(ProcessState.MFMStarted);
            _mfmCts = new CancellationTokenSource();

            string res = "";

            try
            {
                res = await MFMCoreAsync(_mfmCts.Token);
            }
            catch (OperationCanceledException)
            {
                await messageService.ShowMessage("Operation Canceled");
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
            }
            finally
            {
                if(res == "canceled")
                {
                    await ChangeState(ProcessState.Initial);
                    // VC
                    await CommMFCAsyncType1("VC", _mfmCts.Token);

                    await messageService.ShowMessage("Operation Canceled");
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();                    
                }
                else if(res == "failed")
                {
                    // VC
                    await CommMFCAsyncType1("VC", _mfmCts.Token);
                    await ChangeState(ProcessState.Initial);
                }
                else
                {
                    await ChangeState(ProcessState.AfterMFM);
                }
                vmService.CanTransit = true;
                _mfmCts?.Dispose();
                _mfmCts = null;
            }
        }

        [RelayCommand]
        /// <summary>
        /// MFMコマンド
        /// </summary>
        private async Task<string> MFMCoreAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            string res = "";

            if (!IsMfcConnected)
            {
                await messageService.ShowMessage("Mfc port isn't opened");
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return "failed";
            }

            if(!IsBalanceConnected)
            {
                await messageService.ShowMessage("Balance port isn't opened");
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return "failed";
            }
            
            if (FlowValue == "")
            {                
                return "failed";
            }

            try
            {
                // True値の計算・格納
                CalAndSetTrueValue();

                token.ThrowIfCancellationRequested();

                // 10点リニア係数(FB90~A3)初期化前のダイアログ表示
                var confirm = await messageService.ShowModalAsync("10点リニア係数を全て初期化します");
                if (!confirm.Value)
                {                    
                    return "canceled";
                }

                // 10点リニア係数(FB90~A3)初期化
                // ver2.98以前はinitdata 08 4.00以降は10　↓に反映する 
                string initVal = VersionValue == VersionType.Ver298 ? "08" : "10";

                List<Tuple<string, string>> InitPairList = new List<Tuple<string, string>>()
                {
                    Tuple.Create("FB90", "00"), Tuple.Create("FB91", initVal),
                    Tuple.Create("FB92", "00"), Tuple.Create("FB93", initVal),
                    Tuple.Create("FB94", "00"), Tuple.Create("FB95", initVal),
                    Tuple.Create("FB96", "00"), Tuple.Create("FB97", initVal),
                    Tuple.Create("FB98", "00"), Tuple.Create("FB99", initVal),
                    Tuple.Create("FB9A", "00"), Tuple.Create("FB9B", initVal),
                    Tuple.Create("FB9C", "00"), Tuple.Create("FB9D", initVal),
                    Tuple.Create("FB9E", "00"), Tuple.Create("FB9F", initVal),
                    Tuple.Create("FBA0", "00"), Tuple.Create("FBA1", initVal),
                    Tuple.Create("FBA2", "00"), Tuple.Create("FBA3", initVal),
                };

                List<Tuple<string, string>> gainPairList = new List<Tuple<string, string>>()
                {
                    Tuple.Create("FB41", "00"), Tuple.Create("FB42", "00"),
                };

                // 書き込み
                foreach (var initPair in InitPairList)
                {
                    token.ThrowIfCancellationRequested();

                    res = await CommMFCAsyncTypeRW("EW", initPair, token);
                    if (res == "failed" || res == "canceled")
                    {                        
                        return res;
                    }
                }

                // 読み込み結果格納用
                List<string> linearValues = new List<string>();

                // 書き込み結果の確認
                foreach (var initPair in InitPairList)
                {
                    token.ThrowIfCancellationRequested();

                    res = await CommMFCAsyncType3("ER", initPair.Item1, token);
                    if (res == "failed" || res == "canceled")
                    {                        
                        return res;
                    }

                    if (res.Length < 5)
                    {                        
                        return "failed";
                    }

                    var temp = res.Substring(3, 2);
                    if (temp != initPair.Item2)
                    {                        
                        return "failed";
                    }
                    linearValues.Add(temp);
                }

                // ゲインも更新用に取得
                foreach (var gainPair in gainPairList)
                {
                    token.ThrowIfCancellationRequested();

                    res = await CommMFCAsyncType3("ER", gainPair.Item1, token);
                    if (res == "failed" || res == "canceled")
                    {                        
                        return res;
                    }

                    if (res.Length < 5)
                    {                        
                        return "failed";
                    }

                    var temp = res.Substring(3, 2);
                    linearValues.Add(temp);
                }

                // 10点リニア補正値表の更新
                var props = this.GetType().GetProperties()
                    .Select(p => new
                    {
                        Property = p,
                        Attr = p.GetCustomAttributes(typeof(FbCodeAttribute), false)
                                .Cast<FbCodeAttribute>()
                                .FirstOrDefault()
                    })
                    .Where(x => x.Attr != null)
                    .ToList();

                // vmのプロパティへセット  
                int cnt = 0;
                foreach (var p in props)
                {
                    p.Property.SetValue(this, linearValues[cnt]);
                    cnt++;
                }

                // 10点リニア閾値(FBA4~FBB5)の初期化
                List<Tuple<string, string>> cmdPairList = new List<Tuple<string, string>>()
                {
                    Tuple.Create("FBA4", "3D"), Tuple.Create("FBA5", "0A"),
                    Tuple.Create("FBA6", "7A"), Tuple.Create("FBA7", "14"),
                    Tuple.Create("FBA8", "B7"), Tuple.Create("FBA9", "1E"),
                    Tuple.Create("FBAA", "F4"), Tuple.Create("FBAB", "28"),
                    Tuple.Create("FBAC", "31"), Tuple.Create("FBAD", "33"),
                    Tuple.Create("FBAE", "6E"), Tuple.Create("FBAF", "3D"),
                    Tuple.Create("FBB0", "AB"), Tuple.Create("FBB1", "47"),
                    Tuple.Create("FBB2", "E8"), Tuple.Create("FBB3", "51"),
                    Tuple.Create("FBB4", "25"), Tuple.Create("FBB5", "5C")
                };

                // 書き込み
                foreach (var cmdPair in cmdPairList)
                {
                    token.ThrowIfCancellationRequested();

                    res = await CommMFCAsyncTypeRW("EW", cmdPair, token);
                    if (res == "failed" || res == "canceled")
                    {                        
                        return res;
                    }
                }

                // 書き込み結果の確認
                foreach (var cmdPair in cmdPairList)
                {
                    token.ThrowIfCancellationRequested();

                    res = await CommMFCAsyncType3("ER", cmdPair.Item1, token);
                    if (res == "failed" || res == "canceled")
                    {                        
                        return res;
                    }

                    if (res.Length < 5)
                    {                        
                        return "failed";
                    }

                    if (res.Substring(3, 2) != cmdPair.Item2)
                    {                        
                        return "failed";
                    }
                }

                // CD送信
                res = await CommMFCAsyncType1("CD", token);
                if (res == "failed" || res == "canceled")
                {                    
                    return res;
                }

                token.ThrowIfCancellationRequested();

                // ゼロ調整確認
                confirm = await messageService.ShowModalAsync("ゼロ確認を行います。\nガスを止めてバルブをクローズにしてください");
                if (!confirm.Value)
                {                    
                    return "canceled";
                }
                
                token.ThrowIfCancellationRequested();

                // ゼロ調整実行
                await ChangeState(ProcessState.ZeroAdjust);
                res = await ZeroAdjust(token);
                if (res == "failed" || res == "canceled")
                {                    
                    return res;
                }

                token.ThrowIfCancellationRequested();

                //Span合わせ
                await ChangeState(ProcessState.Span);
                res = await SpanAdjust(linearValues, token);
                if (res == "failed" || res == "canceled")
                {                    
                    return res;
                }
            }
            catch (OperationCanceledException)
            {                
                return "canceled";
            }
            catch (NullReferenceException)
            {
                return "failed";
            }
            catch (TimeoutException)
            {
                return "failed";
            }
            catch (IOException ex)
            {
                return "failed";
            }
            catch (InvalidOperationException ex)
            {
                return "failed";
            }
            catch (Exception ex)
            {
                return "failed";
            }

            return "";
        }

        /// <summary>
        /// ゼロ調整
        /// </summary>
        /// <returns></returns>
        private async Task<string> ZeroAdjust(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            string res = "";

            SwitchZeroBtn(true);

            // VC送信
            res = await CommMFCAsyncType1("VC", token);
            if (res == "failed" || res == "canceled")
            {
                return res;
            }

            // Zero OK, Send Zero押下可能にする

            CanZeroSend = true;

            // ORのループで流量出力値を更新
            // Send Zero押下まで待機
            // 押下でコマンド発火→type1で"ZS"送信
            // ORのループで流量出力値を更新
            // Zero OKでORループ終了、全ボタンを無効化
            while (true)
            {
                token.ThrowIfCancellationRequested();

                // 操作受付
                await Task.Delay(50);

                if (isZeroOK)
                {
                    break;
                }

                if (isZeroSend)
                {
                    CanZeroOK = true;
                    continue;
                }

                var output = await CommMFCAsyncType2("OR", token);
                if (output == "failed" || output == "canceled")
                {
                    return output;
                }
                FlowOut = output.Substring(3);
            }

            CanZeroSend = false;
            CanZeroOK = false;

            SwitchZeroBtn(false);

            return "";
        }

        /// <summary>
        /// Zero Send押下時の処理
        /// </summary>
        [RelayCommand]
        private async Task ZeroSendExecute(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            string res = "";

            if (IsMfmStarted)
            {
                if (!isZeroSend)
                {
                    isZeroSend = true;
                    await Task.Delay(100);
                    res = await CommMFCAsyncType1("ZS", token);
                    if(res == "failed")
                    {
                        return;
                    }
                    else if(res == "canceled")
                    {
                        await messageService.ShowMessage("Operation Canceled");
                        await Task.Delay(messageFadeTime);
                        await messageService.CloseWithFade();
                    }

                    isZeroSend = false;
                }
                else
                {
                    ZStext = "Zero Send";
                    isZeroSend = false;
                }
            }
        }

        /// <summary>
        /// Zero OK押下時の処理
        /// </summary>
        [RelayCommand]
        private void ZeroOKExecute()
        {
            if (IsMfmStarted)
            {
                isZeroOK = true;
            }
        }

        /// <summary>
        /// スパン合わせ
        /// </summary>
        /// <param name="linearValues"></param>
        /// <returns></returns>
        private async Task<string> SpanAdjust(List<string> linearValues, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // 初期化
            var res = await SpanInit(linearValues, token);
            if (res == "failed" || res == "canceled")
            {
                return res;
            }

            token.ThrowIfCancellationRequested();

            // 天秤との定期通信開始
            res = await StartTimerWIthBalCom(token);
            if (res == "failed" || res == "canceled")
            {
                return res;
            }

            token.ThrowIfCancellationRequested();

            // ゲイン書き込み 冗長な処理だが念のため
            var result = await WriteGainValue(token);
            if (res == "failed" || res == "canceled")
            {
                return res;
            }

            // FB41,42の値をリストに反映
            Fb41 = Fb41Val;
            Fb42 = Fb42Val;

            // Span合わせ関連のボタン群を無効化
            SwitchSpanBtn(false);

            return "";
        }

        /// <summary>
        /// Span合わせ前の初期化処理
        /// </summary>
        private async Task<string> SpanInit(List<string> linearValues, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // Span合わせ関連のボタン群を有効化
            SwitchSpanBtn(true);

            // CD送信
            var res = await CommMFCAsyncType1("CD", token);
            if (res == "failed" || res == "canceled")
            {
                return res;
            }

            // VS送信
            res = await CommMFCAsyncType1("VS", token);
            if (res == "failed" || res == "canceled")
            {
                return res;
            }

            // 読み込み済のFB41/42を10進数に変換する
            // VB6 : dat4142 = 256 * Val("&H" + Text6(10).Text) + Val("&H" + Text5(10).Text)
            var upperByte = Convert.ToInt32(linearValues[linearValues.Count - 1], 16);
            var lowerByte = Convert.ToInt32(linearValues[linearValues.Count - 2], 16);
            var hex4142 = 256 * upperByte + lowerByte;

            // 出力設定を10000にセット
            res = await CommMFCAsyncType3("SW", "10000", token);
            if (res == "failed" || res == "canceled")
            {
                return res;
            }

            // FB41, FB42の値を取得
            Fb41Val = linearValues[linearValues.Count - 2];
            Fb42Val = linearValues[linearValues.Count - 1];

            // 計測結果の表を再形成
            ResetMeasureResult();

            return "";
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
            if (firstVal == "failed" || firstVal == "canceled")
            {
                return firstVal;
            }
            lastBalanceVal = ConvertBalanceResToMS(firstVal);
            balNumList[0] = lastBalanceVal;
            dateList[0] = lastUTC;
            cntBalCom = 0;

            int interval = int.Parse(IntervalValue) * 1000;

            // 比較値格納インデクス
            int index = 1;

            // 非同期精密タイマースレッド開始
            // →天秤とインターバル値間隔で通信
            // →結果をテキストボックスに表示
            precisionTimer.Start(async() =>
            {
                cntBalCom++;
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
            }, interval, token);

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

                    if (isSpanOK)
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

                    return "canceled";
                }
            }

            if (isSucceed) return "";
            else return "failed";
        }

        /// <summary>
        /// Span OKボタン押下時の処理
        /// </summary>
        [RelayCommand]
        private void SpanOKExecute()
        {
            isSpanOK = true;

            foreach (var col in MesurementValues)
            {
                col.IsUpdate = false;
            }
        }

        /// <summary>
        /// ゲイン調整ボタン押下により、ゲインを増減させる
        /// </summary>
        [RelayCommand]
        private async Task GainAdjust(string _gain, CancellationToken token)
        {
            int gain = int.Parse(_gain);

            // 16進数→10進数
            var upperByte = Convert.ToInt32(Fb42Val, 16);
            var lowerByte = Convert.ToInt32(Fb41Val, 16);

            int current = 256 * upperByte + lowerByte;

            current += gain;

            upperByte = current / 256;
            lowerByte = current % 256;

            // 10進数→16進数
            Fb42Val = upperByte.ToString("X2");
            Fb41Val = lowerByte.ToString("X2");

            // FB41,42の値をリストに反映
            Fb41 = Fb41Val;
            Fb42 = Fb42Val;

            // ゲイン書き込み
            var res = await WriteGainValue(token);
            if (res == "failed")
            {
                return;
            }
            else if (res == "canceled")
            {
                await messageService.ShowMessage("Operation Canceled");
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
            }

            return;
        }

        /// <summary>
        /// Balanceの返信文字列をfloat型のx[mg]に変換する
        /// </summary>
        /// <param name="_res"></param>
        /// <returns></returns>
        private float ConvertBalanceResToMS(string _res)
        {
            var res = _res;
            res = res.Substring(3, res.Length - 3);
            res = res.Substring(0, res.Length - 2);
            var val = float.Parse(res) * 1000;

            return val;
        }

        /// <summary>
        /// クラス変数で保持しているゲイン(FB41/42)の値をEEPROMに書き込む指示を与える
        /// </summary>
        /// <returns></returns>
        private async Task<string> WriteGainValue(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            string res = "";
            // MFCに書き込み
            List<Tuple<string, string>> gainPairList = new List<Tuple<string, string>>()
            {
                Tuple.Create("FB41", Fb41Val), Tuple.Create("FB42", Fb42Val),
            };

            foreach (var gainPair in gainPairList)
            {
                token.ThrowIfCancellationRequested();

                res = await CommMFCAsyncTypeRW("EW", gainPair, token);
                if (res == "failed" || res == "canceled")
                {
                    return res;
                }
            }

            // 読み込み結果格納用
            List<string> resultValues = new List<string>();

            // 書き込み結果の確認
            foreach (var gainPair in gainPairList)
            {
                token.ThrowIfCancellationRequested();

                res = await CommMFCAsyncType3("ER", gainPair.Item1, token);
                if (res == "failed" || res == "canceled")
                {
                    return res;
                }

                if (res.Length < 5)
                {
                    return "failed";
                }

                var temp = res.Substring(3, 2);
                if (temp != gainPair.Item2)
                {
                    return "failed";
                }
            }

            return "";
        }


    }
}
