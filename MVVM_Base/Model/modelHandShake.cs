using System.IO.Ports;
using Microsoft.Win32; // レジストリ参照用
using System.Management;

namespace MVVM_Base.Model
{
    public class ModelHandShake
    {
        private SerialPort _serialPort;

        /// <summary>接続状態</summary>
        public bool IsConnected => _serialPort != null && _serialPort.IsOpen;

        /// <summary>現在接続中のポート名</summary>
        public string ConnectedPortName => _serialPort?.PortName;

        /// <summary>
        /// 利用可能なCOMポート一覧（フレンドリ名付き）
        /// 例: COM3 - Moxa USB Serial Port
        /// </summary>
        public static List<string> GetAvailablePorts()
        {
            var portList = new List<string>();

            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        string name = obj["Name"]?.ToString() ?? "";
                        if (name.Contains("(COM"))
                        {
                            // COM番号抽出（例："COM3"）
                            int start = name.LastIndexOf("(COM");
                            if (start >= 0)
                            {
                                string com = name.Substring(start + 1);
                                com = com.TrimEnd(')');
                                portList.Add($"{com} - {name.Replace($" ({com})", "")}");
                            }
                        }
                    }
                }

                // COM番号順に並べ替え
                return portList.OrderBy(p => p).ToList();
            }
            catch
            {
                // 取得に失敗した場合は従来の方法にフォールバック
                return SerialPort.GetPortNames()
                                 .OrderBy(p => p)
                                 .Select(p => $"{p} - Unknown Device")
                                 .ToList();
            }
        }
        
        /// <summary>
        /// 接続
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="baudRate"></param>
        /// <returns></returns>
        public bool Connect(string portName, int baudRate)
        {
            try
            {
                if (_serialPort != null && _serialPort.IsOpen)
                    _serialPort.Close();

                _serialPort = new SerialPort(portName, baudRate)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500
                };

                _serialPort.Open();

                // 必要に応じて簡易ハンドシェイク処理
                // _serialPort.WriteLine("HELLO");
                // var response = _serialPort.ReadLine();

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解除
        /// </summary>
        public void Disconnect()
        {
            if (_serialPort != null && _serialPort.IsOpen)
                _serialPort.Close();
        }

        /// <summary>シリアルポートを返す</summary>
        public SerialPort GetSerialPort() => _serialPort;
    }
}