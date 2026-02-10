using Microsoft.VisualBasic.Logging;
using System;
using MVVM_Base.Common;
using System.IO;
using System.IO.Ports;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MVVM_Base.Model
{
    public class MfcSerialService : IMFCSerialService
    {
        /// <summary>
        /// Singleton
        /// </summary>
        private static readonly Lazy<MfcSerialService> instance = new(() => new MfcSerialService());

        /// <summary>
        /// シングルトンオブジェクトのアクセスプロパティ
        /// </summary>
        public static MfcSerialService Instance => instance.Value;

        private string? deviceNum;

        /// <summary>
        /// ポートオブジェクト
        /// </summary>
        public SerialPort? Port { get; private set; }

        private MfcSerialService() {}

        private int timeout = 100;

        List<int> BaudRates = new List<int>() { 4800, 9600, 19200 };

        public async Task<bool> Connect(SerialPortInfo serialPortInfo, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (Port != null)
                {
                    if (Port.IsOpen) Port.Close();
                }

                var parity = serialPortInfo.Paritybit switch
                {
                    "None" => Parity.None,
                    "Odd" => Parity.Odd,
                    "Even" => Parity.Even,
                    _ => Parity.None
                };

                var stopBits = serialPortInfo.Stopbit switch
                {
                    1 => StopBits.One,
                    2 => StopBits.Two,
                    _ => StopBits.One
                };

                Port = new SerialPort
                {
                    PortName = serialPortInfo.PortName,
                    BaudRate = serialPortInfo.Baudrate,
                    Parity = parity,
                    DataBits = serialPortInfo.Databit,
                    StopBits = stopBits,
                    ReadTimeout = timeout,
                    WriteTimeout = timeout,
                    NewLine = "\r\n"
                };

                Port.Open();

                Port.DiscardInBuffer();
                Port.DiscardOutBuffer();

                token.ThrowIfCancellationRequested();

                // 兼通信テスト
                var result = await RequestDeviceNumber(token);
                deviceNum = result.Payload;

                token.ThrowIfCancellationRequested();

                if (deviceNum == "")
                {
                    // ボーレートを変えて通信することで復帰できるケース
                    // ファームウェア側の処理にバグがある可能性大
                    // 通信ハング時にポートリセットしているかどうか確認
                    foreach(var baudrate in BaudRates)
                    {
                        Port.DiscardInBuffer();
                        Port.DiscardOutBuffer();
                        Port.BaudRate = baudrate;

                        token.ThrowIfCancellationRequested();

                        result = await RequestDeviceNumber(token);
                        deviceNum = result.Payload;

                        if (deviceNum != "")
                        {
                            return Port != null && Port.IsOpen;
                        }
                    }

                    // ごくまれにこちらのケースも発生する
                    if(deviceNum == "")
                    {
                        foreach (int value in Enumerable.Range(0, 64)) // 0～63
                        {
                            token.ThrowIfCancellationRequested();

                            var num = await RequestType3Async(value.ToString(), "DR", token);
                            if(num.Status == OperationResultType.Success)
                            {
                                deviceNum = num.Payload;
                                return Port != null && Port.IsOpen;
                            }
                        }
                    }

                    Disconnect();
                    return false;
                }

                return Port != null && Port.IsOpen;
            }
            catch (NullReferenceException)
            {
                return false;
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (IOException ex)
            {
                return false;
            }
            catch (InvalidOperationException ex)
            {
                return false;
            }
            catch (OperationCanceledException ex)
            {
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 接続解除。同期処理。
        /// </summary>
        public void Disconnect()
        {
            if (Port == null) return;

            var portToClose = Port;
            Port = null; // アプリ側は即 null にして安全

            try
            {
                if (portToClose.IsOpen)
                {
                    var closeTask = Task.Run(() =>
                    {
                        try
                        {
                            portToClose.Close();
                        }
                        catch (NullReferenceException)
                        {
                        }
                        catch (TimeoutException)
                        {

                        }
                        catch (IOException ex)
                        {

                        }
                        catch (InvalidOperationException ex)
                        {

                        }
                        catch (OperationCanceledException ex)
                        {

                        }
                        catch (Exception ex)
                        {

                        }
                    });

                    if (!closeTask.Wait(500))
                    {
                        // Closeが帰ってこない
                    }
                }
            }
            catch (NullReferenceException)
            {
            }
            catch (TimeoutException)
            {

            }
            catch (IOException ex)
            {

            }
            catch (InvalidOperationException ex)
            {

            }
            catch (OperationCanceledException ex)
            {

            }
            catch (Exception ex)
            {

            }

            try 
            { 
                //portToClose.Dispose();
                var closeTask = Task.Run(() =>
                {
                    try
                    {
                        portToClose.Dispose();
                    }
                    catch (NullReferenceException)
                    {
                    }
                    catch (TimeoutException)
                    {

                    }
                    catch (IOException ex)
                    {

                    }
                    catch (InvalidOperationException ex)
                    {

                    }
                    catch (OperationCanceledException ex)
                    {

                    }
                    catch (Exception ex)
                    {

                    }
                });

                if (!closeTask.Wait(500))
                {
                    // Closeが帰ってこない
                }
            }
            catch (NullReferenceException)
            {
            }
            catch (TimeoutException)
            {

            }
            catch (IOException ex)
            {

            }
            catch (InvalidOperationException ex)
            {

            }
            catch (OperationCanceledException ex)
            {

            }
            catch (Exception ex)
            {

            }

        }

        /// <summary>
        /// レスポンス受信
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<OperationResult> ReadLineAsync(CancellationToken token)
        {
            if (Port == null)
            {
                return OperationResult.Failed("port is null");
            }

            if (!Port.IsOpen)
            {
                return OperationResult.Failed("port is not opened");
            }
            
            return await Task.Run(() =>
            {
                try
                {
                    token.ThrowIfCancellationRequested();
                    var line = Port.ReadLine();
                    return OperationResult.Success(line);
                }
                catch (TimeoutException)
                {
                    return OperationResult.Failed("Communicate with MFC is timeout");
                }
                catch (IOException ex)
                {
                    return OperationResult.Failed($"IO Exception: {ex.Message}");
                }
                catch (InvalidOperationException ex)
                {
                    return OperationResult.Failed($"Invalid Operation: {ex.Message}");
                }
                catch (OperationCanceledException ex)
                {
                    return OperationResult.Failed("canceled");
                }
                catch (Exception ex)
                {
                    return OperationResult.Failed($"Unexpected error: {ex.GetType().Name} {ex.Message}");
                }
            });
        }

        /// <summary>
        /// リクエスト送信
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private OperationResult WriteLine(string text)
        {
            if (Port == null)
            {
                return OperationResult.Failed("Port is null.");
            }

            if (!Port.IsOpen)
            {
                return OperationResult.Failed("Port is not open.");
            }
            try
            {
                // 直前の通信でのバッファクリア
                Port.DiscardInBuffer();
                Port.DiscardOutBuffer();

                Port.WriteLine(text);
                return OperationResult.Success();
            }
            catch(NullReferenceException)
            {
                return OperationResult.Failed("Port is NULL.");
            }
            catch (TimeoutException)
            {
                return OperationResult.Failed("Write operation timed out.");
            }
            catch (IOException ex)
            {
                return OperationResult.Failed($"IO Exception: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult.Failed($"Invalid Operation: {ex.Message}");
            }
            catch (OperationCanceledException ex)
            {
                return OperationResult.Failed("canceled");
            }
            catch (Exception ex)
            {
                return OperationResult.Failed($"Unexpected error: {ex.GetType().Name} {ex.Message}");
            }
        }

        public async Task<OperationResult> RequestDeviceNumber(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                var result = WriteLine($"AL,DR");
                result = await ReadLineAsync(token);
                if (result.Status == OperationResultType.Success)
                {
                    return OperationResult.Success(result.Payload.Substring(0, 2) ?? "");
                }
                else
                {
                    return OperationResult.Failed();
                }
            }
            catch(OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// Type1: 返信なし
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public Task<OperationResult?> SendType1Async(string cmd, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var result = WriteLine($"{deviceNum},{cmd}");

            return Task.FromResult<OperationResult?>(OperationResult.Success());
        }

        /// <summary>
        /// Type2: 返信あり
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<OperationResult?> SendType2Async(string cmd, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var result = WriteLine($"{deviceNum},{cmd}");
            return await ReadLineAsync(token);
        }

        /// <summary>
        /// Type3: 二段階通信
        /// </summary>
        /// <param name="cmd1"></param>
        /// <param name="cmd2"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<OperationResult?> SendType3Async(string cmd1, string cmd2, CancellationToken token)
        {
            try
            {
                // 1回目送信
                WriteLine($"{deviceNum},{cmd1}");

                token.ThrowIfCancellationRequested();

                // 機器から "デバイス番号, AK\r\n" が返るか確認
                var first = await ReadLineAsync(token);

                if (first.Status == OperationResultType.Success)
                {
                    if (first?.Payload.Trim() != $"{deviceNum},AK") return null;

                    token.ThrowIfCancellationRequested();

                    // 2回目送信
                    var result = WriteLine($"{deviceNum},{cmd2}");

                    if (result.Status == OperationResultType.Success)
                    {
                        token.ThrowIfCancellationRequested();

                        // 2回目の返信受信
                        var second = await ReadLineAsync(token);

                        if (second.Status == OperationResultType.Success)
                        {
                            if (cmd1 == "DW")
                            {
                                deviceNum = second?.Payload.Substring(0, 2);
                            }
                        }

                        return second;
                    }
                    else
                    {
                        return result;
                    }
                }
                else
                {
                    return first;
                }
            }
            catch(OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// Type3: 二段階通信
        /// </summary>
        /// <param name="cmd1"></param>
        /// <param name="cmd2"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<OperationResult?> SendTypeRWAsync(string cmd1, Tuple<string, string> cmdPair, CancellationToken token)
        {
            try
            {
                // 1回目送信
                WriteLine($"{deviceNum},{cmd1}");

                token.ThrowIfCancellationRequested();

                // 機器から "デバイス番号, AK\r\n" が返るか確認
                var first = await ReadLineAsync(token);

                if (first.Status == OperationResultType.Success)
                {
                    if (first?.Payload.Trim() != $"{deviceNum},AK") return null;

                    token.ThrowIfCancellationRequested();

                    // 2回目送信
                    var result = WriteLine($"{cmdPair.Item1},{cmdPair.Item2}");

                    if (result.Status == OperationResultType.Success)
                    {
                        token.ThrowIfCancellationRequested();

                        // 2回目の返信受信
                        var second = await ReadLineAsync(token);

                        return second;
                    }
                    else
                    {
                        return result;
                    }
                }
                else
                {
                    return first;
                }
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// MFC通信タイプ1
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult?> RequestType1Async(string cmd, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (deviceNum == null ||
                    deviceNum.Length != 2 ||
                    !char.IsDigit(deviceNum[0]) ||
                    !char.IsDigit(deviceNum[1]))
                {
                    var result = await RequestDeviceNumber(token);
                    deviceNum = result.Payload;
                }

                token.ThrowIfCancellationRequested();

                return await SendType1Async(cmd, token);
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// MFC通信タイプ2
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult?> RequestType2Async(string cmd, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (deviceNum == null ||
                    deviceNum.Length != 2 ||
                    !char.IsDigit(deviceNum[0]) ||
                    !char.IsDigit(deviceNum[1]))
                {
                    var result = await RequestDeviceNumber(token);
                    deviceNum = result.Payload;
                }

                token.ThrowIfCancellationRequested();

                return await SendType2Async(cmd, token);
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// MFC通信タイプ3
        /// </summary>
        /// <param name="cmd1"></param>
        /// <param name="cmd2"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult?> RequestType3Async(string cmd1, string cmd2, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (deviceNum == null ||
                    deviceNum.Length != 2 ||
                    !char.IsDigit(deviceNum[0]) ||
                    !char.IsDigit(deviceNum[1]))
                {
                    var result = await RequestDeviceNumber(token);
                    deviceNum = result.Payload;
                }

                token.ThrowIfCancellationRequested();

                return await SendType3Async(cmd1, cmd2, token);
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// MFC通信 EEPROM R/W専用
        /// </summary>
        /// <param name="cmd1"></param>
        /// <param name="cmd2"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<OperationResult?> RequestReadWriteAsync(string cmd1, Tuple<string, string> cmdPair, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (deviceNum == null ||
                deviceNum.Length != 2 ||
                !char.IsDigit(deviceNum[0]) ||
                !char.IsDigit(deviceNum[1]))
                {
                    var result = await RequestDeviceNumber(token);
                    deviceNum = result.Payload;
                }

                return await SendTypeRWAsync(cmd1, cmdPair, token);
            }
            catch (OperationCanceledException)
            {
                return OperationResult.Canceled();
            }
        }

        /// <summary>
        /// 保持するデバイス番号を返す
        /// </summary>
        /// <returns></returns>
        public string GetDeviceNumber()
        {
            if (deviceNum != null)
            {
                return deviceNum;
            }

            else
            {
                return string.Empty;
            }
        }
    }
}
