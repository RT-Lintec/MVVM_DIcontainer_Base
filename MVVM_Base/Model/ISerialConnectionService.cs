using System.IO.Ports;

namespace MVVM_Base.Model
{
    public class OperationResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }

        public OperationResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public static OperationResult Success(string message = "") => new(true, message);
        public static OperationResult Fail(string message) => new(false, message);
    }

    // 共通接続・切断操作
    public interface ISerialConnectionService
    {
        Task<bool> Connect(SerialPortInfo serialPortInfo);
        void Disconnect();

        //void Destroy();

        SerialPort? Port { get; }
    }

    // 天秤専用の非同期リクエスト
    public interface IMFCSerialService : ISerialConnectionService
    {
        /// <summary>
        /// MFC通信タイプ1
        /// </summary>
        Task<OperationResult?> RequestType1Async(string cmd, CancellationToken cancellationToken = default);

        /// <summary>
        /// MFC通信タイプ2
        /// </summary>
        Task<OperationResult?> RequestType2Async(string cmd, CancellationToken cancellationToken = default);

        /// <summary>
        /// MFC通信タイプ3
        /// </summary>
        Task<OperationResult?> RequestType3Async(string cmd1, string cmd2, CancellationToken cancellationToken = default);
    }

    // 天秤専用の非同期リクエスト
    public interface IBalanceSerialService : ISerialConnectionService
    {
        /// <summary>
        /// 天秤から重量取得要求 "Q\r\n" を送り、応答を受信する
        /// </summary>
        Task<OperationResult?> RequestWeightAsync(CancellationToken cancellationToken = default);
    }
}
