using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Model;
using MVVM_Base.Common;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;

namespace MVVM_Base.ViewModel
{
    public partial class vmLinear : ObservableObject, IViewModel
    {        
        #region MFC通信処理
        /// <summary>
        /// MFCコマンドタイプ1(W)　書き込みのみ
        /// </summary>
        /// <returns></returns>
        private async Task<OperationResult> CommMFCAsyncType1(string command, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (mfcService.Port == null || !mfcService.Port.IsOpen)
                {
                    await messageService.ShowMessage(languageService.MfcPortError);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                    return OperationResult.Failed();
                }

                var result = await mfcService.RequestType1Async(command, token);
                if (result != null)
                {
                    if (result.Status == OperationResultType.Success)
                    {
                        return result;
                    }
                    else
                    {
                        if (result.Status == OperationResultType.Canceled)
                        {
                            return OperationResult.Canceled();
                        }
                        else
                        {
                            await messageService.ShowMessage(languageService.MFCCommandCommError(command));
                            await Task.Delay(messageFadeTime);
                            await messageService.CloseWithFade();
                            return OperationResult.Failed();
                        }
                    }
                }

                return OperationResult.Success();
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// MFCコマンドタイプ1(W)　書き込みのみ
        /// </summary>
        /// <returns></returns>
        private async Task<OperationResult> CommMFCAsyncType2(string command, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (mfcService.Port == null || !mfcService.Port.IsOpen)
                {
                    await messageService.ShowMessage(languageService.MfcPortError);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                    return OperationResult.Failed();
                }

                var result = await mfcService.RequestType2Async(command, token);
                if (result != null)
                {
                    if (result.Status == OperationResultType.Success)
                    {
                        return result;
                    }
                    else
                    {
                        if (result.Status == OperationResultType.Canceled)
                        {
                            return OperationResult.Canceled();
                        }
                        else
                        {
                            await messageService.ShowMessage(languageService.MFCCommandCommError(command));
                            await Task.Delay(messageFadeTime);
                            await messageService.CloseWithFade();
                            return OperationResult.Failed();
                        }
                    }
                }

                return OperationResult.Success();
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// MFCコマンドタイプ3(W)　AK返信あり、エコーあり
        /// </summary>
        /// <returns></returns>
        private async Task<OperationResult> CommMFCAsyncType3(string command1, string command2, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (mfcService.Port == null || !mfcService.Port.IsOpen)
                {
                    await messageService.ShowMessage(languageService.MfcPortError);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                    return OperationResult.Failed();
                }

                var result = await mfcService.RequestType3Async(command1, command2, token);
                if (result != null)
                {
                    if (result.Status == OperationResultType.Success)
                    {
                        return result;
                    }
                    else
                    {
                        if (result.Status == OperationResultType.Canceled)
                        {
                            return OperationResult.Canceled();
                        }
                        else
                        {
                            await messageService.ShowMessage(languageService.MFCCommandCommError(command1));
                            await Task.Delay(messageFadeTime);
                            await messageService.CloseWithFade();
                            return OperationResult.Failed();
                        }
                    }
                }

                return OperationResult.Success();
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// MFCコマンドタイプ3(W)　AK返信あり、エコーあり
        /// </summary>
        /// <returns></returns>
        private async Task<OperationResult> CommMFCAsyncTypeRW(string command, Tuple<string, string> cmdPair, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (mfcService.Port == null || !mfcService.Port.IsOpen)
                {
                    await messageService.ShowMessage(languageService.MfcPortError);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                    return OperationResult.Failed();
                }

                var result = await mfcService.RequestReadWriteAsync(command, cmdPair, token);
                if (result != null)
                {
                    if (result.Status == OperationResultType.Success)
                    {
                        return result;
                    }
                    else
                    {
                        if (result.Status == OperationResultType.Canceled)
                        {
                            return OperationResult.Canceled();
                        }
                        else
                        {
                            await messageService.ShowMessage(languageService.MFCCommandCommError(command));
                            await Task.Delay(messageFadeTime);
                            await messageService.CloseWithFade();
                            return OperationResult.Failed();
                        }
                    }
                }

                return OperationResult.Success();
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// シリアル番号の取得
        /// </summary>
        /// <returns></returns>
        private async Task<OperationResult> ReadSerialNumber(CancellationToken token)
        {
            OperationResult result = new OperationResult(OperationResultType.Success, null, null);
            var sb = new StringBuilder();

            try
            {
                token.ThrowIfCancellationRequested();

                // シリアルナンバー読み出し
                List<string> commandList = new List<string>() { "FC00", "FC01", "FC02", "FC03", "FC04", "FC05", "FC06", "FC07" };
                if (IsMfcConnected)
                {
                    foreach (var command in commandList)
                    {
                        var temp = await CommMFCAsyncType3("ER", command, token);
                        if (temp.Status == OperationResultType.Failure || temp.Status == OperationResultType.Canceled)
                        {
                            return temp;
                        }

                        if (temp.Payload == null)
                        {
                            return OperationResult.Failed("ER");
                        }

                        temp.Payload = temp.Payload.Substring(3, 2);

                        sb.Append((char)Convert.ToInt32(temp.Payload, 16));
                    }
                }

                result.Payload = sb.ToString();
                return result;
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
            catch (OperationCanceledException ex)
            {
                return OperationResult.Canceled();
            }
            catch (Exception ex)
            {
                return OperationResult.Failed();
            }
        }

        /// <summary>
        /// FbCode属性プロパティをリスト化してERコマンドの結果を格納する
        /// viewロード時にviewが呼び出して値を初期化する
        /// </summary>
        /// <returns></returns>
        private async Task<OperationResult> FBDataRead(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                // 10点リニア補正値読み出し
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

                // アドレスはfbMapから取得する、値はPropertyInfoへ書き込む
                foreach (var p in props)
                {
                    token.ThrowIfCancellationRequested();

                    // アドレス
                    string code = FbMap[p.Attr.Code];

                    var resp = await CommMFCAsyncType3("ER", code, token);
                    if (resp.Status == OperationResultType.Failure || resp.Status == OperationResultType.Canceled)
                    {
                        return resp;
                    }

                    if (resp.Payload == null)
                    {
                        return OperationResult.Failed("ER");
                    }

                    // 16進数のみ想定
                    string hex = resp.Payload.Substring(3, 2);

                    // PropertyInfoへ値をセット
                    p.Property.SetValue(this, hex);
                    
                }

                return OperationResult.Success();
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
            catch (OperationCanceledException ex)
            {
                return OperationResult.Failed();
            }
            catch (Exception ex)
            {
                return OperationResult.Failed();
            }
        }
        /// <summary>
        /// ゲイン表の値をMFCに書き込む
        /// </summary>
        /// <returns></returns>
        private async Task<OperationResult> FBDataWrite(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                // 10点リニア補正値読み出し
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

                for (int i = 0; i < props.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    string value = (string)props[i].Property.GetValue(this);
                    string code = FbMap[props[i].Attr.Code];

                    var command = Tuple.Create(code, value);

                    // 該当アドレスに書き込む
                    var res = await CommMFCAsyncTypeRW("EW", command, token);
                    if (res.Status == OperationResultType.Failure || res.Status == OperationResultType.Canceled)
                    {
                        return res;
                    }
                }

                return OperationResult.Success();
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
            catch (OperationCanceledException ex)
            {
                return OperationResult.Canceled();
            }
            catch (Exception ex)
            {
                return OperationResult.Failed();
            }
        }

        #endregion
        List<string> debugList = new List<string>();
        #region 天秤通信処理
        /// <summary>
        /// 天秤との通信
        /// </summary>
        /// <returns></returns>
        private async Task<OperationResult> CommBalanceAsyncCommand(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (BalanceSerialService.Instance.Port == null || !BalanceSerialService.Instance.Port.IsOpen)
                {
                    await messageService.ShowMessage(languageService.BalancePortError);
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                    return OperationResult.Failed();
                }

                var result = await BalanceSerialService.Instance.RequestWeightAsync(token);
                if (result != null)
                {
                    if (result.Status == OperationResultType.Success)
                    {
                        return result;
                    }
                    else
                    {
                        await messageService.ShowMessage(languageService.BalanceCommError);
                        await Task.Delay(messageFadeTime);
                        await messageService.CloseWithFade();
                        return OperationResult.Failed();
                    }
                }

                return OperationResult.Success();
            }
            catch(OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// Span合わせでの5個前データ比較計算
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private async Task<OperationResult> Gn5GnComm(int index, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var currentUTC = DateTime.UtcNow;
                var result = await CommBalanceAsyncCommand(token);
                if (result.Status == OperationResultType.Failure)
                {
                    return OperationResult.Failed();
                }
                var val = ConvertBalanceResToMS(result.Payload);
                if (result != null && result.Payload != "")
                {
                    //計測結果の表に結果を格納
                    if (index < 11)
                    {
                        token.ThrowIfCancellationRequested();

                        var intervalUTC = currentUTC - lastUTC;

                        // 取得した値を分単位のmg変化量に変換して格納
                        MeasurementValues[index].Value = (60 / intervalUTC.TotalSeconds * (val - lastBalanceVal)).ToString("F3");
                        MarkUpdatedTemporarily(index, int.Parse(intervalValue) * 1000);
                        balNumList[index] = val;

                        // 前回の値を保持
                        lastBalanceVal = val;
                        lastUTC = DateTime.UtcNow;

                        // 計測時間の格納
                        dateList[index] = currentUTC;

                        // g(n+5) - g(n)を計算
                        if (cntBalCom >= 5)
                        {
                            if (cntBalCom < 12)
                            {
                                if (index >= 5)
                                {
                                    MeasurementValues[0].Value = (60 / (dateList[index] - dateList[index - 5]).TotalSeconds * (balNumList[index] - balNumList[index - 5])).ToString("F3");
                                }
                                else
                                {
                                    MeasurementValues[0].Value = (60 / (dateList[index] - dateList[index + 5]).TotalSeconds * (balNumList[index] - balNumList[index + 5])).ToString("F3");
                                }
                            }
                            else
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
                        }

                        return result;
                    }
                }

                return OperationResult.Success();
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// Calculate, Confirmでの定間隔計測にて前回計測値比較して結果を格納
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private async Task<OperationResult> Gn1GnComm(int index, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var currentUTC = DateTime.UtcNow;
                var result = await CommBalanceAsyncCommand(token);
                if (result.Status == OperationResultType.Failure)
                {
                    return OperationResult.Failed();
                }
                var val = ConvertBalanceResToMS(result.Payload);
                if (result != null && result.Payload != "")
                {
                    //計測結果の表に結果を格納
                    if (index < 11)
                    {
                        token.ThrowIfCancellationRequested();

                        var intervalUTC = currentUTC - lastUTC;

                        // 測定値をindexの指す位置に格納
                        MeasurementValues[index].Value = val.ToString();
                        balNumList[index] = val;

                        // 前回の値を保持
                        lastBalanceVal = val;
                        lastUTC = DateTime.UtcNow;

                        // 計測時間の格納
                        dateList[index] = currentUTC;

                        // 値の格納
                        MeasurementValues[index - 1].Value = (60 / (dateList[index] - dateList[index - 1]).TotalSeconds * (float.Parse(MeasurementValues[index].Value) - float.Parse(MeasurementValues[index - 1].Value))).ToString("F3");

                        return result;
                    }
                }

                return OperationResult.Success();
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
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
    }
}
