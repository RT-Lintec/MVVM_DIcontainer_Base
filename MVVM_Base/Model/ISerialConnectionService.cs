using System.IO.Ports;
using MVVM_Base.Common;

namespace MVVM_Base.Model
{
    /// <summary>
    /// 共通接続・切断操作
    /// </summary>
    public interface ISerialConnectionService
    {
        /// <summary>
        /// 接続
        /// </summary>
        /// <param name="serialPortInfo"></param>
        /// <returns></returns>
        Task<bool> Connect(SerialPortInfo serialPortInfo, CancellationToken token);

        /// <summary>
        /// 接続解除
        /// </summary>
        void Disconnect();

        /// <summary>
        /// ポート情報
        /// </summary>
        SerialPort? Port { get; }
    }

    /// <summary>
    /// MFC専用の非同期リクエスト
    /// </summary>
    public interface IMFCSerialService : ISerialConnectionService
    {
        /// <summary>
        /// MFC通信タイプ1
        /// </summary>
        Task<OperationResult?> RequestType1Async(string cmd, CancellationToken token);

        /// <summary>
        /// MFC通信タイプ2
        /// </summary>
        Task<OperationResult?> RequestType2Async(string cmd, CancellationToken token);

        /// <summary>
        /// MFC通信タイプ3
        /// </summary>
        Task<OperationResult?> RequestType3Async(string cmd1, string cmd2, CancellationToken token);

        /// <summary>
        /// ER,EWコマンド対応
        /// </summary>
        /// <param name="cmd1"></param>
        /// <param name="cmd2"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<OperationResult?> RequestReadWriteAsync(string cmd1, Tuple<string, string> cmd2, CancellationToken token);
    }

    /// <summary>
    /// 天秤専用の非同期リクエスト
    /// </summary>
    public interface IBalanceSerialService : ISerialConnectionService
    {
        /// <summary>
        /// 天秤から重量取得要求 "Q\r\n" を送り、応答を受信する
        /// </summary>
        Task<OperationResult> RequestWeightAsync(CancellationToken token);
    }
}
