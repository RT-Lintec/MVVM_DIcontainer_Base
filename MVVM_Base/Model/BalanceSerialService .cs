using System.IO;
using System.IO.Ports;
using System.Text;

namespace MVVM_Base.Model
{
    public class BalanceSerialService : IBalanceSerialService
    {
        private static readonly Lazy<BalanceSerialService> _instance = new(() => new BalanceSerialService());
        public static BalanceSerialService Instance => _instance.Value;

        public SerialPort? Port { get; private set; }

        private readonly StringBuilder _buffer = new();
        private TaskCompletionSource<string?>? lineTcs;

        int timeoutMilliseconds = 1000;
        /// <summary>
        /// ポート接続オープン
        /// </summary>
        /// <param name="serialPortInfo"></param>
        public async Task<bool> Connect(SerialPortInfo serialPortInfo)
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

                //Port.DiscardInBuffer();
                //Port.DiscardOutBuffer();

                // 通信テスト                
                var result = await RequestWeightAsync();

                if (result != null && result != null && result.IsSuccess)
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

            //Task.Run(() =>
            //{
            try
            {
                if (portToClose.IsOpen)
                {
                    // Close がブロックされても、別スレッドなら UI は止まらない
                    portToClose.Close();
                }
            }
            catch { }

            try { portToClose.Dispose(); } catch {  }
            //});
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

                // 1行ごとに分割
                while (true)
                {
                    string? line = null;

                    // ここも必ず port を使う（Port を直接参照しない！）
                    int idx = _buffer.ToString().IndexOf(port.NewLine);
                    if (idx >= 0)
                    {
                        line = _buffer.ToString(0, idx);
                        _buffer.Remove(0, idx + port.NewLine.Length);
                    }

                    if (line == null) break;

                    // 待機中のタスクがあれば結果を渡す
                    if (lineTcs != null && !lineTcs.Task.IsCompleted)
                    {
                        lineTcs.TrySetResult(line);
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
        private Task<string?> ReadLineAsync(CancellationToken token = default)
        {
            if (Port == null || !Port.IsOpen)
                return Task.FromResult<string?>(null);

            // lineTcsを発生させ、bufferからのデータ格納を待つ
            lineTcs = new TaskCompletionSource<string?>();

            if (token != default)
                token.Register(() => lineTcs?.TrySetCanceled());

            return lineTcs.Task;
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
            catch (NullReferenceException)
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

        /// <summary>
        /// 天秤との通信リクエストの窓口（タイムアウト対応版）
        /// </summary>
        public async Task<OperationResult?> RequestWeightAsync(CancellationToken cancellationToken = default)
        {
            if (Port == null || !Port.IsOpen)
            {
                return OperationResult.Fail("Port is not connected");
            }

            _buffer.Clear(); // 古いデータを消去

            var result = WriteLine("Q");

            if (!result.IsSuccess)
            {
                return result;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutMilliseconds); // タイムアウト設定

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // タイムアウト対応のReadLineAsync
                    var line = await ReadLineAsync(cts.Token);
                    if (line != null && (line.StartsWith("ST") || line.StartsWith("US")))
                    {
                        return OperationResult.Success(line);
                    }                     
                }
            }
            catch (TaskCanceledException)
            {
                // タイムアウトもしくは外部キャンセル
                return OperationResult.Fail("Operation canceled");
            }

            return OperationResult.Fail("Fail to communicate with balance.");
        }

    }
}
