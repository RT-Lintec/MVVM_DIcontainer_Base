namespace MVVM_Base.Common
{
    /// <summary>
    /// 操作結果タイプ
    /// </summary>
    public enum OperationResultType
    {
        Success,
        Failure,
        Canceled
    }

    /// <summary>
    /// 操作結果
    /// </summary>
    public class OperationResult
    {
        /// <summary>
        /// 状態
        /// </summary>
        public OperationResultType Status { get; }


        private string payload = string.Empty;
        /// <summary>
        /// メッセージ
        /// </summary>
        public string? Payload { 
            get => payload;
            set => payload = value;
        }

        /// <summary>
        /// エラーコード
        /// </summary>
        public string? ErrorCode { get; }

        public OperationResult(OperationResultType status, string? payload = null, string? errorCode = null)
        {
            Status = status;
            Payload = payload;
            ErrorCode = errorCode;
        }

        /// <summary>
        /// 成功(メッセージ有り)
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public static OperationResult Success(string payload)
            => new(OperationResultType.Success, payload);

        /// <summary>
        /// 成功(メッセージ無し)
        /// </summary>
        /// <returns></returns>
        public static OperationResult Success()
            => new(OperationResultType.Success);

        /// <summary>
        /// 失敗
        /// </summary>
        /// <param name="errorCode"></param>
        /// <returns></returns>
        public static OperationResult Failed(string? errorCode = null)
            => new(OperationResultType.Failure, null, errorCode);

        /// <summary>
        /// キャンセル
        /// </summary>
        /// <returns></returns>
        public static OperationResult Canceled()
            => new(OperationResultType.Canceled);
    }
}
