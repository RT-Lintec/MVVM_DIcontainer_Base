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
        /// ポート番号　"COM3"
        /// </summary>
        public string? PortName { get; set; }

        /// <summary>
        /// フレンドリ名　"Moxa USB Serial Port"
        /// </summary>
        public string? FriendlyName { get; set; }

        /// <summary>
        /// viewからの参照に対して保持するPortNameを返す
        /// </summary>
        /// <returns></returns>
        public override string? ToString()
        {
            return PortName;
        }
    }
}
