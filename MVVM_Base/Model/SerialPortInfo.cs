using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVVM_Base.Model
{
    /// <summary>
    /// シリアル通信のポー番号およびフレンドリ名の保持クラス
    /// </summary>
    public class SerialPortInfo
    {
        /// <summary>
        /// ポート番号　"COM1"など
        /// </summary>
        public string? PortName { get; set; }

        /// <summary>
        /// フレンドリ名　"Moxa USB Serial Port"など
        /// </summary>
        public string? FriendlyName { get; set; }

        /// <summary>
        /// ボーレート
        /// </summary>
        public int Baudrate { get; set; }

        /// <summary>
        /// データビット
        /// </summary>
        public int Databit {  get; set; }

        /// <summary>
        /// ストップビット
        /// </summary>
        public int Stopbit { get; set; }

        /// <summary>
        /// パリティビット
        /// </summary>
        public string? Paritybit { get; set; }
    }
}
