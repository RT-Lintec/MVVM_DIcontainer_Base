using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM_Base.Common;
using MVVM_Base.Model;
using MathNet.Numerics.LinearAlgebra;
using System.Collections.ObjectModel;
using System.Windows;

namespace MVVM_Base.ViewModel
{
    public partial class vmLinear : ObservableObject, IViewModel
    {
        private string calc = "calc";
        public string Calc
        {
            get => calc;
            set
            {
                if (calc != value)
                {
                    calc = value;
                }
            }
        }

        private string conf = "conf";
        public string Conf
        {
            get => conf;
            set
            {
                if (conf != value)
                {
                    conf = value;
                }
            }
        }

        private bool isCalculated = false;
        private bool isCalcedAndConfed = false;

        /// <summary>
        /// 停止
        /// </summary>
        [RelayCommand]
        private void StopCmd()
        {
            if (_calculateCts != null)
            {
                _calculateCts.Cancel();
                ConfIndex = -1;
            }
        }

        /// <summary>
        /// calc & conf
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task CalAndConf()
        {
            if (!commStatusService.IsMfcConnected)
            {
                await messageService.ShowMessage(languageService.MfcPortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }
            else if (!commStatusService.IsBalanceConnected)
            {
                await messageService.ShowMessage(languageService.BalancePortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }

            // 5%刻みでは実施させない
            if (incrementValue == IncerementType.FivePercent)
            {
                await messageService.ShowMessage(languageService.CannotCalWith5per);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }

            isCalculated = false;
            isCalcedAndConfed = false;
            lastState = curState;
            vmService.CanTransit = false;
            await ChangeState(ProcessState.Measurement);
            _calculateCts = new CancellationTokenSource();
            OperationResult res = new OperationResult(OperationResultType.Success, null, null);

            // Calc
            try
            {
                res = await CalculateCoreAsync(Calc, _calculateCts.Token);
            }
            catch (OperationCanceledException)
            {
                await messageService.ShowMessage(languageService.OperationCanceled);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
            }
            finally
            {
                if (res.Status == OperationResultType.Canceled)
                {
                    await messageService.ShowMessage(languageService.OperationCanceled);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();

                    await ChangeState(/*ProcessState.AfterMFM*/lastState);
                }
                else if (res.Status == OperationResultType.Failure)
                {
                    // VC
                    await CommMFCAsyncType1("VC", _calculateCts.Token);
                    await ChangeState(/*ProcessState.AfterMFM*/lastState);
                }
                else
                {                    
                    ExportReadingCsv();
                    //await ChangeState(ProcessState.AfterCalc);                    
                }
            }

            if(_calculateCts.IsCancellationRequested)
            {
                return;
            }

            // Conf
            try
            {
                res = await CalculateCoreAsync(Conf, _calculateCts.Token);
            }
            catch (OperationCanceledException)
            {
                await messageService.ShowMessage(languageService.OperationCanceled);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
            }
            finally
            {
                if (res.Status == OperationResultType.Canceled)
                {
                    await messageService.ShowMessage(languageService.OperationCanceled);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();

                    if (!isCalculated)
                    {
                        await ChangeState(ProcessState.AfterMFM);
                    }
                    else
                    {
                        await ChangeState(ProcessState.AfterCalc);
                    }
                }
                else if (res.Status == OperationResultType.Failure)
                {
                    // VC
                    await CommMFCAsyncType1("VC", _calculateCts.Token);

                    if (!isCalculated)
                    {
                        await ChangeState(ProcessState.AfterMFM);
                    }
                    else
                    {
                        await ChangeState(ProcessState.AfterCalc);
                    }
                }
                else
                {
                    isCalcedAndConfed = true;
                    vmService.HasNonsavedOutput = true;
                    await ChangeState(ProcessState.AfterCalcAndConf);                    
                }
            }

            vmService.CanTransit = true;
            _calculateCts?.Dispose();
            _calculateCts = null;
        }

        [RelayCommand]
        private async Task GainReadWrite(string mode)
        {
            lastState = curState;
            noNeedConfirmUnsaved = true;
            await ChangeState(ProcessState.Transit);

            if (!commStatusService.IsMfcConnected)
            {
                await messageService.ShowMessage(languageService.MfcPortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                await ChangeState(lastState);
                return;
            }
            _fbRWCts = new CancellationTokenSource();

            OperationResult res = new OperationResult(OperationResultType.Success, null, null);

            // read
            if (mode == "r")
            {             
                res = await FBDataRead(_fbRWCts.Token);
            }
            // write
            else if(mode == "w")
            {
                res = await FBDataWrite(_fbRWCts.Token);

                // 表のゲインとEEPROMのゲインは同一である
                isGainDirectChanged = false;
            }

            if (res.Status == OperationResultType.Failure)
            {
                await messageService.ShowMessage(languageService.MfcCommError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
            }

            await ChangeState(lastState);
            noNeedConfirmUnsaved = false;
        }

        /// <summary>
        /// Manual
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task Manual()
        {            
            if (!commStatusService.IsMfcConnected)
            {
                await messageService.ShowMessage(languageService.MfcPortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }
            if (!commStatusService.IsBalanceConnected)
            {
                await messageService.ShowMessage(languageService.BalancePortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }

            _calculateCts = new CancellationTokenSource();
            CalAndSetTrueValue();
            ResetMeasureResult();

            vmService.CanTransit = false;
            lastState = curState;
            await ChangeState(ProcessState.Manual);

            // Reading値のコピーを取得しておく
            var oldReadingArray = new ObservableCollection<ReadingValue>(
                                    ReadingValueArray.Select(x => new ReadingValue { Value = x.Value }));

            bool isSame = true;
            noNeedConfirmUnsaved = true;
            // 天秤との定期通信開始
            var res = await StartTimerWIthBalCom(_calculateCts.Token);

            if (res.Status == OperationResultType.Canceled)
            {
                // VC
                //await CommMFCAsyncType1("VC", _calculateCts.Token);

                await messageService.ShowMessage(languageService.OperationCanceled);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();

                await ChangeState(lastState);
            }
            // MFM終了後なら
            else if (isFinishedMFM)
            {
                await ChangeState(ProcessState.AfterMFM);                
            }
            else if (isCalculated)
            {
                // Reading値の変化を確認する
                for (int i = 1; i < ReadingValueArray.Count; i++)
                {
                    if (ReadingValueArray[i].Value != oldReadingArray[i].Value)
                    {
                        isSame = false;
                        break;
                    }
                }

                // Reading値が書き変えられていない場合
                if (isSame)
                {
                    if (!isCalcedAndConfed)
                    {
                        await ChangeState(ProcessState.AfterCalc);
                    }
                    else
                    {
                        await ChangeState(ProcessState.AfterCalcAndConf);
                    }
                }
                // Reading値が書き変えられている場合
                else
                {
                    // MFM終了後なら
                    if (isCalculated)
                    {
                        await ChangeState(ProcessState.AfterMFM);                        
                    }
                    else
                    {
                        await ChangeState(ProcessState.Initial);
                    }
                }
            }
            // 未計算なら initial
            else
            {
                await ChangeState(ProcessState.Initial);
            }

            // VC
            await CommMFCAsyncType1("VC", _calculateCts.Token);
            vmService.CanTransit = true;
            noNeedConfirmUnsaved = false;
            return;
        }

        /// <summary>
        /// ゲイン計算
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task CalculateGain()
        {
            bool HasNull = false;
            foreach (var readingValue in ReadingValueArray)
            {
                if(readingValue.Value == "" || readingValue.Value == null)
                {
                    HasNull = true;
                    break;
                }
            }
            if (HasNull)
            {
                await messageService.ShowMessage(languageService.CalAgainConfirm);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }

            if (!commStatusService.IsMfcConnected)
            {
                await messageService.ShowMessage(languageService.MfcPortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }
            else if (!commStatusService.IsBalanceConnected)
            {
                await messageService.ShowMessage(languageService.BalancePortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }

            // Fix me : 計算実行によってゲイン値書き換わる注意喚起メッセージ

            vmService.CanTransit = false;
            await ChangeState(ProcessState.Measurement);

            _calculateCts = new CancellationTokenSource();
            var token = _calculateCts.Token;

            // 10点リニア補正値の計算
            // 可変BPではない場合
            if (BPValue == BPType.Invariable)
            {
                var res = await CorrectLinearData(0, null, token);

                if (res.Status == OperationResultType.Canceled)
                {
                    await messageService.ShowMessage(languageService.OperationCanceled);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                }
                else if (res.Status == OperationResultType.Failure)
                {
                    await messageService.ShowMessage(languageService.MfcCommError);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                }
            }
            // 可変BPの場合
            else
            {
                var bpList = await CalculateAndWriteBP(token);
                if (bpList == null)
                {
                    // TODO : CalculateAndWriteBPの返り値の型をOperationResultに統一する
                    await messageService.ShowMessage(languageService.MfcCommError);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                    return;
                }
                else if (bpList.Length == 1 && bpList[0] == double.MaxValue)
                {
                    await messageService.ShowMessage(languageService.OperationCanceled);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                    return;
                }

                var res = await CorrectLinearData(1, bpList, token);
                if (res.Status == OperationResultType.Canceled)
                {
                    // VC
                    //res = await CommMFCAsyncType1("VC", _calculateCts.Token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return;
                    }

                    await messageService.ShowMessage(languageService.OperationCanceled);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                }
            }

            // 計算終了後なら
            if (isCalculated)
            {
                await ChangeState(ProcessState.AfterCalc);
            }
            // 未計算ならinitial
            else
            {
                await ChangeState(ProcessState.Initial);
            }

            // VC
            await CommMFCAsyncType1("VC", _calculateCts.Token);
            vmService.CanTransit = true;
        }

        /// <summary>
        /// calcとconfをmodeで判別して実行
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        [RelayCommand]
        private async Task Calculate(string mode)
        {
            if (!commStatusService.IsMfcConnected)
            {
                await messageService.ShowMessage(languageService.MfcPortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }
            else if (!commStatusService.IsBalanceConnected)
            {
                await messageService.ShowMessage(languageService.BalancePortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }

            if (mode == Calc)
            {
                isCalculated = false;
                isCalcedAndConfed = false;
            }

            vmService.CanTransit = false;

            lastState = curState;
            // 5%刻みとの状態遷移を分ける
            if (IncrementValue != IncerementType.FivePercent)
            {
                await ChangeState(ProcessState.Measurement);
            }
            else
            {
                noNeedConfirmUnsaved = true;
                await ChangeState(ProcessState.FiveperConf);
            }

            _calculateCts = new CancellationTokenSource();
            OperationResult res = new OperationResult(OperationResultType.Success, null, null);

            try
            {
                // 5%刻みのConfのみ別
                if (mode == conf && IncrementValue == IncerementType.FivePercent)
                {
                    res = await ConfBy5Per(mode, _calculateCts.Token);
                }                 
                else
                {
                    res = await CalculateCoreAsync(mode, _calculateCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                await messageService.ShowMessage(languageService.OperationCanceled);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
            }
            finally
            {
                if (res.Status == OperationResultType.Canceled)
                {
                    // VC
                    //await CommMFCAsyncType1("VC", _calculateCts.Token);
                    await messageService.ShowMessage(languageService.OperationCanceled);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();

                    if (mode == calc)
                    {
                        await ChangeState(ProcessState.AfterMFM);
                    }
                    else
                    {
                        await ChangeState(lastState);
                    }
                }
                else if (res.Status == OperationResultType.Failure)
                {
                    // VC
                    //await CommMFCAsyncType1("VC", _calculateCts.Token);
                    await ChangeState(lastState);                    
                }
                else
                {
                    if (curState == ProcessState.FiveperConf)
                    {
                        await ChangeState(lastState);
                    }
                    else if (mode == Calc)
                    {
                        ExportReadingCsv();
                        await ChangeState(ProcessState.AfterCalc);
                    }
                    else
                    {
                        if (isCalculated)
                        {
                            isCalcedAndConfed = true;
                            vmService.HasNonsavedOutput = true;
                            await ChangeState(ProcessState.AfterCalcAndConf);
                        }
                        else if (!isFinishedMFM)
                        {
                            await ChangeState(ProcessState.Initial);
                        }
                        else
                        {
                            await ChangeState(ProcessState.AfterMFM);
                        }
                    }
                }

                // VC
                await CommMFCAsyncType1("VC", _calculateCts.Token);
                lastState = curState;
                vmService.CanTransit = true;
                _calculateCts?.Dispose();
                _calculateCts = null;
                noNeedConfirmUnsaved = false;
            }
        }

        /// <summary>
        /// CalculateおよびConfirm押下時の処理
        /// </summary>
        /// <param name="mode"></param>
        private async Task<OperationResult> CalculateCoreAsync(string mode, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            OperationResult res;

            if (mode == Calc)
            {
                ResetOutputResult(false);
            }
            else
            {
                ResetOutputResult_Confrim();
            }

            List<string> swValueList = new List<string>() 
            { 
                "01000", "02000", "03000", "04000", "05000", "06000", "07000", "08000", "09000", "10000", 
            };

            int outputIndex = 1;
            foreach (var swValue in swValueList) 
            {
                // OSさせる
                res = await DoOverShoot(token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                // オーバーシュート後の安定待ち
                await WaitUntilStableAfterOS(token);

                // 繰り返し処理 各流量出力において計測開始
                // CD, VS, SW, 01000~10000
                res = await CommMFCAsyncType1("CD", token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                // CD送信
                res = await CommMFCAsyncType1("VS", token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                // 流量設定
                res = await CommMFCAsyncType3("SW", swValue, token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                // 捨て待ち
                int waitTime = int.Parse(waitOSValue) * 1000;
                res = await WaitForDispose(waitTime, token);
                if (res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                // 天秤との通信を指定回数行い、測定ボックスに格納していく
                // 開始してすぐに初期値を取る。これは回数に含める
                // 2回目以降の測定ではは(60 / 実際のインターバル * g(n-1) - g(n))をn行に書き込んでいく

                // 初期化処理
                ResetMeasureResult();

                // vtempタイマー生成(Calcモードの場合のみ。毎回生成)
                // vtempタイマースタート とりあえず一回読んでおく
                await GetAndOutputVtemp(mode, token);
                int vtInterval = int.Parse(VtempInterval) * 1000;
                precisionTimer.Start(async () =>
                {
                    cntBalCom++;
                    // Vtemp 基準1を取得
                    await GetAndOutputVtemp(mode,token);

                    return;
                }, vtInterval, token);

                lastUTC = DateTime.UtcNow;
                dateList[1] = lastUTC;

                // 計算式においてn, n-1項目を必要とするため、一度目の通信を先行して行っておく
                var firstVal = await CommBalanceAsyncCommand(token);
                Logging(firstVal.Payload, false);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }
                lastBalanceVal = ConvertBalanceResToMS(firstVal.Payload);
                MeasurementValues[1].Value = lastBalanceVal.ToString();
               
                // 比較値格納インデクス
                int interval = int.Parse(IntervalValue) * 1000;
                int index = 1;
                int attempts = int.Parse(AttemptsValue);

                // ループ回数は一回少ない
                attempts = (attempts - 1);

                while (true)
                {
                    // 2回目以降はインターバル待ち
                    await WaitForDispose(interval, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return res;
                    }

                    // index進める
                    index++;

                    // indexが規定回数を超えたら終了
                    if (index > attempts)
                    {
                        break;
                    }

                    // 通信&リスト書き込み                    
                    res = await Gn1GnComm(index, token);
                    Logging(res.Payload, false);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return res;
                    }
                }

                // 最後の通信はループ外でawaitしないと、通信がワーカースレッドによる
                // レースコンディションにより意図しないタイミングで終了する
                res = await Gn1GnComm(index, token);
                Logging(res.Payload, true);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                // vtempタイマーストップ
                precisionTimer.Stop();        

                // calculate時のみ
                if (mode == "calc")
                {
                    // ***出力処理***
                    // インターバル回数分の測定が完了したら
                    // 1. VO値を取得、0.5掛けしてVOUTに格納
                    // 2. 初期VO：FE26,27(空き領域)の値を読み取り加工して格納
                    res = await CalcAsync(outputIndex, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return res;
                    }

                    outputIndex++;
                }
                // confrim時
                else
                {
                    res = await ConfAsync(outputIndex, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return res;
                    }

                    outputIndex++;
                }
            }

            // calculate時のみ
            if (mode == Calc)
            {
                // 10点リニア補正値の計算
                // 可変BPではない場合
                if (BPValue == BPType.Invariable)
                {
                    res = await CorrectLinearData(0, null, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return res;
                    }
                }
                // 可変BPの場合
                else
                {
                    var bpList = await CalculateAndWriteBP(token);
                    if (bpList == null)
                    {
                        await messageService.ShowMessage(languageService.MfcCommError);
                        await Task.Delay(messageFadeTime);
                        await messageService.CloseWithFade();
                        return OperationResult.Failed();
                    }
                    else if (bpList.Length == 1 && bpList[0] == double.MaxValue)
                    {
                        await messageService.ShowMessage(languageService.OperationCanceled);
                        await Task.Delay(messageFadeTime);
                        await messageService.CloseWithFade();
                        return OperationResult.Canceled();
                    }

                    res = await CorrectLinearData(1, bpList, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return res;
                    }
                }

                var result = await FBDataRead(token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }
            }

            // VC
            res = await CommMFCAsyncType1("VC", token);
            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
            {
                return res;
            }

            // CA
            res = await CommMFCAsyncType1("CA", token);
            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
            {
                return res;
            }

            return OperationResult.Success();
        }



        /// <summary>
        /// 5%刻みのconf
        /// </summary>
        /// <param name="mode"></param>
        private async Task<OperationResult> ConfBy5Per(string mode, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            OperationResult res;

            // 出力表の初期化
            ResetOutputResult(true);

            List<string> swValueList = new List<string>()
            {
                "00500", "01000", "01500", "02000", "02500", "03000", "03500", "04000", "04500", "05000",
                "05500", "06000", "06500", "07000", "07500", "08000", "08500", "09000", "09500", "10000",
            };

            int outputIndex = 1;
            foreach (var swValue in swValueList)
            {
                // OSさせる
                res = await DoOverShoot(token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                // オーバーシュート後の安定待ち
                await WaitUntilStableAfterOS(token);

                // 繰り返し処理 各流量出力において計測開始
                // CD, VS, SW, 01000~10000
                res = await CommMFCAsyncType1("CD", token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                // CD送信
                res = await CommMFCAsyncType1("VS", token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                // 流量設定
                res = await CommMFCAsyncType3("SW", swValue, token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                // 捨て待ち
                int waitTime = int.Parse(waitOSValue) * 1000;
                res = await WaitForDispose(waitTime, token);
                if (res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                // 天秤との通信を指定回数行い、測定ボックスに格納していく
                // 開始してすぐに初期値を取る。これは回数に含める
                // 2回目以降の測定ではは(60 / 実際のインターバル * g(n-1) - g(n))をn行に書き込んでいく

                // 初期化処理
                ResetMeasureResult();

                // vtempタイマー生成(Calcモードの場合のみ。毎回生成)
                // vtempタイマースタート とりあえず一回読んでおく
                await GetAndOutputVtemp(mode, token);
                int vtInterval = int.Parse(VtempInterval) * 1000;
                precisionTimer.Start(async () =>
                {
                    cntBalCom++;
                    // Vtemp 基準1を取得
                    await GetAndOutputVtemp(mode, token);

                    return;
                }, vtInterval, token);

                lastUTC = DateTime.UtcNow;
                dateList[1] = lastUTC;

                // 計算式においてn, n-1項目を必要とするため、一度目の通信を先行して行っておく
                var firstVal = await CommBalanceAsyncCommand(token);
                Logging(firstVal.Payload, false);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }
                lastBalanceVal = ConvertBalanceResToMS(firstVal.Payload);
                MeasurementValues[1].Value = lastBalanceVal.ToString();

                // 比較値格納インデクス
                int interval = int.Parse(IntervalValue) * 1000;
                int index = 1;
                int attempts = int.Parse(AttemptsValue);

                // ループ回数は一回少ない
                attempts = (attempts - 1);

                while (true)
                {
                    // 2回目以降はインターバル待ち
                    await WaitForDispose(interval, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return res;
                    }

                    // index進める
                    index++;

                    // indexが規定回数を超えたら終了
                    if (index > attempts)
                    {
                        break;
                    }

                    // 通信&リスト書き込み                    
                    res = await Gn1GnComm(index, token);
                    Logging(res.Payload, false);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return res;
                    }
                }

                // 最後の通信はループ外でawaitしないと、通信がワーカースレッドによる
                // レースコンディションにより意図しないタイミングで終了する
                res = await Gn1GnComm(index, token);
                Logging(res.Payload, true);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                // vtempタイマーストップ
                precisionTimer.Stop();

                res = await CalculateReadingValue(index, token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                if (outputIndex <= 10)
                {
                    ReadingValueBelow50Array[outputIndex] = res.Payload;
                }
                else 
                {
                    ReadingValueAbove50Array[outputIndex - 10] = res.Payload;
                }

                outputIndex++;        
            }

            // VC
            res = await CommMFCAsyncType1("VC", token);
            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
            {
                return res;
            }

            // CA
            res = await CommMFCAsyncType1("CA", token);
            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
            {
                return res;
            }

            return OperationResult.Success();
        }

        private async Task<OperationResult> GetAndOutputVtemp(string mode, CancellationToken token)
        {
            var vTempLower = await CommMFCAsyncType3("ER", "FC1C", token);
            if (vTempLower.Status == OperationResultType.Failure || vTempLower.Status == OperationResultType.Canceled)
            {
                return vTempLower;
            }

            var vTempUpper = await CommMFCAsyncType3("ER", "FC1D", token);
            if (vTempUpper.Status == OperationResultType.Failure || vTempUpper.Status == OperationResultType.Canceled)
            {
                return vTempUpper;
            }

            if (vTempLower.Payload == null || vTempUpper.Payload == null)
            {
                return OperationResult.Failed("ER");
            }

            var vtLowerVal = vTempLower.Payload.Substring(3, 2);
            var vtUpperVal = vTempUpper.Payload.Substring(3, 2);

            if (mode == calc)
            {
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    VtempValueCal = vtUpperVal + "/" + vtLowerVal;
                });
            }
            else if (mode == conf)
            {
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    VtempValueConf = vtUpperVal + "/" + vtLowerVal;
                });
            }

            return OperationResult.Success();
        }

        /// <summary>
        /// オーバーシュート実行
        /// </summary>
        /// <returns></returns>
        private async Task<OperationResult> DoOverShoot(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            // CD送信
            var res = await CommMFCAsyncType1("CD", token);
            if(res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
            {
                return res;
            }

            // CD送信
            res = await CommMFCAsyncType1("VS", token);
            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
            {
                return res;
            }

            // OSさせる
            res = await CommMFCAsyncType3("SW", "11000", token);
            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
            {
                return res;
            }

            return OperationResult.Success();
        }

        /// <summary>
        /// オーバーシュート後の待ち
        /// </summary>
        /// <returns></returns>
        private async Task WaitUntilStableAfterOS(CancellationToken token)
        {
            int stableTime = int.Parse(StableOSValue) * 1000;
            var stableOS = new HighPrecisionDelay();
            await stableOS.WaitAsync(stableTime, token);
        }

        /// <summary>
        /// オーバーシュート後、計測前の捨て待ち
        /// </summary>
        /// <param name="waitTime"></param>
        /// <returns></returns>
        private async Task<OperationResult> WaitForDispose(int waitTime, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var waitOS = new HighPrecisionDelay();
                var res = await waitOS.WaitAsync(waitTime, token);
                if(res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                return res;
            }
            catch(OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// Reading値、Vout値、初期VO値の算出タスクを同時発火、計算結果を格納する
        /// 通信処理はセマフォで直列処理としている
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private async Task<OperationResult> CalcAsync(int index, CancellationToken token)
        {
            var readingTask = CalculateReadingValue(index, token);
            var voutTask = GetAndCalVOUT(index, token);
            var voTask = GetAndCalInitialVO(index, token);

            var reading = await readingTask;
            var vout = await voutTask;
            var vo = await voTask;

            if (reading.Status == OperationResultType.Failure || vout.Status == OperationResultType.Failure || vo.Status == OperationResultType.Failure)
            {
                return OperationResult.Failed();
            }

            //await Task.WhenAll(readingTask, voutTask, voTask);

            // UI更新
            UpdateUI_WithCacl(index, readingTask.Result.Payload, voutTask.Result.Payload, voTask.Result.Payload);
            return OperationResult.Success();
        }

        /// <summary>
        /// C_Data(補正後のReading値)、VOUT更新、VO(補正後)の算出タスクを同時発火、計算結果を格納する
        /// 通信処理はセマフォで直列処理としている
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private async Task<OperationResult> ConfAsync(int index, CancellationToken token)
        {
            var readingTask = CalculateReadingValue(index, token);
            var voutTask = GetAndCalVOUT(index, token);
            var voTask = GetAndCalInitialVO(index, token);

            var reading = await readingTask;
            var vout = await voutTask;
            var vo = await voTask;

            if (reading.Status == OperationResultType.Failure || vout.Status == OperationResultType.Failure || vo.Status == OperationResultType.Failure)
            {
                return OperationResult.Failed();
            }

            //await Task.WhenAll(readingTask, voutTask, voTask);

            // UI更新
            UpdateUI_WithConf(index, readingTask.Result.Payload, voutTask.Result.Payload, voTask.Result.Payload);
            return OperationResult.Success();
        }

        /// <summary>
        /// Reading値を計算して、該当グリッドに格納する
        /// </summary>
        /// <param name="index"></param>
        private Task<OperationResult> CalculateReadingValue(int index, CancellationToken token)
        {
            // 計測回数の取得
            var attemptNum = int.Parse(attemptsValue);

            // 計測結果リストから最後の項目を除いたリスト(最後の計測結果は含まない)
            // 最初の項目はグリッド最上段のため(空欄)除去している
            var target = MeasurementValues.Skip(1).Take(attemptNum - 1).ToList();

            // 測定結果の内のmax値およびインデクスを求める。
            var max = target.Select((v, i) => (v, i)).MaxBy(x => float.Parse(x.v.Value));
            var maxNum = float.Parse(max.v.Value);
            var maxNumIndex = max.i;

            // 測定結果の内のmin値およびインデクスを求める。
            var min  = target.Select((v, i) => (v, i)).MinBy(x => float.Parse(x.v.Value));
            var minNum = float.Parse(min.v.Value);
            var minNumIndex = min.i;

            // 合計値を計算する。最小値と最大値は加算しない。
            float sum = 0f;
            for (int i = 0; i < target.Count(); i++)
            {
                if (i != maxNumIndex && i != minNumIndex)
                {
                    sum += float.Parse(target[i].Value);
                }
            }

            // 加算回数を計算する。
            int sumCounts = target.Count() - 2;

            // (合計値 ÷ 加算回数)を小数第一位まで計算し、該当グリッドに格納する。
            float readingVal = sum / sumCounts;

            return Task<OperationResult>.FromResult(OperationResult.Success(readingVal.ToString("F1")));
        }

        // 通信処理用のセマフォ　GetAndCalVOUTとGetAndCalInitialVOで共有するのでここに記述
        private static readonly SemaphoreSlim _commLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// インターバル毎の計測が完了してからのVOUT計算
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private async Task<OperationResult> GetAndCalVOUT(int index, CancellationToken token)
        {
            await _commLock.WaitAsync();

            OperationResult res = new OperationResult(OperationResultType.Success, null, null);

            try
            {
                var result = await CommMFCAsyncType2("OR", token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return OperationResult.Canceled();
                }

                result.Payload = result.Payload.Substring(3, result.Payload.Length - 3);
                int val = int.Parse(result.Payload);
                int rest = (int)(val / 2.0 + (val >= 0 ? 0.5 : -0.5));
                res.Payload = rest.ToString();                

                return res;
            }
            finally 
            { 
                _commLock.Release(); 
            }
        }

        /// <summary>
        /// インターバル毎の計測が完了してからの初期VO計算(VO追加)
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private async Task<OperationResult> GetAndCalInitialVO(int index, CancellationToken token)
        {
            await _commLock.WaitAsync();

            try
            {
                token.ThrowIfCancellationRequested();

                // 上位バイト
                var res = await CommMFCAsyncType3("ER", "FE27", token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                if (res.Payload.Length < 5)
                {
                    return OperationResult.Failed("ER");
                }

                var upper = res.Payload.Substring(3, 2);
                var upperVal = Convert.ToInt32(upper, 16);

                // 下位バイト
                res = await CommMFCAsyncType3("ER", "FE26", token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }

                if (res.Payload.Length < 5)
                {
                    return OperationResult.Failed("ER");
                }
                var lower = res.Payload.Substring(3, 2);
                var lowerVal = Convert.ToInt32(lower, 16);

                var hex80 = Convert.ToInt32("80", 16);

                var temp = 5 * ((upperVal - hex80) * 256 + lowerVal);
                decimal tempD = (decimal)temp / ((decimal)hex80 * 256);
                decimal x = Math.Round(tempD, 3, MidpointRounding.AwayFromZero);

                return OperationResult.Success(x.ToString());
            }
            catch(OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
            finally 
            { 
                _commLock.Release(); 
            }
        }

        /// <summary>
        /// Reading値、Vout値、初期VO値を更新する
        /// </summary>
        /// <param name="index"></param>
        /// <param name="readingVal"></param>
        /// <param name="vOut"></param>
        /// <param name="initialVo"></param>
        private void UpdateUI_WithCacl(int index, string readingVal, string vOut, string initialVo)
        {
            var rv = new ReadingValue();
            rv.Value = readingVal;
            ReadingValueArray[index] = rv;
            VoutArray[index] = vOut;
            InitialVoArray[index] = initialVo;
        }

        /// <summary>
        /// Reading値、Vout値、初期VO値を更新する
        /// </summary>
        /// <param name="index"></param>
        /// <param name="readingVal"></param>
        /// <param name="vOut"></param>
        /// <param name="initialVo"></param>
        private void UpdateUI_WithConf(int index, string readingVal, string vOut, string Vo)
        {
            CorrectDataArray[index] = readingVal;
            VoutArray[index] = vOut;
            VOArray[index] = Vo;
        }


        List<string> test = new List<string>()
        {
            //"",
            //"14.5567",
            //"25.918",
            //"53.2634",
            //"67.4411",
            //"111.911",
            //"159.11222",
            //"180.1222",
            //"245.9924",
            //"309.12455",
            //"400.1213"
            "",
            "-687.3",
            "-1002.5",
            "2051",
            "-45",
            "342.2",
            "-2663.9",
            "1757.4",
            "-441",
            "6456.8",
            "-750.2"
        };

        /// <summary>
        /// 10点リニア補正値の計算を行う。非可変BP/可変BP対応
        /// </summary>
        private async Task<OperationResult> CorrectLinearData(int mode, double[]? bpValueList, CancellationToken token)
        {
            try
            {
                int loopCnt, mulNum;

                if (mode == 0)
                {
                    loopCnt = 10;
                }
                else
                {
                    loopCnt = 11;
                }

                mulNum = 10;

                // 通信処理の返り値
                OperationResult res;

                // バージョンによってゲイン初期値が変わる
                string initValStr = VersionValue == VersionType.Ver298 ? "08" : "10";
                int initVal = int.Parse(initValStr);

                // ゲイン初期値
                var initialGain = initVal == 10 ? 4096 : 2048;

                // 計算したゲイン
                double newGain = 0;

                // ゲインの合計
                double gainSum = 0;

                // ゲインの整数変換後
                long iGain = 0;

                // 整数ゲインの上位8bitの10進数表記, 下位8bitの10進数標記
                long upperGain, lowerGain;

                // 整数ゲインの上位8bit, 下位8bit
                string hexUpperGain, hexLowerGain;

                //// debug: initialGainを乗じる前のゲイン値リスト
                //List<double> noMulInitialGainList = new List<double>();

                // 算出したゲインの上位/下位バイトのペアリスト item1が下位、item2が上位
                List<Tuple<string, string>> ULGainList = new List<Tuple<string, string>>();

                // 10点リニア係数のFBアドレス(1~10までのペア)
                List<Tuple<string, string>> InitPairList = new List<Tuple<string, string>>()
                {
                    Tuple.Create(FbMap["FB90"], FbMap["FB91"]), Tuple.Create(FbMap["FB92"], FbMap["FB93"]),
                    Tuple.Create(FbMap["FB94"], FbMap["FB95"]), Tuple.Create(FbMap["FB96"], FbMap["FB97"]),
                    Tuple.Create(FbMap["FB98"], FbMap["FB99"]), Tuple.Create(FbMap["FB9A"], FbMap["FB9B"]),
                    Tuple.Create(FbMap["FB9C"], FbMap["FB9D"]), Tuple.Create(FbMap["FB9E"], FbMap["FB9F"]),
                    Tuple.Create(FbMap["FBA0"], FbMap["FBA1"]), Tuple.Create(FbMap["FBA2"], FbMap["FBA3"])
                };

                // 非可変BPモード
                if (mode == 0)
                {
                    {
                        // 1~9のループ
                        for (int i = 1; i < loopCnt; i++)
                        {
                            if (i == 1)
                            {
                                double temp = double.Parse(ReadingValueArray[i].Value) / double.Parse(TrueValueArray[i]);
                                //noMulInitialGainList.Add(temp);
                                newGain = initialGain * temp;
                            }
                            else
                            {
                                double temp = (double.Parse(ReadingValueArray[i].Value) - double.Parse(ReadingValueArray[i - 1].Value)) /
                                              (double.Parse(TrueValueArray[i]) - double.Parse(TrueValueArray[i - 1]));
                                //noMulInitialGainList.Add(temp);
                                newGain = initialGain * temp;
                            }

                            gainSum += newGain;

                            // VB6のNEWFBDAT = Int(NEWGAIN)にあたる。
                            // これはFloor変換に該当するため、再現している。暗黙変換ではない。
                            iGain = (long)Math.Floor(newGain);

                            // 整数ゲインから上位/下位8bitの10進数を求める
                            upperGain = iGain / 256;
                            lowerGain = iGain % 256;

                            // 16進数表記に変換する
                            hexUpperGain = upperGain.ToString("X2");
                            hexUpperGain = "00" + hexUpperGain;
                            hexUpperGain = hexUpperGain.Substring(hexUpperGain.Length - 2, 2);

                            hexLowerGain = lowerGain.ToString("X2");
                            hexLowerGain = "00" + hexLowerGain;
                            hexLowerGain = hexLowerGain.Substring(hexLowerGain.Length - 2, 2);
                            ULGainList.Add(Tuple.Create(hexLowerGain, hexUpperGain));

                            // 該当アドレスに書き込む
                            var command1 = Tuple.Create(InitPairList[i - 1].Item2, hexUpperGain);
                            res = await CommMFCAsyncTypeRW("EW", command1, token);
                            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                            {
                                return res;
                            }

                            var command2 = Tuple.Create(InitPairList[i - 1].Item1, hexLowerGain);
                            res = await CommMFCAsyncTypeRW("EW", command2, token);
                            if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                            {
                                return res;
                            }
                        }

                        // 10番目のデータに対する処理
                        newGain = mulNum * initialGain - gainSum;
                    }

                    iGain = (long)Math.Floor(newGain);

                    // 整数ゲインから上位/下位8bitの10進数を求める
                    upperGain = iGain / 256;
                    lowerGain = iGain % 256;

                    // 16進数表記に変換する
                    hexUpperGain = upperGain.ToString("X2");
                    hexUpperGain = "00" + hexUpperGain;
                    hexUpperGain = hexUpperGain.Substring(hexUpperGain.Length - 2, 2);

                    hexLowerGain = lowerGain.ToString("X2");
                    hexLowerGain = "00" + hexLowerGain;
                    hexLowerGain = hexLowerGain.Substring(hexLowerGain.Length - 2, 2);
                    ULGainList.Add(Tuple.Create(hexLowerGain, hexUpperGain));

                    // 該当アドレスに書き込む
                    var command10_1 = Tuple.Create(InitPairList[InitPairList.Count() - 1].Item2, hexUpperGain);
                    res = await CommMFCAsyncTypeRW("EW", command10_1, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return res;
                    }

                    var command10_2 = Tuple.Create(InitPairList[InitPairList.Count() - 1].Item1, hexLowerGain);
                    res = await CommMFCAsyncTypeRW("EW", command10_2, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return res;
                    }
                }

                // 可変BPモード
                else
                {
                    // 1~9のループ
                    for (int i = 1; i < loopCnt; i++)
                    {
                        if (i == 1)
                        {
                            double temp = 10d / bpValueList[i];
                            newGain = initialGain * temp;
                        }
                        else
                        {
                            double temp = 10d / (bpValueList[i] - bpValueList[i - 1]);
                            newGain = initialGain * temp;
                        }

                        // VB6のNEWFBDAT = Int(NEWGAIN)にあたる。
                        // これはFloor変換に該当するため、再現している。暗黙変換ではない。
                        iGain = (long)Math.Floor(newGain);

                        // 整数ゲインから上位/下位8bitの10進数を求める
                        upperGain = iGain / 256;
                        lowerGain = iGain % 256;

                        // 16進数表記に変換する
                        hexUpperGain = upperGain.ToString("X2");
                        hexUpperGain = "00" + hexUpperGain;
                        hexUpperGain = hexUpperGain.Substring(hexUpperGain.Length - 2, 2);

                        hexLowerGain = lowerGain.ToString("X2");
                        hexLowerGain = "00" + hexLowerGain;
                        hexLowerGain = hexLowerGain.Substring(hexLowerGain.Length - 2, 2);
                        ULGainList.Add(Tuple.Create(hexLowerGain, hexUpperGain));

                        // 該当アドレスに書き込む
                        var command1 = Tuple.Create(InitPairList[i - 1].Item2, hexUpperGain);
                        res = await CommMFCAsyncTypeRW("EW", command1, token);
                        if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                        {
                            return res;
                        }

                        var command2 = Tuple.Create(InitPairList[i - 1].Item1, hexLowerGain);
                        res = await CommMFCAsyncTypeRW("EW", command2, token);
                        if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                        {
                            return res;
                        }
                    }
                }

                // 書き込み値の確認
                int index = 0;
                foreach (var pair in InitPairList)
                {
                    // 上位/下位バイト
                    var bytes = ULGainList[index];

                    // 上位確認
                    res = await CommMFCAsyncType3("ER", pair.Item2, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return res;
                    }

                    if (res.Payload.Length < 5)
                    {
                        return OperationResult.Failed("ER");
                    }

                    var temp = res.Payload.Substring(3, 2);
                    if (temp != bytes.Item2)
                    {
                        return OperationResult.Failed();
                    }

                    // 下位確認
                    res = await CommMFCAsyncType3("ER", pair.Item1, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return res;
                    }

                    if (res.Payload.Length < 5)
                    {
                        return OperationResult.Failed("ER");
                    }

                    // アドレスが重複していると必ずここで失敗する
                    temp = res.Payload.Substring(3, 2);
                    if (temp != bytes.Item1)
                    {
                        return OperationResult.Failed();
                    }

                    index++;
                }

                // リセット実行
                res = await CommMFCAsyncType1("RE", token);
                if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                {
                    return res;
                }
                isCalculated = true;
                await Task.Delay(100);
                return OperationResult.Success();
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// 可変BP処理のため、BPを計算してEEPROMに書き込む
        /// 回帰分析により5次多項式曲線を求める
        /// </summary>
        private async Task<double[]?> CalculateAndWriteBP(CancellationToken token)
        {
            try
            {
                List<Tuple<string, string>> bpValueList = new List<Tuple<string, string>>();

                OperationResult res = new OperationResult(OperationResultType.Success, null, null);

                // Reading値を格納
                var readingList = ReadingValueArray; // test;

                // 5次多項式の係数計算のための数学アルゴリズム検証
                double[] xData = {0.1d, 0.2d, 0.3d, 0.4d, 0.5d, 0.6d, 0.7d, 0.8d, 0.9d, 1d};

                double fullScaleOutput = double.Parse(readingList[10].Value);

                double[] yData =
                {
                    double.Parse(readingList[1].Value)  / fullScaleOutput * 100d,
                    double.Parse(readingList[2].Value)  / fullScaleOutput * 100d,
                    double.Parse(readingList[3].Value)  / fullScaleOutput * 100d,
                    double.Parse(readingList[4].Value)  / fullScaleOutput * 100d,
                    double.Parse(readingList[5].Value)  / fullScaleOutput * 100d,
                    double.Parse(readingList[6].Value)  / fullScaleOutput * 100d,
                    double.Parse(readingList[7].Value)  / fullScaleOutput * 100d,
                    double.Parse(readingList[8].Value)  / fullScaleOutput * 100d,
                    double.Parse(readingList[9].Value)  / fullScaleOutput * 100d,
                    double.Parse(readingList[10].Value) / fullScaleOutput * 100d
                };

                // 5次多項式の回帰曲線を求める
                int degree = 5;

                int n = xData.Length;

                // 設計行列 X (n行 x degree列)
                var X = Matrix<double>.Build.Dense(n, degree, (i, j) => Math.Pow(xData[i], j + 1));

                // ベクトルY
                var Y = Vector<double>.Build.Dense(yData);

                // QR分解で最小二乗解 切片無し
                Vector<double> coeffs = X.QR().Solve(Y);

                double[] newBP = { 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d };
                double[] BPCopyList = { 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d, 0d };

                // 多項式に対して10~100%の解xを求める
                // 二分探索&ホーナー法を使う
                for (int i = 1; i < 11; i++)
                {
                    newBP[i] = BinarySearch(i * 10d, coeffs);
                }

                // 返り値用にシャロ―コピー
                newBP.AsSpan().CopyTo(BPCopyList);

                // 既存手法に基づき BP *32768 / 125したものを16進数に直す
                for (int i = 1; i < 11; i++)
                {
                    newBP[i] = newBP[i] * 32768d / 125d;
                    var bp = (long)Math.Round(newBP[i], MidpointRounding.ToEven);

                    // マクロのDEC2HEX部における暗黙変換を再現
                    long upperBp = bp / 256;
                    long lowerBp = bp % 256;

                    // 16進数表記に変換する
                    var hexUpperBp = upperBp.ToString("X2");
                    hexUpperBp = "00" + hexUpperBp;
                    hexUpperBp = hexUpperBp.Substring(hexUpperBp.Length - 2, 2);

                    var hexLowerBp = lowerBp.ToString("X2");
                    hexLowerBp = "00" + hexLowerBp;
                    hexLowerBp = hexLowerBp.Substring(hexLowerBp.Length - 2, 2);
                    bpValueList.Add(Tuple.Create(hexLowerBp, hexUpperBp));
                }

                // 新BP書き込み
                // 10点リニア閾値(FBA4~FBB5)の初期化
                List<Tuple<string, string>> cmdPairList = new List<Tuple<string, string>>()
                {
                    Tuple.Create(ThresholdMap["FBA4"], ThresholdMap["FBA5"]), Tuple.Create(ThresholdMap["FBA6"], ThresholdMap["FBA7"]),
                    Tuple.Create(ThresholdMap["FBA8"], ThresholdMap["FBA9"]), Tuple.Create(ThresholdMap["FBAA"], ThresholdMap["FBAB"]),
                    Tuple.Create(ThresholdMap["FBAC"], ThresholdMap["FBAD"]), Tuple.Create(ThresholdMap["FBAE"], ThresholdMap["FBAF"]),
                    Tuple.Create(ThresholdMap["FBB0"], ThresholdMap["FBB1"]), Tuple.Create(ThresholdMap["FBB2"], ThresholdMap["FBB3"]),
                    Tuple.Create(ThresholdMap["FBB4"], ThresholdMap["FBB5"])
                };

                // 該当アドレスに書き込む。最後の要素(B4/B5)は除く。
                for (int i = 0; i < cmdPairList.Count() - 1; i++)
                {
                    var command1 = Tuple.Create(cmdPairList[i].Item2, bpValueList[i].Item2);
                    res = await CommMFCAsyncTypeRW("EW", command1, token);
                    if (res.Status == OperationResultType.Failure)
                    {
                        return null;
                    }
                    else if (res.Status == OperationResultType.Canceled)
                    {
                        double[] canceledArray = new double[1];
                        canceledArray[0] = double.MaxValue;
                        return canceledArray;
                    }

                    var command2 = Tuple.Create(cmdPairList[i].Item1, bpValueList[i].Item1);
                    res = await CommMFCAsyncTypeRW("EW", command2, token);
                    if (res.Status == OperationResultType.Failure)
                    {
                        return null;
                    }
                    else if (res.Status == OperationResultType.Canceled)
                    {
                        double[] canceledArray = new double[1];
                        canceledArray[0] = double.MaxValue;
                        return canceledArray;
                    }
                }

                // 書き込み結果の確認
                foreach (var cmdPair in cmdPairList)
                {
                    var result = await CommMFCAsyncType3("ER", cmdPair.Item1, token);
                    if (res.Status == OperationResultType.Failure)
                    {
                        return null;
                    }
                    else if (res.Status == OperationResultType.Canceled)
                    {
                        double[] canceledArray = new double[1];
                        canceledArray[0] = double.MaxValue;
                        return canceledArray;
                    }

                    if (result.Payload.Length < 5)
                    {
                        return null;
                    }

                    result = await CommMFCAsyncType3("ER", cmdPair.Item2, token);
                    if (res.Status == OperationResultType.Failure)
                    {
                        return null;
                    }
                    else if (res.Status == OperationResultType.Canceled)
                    {
                        double[] canceledArray = new double[1];
                        canceledArray[0] = double.MaxValue;
                        return canceledArray;
                    }

                    if (result.Payload.Length < 5)
                    {
                        return null;
                    }
                }

                return BPCopyList;
            }
            catch (OperationCanceledException)
            {
                double[] canceledArray = new double[1];
                canceledArray[0] = double.MaxValue;
                return canceledArray;
            }
        }

        /// <summary>
        /// confボタン押下でセットポイントを変える
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        [RelayCommand]
        private async Task ManualChangeSetpoint(string index)
        {
            if (!commStatusService.IsMfcConnected)
            {
                await messageService.ShowMessage(languageService.MfcPortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }
            else if (!commStatusService.IsBalanceConnected)
            {
                await messageService.ShowMessage(languageService.BalancePortError);
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }

            // viewに通知いくが、使っていない。アニメーション実装を要する場合に使う。
            ConfIndex = int.Parse(index);

            // Manual押下前提なので、CancellationTokenSourceはManualのものを共用する
            if (_calculateCts != null)
            {
                var token = _calculateCts.Token;

                List<string> swValueList = new List<string>()
                {
                    "01000", "02000", "03000", "04000", "05000", "06000", "07000", "08000", "09000", "10000",
                };

                // 繰り返し処理 各流量出力において計測開始
                // CD, VS, SW, 01000~10000
                var res = await CommMFCAsyncType1("CD", token);
                if (res.Status == OperationResultType.Canceled)
                {
                    await messageService.ShowMessage(languageService.OperationCanceled);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                }

                // CD送信
                res = await CommMFCAsyncType1("VS", token);
                if (res.Status == OperationResultType.Canceled)
                {
                    await messageService.ShowMessage(languageService.OperationCanceled);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                }

                // 流量設定
                res = await CommMFCAsyncType3("SW", swValueList[ConfIndex - 1], token);
                if (res.Status == OperationResultType.Canceled)
                {
                    await messageService.ShowMessage(languageService.OperationCanceled);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                }
            }
        }

        /// <summary>
        /// ログクリア
        /// </summary>
        [RelayCommand]
        private void ClearLog()
        {
            Logs.Clear();
        }

        /// <summary>
        /// 解の二分探索
        /// </summary>
        public double BinarySearch(double target, Vector<double> coeffs)
        {
            double low = 0d;
            double high = 1d;
            double mid = 0d;

            for (int i = 0; i < 31; i++)
            {
                mid = 0.5d * (low + high);
                
                // ホーナー法による計算量抑制　O(150)
                var ans = ((((mid * coeffs[4] + coeffs[3]) * mid + coeffs[2]) * mid + coeffs[1]) * mid + coeffs[0]) * mid;
                // 多項式そのまま計算した場合　計算量はO(450)で三倍　O(N)なので最軽量ではある
                //var ans = Math.Pow(mid, 5) * coeffs[4] + Math.Pow(mid, 4) * coeffs[3] + Math.Pow(mid, 3) * coeffs[2] + Math.Pow(mid, 2) * coeffs[1] + mid * coeffs[0];
                
                if (ans < target)
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            return mid * 100;
        }
    }
}
