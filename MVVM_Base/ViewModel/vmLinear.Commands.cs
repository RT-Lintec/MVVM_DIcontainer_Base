using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM_Base.Model;
using MathNet.Numerics.LinearAlgebra;

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

        /// <summary>
        /// 停止
        /// </summary>
        [RelayCommand]
        private void StopCmd()
        {
            if (_calculateCts != null)
            {
                _calculateCts.Cancel();
            }
        }

        /// <summary>
        /// calc & conf
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task CalAndConf()
        {
            await Calculate(Calc);
            await Calculate(Conf);
        }

        /// <summary>
        /// Manual
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task Manual()
        {
            _calculateCts = new CancellationTokenSource();
            ChangeState(ProcessState.Measurement);

            // 天秤との定期通信開始
            var res = await StartTimerWIthBalCom(_calculateCts.Token);
            if (res == "canceled")
            {
                await messageService.ShowMessage("Operation Canceled");
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
            }

            // 計算終了後なら
            if (isCalculated)
            {
                ChangeState(ProcessState.AfterMFM);
            }
            // 未計算なら initial
            else
            {
                ChangeState(ProcessState.Initial);
            }
                
            return;
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
                await messageService.ShowMessage("MFC isn't connected");
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }
            else if (!commStatusService.IsBalanceConnected)
            {
                await messageService.ShowMessage("Balance isn't connected");
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
                return;
            }

            vmService.CanTransit = false;
            ChangeState(ProcessState.Measurement);
            _calculateCts = new CancellationTokenSource();
            string res = "";

            try
            {
                res = await CalculateCoreAsync(mode, _calculateCts.Token);
            }
            catch (OperationCanceledException)
            {
                await messageService.ShowMessage("Operation Canceled");
                await Task.Delay(messageFadeTime);
                await messageService.CloseWithFade();
            }
            finally
            {
                if (res == "canceled")
                {
                    await messageService.ShowMessage("Operation Canceled");
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                }
                else if (res == "failed")
                {
                    //await messageService.ShowMessage("Operation failed");
                    //await Task.Delay(messageFadeTime);
                    //await messageService.CloseWithFade();
                }
                else
                {

                }

                ChangeState(ProcessState.AfterMFM);
                vmService.CanTransit = true;
                _mfmCts?.Dispose();
                _mfmCts = null;
            }
        }


        /// <summary>
        /// CalculateおよびConfirm押下時の処理
        /// </summary>
        /// <param name="mode"></param>
        private async Task<string> CalculateCoreAsync(string mode, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            string res = "";

            if (mode == Calc)
            {
                ResetOutputResult();
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
                if (res == "failed" || res == "canceled")
                {
                    return res;
                }

                // オーバーシュート後の安定待ち
                await WaitUntilStableAfterOS(token);

                // 繰り返し処理 各流量出力において計測開始
                // CD, VS, SW, 01000~10000
                res = await CommMFCAsyncType1("CD", token);
                if (res == "failed" || res == "canceled")
                {
                    return res;
                }

                // CD送信
                res = await CommMFCAsyncType1("VS", token);
                if (res == "failed" || res == "canceled")
                {
                    return res;
                }

                // 流量設定
                res = await CommMFCAsyncType3("SW", swValue, token);
                if (res == "failed" || res == "canceled")
                {
                    return res;
                }

                // 捨て待ち
                int waitTime = int.Parse(waitOSValue) * 1000;
                res = await WaitForDispose(waitTime, token);
                if (res == "canceled")
                {
                    return res;
                }

                // 天秤との通信を指定回数行い、測定ボックスに格納していく
                // 開始してすぐに初期値を取る。これは回数に含める
                // 2回目以降の測定ではは(60 / 実際のインターバル * g(n-1) - g(n))をn行に書き込んでいく

                // 初期化処理
                ResetMeasureResult();

                lastUTC = DateTime.UtcNow;
                dateList[1] = lastUTC;

                // 計算式においてn, n-1項目を必要とするため、一度目の通信を先行して行っておく
                var firstVal = await CommBalanceAsyncCommand(token);
                if (res == "failed" || res == "canceled")
                {
                    return res;
                }
                lastBalanceVal = ConvertBalanceResToMS(firstVal);
                Column1[1].Value = lastBalanceVal.ToString();
               
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
                    if (res == "failed" || res == "canceled")
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
                    if (res == "failed" || res == "canceled")
                    {
                        return res;
                    }
                }

                // 最後の通信はループ外でawaitしないと、通信がワーカースレッドによる
                // レースコンディションにより意図しないタイミングで終了する
                res = await Gn1GnComm(index, token);
                if (res == "failed" || res == "canceled")
                {
                    return res;
                }

                // calculate時のみ
                if (mode == "calc")
                {
                    // ***出力処理***
                    // インターバル回数分の測定が完了したら
                    // 1. VO値を取得、0.5掛けしてVOUTに格納
                    // 2. 初期VO：FE26,27(空き領域)の値を読み取り加工して格納
                    res = await CalcAsync(outputIndex, token);
                    if (res == "failed" || res == "canceled")
                    {
                        return res;
                    }

                    outputIndex++;
                }
                // confrim時
                else
                {
                    res = await ConfAsync(outputIndex, token);
                    if (res == "failed" || res == "canceled")
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
                    if (res == "failed" || res == "canceled")
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
                        // TODO : エラーメッセージ
                        vmService.CanTransit = true;
                        return "failed";
                    }
                    else if (bpList.Length == 1 && bpList[0] == double.MaxValue)
                    {
                        return "canceled";
                    }

                    res = await CorrectLinearData(1, bpList, token);
                    if (res == "failed" || res == "canceled")
                    {
                        return res;
                    }
                }

                var result = await FBDataRead(token);
                if (res == "failed" || res == "canceled")
                {
                    return res;
                }
            }

            // VC
            res = await CommMFCAsyncType1("VC", token);
            if (res == "failed" || res == "canceled")
            {
                return res;
            }

            // CA
            res = await CommMFCAsyncType1("CA", token);
            if (res == "failed" || res == "canceled")
            {
                return res;
            }

            vmService.CanTransit = true;

            return "";
        }

        /// <summary>
        /// オーバーシュート実行
        /// </summary>
        /// <returns></returns>
        private async Task<string> DoOverShoot(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            // CD送信
            var res = await CommMFCAsyncType1("CD", token);
            if(res == "failed" || res == "canceled")
            {
                return res;
            }

            // CD送信
            res = await CommMFCAsyncType1("VS", token);
            if (res == "failed" || res == "canceled")
            {
                return res;
            }

            // OSさせる
            res = await CommMFCAsyncType3("SW", "11000", token);
            if (res == "failed" || res == "canceled")
            {
                return res;
            }

            return "success";
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
        private async Task<string> WaitForDispose(int waitTime, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var waitOS = new HighPrecisionDelay();
                var res = await waitOS.WaitAsync(waitTime, token);
                if(res == "canceled")
                {
                    return res;
                }

                return "";
            }
            catch(OperationCanceledException)
            {
                return "canceled";
            }
        }

        /// <summary>
        /// Reading値、Vout値、初期VO値の算出タスクを同時発火、計算結果を格納する
        /// 通信処理はセマフォで直列処理としている
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private async Task<string> CalcAsync(int index, CancellationToken token)
        {
            var readingTask = CalculateReadingValue(index, token);
            var voutTask = GetAndCalVOUT(index, token);
            var voTask = GetAndCalInitialVO(index, token);

            var reading = await readingTask;
            var vout = await voutTask;
            var vo = await voTask;

            if (reading == "failed" || vout == "failed" || vo == "failed")
            {
                return "failed";
            }

            //await Task.WhenAll(readingTask, voutTask, voTask);

            // UI更新
            UpdateUI_WithCacl(index, readingTask.Result, voutTask.Result, voTask.Result);
            return "";
        }

        /// <summary>
        /// C_Data(補正後のReading値)、VOUT更新、VO(補正後)の算出タスクを同時発火、計算結果を格納する
        /// 通信処理はセマフォで直列処理としている
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private async Task<string> ConfAsync(int index, CancellationToken token)
        {
            var readingTask = CalculateReadingValue(index, token);
            var voutTask = GetAndCalVOUT(index, token);
            var voTask = GetAndCalInitialVO(index, token);

            var reading = await readingTask;
            var vout = await voutTask;
            var vo = await voTask;

            if (reading == "failed" || vout == "failed" || vo == "failed")
            {
                return "failed";
            }

            //await Task.WhenAll(readingTask, voutTask, voTask);

            // UI更新
            UpdateUI_WithConf(index, readingTask.Result, voutTask.Result, voTask.Result);
            return "";
        }

        /// <summary>
        /// Reading値を計算して、該当グリッドに格納する
        /// </summary>
        /// <param name="index"></param>
        private Task<string> CalculateReadingValue(int index, CancellationToken token)
        {
            // 計測回数の取得
            var attemptNum = int.Parse(attemptsValue);

            // 計測結果リストから最初と最後の項目を除いたリスト
            var target = Column1.Skip(1).Take(attemptNum - 1).ToList();

            // 測定結果の内のmax値およびインデクスを求める。最後の計測結果は含まない。
            var max = target.Select((v, i) => (v, i)).MaxBy(x => float.Parse(x.v.Value));
            var maxNum = float.Parse(max.v.Value);
            var maxNumIndex = max.i;

            // 測定結果の内のmin値およびインデクスを求める。最後の計測結果は含まない。
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

            return Task<string>.FromResult(readingVal.ToString("F1"));
        }

        // 通信処理用のセマフォ　GetAndCalVOUTとGetAndCalInitialVOで共有するのでここに記述
        private static readonly SemaphoreSlim _commLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// インターバル毎の計測が完了してからのVOUT計算
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private async Task<string> GetAndCalVOUT(int index, CancellationToken token)
        {
            await _commLock.WaitAsync();

            string res = "";

            try
            {
                var result = await CommMFCAsyncType2("OR", token);
                if (res == "failed" || res == "canceled")
                {
                    return res;
                }

                result = result.Substring(3, result.Length - 3);
                int val = int.Parse(result);
                int rest = (int)(val / 2.0 + (val >= 0 ? 0.5 : -0.5));
                res = rest.ToString();                

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
        private async Task<string> GetAndCalInitialVO(int index, CancellationToken token)
        {
            await _commLock.WaitAsync();

            try
            {
                token.ThrowIfCancellationRequested();

                // 上位バイト
                var res = await CommMFCAsyncType3("ER", "FE27", token);
                if (res == "failed" || res == "canceled")
                {
                    return res;
                }

                if (res.Length < 5)
                {
                    return "falied";
                }

                var upper = res.Substring(3, 2);
                var upperVal = Convert.ToInt32(upper, 16);

                // 下位バイト
                res = await CommMFCAsyncType3("ER", "FE26", token);
                if (res == "failed" || res == "canceled")
                {
                    return res;
                }

                if (res.Length < 5)
                {
                    return "failed";
                }
                var lower = res.Substring(3, 2);
                var lowerVal = Convert.ToInt32(lower, 16);

                var hex80 = Convert.ToInt32("80", 16);

                var temp = 5 * ((upperVal - hex80) * 256 + lowerVal);
                decimal tempD = (decimal)temp / ((decimal)hex80 * 256);
                decimal x = Math.Round(tempD, 3, MidpointRounding.AwayFromZero);

                return x.ToString();
            }
            catch(OperationCanceledException)
            {
                return "canceled";
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
            ReadingValueArray[index] = readingVal;
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
                "",
                "14.5567",
                "25.918",
                "53.2634",
                "67.4411",
                "111.911",
                "159.11222",
                "180.1222",
                "245.9924",
                "309.12455",
                "400.1213"
            };


        /// <summary>
        /// 10点リニア補正値の計算を行う。非可変BPの場合。
        /// </summary>
        private async Task<string> CorrectLinearData(int mode, double[]? bpValueList, CancellationToken token)
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
                string res = "";

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
                Tuple.Create("FB90", "FB91"), Tuple.Create("FB92", "FB93"),
                Tuple.Create("FB94", "FB95"), Tuple.Create("FB96", "FB97"),
                Tuple.Create("FB98", "FB99"), Tuple.Create("FB9A", "FB9B"),
                Tuple.Create("FB9C", "FB9D"), Tuple.Create("FB9E", "FB9F"),
                Tuple.Create("FBA0", "FBA1"), Tuple.Create("FBA2", "FBA3")
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
                                double temp = double.Parse(ReadingValueArray[i]) / double.Parse(TrueValueArray[i]);
                                //noMulInitialGainList.Add(temp);
                                newGain = initialGain * temp;
                            }
                            else
                            {
                                double temp = (double.Parse(ReadingValueArray[i]) - double.Parse(ReadingValueArray[i - 1])) /
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
                            if (res == "failed" || res == "canceled")
                            {
                                return res;
                            }

                            var command2 = Tuple.Create(InitPairList[i - 1].Item1, hexLowerGain);
                            res = await CommMFCAsyncTypeRW("EW", command2, token);
                            if (res == "failed" || res == "canceled")
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
                    if (res == "failed" || res == "canceled")
                    {
                        return res;
                    }

                    var command10_2 = Tuple.Create(InitPairList[InitPairList.Count() - 1].Item1, hexLowerGain);
                    res = await CommMFCAsyncTypeRW("EW", command10_2, token);
                    if (res == "failed" || res == "canceled")
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
                        if (res == "failed" || res == "canceled")
                        {
                            return res;
                        }

                        var command2 = Tuple.Create(InitPairList[i - 1].Item1, hexLowerGain);
                        res = await CommMFCAsyncTypeRW("EW", command2, token);
                        if (res == "failed" || res == "canceled")
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
                    if (res == "failed" || res == "canceled")
                    {
                        return res;
                    }

                    if (res.Length < 5)
                    {
                        return "failed";
                    }

                    var temp = res.Substring(3, 2);
                    if (temp != bytes.Item2)
                    {
                        return "failed";
                    }

                    // 下位確認
                    res = await CommMFCAsyncType3("ER", pair.Item1, token);
                    if (res == "failed" || res == "canceled")
                    {
                        return res;
                    }

                    if (res.Length < 5)
                    {
                        return "failed";
                    }

                    temp = res.Substring(3, 2);
                    if (temp != bytes.Item1)
                    {
                        return "failed";
                    }

                    index++;
                }

                // リセット実行
                res = await CommMFCAsyncType1("RE", token);
                if (res == "failed" || res == "canceled")
                {
                    return res;
                }

                await Task.Delay(100);
                isCalculated = true;
                return "";
            }
            catch (OperationCanceledException)
            {
                return "canceled";
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

                string res = "";

                // Reading値を格納　要素数はr11[10]
                var readingList = ReadingValueArray; // test;

                // 次に5次多項式の係数計算のための数学アルゴリズム検証
                double[] xData =
                {
                0.1d,// 0.01d, 0.001d, 0.0001d, 0.00001d,
                0.2d,// 0.04d, 0.008d, 0.0016d, 0.00032d,
                0.3d,// 0.09d, 0.027d, 0.0081d, 0.00243d,
                0.4d,// 0.16d, 0.064d, 0.0256d, 0.01024d,
                0.5d,// 0.25d, 0.125d, 0.0625d, 0.03125d,
                0.6d,// 0.36d, 0.216d, 0.1296d, 0.07776d,
                0.7d,// 0.49d, 0.343d, 0.2401d, 0.16807d,
                0.8d,// 0.64d, 0.512d, 0.4096d, 0.32768d,
                0.9d,// 0.81d, 0.729d, 0.6561d, 0.59049d,
                1d//,1d,1d,1d,1d
            };

                double fullScaleOutput = double.Parse(readingList[10]);

                double[] yData =
                {
                double.Parse(readingList[1])  / fullScaleOutput * 100d,
                double.Parse(readingList[2])  / fullScaleOutput * 100d,
                double.Parse(readingList[3])  / fullScaleOutput * 100d,
                double.Parse(readingList[4])  / fullScaleOutput * 100d,
                double.Parse(readingList[5])  / fullScaleOutput * 100d,
                double.Parse(readingList[6])  / fullScaleOutput * 100d,
                double.Parse(readingList[7])  / fullScaleOutput * 100d,
                double.Parse(readingList[8])  / fullScaleOutput * 100d,
                double.Parse(readingList[9])  / fullScaleOutput * 100d,
                double.Parse(readingList[10]) / fullScaleOutput * 100d
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
                Tuple.Create("FBA4", "FBA5"), Tuple.Create("FBA6", "FBA7"),
                Tuple.Create("FBA8", "FBA9"), Tuple.Create("FBAA", "FBAB"),
                Tuple.Create("FBAC", "FBAD"), Tuple.Create("FBAE", "FBAF"),
                Tuple.Create("FBB0", "FBB1"), Tuple.Create("FBB2", "FBB3"),
                Tuple.Create("FBB4", "FBB5")
            };

                // 該当アドレスに書き込む。最後の要素(B4/B5)は除く。
                for (int i = 0; i < cmdPairList.Count() - 1; i++)
                {
                    var command1 = Tuple.Create(cmdPairList[i].Item2, bpValueList[i].Item2);
                    res = await CommMFCAsyncTypeRW("EW", command1, token);
                    if (res == "failed")
                    {
                        return null;
                    }
                    else if (res == "canceled")
                    {
                        double[] canceledArray = new double[1];
                        canceledArray[0] = double.MaxValue;
                        return canceledArray;
                    }

                    var command2 = Tuple.Create(cmdPairList[i].Item1, bpValueList[i].Item1);
                    res = await CommMFCAsyncTypeRW("EW", command2, token);
                    if (res == "failed")
                    {
                        return null;
                    }
                    else if (res == "canceled")
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
                    if (res == "failed")
                    {
                        return null;
                    }
                    else if (res == "canceled")
                    {
                        double[] canceledArray = new double[1];
                        canceledArray[0] = double.MaxValue;
                        return canceledArray;
                    }

                    if (result.Length < 5)
                    {
                        return null;
                    }

                    result = await CommMFCAsyncType3("ER", cmdPair.Item2, token);
                    if (res == "failed")
                    {
                        return null;
                    }
                    else if (res == "canceled")
                    {
                        double[] canceledArray = new double[1];
                        canceledArray[0] = double.MaxValue;
                        return canceledArray;
                    }

                    if (result.Length < 5)
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
