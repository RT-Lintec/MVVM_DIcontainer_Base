using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Model;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace MVVM_Base.ViewModel
{
    public partial class vmMain : ObservableObject
    {
        /// <summary>
        /// シリアルポートイベントの管理クラス
        /// </summary>
        private readonly PortWatcherService _portWatcher;

        /// <summary>
        /// カスタマイズダイアログクラス
        /// </summary>
        private readonly IMessageService _messageService;
        private int delayTime = 50; // ダイアログ表示までの間

        /// <summary>
        /// 現在のビュー
        /// </summary>
        [ObservableProperty]
        private UserControl? _currentView;

        /// <summary>
        /// 有効なシリアルポート番号リスト
        /// </summary>
        public ObservableCollection<SerialPortInfo> AvailablePortList { get; } = new();

        // MFCの各コンボボックスにおける選択中の値
        /// <summary>
        /// MFCポート番号
        /// </summary>
        [ObservableProperty] private string? _mfcPortNum;

        /// <summary>
        /// MFCボーレート
        /// </summary>
        [ObservableProperty] private int _mfcBaudrate;

        /// <summary>
        /// MFCデータビット
        /// </summary>
        [ObservableProperty] private string? _mfcDatabit;

        /// <summary>
        /// MFCストップビット
        /// </summary>
        [ObservableProperty] private string? _mfcStopbit;

        /// <summary>
        /// MFCパリティビット
        /// </summary>
        [ObservableProperty] private string? _mfcParitybit;

        // 天秤の各コンボボックスにおける選択中の値
        /// <summary>
        /// 天秤ポート番号
        /// </summary>
        [ObservableProperty] private string? _balancePortNum;

        /// <summary>
        /// 天秤ボーレート
        /// </summary>
        [ObservableProperty] private int _balanceBaudrate;

        /// <summary>
        /// 天秤データビット
        /// </summary>
        [ObservableProperty] private string? _balanceDatabit;

        /// <summary>
        /// 天秤ストップビット
        /// </summary>
        [ObservableProperty] private string? _balanceStopbit;

        /// <summary>
        /// 天秤パリティビット
        /// </summary>
        [ObservableProperty] private string? _balanceParitybit;

        // MFC通信設定リスト
        /// <summary>
        /// MFCボーレートリスト
        /// </summary>
        public List<int> MfcBaudrateList { get; } = new() { 4800, 9600, 19200, 38400, 57600 };

        /// <summary>
        /// MFCデータビットリスト
        /// </summary>
        public List<string?> MfcDatabitList { get; } = new() { "7", "8" };

        /// <summary>
        /// MFCストップビットリスト
        /// </summary>
        public List<string?> MfcStopbitList { get; } = new() { "1", "2" };

        /// <summary>
        /// MFCパリティビットリスト
        /// </summary>
        public List<string?> MfcParitybitList { get; } = new() { "None", "Odd", "Even" };

        // 天秤通信設定リスト
        /// <summary>
        /// MFCボーレートリスト
        /// </summary>
        public List<int> BalanceBaudrateList { get; } = new() { 4800, 9600, 19200, 38400, 57600 };

        /// <summary>
        /// MFCデータビットリスト
        /// </summary>
        public List<string?> BalanceDatabitList { get; } = new() { "7", "8" };

        /// <summary>
        /// MFCストップビットリスト
        /// </summary>
        public List<string?> BalanceStopbitList { get; } = new() { "1", "2" };

        /// <summary>
        /// MFCパリティビットリスト
        /// </summary>
        public List<string?> BalanceParitybitList { get; } = new() { "None", "Odd", "Even" };

        /// <summary>
        /// MFCポート情報
        /// </summary>
        private SerialPortInfo? _mfcPort;
        public SerialPortInfo? MfcPort
        {
            get => _mfcPort;
            set
            {
                _mfcPort = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MfcFriendlyName));
            }
        }

        /// <summary>
        /// 天秤ポート情報
        /// </summary>
        private SerialPortInfo? _balancePort;
        public SerialPortInfo? BalancePort
        {
            get => _balancePort;
            set
            {
                if (_balancePort != value)
                {
                    _balancePort = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(BalanceFriendlyName));
                }
            }
        }

        /// <summary>
        /// MFCフレンドリ名を取得する。MfcPortがnullなら空文字列を返す。
        /// </summary>
        public string? MfcFriendlyName => MfcPort?.FriendlyName ?? "";

        /// <summary>
        /// 天秤フレンドリ名を取得する。BalancePortがnullなら空文字列を返す。
        /// </summary>
        public string? BalanceFriendlyName => BalancePort?.FriendlyName ?? "";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="portWatcher"></param>
        /// <param name="messageService"></param>
        public vmMain(PortWatcherService portWatcher, IMessageService messageService)
        {
            InitParameter();
            RefreshAvailablePorts();

            _portWatcher = portWatcher;
            _portWatcher.messageShowAdded += MessageShowAdded;
            _portWatcher.messageShowRemoved += MessageShowRemoved;
            _portWatcher.noChangeDetected += DeleteDialog;
            _portWatcher.PortAdded += OnPortAdded;
            _portWatcher.PortRemoved += OnPortRemoved;

            _messageService = messageService;
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~vmMain()
        {
            StopPortWatcher();
        }

        /// <summary>
        /// 初期値設定
        /// </summary>
        private void InitParameter()
        {
            MfcBaudrate = 9600;
            MfcDatabit = "7";
            MfcStopbit = "1";
            MfcParitybit = "Even";

            BalanceBaudrate = 4800;
            BalanceDatabit = "7";
            BalanceStopbit = "1";
            BalanceParitybit = "Even";
        }

        /// <summary>
        /// ポート一覧を更新
        /// </summary>
        public void RefreshAvailablePorts()
        {
            AvailablePortList.Clear();
            foreach (var info in SerialPortSearcher.GetPortList())
                AvailablePortList.Add(info);
        }

        /// <summary>
        /// シリアルポートUSBケーブルが差し込まれた場合のメッセージ表示処理
        /// </summary>
        private void MessageShowAdded()
        {
            _messageService.ShowMessage("ポート再読み込み中...");
        }

        /// <summary>
        /// シリアルポートUSBケーブルが抜かれた場合のメッセージ表示処理
        /// </summary>
        private void MessageShowRemoved()
        {
            _messageService.ShowMessage("ポートが切断されました...");
        }

        /// <summary>
        /// ポート抜き差しするも変化が無い場合にダイアログを消去する
        /// </summary>
        private void DeleteDialog()
        {
            _messageService.CloseWithFade();
        }

        /// <summary>
        /// ポート差し込みイベントハンドラ（非同期）
        /// </summary>
        private async void OnPortAdded(string portName)
        {
            // 安定化のため短時間待つ（WMI と併用する場合に有効）
            await Task.Delay(delayTime);

            App.Current.Dispatcher.Invoke(() =>
            {
                RefreshAvailablePorts();
            });

            _messageService.CloseWithFade();
        }

        /// <summary>
        /// ポート抜き出しイベントハンドラ（非同期）
        /// </summary>
        private async void OnPortRemoved(string portName)
        {
            await Task.Delay(delayTime);

            // ポートリストを空にする
            App.Current.Dispatcher.Invoke(() =>
            {                
                AvailablePortList.Clear();
            });

            App.Current.Dispatcher.Invoke(() =>
            {
                RefreshAvailablePorts();
            });

            _messageService.CloseWithFade();
        }

        /// <summary>
        /// イベント削除
        /// </summary>
        public void StopPortWatcher()
        {
            _portWatcher.noChangeDetected -= DeleteDialog;
            _portWatcher.messageShowAdded -= MessageShowAdded;
            _portWatcher.messageShowRemoved -= MessageShowRemoved;
            _portWatcher.PortAdded -= OnPortAdded;
            _portWatcher.PortRemoved -= OnPortRemoved;
        }
    }
}
