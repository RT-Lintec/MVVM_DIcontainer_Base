using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM_Base.Common;
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

        // 初期化したかどうか
        bool isInitGain = false;

        [RelayCommand]
        private async Task MFMExecute(string mode)
        {
            if(!commStatusService.IsMfcConnected)
            {
                await messageService.ShowMessage(languageService.MfcPortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }
            else if(!commStatusService.IsBalanceConnected)
            {
                await messageService.ShowMessage(languageService.BalancePortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }

            // 画面遷移禁止
            vmService.CanTransit = false;

            // 状態記憶
            lastState = curState;

            // 未保存データの保存確認を出さない
            noNeedConfirmUnsaved = true;

            await ChangeState(ProcessState.MFMStarted);
            _mfmCts = new CancellationTokenSource();

            OperationResult res = new OperationResult(OperationResultType.Success, null, null);

            try
            {
                res = await MFMCoreAsync(_mfmCts.Token);
            }
            catch (OperationCanceledException)
            {
                await messageService.ShowMessage(languageService.OperationCanceled);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
            }
            finally
            {
                if(res.Status == OperationResultType.Canceled)
                {
                    // VC
                    await CommMFCAsyncType1("VC", _mfmCts.Token);

                    await messageService.ShowMessage(languageService.OperationCanceled);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();

                    if (isInitGain)
                    {
                        await FBDataRead(_mfmCts.Token);
                        await ChangeState(ProcessState.Initial);
                    }
                    else
                    {
                        await ChangeState(lastState);
                    }
                }
                else if(res.Status == OperationResultType.Failure)
                {
                    // VC
                    await CommMFCAsyncType1("VC", _mfmCts.Token);
                    await messageService.ShowMessage(languageService.OperationFailed);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();

                    if (isInitGain)
                    {
                        await FBDataRead(_mfmCts.Token);
                        await ChangeState(ProcessState.Initial);
                    }
                    else
                    {
                        await ChangeState(lastState);
                    }
                }
                else
                {
                    await ChangeState(ProcessState.AfterMFM);
                }

                isInitGain = false;
                noNeedConfirmUnsaved = false;
                vmService.CanTransit = true;
                _mfmCts?.Dispose();
                _mfmCts = null;
            }
        }

        [RelayCommand]
        /// <summary>
        /// MFMコマンド
        /// </summary>
        private async Task<OperationResult> MFMCoreAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            OperationResult res = new OperationResult(OperationResultType.Success, null, null);

            if (!IsMfcConnected)
            {
                await messageService.ShowMessage(languageService.MfcPortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return OperationResult.Failed();
            }

            if(!IsBalanceConnected)
            {
                await messageService.ShowMessage(languageService.BalancePortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return OperationResult.Failed();
            }
            
            if (FlowValue == "")
            {                
                return OperationResult.Failed();
            }

            try
            {
                // True値の計算・格納
                CalAndSetTrueValue();

                token.ThrowIfCancellationRequested();

                // 10点リニア係数(FB90~A3)初期化前のダイアログ表示
                var confirm = await messageService.ShowModalAsync(languageService.MfmStart);
                if (!confirm.Value)
                {                    
                    return OperationResult.Canceled();
                }

                isInitGain = true;

                // 10点リニア係数(FB90~A3)初期化
                // ver2.98以前はinitdata 08 4.00以降は10　↓に反映する 
                string initVal = VersionValue == VersionType.Ver298 ? "08" : "10";

                List<Tuple<string, string>> InitPairList = new List<Tuple<string, string>>()
                {
                    Tuple.Create(FbMap["FB90"], "00"), Tuple.Create(FbMap["FB91"], initVal),
                    Tuple.Create(FbMap["FB92"], "00"), Tuple.Create(FbMap["FB93"], initVal),
                    Tuple.Create(FbMap["FB94"], "00"), Tuple.Create(FbMap["FB95"], initVal),
                    Tuple.Create(FbMap["FB96"], "00"), Tuple.Create(FbMap["FB97"], initVal),
                    Tuple.Create(FbMap["FB98"], "00"), Tuple.Create(FbMap["FB99"], initVal),
                    Tuple.Create(FbMap["FB9A"], "00"), Tuple.Create(FbMap["FB9B"], initVal),
                    Tuple.Create(FbMap["FB9C"], "00"), Tuple.Create(FbMap["FB9D"], initVal),
                    Tuple.Create(FbMap["FB9E"], "00"), Tuple.Create(FbMap["FB9F"], initVal),
                    Tuple.Create(FbMap["FBA0"], "00"), Tuple.Create(FbMap["FBA1"], initVal),
                    Tuple.Create(FbMap["FBA2"], "00"), Tuple.Create(FbMap["FBA3"], initVal),
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
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
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
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {                        
                        return res;
                    }

                    if (res.Payload.Length < 5)
                    {                        
                        return OperationResult.Failed("ER");
                    }

                    var temp = res.Payload.Substring(3, 2);
                    if (temp != initPair.Item2)
                    {                        
                        return OperationResult.Failed();
                    }
                    linearValues.Add(temp);
                }

                // ゲインも更新用に取得
                foreach (var gainPair in gainPairList)
                {
                    token.ThrowIfCancellationRequested();

                    res = await CommMFCAsyncType3("ER", gainPair.Item1, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {                        
                        return res;
                    }

                    if (res.Payload.Length < 5)
                    {                        
                        return OperationResult.Failed("ER");
                    }

                    var temp = res.Payload.Substring(3, 2);
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
                    Tuple.Create(ThresholdMap["FBA4"], "3D"), Tuple.Create(ThresholdMap["FBA5"], "0A"),
                    Tuple.Create(ThresholdMap["FBA6"], "7A"), Tuple.Create(ThresholdMap["FBA7"], "14"),
                    Tuple.Create(ThresholdMap["FBA8"], "B7"), Tuple.Create(ThresholdMap["FBA9"], "1E"),
                    Tuple.Create(ThresholdMap["FBAA"], "F4"), Tuple.Create(ThresholdMap["FBAB"], "28"),
                    Tuple.Create(ThresholdMap["FBAC"], "31"), Tuple.Create(ThresholdMap["FBAD"], "33"),
                    Tuple.Create(ThresholdMap["FBAE"], "6E"), Tuple.Create(ThresholdMap["FBAF"], "3D"),
                    Tuple.Create(ThresholdMap["FBB0"], "AB"), Tuple.Create(ThresholdMap["FBB1"], "47"),
                    Tuple.Create(ThresholdMap["FBB2"], "E8"), Tuple.Create(ThresholdMap["FBB3"], "51"),
                    Tuple.Create(ThresholdMap["FBB4"], "25"), Tuple.Create(ThresholdMap["FBB5"], "5C")
                };

                // 書き込み
                foreach (var cmdPair in cmdPairList)
                {
                    token.ThrowIfCancellationRequested();

                    res = await CommMFCAsyncTypeRW("EW", cmdPair, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {                        
                        return res;
                    }
                }

                // 書き込み結果の確認
                foreach (var cmdPair in cmdPairList)
                {
                    token.ThrowIfCancellationRequested();

                    res = await CommMFCAsyncType3("ER", cmdPair.Item1, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {                        
                        return res;
                    }

                    if (res.Payload.Length < 5)
                    {                        
                        return OperationResult.Failed("ER");
                    }

                    if (res.Payload.Substring(3, 2) != cmdPair.Item2)
                    {                        
                        return OperationResult.Failed();
                    }
                }

                // CD送信
                res = await CommMFCAsyncType1("CD", token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {                    
                    return res;
                }

                token.ThrowIfCancellationRequested();

                // ゼロ調整確認
                confirm = await messageService.ShowModalAsync(languageService.ZeroCheckConfirm);
                if (!confirm.Value)
                {                    
                    return OperationResult.Canceled();
                }
                
                token.ThrowIfCancellationRequested();

                // ゼロ調整実行
                await ChangeState(ProcessState.ZeroAdjust);
                res = await ZeroAdjust(token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {                    
                    return res;
                }

                token.ThrowIfCancellationRequested();

                //Span合わせ
                await ChangeState(ProcessState.Span);
                res = await SpanAdjust(linearValues, token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {                    
                    return res;
                }
            }
            catch (OperationCanceledException)
            {                
                return OperationResult.Canceled();
            }
            catch (NullReferenceException)
            {
                return OperationResult.Failed();
            }
            catch (TimeoutException)
            {
                return OperationResult.Failed();
            }
            catch (IOException ex)
            {
                return OperationResult.Failed();
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult.Failed();
            }
            catch (Exception ex)
            {
                return OperationResult.Failed();
            }

            return OperationResult.Success();
        }

        /// <summary>
        /// ゼロ調整
        /// </summary>
        /// <returns></returns>
        private async Task<OperationResult> ZeroAdjust(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            OperationResult res;

            SwitchZeroBtn(true);

            // VC送信
            res = await CommMFCAsyncType1("VC", token);
            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
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
                if (output.Status == OperationResultType.Failure || output.Status == OperationResultType.Canceled)
                {
                    return output;
                }
                FlowOut = output.Payload.Substring(3);
            }

            CanZeroSend = false;
            CanZeroOK = false;

            SwitchZeroBtn(false);

            return OperationResult.Success();
        }

        /// <summary>
        /// Zero Send押下時の処理
        /// </summary>
        [RelayCommand]
        private async Task ZeroSendExecute(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            OperationResult res;

            if (IsMfmStarted)
            {
                if (!isZeroSend)
                {
                    isZeroSend = true;
                    await Task.Delay(100);
                    res = await CommMFCAsyncType1("ZS", token);
                    if(res.Status == OperationResultType.Failure)
                    {
                        // TODO 失敗メッセージの出力
                        return;
                    }
                    else if(res.Status == OperationResultType.Canceled)
                    {
                        await messageService.ShowMessage(languageService.OperationCanceled);
                        await Task.Delay(messageFadeTime);
                        await messageService.CloseWithFade();
                    }

                    isZeroSend = false;
                }
                else
                {
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
        private async Task<OperationResult> SpanAdjust(List<string> linearValues, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // 初期化
            var res = await SpanInit(linearValues, token);
            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
            {
                return res;
            }

            token.ThrowIfCancellationRequested();

            // 天秤との定期通信開始
            res = await StartTimerWIthBalCom(token);
            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
            {
                return res;
            }

            token.ThrowIfCancellationRequested();

            // ゲイン書き込み 冗長な処理だが念のため
            var result = await WriteGainValue(token);
            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
            {
                return res;
            }

            // FB41,42の値をリストに反映
            Fb41 = Fb41Val;
            Fb42 = Fb42Val;

            // Span合わせ関連のボタン群を無効化
            SwitchSpanBtn(false);

            return OperationResult.Success();
        }

        /// <summary>
        /// Span合わせ前の初期化処理
        /// </summary>
        private async Task<OperationResult> SpanInit(List<string> linearValues, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // Span合わせ関連のボタン群を有効化
            SwitchSpanBtn(true);

            // CD送信
            var res = await CommMFCAsyncType1("CD", token);
            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
            {
                return res;
            }

            // VS送信
            res = await CommMFCAsyncType1("VS", token);
            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
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
            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
            {
                return res;
            }

            // FB41, FB42の値を取得
            Fb41Val = linearValues[linearValues.Count - 2];
            Fb42Val = linearValues[linearValues.Count - 1];

            // 計測結果の表を再形成
            ResetMeasureResult();

            return OperationResult.Success();
        }

        /// <summary>
        /// 定間隔での天秤通信を開始する Mabiki99がベース
        /// </summary>
        /// <returns></returns>
        private async Task<OperationResult> StartTimerWIthBalCom(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            bool isSucceed = true;
            // 比較初期値として天秤からの値を取得しておく
            lastUTC = DateTime.UtcNow;
            //LogQ();
            var firstVal = await CommBalanceAsyncCommand(token);
            if (firstVal.Status == OperationResultType.Failure || firstVal.Status == OperationResultType.Canceled)
            {
                return firstVal;
            }
            //Logging(firstVal.Payload, false);
            lastBalanceVal = ConvertBalanceResToMS(firstVal.Payload);
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
                LogQ();
                var res = await Gn5GnComm(index, token);
                
                if (index >= 10)
                {
                    index = 1;
                    Logging(res.Payload, true);
                    return;
                }
                else if (!commStatusService.IsBalanceConnected)
                {
                    isSucceed = false;
                    precisionTimer.Stop();
                }
                else
                {
                    Logging(res.Payload, false);
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

                    return OperationResult.Canceled();
                }
            }

            if (isSucceed) return OperationResult.Success();
            else return OperationResult.Failed();
        }

        /// <summary>
        /// Span OKボタン押下時の処理
        /// </summary>
        [RelayCommand]
        private void SpanOKExecute()
        {
            isSpanOK = true;

            foreach (var col in MeasurementValues)
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
            if (res.Status == OperationResultType.Failure)
            {
                return;
            }
            else if (res.Status == OperationResultType.Canceled)
            {
                await messageService.ShowMessage(languageService.OperationCanceled);
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
        private async Task<OperationResult> WriteGainValue(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            OperationResult res;
            // MFCに書き込み
            List<Tuple<string, string>> gainPairList = new List<Tuple<string, string>>()
            {
                Tuple.Create("FB41", Fb41Val), Tuple.Create("FB42", Fb42Val),
            };

            foreach (var gainPair in gainPairList)
            {
                token.ThrowIfCancellationRequested();

                res = await CommMFCAsyncTypeRW("EW", gainPair, token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
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
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                if (res.Payload.Length < 5)
                {
                    return OperationResult.Failed("ER");
                }

                var temp = res.Payload.Substring(3, 2);
                if (temp != gainPair.Item2)
                {
                    return OperationResult.Failed();
                }
            }

            return OperationResult.Success();
        }


    }
}
