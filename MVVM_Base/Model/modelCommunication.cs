using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVVM_Base.Model
{
    public class ModelCommunication
    {
        private readonly ModelHandShake _handShake;

        public ModelCommunication(ModelHandShake handShake)
        {
            _handShake = handShake;
        }

        // 例: MFC 機器の状態取得
        public string GetMfcStatus()
        {
            if (!_handShake.IsConnected)
                throw new InvalidOperationException("MFC機器が接続されていません");

            var port = _handShake.GetSerialPort();
            // ここで RS232C コマンド送受信
            // port.WriteLine("STATUS?");
            // return port.ReadLine();

            return "OK"; // サンプル
        }

        // 例: Balance 機器の状態取得
        public string GetBalanceStatus()
        {
            if (!_handShake.IsConnected)
                throw new InvalidOperationException("Balance機器が接続されていません");

            var port = _handShake.GetSerialPort();
            // port.WriteLine("BALANCE?");
            // return port.ReadLine();

            return "Ready"; // サンプル
        }

        // 任意: コマンド送信
        public void SendCommand(string cmd)
        {
            if (!_handShake.IsConnected)
                throw new InvalidOperationException("機器が接続されていません");

            var port = _handShake.GetSerialPort();
            port.WriteLine(cmd);
        }
    }
}
