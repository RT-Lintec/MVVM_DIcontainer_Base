using System.IO.Ports;
using System.Management;

namespace MVVM_Base.Model
{
    public class PortWatcherService
    {
        /// <summary>
        /// シリアルポートUSBケーブル差し込み監視オブジェクト
        /// </summary>
        private ManagementEventWatcher? _arrivalWatcher;

        /// <summary>
        /// シリアルポートUSBケーブル抜き出し監視オブジェクト
        /// </summary>
        private ManagementEventWatcher? _removalWatcher;

        /// <summary>
        /// シリアルポートのハッシュリスト(重複許可しない)
        /// </summary>
        private HashSet<string> _currentPorts;

        /// <summary>
        /// 排他制御オブジェクト
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// タイマー デバウンス時間後にセットしたラムダ式を実行する
        /// </summary>
        private System.Timers.Timer? _debounceTimer;

        /// <summary>
        /// デバウンス時間(メッセージ表示～消えるまでの時間)
        /// </summary>
        private int debounceTime = 2000;

        /// <summary>
        /// シリアルポートUSBケーブル抜き差ししてもポート変化無いときのイベント
        /// </summary>
        public event Action? noChangeDetected;

        /// <summary>
        /// シリアルポートUSBケーブル差し込み時のダイアログ表示イベント
        /// </summary>
        public event Action? messageShowAdded;

        /// <summary>
        /// シリアルポートUSBケーブル抜き出し時のダイアログ表示イベント
        /// </summary>
        public event Action? messageShowRemoved;

        /// <summary>
        /// シリアルポートUSBケーブル差し込み時の処理イベント
        /// </summary>
        public event Action<string>? PortAdded;

        /// <summary>
        /// シリアルポートUSBケーブル抜き出し時の処理イベント
        /// </summary>
        public event Action<string>? PortRemoved;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="messageService"></param>
        public PortWatcherService()
        {
            // 初期ポートリスト取得
            _currentPorts = new HashSet<string>(SerialPort.GetPortNames());
        }

        /// <summary>
        /// USBポート差し込み監視の開始（WMI監視）
        /// </summary>
        public void Start()
        {
            var arrivalQuery = new WqlEventQuery(
                "SELECT * FROM Win32_DeviceChangeEvent WITHIN 1 WHERE EventType = 2");
            _arrivalWatcher = new ManagementEventWatcher(arrivalQuery);
            _arrivalWatcher.EventArrived += Arrival_EventArrived;
            _arrivalWatcher.Start();

            var removalQuery = new WqlEventQuery(
                "SELECT * FROM Win32_DeviceChangeEvent WITHIN 1 WHERE EventType = 3");
            _removalWatcher = new ManagementEventWatcher(removalQuery);
            _removalWatcher.EventArrived += Removal_EventArrived;
            _removalWatcher.Start();
        }

        /// <summary>
        /// USBポート差し込み監視の終了
        /// </summary>
        public void Stop()
        {
            _arrivalWatcher?.Stop();
            _arrivalWatcher?.Dispose();
            _arrivalWatcher = null;

            _removalWatcher?.Stop();
            _removalWatcher?.Dispose();
            _removalWatcher = null;

            _debounceTimer?.Stop();
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        /// <summary>
        /// Debounce処理：短時間(間隔：debounceTime msec)に複数イベントが発生してもまとめて1回だけ処理
        /// </summary>
        private void DebounceCheckPorts()
        {
            if (_debounceTimer == null)
            {
                _debounceTimer = new System.Timers.Timer(debounceTime);
                _debounceTimer.AutoReset = false;
                _debounceTimer.Elapsed += (s, e) => CheckPortsNow();
            }
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        /// <summary>
        /// 現在のポートと前回のポートを比較して追加/削除を通知
        /// </summary>
        private void CheckPortsNow()
        {
            // クリティカルセクション
            lock (_lock)
            {
                var ports = new HashSet<string>(SerialPort.GetPortNames());

                // 追加されたポートを通知
                var added = ports.Except(_currentPorts);
                foreach (var port in added)
                    PortAdded?.Invoke(port);

                // 削除されたポートを通知
                var removed = _currentPorts.Except(ports);
                foreach (var port in removed)
                    PortRemoved?.Invoke(port);

                _currentPorts = ports;

                // ポート情報に変化があるかどうか
                bool isNoChange = !added.Except(removed).Any() && !removed.Except(added).Any();
                // 何も変更がないのに呼ばれた場合
                if (isNoChange)
                {
                    noChangeDetected?.Invoke();
                }
            }
        }

        /// <summary>
        /// シリアルポートUSBケーブルの差し込みイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Arrival_EventArrived(object sender, EventArrivedEventArgs e)
        {
            messageShowAdded?.Invoke();
            DebounceCheckPorts();
        }

        /// <summary>
        /// シリアルポートUSBケーブルの抜き出しイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Removal_EventArrived(object sender, EventArrivedEventArgs e)
        {
            messageShowRemoved?.Invoke();
            DebounceCheckPorts();
        }
    }
}
