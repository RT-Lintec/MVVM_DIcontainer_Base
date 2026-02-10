using MVVM_Base.Common;
using System.Buffers;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Windows.Shapes;

namespace MVVM_Base.Model
{
    public class BalanceSerialService : IBalanceSerialService
    {
        private static readonly Lazy<BalanceSerialService> _instance = new(() => new BalanceSerialService());
        public static BalanceSerialService Instance => _instance.Value;

        public SerialPort? Port { get; private set; }

        private readonly StringBuilder _buffer = new();
        private TaskCompletionSource<OperationResult?>? lineTcs;

        int timeoutMilliseconds = 1000;
        /// <summary>
        /// ポート接続オープン
        /// </summary>
        /// <param name="serialPortInfo"></param>
        public async Task<bool> Connect(SerialPortInfo serialPortInfo, CancellationToken token)
        {
            try
            {
                // 既存ポートの後処理
                if (Port != null)
                {
                    Port.DataReceived -= Port_DataReceived;
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
                    ReadTimeout = timeoutMilliseconds,
                    WriteTimeout = timeoutMilliseconds,
                    NewLine = "\r\n"
                };

                Port.DataReceived += Port_DataReceived;

                Port.Open();

                token.ThrowIfCancellationRequested();

                // 通信テスト                
                var result = await RequestWeightAsync(token);

                if (result != null && result != null && result.Status == OperationResultType.Success)
                {
                    return Port != null && Port.IsOpen;
                }
                else
                {
                    Disconnect();
                    return false;
                }
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
        /// ポート接続クローズ
        /// </summary>
        public void Disconnect()
        {
            if (Port == null) return;
            
            Port.DataReceived -= Port_DataReceived;

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
        /// シリアルポートにデータ受信時のイベント処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // 外部から Port が null にされても、ここから先で null にならないようにする
            var port = Port;
            if (port == null) return;

            try
            {
                // 受信バッファ内の全データを取得（port 使用）
                string data = port.ReadExisting();
                _buffer.Append(data);

                // バッファに返信メッセージが格納されてからOperationResultを
                // 作成し、payload込みでlineTcsに渡す
                while (true)
                {                    
                    int idx = _buffer.ToString().IndexOf(port.NewLine);
                    if (idx < 0) break;

                    string payload = _buffer.ToString(0, idx);
                    _buffer.Remove(0, idx + port.NewLine.Length);

                    var result = OperationResult.Success(payload);

                    // 待機中のタスクがあれば結果を渡す
                    if (lineTcs != null && !lineTcs.Task.IsCompleted)
                    {
                        lineTcs.TrySetResult(result);
                        lineTcs = null;
                    }
                }
            }
            catch (TimeoutException ex)
            {
                lineTcs?.TrySetResult(null);
                lineTcs = null;
            }
            catch (InvalidOperationException ex)
            {
                lineTcs?.TrySetResult(null);
                lineTcs = null;
            }
            catch (IOException ex)
            {
                lineTcs?.TrySetResult(null);
                lineTcs = null;
            }
            catch (UnauthorizedAccessException ex)
            {
                lineTcs?.TrySetResult(null);
                lineTcs = null;
            }
            catch (Exception ex)
            {
                lineTcs?.TrySetResult(null);
                lineTcs = null;
            }
        }

        /// <summary>
        /// 一回分の通信データを受け取るまで待機するタスクを生成
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private Task<OperationResult?> ReadLineAsync(CancellationToken token)
        {
            if (Port == null || !Port.IsOpen)
                return Task.FromResult<OperationResult?>(null);

            var tcs = new TaskCompletionSource<OperationResult?>(TaskCreationOptions.RunContinuationsAsynchronously);

            // token でキャンセル
            CancellationTokenRegistration? registration = null;
            if (token.CanBeCanceled)
            {
                registration = token.Register(() =>
                {
                    tcs.TrySetCanceled();
                });
            }

            // ここでデータ受信側にtcsを渡す
            lineTcs = tcs;

            // tcs 完了後に登録解除
            tcs.Task.ContinueWith(_ => registration?.Dispose(), TaskScheduler.Default);

            return tcs.Task;
        }

        /// <summary>
        /// リクエスト送信
        /// </summary>
        /// <param name="text"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private OperationResult WriteLine(string text)
        {
            if (Port == null)
            {
                return OperationResult.Failed();
            }

            if (!Port.IsOpen)
            {
                return OperationResult.Failed();
            }
            try
            {
                // 直前の通信でのバッファクリア
                Port.DiscardInBuffer();
                Port.DiscardOutBuffer();

                Port.WriteLine(text);
                return OperationResult.Success();
            }
            catch (NullReferenceException)
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
                return OperationResult.Failed($"Operation canceled: {ex.Message}");
            }
            catch (Exception ex)
            {
                return OperationResult.Failed($"Unexpected error: {ex.GetType().Name} {ex.Message}");
            }
        }

        /// <summary>
        /// 天秤との通信リクエストの窓口（タイムアウト対応版）
        /// </summary>
        public async Task<OperationResult> RequestWeightAsync(CancellationToken token)
        {
            if (Port == null || !Port.IsOpen)
            {
                return OperationResult.Failed();
            }

            _buffer.Clear(); // 古いデータを消去

            var result = WriteLine("Q");

            if (result.Status != OperationResultType.Success)
            {
                return result;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(timeoutMilliseconds); // タイムアウト設定

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // タイムアウト対応のReadLineAsync
                    var line = await ReadLineAsync(cts.Token);
                    if (line != null && (line.Payload.StartsWith("ST") || line.Payload.StartsWith("US")))
                    {
                        return OperationResult.Success(line.Payload);
                    }                     
                }
            }
            catch (TaskCanceledException)
            {
                // タイムアウトもしくは外部キャンセル
                return OperationResult.Canceled();
            }

            return OperationResult.Failed();
        }
    }
}
