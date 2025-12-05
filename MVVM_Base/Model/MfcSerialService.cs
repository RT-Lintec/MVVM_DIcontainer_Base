using System;
using System.IO;
using System.IO.Ports;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MVVM_Base.Model
{
    public class MfcSerialService : IMFCSerialService
    {
        // Singleton
        private static readonly Lazy<MfcSerialService> instance = new(() => new MfcSerialService());
        public static MfcSerialService Instance => instance.Value;

        private string? deviceNum;

        public SerialPort? Port { get; private set; }

        private MfcSerialService() {}

        private int timeout = 1000;

        public async Task<bool> Connect(SerialPortInfo serialPortInfo)
        {
            try
            {
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

                // 兼通信テスト
                deviceNum = await RequestDeviceNumber();

                if (deviceNum == "")
                {
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


        public void Disconnect()
        {
            if (Port == null) return;

            var portToClose = Port;
            Port = null; // アプリ側は即 null にして安全

            Task.Run(() =>
            {
                try
                {
                    if (portToClose.IsOpen)
                    {
                        // Close がブロックされても、別スレッドなら UI は止まらない
                        portToClose.Close();
                    }
                }
                catch { }

                try { portToClose.Dispose(); } catch { }
            });
        }

        /// <summary>
        /// レスポンス受信
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<OperationResult> ReadLineAsync(CancellationToken token = default)
        {
            if (Port == null)
            {
                return OperationResult.Fail("port is null");
            }

            if (!Port.IsOpen)
            {
                return OperationResult.Fail("port is not opened");
            }

            return await Task.Run(() =>
            {
                try
                {
                    var line = Port.ReadLine();
                    return OperationResult.Success(line);
                }
                catch (TimeoutException)
                {
                    return OperationResult.Fail("Communicate with MFC is timeout");
                }
                catch (IOException ex)
                {
                    return OperationResult.Fail($"IO Exception: {ex.Message}");
                }
                catch (InvalidOperationException ex)
                {
                    return OperationResult.Fail($"Invalid Operation: {ex.Message}");
                }
                catch (OperationCanceledException ex)
                {
                    return OperationResult.Fail($"Operation canceled: {ex.Message}");
                }
                catch (Exception ex)
                {
                    return OperationResult.Fail($"Unexpected error: {ex.GetType().Name} {ex.Message}");
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
                return OperationResult.Fail("Port is null.");
            }

            if (!Port.IsOpen)
            {
                return OperationResult.Fail("Port is not open.");
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
                return OperationResult.Fail("Port is NULL.");
            }
            catch (TimeoutException)
            {
                return OperationResult.Fail("Write operation timed out.");
            }
            catch (IOException ex)
            {
                return OperationResult.Fail($"IO Exception: {ex.Message}");
            }
            catch (InvalidOperationException ex)
            {
                return OperationResult.Fail($"Invalid Operation: {ex.Message}");
            }
            catch (OperationCanceledException ex)
            {
                return OperationResult.Fail($"Operation canceled: {ex.Message}");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail($"Unexpected error: {ex.GetType().Name} {ex.Message}");
            }
        }

        public async Task<string> RequestDeviceNumber()
        {
            var result = WriteLine($"AL,DR");
            result = await ReadLineAsync();
            if(result.IsSuccess)
            {
                return result.Message.Substring(0, 2) ?? "";
            }
            else
            {
                return "";
            }                
        }

        /// <summary>Type1: 返信なし</summary>
        public Task<OperationResult?> SendType1Async(string cmd, CancellationToken token = default)
        {
            var result = WriteLine($"{deviceNum},{cmd}");

            return Task.FromResult<OperationResult?>(OperationResult.Success());
        }

        /// <summary>Type2: 返信あり</summary>
        public async Task<OperationResult?> SendType2Async(string cmd, CancellationToken token = default)
        {
            var result = WriteLine($"{deviceNum},{cmd}");
            return await ReadLineAsync();
        }

        /// <summary>Type3: 二段階通信</summary>
        public async Task<OperationResult?> SendType3Async(string cmd1, string cmd2, CancellationToken token = default)
        {
            // 1回目送信
            WriteLine($"{deviceNum},{cmd1}");

            // 機器から "デバイス番号, AK\r\n" が返るか確認
            var first = await ReadLineAsync();

            if (first.IsSuccess)
            {
                if (first?.Message.Trim() != $"{deviceNum},AK") return null;

                // 2回目送信
                var result = WriteLine($"{deviceNum},{cmd2}");

                if (result.IsSuccess)
                {
                    // 2回目の返信受信
                    var second = await ReadLineAsync();

                    if (second.IsSuccess)
                    {
                        if (cmd1 == "DW")
                        {
                            deviceNum = second?.Message.Substring(0, 2);
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

        /// <summary>MFC通信タイプ1</summary>
        public async Task<OperationResult?> RequestType1Async(string cmd, CancellationToken cancellationToken = default)
        {
            if (deviceNum == null ||
                deviceNum.Length != 2 ||
                !char.IsDigit(deviceNum[0]) ||
                !char.IsDigit(deviceNum[1]))
            {
                deviceNum = await RequestDeviceNumber();
            }

            return await SendType1Async(cmd, cancellationToken);
        }

        /// <summary>MFC通信タイプ2</summary>
        public async Task<OperationResult?> RequestType2Async(string cmd, CancellationToken cancellationToken = default)
        {
            if (deviceNum == null ||
                deviceNum.Length != 2 ||
                !char.IsDigit(deviceNum[0]) ||
                !char.IsDigit(deviceNum[1]))
            {
                deviceNum = await RequestDeviceNumber();
            }

            return await SendType2Async(cmd, cancellationToken);
        }

        /// <summary>MFC通信タイプ3</summary>
        public async Task<OperationResult?> RequestType3Async(string cmd1, string cmd2, CancellationToken cancellationToken = default)
        {
            if (deviceNum == null ||
                deviceNum.Length != 2 ||
                !char.IsDigit(deviceNum[0]) ||
                !char.IsDigit(deviceNum[1]))
            {
                deviceNum = await RequestDeviceNumber();
            }

            return await SendType3Async(cmd1, cmd2, cancellationToken);
        }

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
