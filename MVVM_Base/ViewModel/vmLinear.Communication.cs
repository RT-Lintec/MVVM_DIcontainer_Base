using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Model;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;

namespace MVVM_Base.ViewModel
{
    public partial class vmLinear : ObservableObject, IViewModel
    {
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
            IsDarkTheme = newTheme == "Dark"; // フラグ例
                                              // 必要であれば PropertyChanged 通知も出す
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

        #region MFC通信処理
        /// <summary>
        /// MFCコマンドタイプ1(W)　書き込みのみ
        /// </summary>
        /// <returns></returns>
        private async Task<string> CommMFCAsyncType1(string command, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (mfcService.Port == null || !mfcService.Port.IsOpen)
                {
                    await messageService.ShowMessage("MFC Port isn't opened");
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                    return "failed";
                }

                var result = await mfcService.RequestType1Async(command, token);
                if (result != null)
                {
                    if (result.IsSuccess)
                    {
                        return result.Message;
                    }
                    else
                    {
                        if (result.Message == "canceled")
                        {
                            return "canceled";
                        }
                        else
                        {
                            await messageService.ShowMessage($"Failed to {command} command");
                            await Task.Delay(messageFadeTime);
                            await messageService.CloseWithFade();
                            return "failed";
                        }
                    }
                }

                return "";
            }
            catch (OperationCanceledException)
            {
                return "canceled";
            }
        }

        /// <summary>
        /// MFCコマンドタイプ1(W)　書き込みのみ
        /// </summary>
        /// <returns></returns>
        private async Task<string> CommMFCAsyncType2(string command, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (mfcService.Port == null || !mfcService.Port.IsOpen)
                {
                    await messageService.ShowMessage("MFC Port isn't opened");
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                    return "failed";
                }

                var result = await mfcService.RequestType2Async(command, token);
                if (result != null)
                {
                    if (result.IsSuccess)
                    {
                        return result.Message;
                    }
                    else
                    {
                        if (result.Message == "canceled")
                        {
                            return "canceled";
                        }
                        else
                        {
                            await messageService.ShowMessage($"Failed to {command} command");
                            await Task.Delay(messageFadeTime);
                            await messageService.CloseWithFade();
                            return "failed";
                        }
                    }
                }

                return "";
            }
            catch (OperationCanceledException)
            {
                return "canceled";
            }
        }

        /// <summary>
        /// MFCコマンドタイプ3(W)　AK返信あり、エコーあり
        /// </summary>
        /// <returns></returns>
        private async Task<string> CommMFCAsyncType3(string command1, string command2, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (mfcService.Port == null || !mfcService.Port.IsOpen)
                {
                    await messageService.ShowMessage("MFC Port isn't opened");
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                    return "failed";
                }

                var result = await mfcService.RequestType3Async(command1, command2, token);
                if (result != null)
                {
                    if (result.IsSuccess)
                    {
                        return result.Message;
                    }
                    else
                    {
                        if (result.Message == "canceled")
                        {
                            return "canceled";
                        }
                        else
                        {
                            await messageService.ShowMessage($"Failed to {command1},{command2} command");
                            await Task.Delay(messageFadeTime);
                            await messageService.CloseWithFade();
                            return "failed";
                        }
                    }
                }

                return "";
            }
            catch (OperationCanceledException)
            {
                return "canceled";
            }
        }

        /// <summary>
        /// MFCコマンドタイプ3(W)　AK返信あり、エコーあり
        /// </summary>
        /// <returns></returns>
        private async Task<string> CommMFCAsyncTypeRW(string command, Tuple<string, string> cmdPair, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (mfcService.Port == null || !mfcService.Port.IsOpen)
                {
                    await messageService.ShowMessage("MFC Port isn't opened");
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                    return "failed";
                }

                var result = await mfcService.RequestReadWriteAsync(command, cmdPair, token);
                if (result != null)
                {
                    if (result.IsSuccess)
                    {
                        return result.Message;
                    }
                    else
                    {
                        if (result.Message == "canceled")
                        {
                            return "canceled";
                        }
                        else
                        {
                            await messageService.ShowMessage($"Failed to {command} command");
                            await Task.Delay(messageFadeTime);
                            await messageService.CloseWithFade();
                            return "failed";
                        }
                    }
                }

                return "";
            }
            catch (OperationCanceledException)
            {
                return "canceled";
            }
        }

        /// <summary>
        /// シリアル番号の取得
        /// </summary>
        /// <returns></returns>
        private async Task<string> ReadSerialNumber(CancellationToken token)
        {
            string result = "";
            var sb = new StringBuilder();

            try
            {
                token.ThrowIfCancellationRequested();

                // シリアルナンバー読み出し
                List<string> commandList = new List<string>() { "FC00", "FC01", "FC02", "FC03", "FC04", "FC05", "FC06", "FC07" };
                if (IsMfcConnected)
                {
                    string temp = "";

                    foreach (var command in commandList)
                    {
                        temp = await CommMFCAsyncType3("ER", command, token);
                        if (temp == "faild" || temp =="canceled")
                        {
                            return temp;
                        }
                        temp = temp.Substring(3, 2);

                        sb.Append((char)Convert.ToInt32(temp, 16));
                    }
                }

                result = sb.ToString();
                return result;
            }
            catch (TimeoutException)
            {
                return result;
            }
            catch (IOException ex)
            {
                return result;
            }
            catch (InvalidOperationException ex)
            {
                return result;
            }
            catch (OperationCanceledException ex)
            {
                return result;
            }
            catch (Exception ex)
            {
                return result;
            }
        }

        /// <summary>
        /// FbCode属性プロパティをリスト化してERコマンドの結果を格納する
        /// viewロード時にviewが呼び出して値を初期化する
        /// </summary>
        /// <returns></returns>
        private async Task<string> FBDataRead(CancellationToken token)
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

                foreach (var p in props)
                {
                    token.ThrowIfCancellationRequested();

                    string code = p.Attr.Code;

                    // 通信実行
                    string resp = await CommMFCAsyncType3("ER", code, token);
                    if (resp == "failed" || resp == "canceled")
                    {
                        return resp;
                    }

                    // "ERxxFF" のような返り値を想定
                    string hex = resp.Substring(3, 2);

                    // VM のプロパティへセット
                    p.Property.SetValue(this, hex);
                }

                return "";
            }
            catch (TimeoutException)
            {
                return "";
            }
            catch (IOException ex)
            {
                return "";
            }
            catch (InvalidOperationException ex)
            {
                return "";
            }
            catch (OperationCanceledException ex)
            {
                return "canceled";
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        #endregion
        List<string> debugList = new List<string>();
        #region 天秤通信処理
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
                    await messageService.ShowMessage("Balance Port isn't opened");
                    await Task.Delay(messageFadeTime);
                    await messageService.CloseWithFade();
                    return "failed";
                }

                var result = await BalanceSerialService.Instance.RequestWeightAsync(token);
                if (result != null)
                {
                    if (result.IsSuccess)
                    {
                        debugList.Add(DateTime.UtcNow + "  :  " + result.Message);
                        return result.Message;
                    }
                    else
                    {
                        await messageService.ShowMessage("Failed to communicate with Balance");
                        await Task.Delay(messageFadeTime);
                        await messageService.CloseWithFade();
                        return "failed";
                    }
                }

                return "";
            }
            catch(OperationCanceledException)
            {
                return "canceled";
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
                if (result == "failed")
                {
                    //canTransitOther = true;
                    return "failed";
                }
                var val = ConvertBalanceResToMS(result);
                if (result != null && result != "")
                {
                    //計測結果の表に結果を格納
                    if (index < 11)
                    {
                        token.ThrowIfCancellationRequested();

                        var intervalUTC = currentUTC - lastUTC;

                        // 取得した値を分単位のmg変化量に変換して格納
                        Column1[index].Value = (60 / intervalUTC.TotalSeconds * (val - lastBalanceVal)).ToString("F3");
                        MarkUpdatedTemporarily(index, int.Parse(intervalValue) * 1000);
                        balNumList[index] = val;

                        // 前回の値を保持
                        lastBalanceVal = val;
                        lastUTC = DateTime.UtcNow;

                        // 計測時間の格納
                        dateList[index] = currentUTC;

                        // 通信回数を数え、5回以上でg(n+5) - gnを計算する
                        //cntBalCom++;

                        // g(n+5) - g(n)を計算
                        if (cntBalCom >= 5)
                        {
                            if (cntBalCom < 12)
                            {
                                if (index >= 5)
                                {
                                    Column1[0].Value = (60 / (dateList[index] - dateList[index - 5]).TotalSeconds * (balNumList[index] - balNumList[index - 5])).ToString("F3");
                                }
                                else
                                {
                                    Column1[0].Value = (60 / (dateList[index] - dateList[index + 5]).TotalSeconds * (balNumList[index] - balNumList[index + 5])).ToString("F3");
                                }
                            }
                            else
                            {
                                if (index > 5)
                                {
                                    Column1[0].Value = (60 / (dateList[index] - dateList[index - 5]).TotalSeconds * (balNumList[index] - balNumList[index - 5])).ToString("F3");
                                }
                                else
                                {
                                    Column1[0].Value = (60 / (dateList[index] - dateList[index + 5]).TotalSeconds * (balNumList[index] - balNumList[index + 5])).ToString("F3");
                                }
                            }

                            //Column1[0].Value = measureResultAverage.Value;
                        }

                        return result;
                    }
                }

                return "";
            }
            catch (OperationCanceledException)
            {
                return "canceled";
            }
        }

        /// <summary>
        /// Calculate, Conformでの定間隔計測にて前回計測値比較して結果を格納
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private async Task<string> Gn1GnComm(int index, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var currentUTC = DateTime.UtcNow;
                var result = await CommBalanceAsyncCommand(token);
                if (result == "failed")
                {
                    //canTransitOther = true;
                    return "failed";
                }
                var val = ConvertBalanceResToMS(result);
                if (result != null && result != "")
                {
                    //計測結果の表に結果を格納
                    if (index < 11)
                    {
                        token.ThrowIfCancellationRequested();

                        var intervalUTC = currentUTC - lastUTC;

                        // 測定値をindexの指す位置に格納
                        Column1[index].Value = val.ToString();
                        balNumList[index] = val;

                        // 前回の値を保持
                        lastBalanceVal = val;
                        lastUTC = DateTime.UtcNow;

                        // 計測時間の格納
                        dateList[index] = currentUTC;

                        // 値の格納
                        Column1[index - 1].Value = (60 / (dateList[index] - dateList[index - 1]).TotalSeconds * (float.Parse(Column1[index].Value) - float.Parse(Column1[index - 1].Value))).ToString("F3");

                        return result;
                    }
                }

                return "";
            }
            catch (OperationCanceledException)
            {
                return "canceled";
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
            Column1[index].IsUpdate = false;

            // 次のフレームで開始
            await Task.Yield();

            Column1[index].IsUpdate = true;

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
                Column1[index].IsUpdate = false;
            }
        }

        #endregion
    }
}
