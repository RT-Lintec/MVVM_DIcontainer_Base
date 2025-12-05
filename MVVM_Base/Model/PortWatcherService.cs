using System.IO.Ports;
using System.Windows.Interop;

namespace MVVM_Base.Model
{
    /// <summary>
    /// ポート抜き差しイベントにはWIn32(主に物理層の状態を通知する)を用いた設計を行う
    /// WMIでもイベント取得可能だが、主にOS層といった高レイヤの状態を通知するため
    /// 誤検知や多重通知が発生する
    /// 物理層イベントのみ検知→念のためデバウンスで多重通知を防御
    /// </summary>
    public class PortWatcherService
    {
        // USB通知用定数
        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

        private IntPtr _hwnd;

        // デバウンス用
        private System.Timers.Timer? debounceTimer;
        private int debounceTime = 2000;

        private HashSet<string> currentPorts = new HashSet<string>();

        private readonly object _lock = new object();

        // 公開イベント
        public event Action? messageShowAdded;
        public event Action? messageShowRemoved;
        public event Action<string>? PortAdded;
        public event Action<string>? PortRemoved;
        public event Action? noChangeDetected;

        public PortWatcherService()
        {
            currentPorts = new HashSet<string>(SerialPort.GetPortNames());
        }

        /// <summary>
        /// HWNDをApp.xaml.csから注入する
        /// </summary>
        /// <param name="hwnd"></param>
        public void Initialize(IntPtr hwnd)
        {
            _hwnd = hwnd;
        }

        /// <summary>
        /// ポート抜き差しイベント監視開始
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void Start()
        {
            if (_hwnd == IntPtr.Zero)
                throw new InvalidOperationException("Initialize(hwnd)が先に必要です");

            AttachWndProc();
        }

        public void Stop()
        {
            debounceTimer?.Stop();
            debounceTimer?.Dispose();
            debounceTimer = null;
        }

        /// <summary>
        /// Win32メッセージをWPFにフック
        /// </summary>
        private void AttachWndProc()
        {
            HwndSource source = HwndSource.FromHwnd(_hwnd);
            source.AddHook(WndProc);
        }

        /// <summary>
        ///  Win32メッセージフック
        /// </summary>
        /// <param name="hwnd"></param>
        /// <param name="msg"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <param name="handled"></param>
        /// <returns></returns>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam,
            IntPtr lParam, ref bool handled)
        {
            if (msg == WM_DEVICECHANGE)
            {
                switch ((int)wParam)
                {                    
                    case DBT_DEVICEARRIVAL:
                        messageShowAdded?.Invoke();
                        DebounceCheckPorts();
                        handled = true;
                        break;

                    case DBT_DEVICEREMOVECOMPLETE:
                        messageShowRemoved?.Invoke();
                        DebounceCheckPorts();
                        handled = true;
                        break;
                }
            }

            return IntPtr.Zero;
        }

        /// <summary>
        /// Debounce時間だけ待ってからポートチェックを行う。その間最初のイベント発生から
        /// のOSからの通知は無視する
        /// </summary>
        private void DebounceCheckPorts()
        {
            if (debounceTimer == null)
            {
                debounceTimer = new System.Timers.Timer(debounceTime);
                debounceTimer.AutoReset = false;
                debounceTimer.Elapsed += (_, __) => CheckPortsNow();
            }
            debounceTimer.Stop();
            debounceTimer.Start();
        }

        /// <summary>
        /// ポート追加と削除を行いイベント発火
        /// 追加後と削除後のポート情報を比較する
        /// </summary>
        private void CheckPortsNow()
        {
            lock (_lock)
            {
                var ports = new HashSet<string>(SerialPort.GetPortNames());

                var added = ports.Except(currentPorts);
                var removed = currentPorts.Except(ports);

                foreach (var p in added)
                    PortAdded?.Invoke(p);

                foreach (var p in removed)
                    PortRemoved?.Invoke(p);

                bool isNoChange =
                    !added.Except(removed).Any() &&
                    !removed.Except(added).Any();

                if (isNoChange)
                    noChangeDetected?.Invoke();

                currentPorts = ports;
            }
        }
    }
}
