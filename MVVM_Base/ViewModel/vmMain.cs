using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM_Base.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MVVM_Base.ViewModel
{
    public partial class vmMain : ObservableObject, IViewModel
    {
        private CancellationTokenSource? vmCts;

        /// <summary>
        /// シリアルポートイベントの管理クラス
        /// </summary>
        private readonly PortWatcherService portWatcher;

        /// <summary>
        /// カスタマイズダイアログクラス
        /// </summary>
        private readonly IMessageService messageService;
        private int delayTime = 50; // ダイアログ表示までの間

        /// <summary>
        /// 
        /// </summary>
        public IAsyncRelayCommand CommBalanceCommand { get; }

        public IAsyncRelayCommand CommMFCType1 { get; }

        public IAsyncRelayCommand CommMFCType2 { get; }

        public IAsyncRelayCommand CommMFCType3 { get; }

        public ICommand UpdateMFCStatusCommand { get; }

        /// <summary>
        /// 現在のビュー
        /// </summary>
        [ObservableProperty]
        private UserControl? _currentView;

        /// <summary>
        /// 有効なシリアルポート番号リスト
        /// </summary>
        public ObservableCollection<SerialPortInfo> AvailablePortList { get; } = new();

        #region ポートボタンの表示可否
        /// <summary>
        /// MFCポートリストの表示可否
        /// </summary>
        private bool isMfcPortListEnabled = true;
        public bool IsMfcPortListEnabled
        {
            get => isMfcPortListEnabled;
            set
            {
                if (isMfcPortListEnabled != value)
                {
                    isMfcPortListEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Balanceポートリストの表示可否
        /// </summary>
        private bool isBalancePortListEnabled = true;
        public bool IsBalancePortListEnabled
        {
            get => isBalancePortListEnabled;
            set
            {
                if (isBalancePortListEnabled != value)
                {
                    isBalancePortListEnabled = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 天秤ポートボタンの有効可否
        /// </summary>
        private bool isMfcPortbtnEnable = true;
        public bool IsMfcPortbtnEnable
        {
            get => isMfcPortbtnEnable;
            set
            {
                if (isMfcPortbtnEnable != value)
                {
                    isMfcPortbtnEnable = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 天秤ポートボタンの有効可否
        /// </summary>
        private bool isBalancePortbtnEnable = true;
        public bool IsBalancePortbtnEnable
        {
            get => isBalancePortbtnEnable;
            set
            {
                if (isBalancePortbtnEnable != value)
                {
                    isBalancePortbtnEnable = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region MFC通信設定リスト
        /// <summary>
        /// MFCボーレートリスト
        /// </summary>
        public List<int> MfcBaudrateList { get; } = new() { 4800, 9600, 19200, 38400, 57600 };

        /// <summary>
        /// MFCデータビットリスト
        /// </summary>
        public List<int> MfcDatabitList { get; } = new() { 7, 8 };

        /// <summary>
        /// MFCストップビットリスト
        /// </summary>
        public List<int> MfcStopbitList { get; } = new() { 1, 2 };

        /// <summary>
        /// MFCパリティビットリスト
        /// </summary>
        public List<string?> MfcParitybitList { get; } = new() { "None", "Odd", "Even" };
        #endregion

        #region 天秤通信設定リスト
        /// <summary>
        /// 天秤ボーレートリスト
        /// </summary>
        public List<int> BalanceBaudrateList { get; } = new() { 4800, 9600, 19200, 38400, 57600 };

        /// <summary>
        /// 天秤データビットリスト
        /// </summary>
        public List<int> BalanceDatabitList { get; } = new() { 7, 8 };

        /// <summary>
        /// 天秤ストップビットリスト
        /// </summary>
        public List<int> BalanceStopbitList { get; } = new() { 1, 2 };

        /// <summary>
        /// 天秤パリティビットリスト
        /// </summary>
        public List<string?> BalanceParitybitList { get; } = new() { "None", "Odd", "Even" };
        #endregion

        #region MFCポート情報
        // ポート番号・フレンドリ名は動的に変化するため、個別にプロパティを用意してバインドして制御している。
        // 一方、静的なボーレート・データビット・ストップビット・パリティビットはPort情報に直接バインドしている。

        /// <summary>
        /// MFCポート本体のバックフィールド
        /// </summary>
        private SerialPortInfo? _mfcPort;

        /// <summary>
        /// プロパティ：MFCポート番号のバックフィールド
        /// </summary>
        private string? _mfcPortNum;

        /// <summary>
        /// プロパティ：MFCフレンドリ名のバックフィールド
        /// </summary>
        private string? _mfcFriendlyName;

        /// <summary>
        /// MFCポート本体のプロパティ
        /// </summary>
        public SerialPortInfo? MfcPort
        {
            get => _mfcPort;
            set
            {
                if (_mfcPort != null)
                {
                    _mfcPort = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// MFCポート番号とフレンドリ名のプロパティ
        /// </summary>
        private string _mfcPortWithName;
        public string MfcPortWithName
        {
            get => _mfcPortWithName;
            set
            {
                if (_mfcPortWithName != value)
                {
                    _mfcPortWithName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// MFCポート番号のプロパティ
        /// </summary>
        public string? MfcPortName
        {
            get => MfcPort?.PortName ?? _mfcPortNum;
            set
            {
                if (MfcPort != null)
                { 
                    MfcPort.PortName = value;

                    if (AvailablePortList.Count > 0)
                    {
                        var port = AvailablePortList.FirstOrDefault(x => x.PortName == value);
                        MfcFriendlyName = port?.FriendlyName ?? "";

                        // MFC通信アイコン表示条件1：ポート情報が選択されていること
                        IsMfcPortSelected = true;
                    }
                }
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// MFCフレンドリ名のプロパティ
        /// </summary>
        public string? MfcFriendlyName
        {
            get => MfcPort?.FriendlyName ?? _mfcFriendlyName;
            set
            {
                if (MfcPort != null) MfcPort.FriendlyName = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region 天秤ポート情報
        // ポート番号・フレンドリ名は動的に変化するため、個別にプロパティを用意してバインドして制御している。
        // 一方、静的なボーレート・データビット・ストップビット・パリティビットはPort情報に直接バインドしている。

        /// <summary>
        /// Balanceポート本体のバックフィールド
        /// </summary>
        private SerialPortInfo? _balancePort;

        /// <summary>
        /// プロパティ：Balanceポート番号のバックフィールド
        /// </summary>
        private string? _balancePortNum;

        /// <summary>
        /// プロパティ：Balanceフレンドリ名のバックフィールド
        /// </summary>
        private string? _balanceFriendlyName;
                
        /// <summary>
        /// Balanceポート本体のプロパティ
        /// </summary>
        public SerialPortInfo? BalancePort
        {
            get => _balancePort;
            set
            {
                if (_balancePort != null)
                {
                    _balancePort = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Balanceポート番号とフレンドリ名のプロパティ
        /// </summary>
        private string _balancePortWithName;
        public string BalancePortWithName
        {
            get => _balancePortWithName;
            set
            {
                if (_balancePortWithName != value)
                {
                    _balancePortWithName = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Balanceポート番号のプロパティ
        /// </summary>
        public string? BalancePortName
        {
            get => BalancePort?.PortName ?? _balancePortNum;
            set
            {
                if (BalancePort != null)
                {
                    BalancePort.PortName = value;

                    if (AvailablePortList.Count > 0)
                    {
                        var port = AvailablePortList.FirstOrDefault(x => x.PortName == value);
                        BalanceFriendlyName = port?.FriendlyName ?? "";

                        // 天秤通信アイコン表示条件1：ポート情報が選択されていること
                        IsBalancePortSelected = true;
                    }
                }
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Balanceフレンドリ名のプロパティ
        /// </summary>
        public string? BalanceFriendlyName
        {
            get => BalancePort?.FriendlyName ?? _balanceFriendlyName;
            set
            {
                if (BalancePort != null) BalancePort.FriendlyName = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region 通信メソッドとイベント

        [ObservableProperty] public string _commBalanceValue;
        [ObservableProperty] public string _commMFCType1Command;
        [ObservableProperty] public string _commMFCType2Command;
        [ObservableProperty] public string _commMFCType3Command1;
        [ObservableProperty] public string _commMFCType3Command2;
        [ObservableProperty] public string _commMFCType2Result;
        [ObservableProperty] public string _commMFCType3Result;
        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));


        #region MFC Statusプロパティ

        /// <summary>
        /// デバイス番号
        /// </summary>
        private string _deviceNum;
        public string DeviceNum
        {
            get => _deviceNum;
            set
            {
                if (_deviceNum != value)
                {
                    _deviceNum = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Alerm A
        /// </summary>
        private string _alermAOption;
        public string AlermAOption
        {
            get => _alermAOption;
            set
            {
                if (_alermAOption != value)
                {
                    _alermAOption = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Alerm B
        /// </summary>
        private string _alermBOption;
        public string AlermBOption
        {
            get => _alermBOption;
            set
            {
                if (_alermBOption != value)
                {
                    _alermBOption = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// analog / digital
        /// </summary>
        private string _aDOption;
        public string ADOption
        {
            get => _aDOption;
            set
            {
                if (_aDOption != value)
                {
                    _aDOption = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// valve
        /// </summary>
        private string _valveOption;
        public string ValveOption
        {
            get => _valveOption;
            set
            {
                if (_valveOption != value)
                {
                    _valveOption = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// slow / fast
        /// </summary>
        private string _sFOption;
        public string SFOption
        {
            get => _sFOption;
            set
            {
                if (_sFOption != value)
                {
                    _sFOption = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Control Mode
        /// </summary>
        private string _cmodeOption;
        public string CmodeOption
        {
            get => _cmodeOption;
            set
            {
                if (_cmodeOption != value)
                {
                    _cmodeOption = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region MFCポート状態管理変数
        private bool isMfcPortSelected = false;
        public bool IsMfcPortSelected
        {
            get => isMfcPortSelected;
            set
            {
                if (isMfcPortSelected != value)
                {
                    isMfcPortSelected = value;
                    OnPropertyChanged();
                    UpdateIsMfcConnected();                    
                }
            }
        }

        private bool isMfcPortOpened = false;
        public bool IsMfcPortOpened
        {
            get => isMfcPortOpened;
            set
            {
                if (isMfcPortOpened != value)
                {
                    isMfcPortOpened = value;
                    OnPropertyChanged();
                    UpdateIsMfcConnected();
                    UpdateMfcPortOpenBtnText();
                }
            }
        }

        private bool isMfcConnected = false;
        public bool IsMfcConnected
        {
            get => isMfcConnected;
            private set
            {
                if (isMfcConnected != value)
                {
                    isMfcConnected = value;
                    OnPropertyChanged(); // CallerMemberName を使っている場合
                }
            }
        }

        private void OnMfcConnectedChanged()
        {
            OnPropertyChanged(nameof(IsMfcConnected));
        }

        private void UpdateIsMfcConnected()
        {
            IsMfcConnected = IsMfcPortSelected && IsMfcPortOpened;
        }

        // MFC ポートオープン/クローズボタンの表示テキスト
        private string _mFCPortButtonText = "Open";
        public string MFCPortButtonText
        {
            get => _mFCPortButtonText;
            private set
            {
                if (_mFCPortButtonText != value)
                {
                    _mFCPortButtonText = value;
                    OnPropertyChanged();
                    OnMfcConnectedChanged();
                }
            }
        }

        private void UpdateMfcPortOpenBtnText()
        {
            MFCPortButtonText = IsMfcPortOpened ? "Close" : "Open";
        }
        #endregion

        #region 天秤ポート状態管理変数
        private bool isBalancePortSelected = false;
        public bool IsBalancePortSelected
        {
            get => isBalancePortSelected;
            set
            {
                if (isBalancePortSelected != value)
                {
                    isBalancePortSelected = value;
                    OnPropertyChanged();
                    UpdateIsBalanceConnected();
                }
            }
        }

        private bool isBalancePortOpened = false;
        public bool IsBalancePortOpened
        {
            get => isBalancePortOpened;
            set
            {
                if (isBalancePortOpened != value)
                {
                    isBalancePortOpened = value;
                    OnPropertyChanged();
                    UpdateIsBalanceConnected();
                }
            }
        }

        private bool isBalanceConnected = false;
        public bool IsBalanceConnected
        {
            get => isBalanceConnected;
            private set
            {
                if (isBalanceConnected != value)
                {
                    isBalanceConnected = value;
                    OnPropertyChanged();
                    UpdateBalancePortOpenBtnText();
                }
            }
        }

        private void OnBalanceConnectedChanged()
        {
            OnPropertyChanged(nameof(IsBalanceConnected));
        }

        private void UpdateIsBalanceConnected()
        {
            IsBalanceConnected = IsBalancePortSelected && IsBalancePortOpened;
        }

        // 天秤ポートオープン/クローズボタンの表示テキスト
        private string _balancePortButtonText = "Open";
        public string BalancePortButtonText
        {
            get => _balancePortButtonText;
            private set
            {
                if (_balancePortButtonText != value)
                {
                    _balancePortButtonText = value;
                    OnPropertyChanged();
                    OnBalanceConnectedChanged();
                }
            }
        }

        private void UpdateBalancePortOpenBtnText()
        {
            BalancePortButtonText = IsBalancePortOpened ? "Close" : "Open";
        }
        #endregion

        #region サイズ変数

        /// <summary>
        /// グループボックスサイズ
        /// </summary>
        private double _groupBoxWidth = 740;
        public double GroupBoxWidth
        {
            get => _groupBoxWidth;
            set
            {
                _groupBoxWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Statusグループボックスサイズ
        /// </summary>
        private double _groupStatusBoxWidth = 240;
        public double GroupBoxStatusWidth
        {
            get => _groupStatusBoxWidth;
            set
            {
                _groupStatusBoxWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// DebugグループボックスサイズA
        /// </summary>
        private double _groupBoxDebugWidthA = 450;
        public double GroupBoxDebugWidthA
        {
            get => _groupBoxDebugWidthA;
            set
            {
                _groupBoxDebugWidthA = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// DebugグループボックスサイズB
        /// </summary>
        private double _groupBoxDebugWidthB = 280;
        public double GroupBoxDebugWidthB
        {
            get => _groupBoxDebugWidthB;
            set
            {
                _groupBoxDebugWidthB = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 大項目フォントサイズ
        /// </summary>
        private double _titleFontSize = 18;
        public double TitleFontSize
        {
            get => _titleFontSize;
            set
            {
                _titleFontSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ラベルフォントサイズ
        /// </summary>
        private double _labelFontSize = 16;
        public double LabelFontSize
        {
            get => _labelFontSize;
            set
            {
                _labelFontSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ステータスのラベルフォントサイズ
        /// </summary>
        private double _statusFontSize = 16;
        public double StatusFontSize
        {
            get => _statusFontSize;
            set
            {
                _statusFontSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// コンボボックスのフォントサイズ
        /// </summary>
        private double _comboFontSize = 16;
        public double ComboFontSize
        {
            get => _comboFontSize;
            set
            {
                _comboFontSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// アイコンサイズ
        /// </summary>
        private double iconSize = 20;
        public double IconSize
        {
            get => iconSize;
            set
            {
                iconSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ステータスのアイコンサイズ
        /// </summary>
        private double _statusIconSize = 18;
        public double StatusIconSize
        {
            get => _statusIconSize;
            set
            {
                _statusIconSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// コンボボックスのパディングサイズ
        /// </summary>
        private double _comboPaddingSize = 10;
        public double ComboPaddingSize
        {
            get => _comboPaddingSize;
            set
            {
                _comboPaddingSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// コンボボックスの幅サイズ
        /// </summary>
        private double _comboWidthSize = 80;
        public double ComboWidthSize
        {
            get => _comboWidthSize;
            set
            {
                _comboWidthSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// コンボボックスの幅長めサイズ
        /// </summary>
        private double _comboWidthLongSize = 300;
        public double ComboWidthLongSize
        {
            get => _comboWidthLongSize;
            set
            {
                _comboWidthLongSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// コンボボックスの高さサイズ
        /// </summary>
        private double _comboHeightSize = 24;
        public double ComboHeighSize
        {
            get => _comboHeightSize;
            set
            {
                _comboHeightSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ポートボタンの幅サイズ
        /// </summary>
        private double _portBtnSize = 50;
        public double PortBtnSize
        {
            get => _portBtnSize;
            set
            {
                _portBtnSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ラジオボタンのサイズ
        /// </summary>
        private int _radioBtnSIze = 14;
        public int RadioBtnSIze
        {
            get => _radioBtnSIze;
            set
            {
                if (_radioBtnSIze != value)
                {
                    _radioBtnSIze = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ラジオボタンブルームのサイズ
        /// </summary>
        private int _radioBtnBloomSIze = 10;
        public int RadioBtnBloomSIze
        {
            get => _radioBtnBloomSIze;
            set
            {
                if (_radioBtnBloomSIze != value)
                {
                    _radioBtnBloomSIze = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// デバッグのテキストボックスサイズ
        /// </summary>
        private int _debugTextBoxSIze = 50;
        public int DebugTextBoxSIze
        {
            get => _debugTextBoxSIze;
            set
            {
                if (_debugTextBoxSIze != value)
                {
                    _debugTextBoxSIze = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// デバッグの長めテキストボックスサイズ
        /// </summary>
        private int _debugTextBoxLongSIze = 150;
        public int DebugTextBoxLongSIze
        {
            get => _debugTextBoxLongSIze;
            set
            {
                if (_debugTextBoxLongSIze != value)
                {
                    _debugTextBoxLongSIze = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        public bool IsDebug { get; } =
#if DEBUG
        true;
#else
        false;
#endif

        // service
        private readonly IMFCSerialService mfcService;
        private readonly ThemeService themeService;
        private readonly CommStatusService commStatusService;
        private readonly ViewModelManagerService vmService;
        private readonly ApplicationStatusService appStatusService;

        [ObservableProperty]
        private string? _readValue;

        [ObservableProperty]
        private string? _writeValue;

        /// <summary>
        /// 終了可否
        /// </summary>
        public bool canQuit { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="portWatcher"></param>
        /// <param name="messageService"></param>
        public vmMain(PortWatcherService _portWatcher, IMessageService _messageService, 
            ThemeService _themeService, CommStatusService _commStatusService, ViewModelManagerService _vmService, ApplicationStatusService _appStatusService)
        {
            InitParameter();
            RefreshAvailablePorts();            

            portWatcher = _portWatcher;
            portWatcher.messageShowAdded += MessageShowAdded;
            portWatcher.messageShowRemoved += MessageShowRemoved;
            portWatcher.noChangeDetected += NoChangeDetected;
            portWatcher.PortAdded += OnPortAdded;
            portWatcher.PortRemoved += OnPortRemoved;

            messageService = _messageService;

            CommBalanceCommand = new AsyncRelayCommand(CommBalanceAsyncCommand);
            CommMFCType1 = new AsyncRelayCommand(CommMFCAsyncType1);
            CommMFCType2 = new AsyncRelayCommand(CommMFCAsyncType2);
            CommMFCType3 = new AsyncRelayCommand(CommMFCAsyncType3);

            mfcService = MfcSerialService.Instance;

            commMFCAsyncType1WithCommand = new AsyncRelayCommand<string>(CommMFCAsyncType1WithCommand);
            UpdateMFCStatusCommand = new AsyncRelayCommand(UpdateMFCStatus);

            themeService = _themeService;
            themeService.PropertyChanged += ThemeService_PropertyChanged;

            commStatusService = _commStatusService;
            vmService = _vmService;
            vmService.Register(this);

            appStatusService = _appStatusService;
            appStatusService.PropertyChanged += AppStatusService_PropertyChanged;
            //this.PropertyChanged += MfcConnected_PropertyChanged;
            vmService.CanTransit = true;
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~vmMain()
        {
            //StopPortWatcher();

            //// 接続中のポート全てに対して接続解除
            //MfcSerialService.Instance.Disconnect();
            //BalanceSerialService.Instance.Disconnect();
        }

        public void Dispose()
        {
            //StopPortWatcher();

            // 接続中のポート全てに対して接続解除
            MfcSerialService.Instance.Disconnect();
            BalanceSerialService.Instance.Disconnect();

            // 終了可否判断
            canQuit = true;

            // 終了可否チェック
            vmService.CheckCanQuit();

            vmService.CanTransit = true;
        }

        /// <summary>
        /// 拡縮回数
        /// </summary>
        private int tcnt = 0;

        /// <summary>
        /// UI拡縮
        /// </summary>
        public ICommand AdjustUICommand => new RelayCommand<object>(e =>
        {
            int delta = 0;

            if (e is KeyEventArgs k)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
                if (k.Key == Key.OemPlus || k.Key == Key.Add) delta = 1;
                else if (k.Key == Key.OemMinus || k.Key == Key.Subtract) delta = -1;
            }
            else if (e is MouseWheelEventArgs me)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Control) == 0) return;
                delta = me.Delta > 0 ? 1 : -1;
            }
            else
            {
                return;
            }

            // delta が決まったらサイズ調整
            AdjustFontSizeByDelta(delta);
        });


        private void AdjustFontSizeByDelta(int delta)
        {
            if (delta > 0 && tcnt > 4) return;
            if (delta < 0 && tcnt < -4) return;

            TitleFontSize += delta;
            LabelFontSize += delta;
            StatusFontSize += delta;
            ComboFontSize += delta;
            IconSize += delta;
            StatusIconSize += delta;

            GroupBoxWidth += delta * 48;
            GroupBoxStatusWidth += delta * 16;
            GroupBoxDebugWidthA += delta * 32;
            GroupBoxDebugWidthB += delta * 16;

            ComboWidthLongSize += delta * 16;
            ComboWidthSize += delta * 4;
            ComboHeighSize += delta * 1;
            ComboPaddingSize += delta * 1;

            PortBtnSize += delta * 3;
            RadioBtnSIze += delta * 1;
            RadioBtnBloomSIze += delta * 1;

            DebugTextBoxSIze += delta * 5;
            DebugTextBoxLongSIze += delta * 8;

            tcnt += delta;
        }

        #region 状態変更通知関連
        /// <summary>
        /// カラーテーマ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ThemeService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ThemeService.CurrentTheme))
            {
                // CurrentTheme変化を検知
                OnThemeChanged(themeService.CurrentTheme);
            }
        }

        private void OnThemeChanged(string newTheme)
        {
            // Viewに依存せずViewModel内で処理可能
            // 例：内部フラグ更新や別プロパティ更新など
            IsDarkTheme = newTheme == "Dark"; // フラグ例
                                              // 必要であれば PropertyChanged 通知も出す
            OnPropertyChanged(nameof(IsDarkTheme));
        }

        /// <summary>
        /// ThemeServiceにイベント通知を委任しているので、プロパティ変化の通知は行わない
        /// 行うと二重発火となり、viewEntryでのプロパティ変更イベントが二回発生する。
        /// </summary>
        private bool isDarkTheme;
        public bool IsDarkTheme
        {
            get => isDarkTheme;
            set
            {
                if (isDarkTheme != value)
                {
                    isDarkTheme = value;
                }
            }
        }
        #endregion

        /// <summary>
        /// 初期値設定
        /// </summary>
        private void InitParameter()
        {
            _mfcPort = new SerialPortInfo 
            { 
                Baudrate = 9600,
                Databit = 7,
                Stopbit = 2, 
                Paritybit = "None"
            };

            _balancePort = new SerialPortInfo
            {
                Baudrate = 4800,
                Databit = 7,
                Stopbit = 1,
                Paritybit = "Even"
            };
        }

        // ポートを開くボタン
        [RelayCommand]
        private async void ControlMFCPort()
        {
            // ポート番号が設定されていなければ中断
            if (MfcPort?.PortName == null) return;

            vmCts = new CancellationTokenSource();
            var token = vmCts.Token;

            try
            {
                // ポートオープン
                if (!IsMfcPortOpened)
                {                    
                    if (MfcPort != null)
                    {
                        // ポートボタンを無効化する
                        IsMfcPortbtnEnable = false;

                        var isSucceed = await MfcSerialService.Instance.Connect(MfcPort, token);

                        // MFCステータスの読み出しと初期値表示
                        if (isSucceed)
                        {
                            await UpdateMFCStatus(token);

                            // ダブルチェック
                            if (MfcSerialService.Instance.Port != null && MfcSerialService.Instance.Port.IsOpen)
                            {
                                DeviceNum = MfcSerialService.Instance.GetDeviceNumber();
                                IsMfcPortOpened = true;

                                // ポート名とフレンドリ名のセット
                                MfcPortWithName = MfcPort.PortName + " - " + MfcPort.FriendlyName;

                                // ポート選択不可能にする
                                IsMfcPortListEnabled = false;

                                // サービスに通知
                                commStatusService.IsMfcConnected = true;
                            }
                        }
                        else
                        {
                            await messageService.ShowMessage("Failed to open the port.");
                            IsMfcPortOpened = false;
                            // 3sec : シリアルポート情報更新などの安定待ち
                            // Open状態で抜き差し直後にClose→Openを連打することでnull参照
                            // さらに原因不明のポートクローズ不可能な状態となる。
                            // 0.5sec程度では安定しないため3sec連打防止させる
                            await Task.Delay(3000);
                            await messageService.CloseWithFade();
                        }

                        // ポートボタンを有効化する
                        IsMfcPortbtnEnable = true;
                    }
                }
                // ポートクローズ
                else
                {                    
                    if (MfcPort != null)
                    {
                        // ポートボタンを無効化する
                        IsMfcPortbtnEnable = false;

                        MfcSerialService.Instance.Disconnect();
                        await UpdateMFCStatus(token);
                        IsMfcPortOpened = false;

                        // デバイス番号とポート名・フレンドリ名の初期化
                        DeviceNum = "";
                        MfcPortWithName = "";

                        // ポート選択可能にする
                        IsMfcPortListEnabled = true;

                        // ポートボタンを有効化する
                        IsMfcPortbtnEnable = true;

                        // サービスに通知
                        commStatusService.IsMfcConnected = false;
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// MFCステータスの更新
        /// </summary>
        /// <returns></returns>
        private async Task UpdateMFCStatus(CancellationToken token)
        {
            if(MfcSerialService.Instance.Port == null)
            {
                DeviceNum = "";
                AlermAOption = "";
                AlermBOption = "";
                ADOption = "";
                ValveOption = "";
                SFOption = "";
                CmodeOption = "";

                return;
            }

            if (MfcSerialService.Instance.Port.IsOpen)
            {
                // MFCステータスの読み出し
                var result = await mfcService.RequestType2Async("ST", token);
                if (result != null)
                {
                    if (result.IsSuccess)
                    {
                        var status = result.Message;
                        status = status[3..];
                        if (status.Length > 3)
                        {
                            DeviceNum = MfcSerialService.Instance.GetDeviceNumber();
                            AlermAOption = status.Substring(0, 1);
                            AlermBOption = status.Substring(1, 1);
                            ADOption = status.Substring(2, 1);
                            ValveOption = status.Substring(3, 1);
                            SFOption = status.Substring(4, 1);
                            CmodeOption = status.Substring(5, 1);
                        }
                        else
                        {
                            DeviceNum = "";
                            AlermAOption = "";
                            AlermBOption = "";
                            ADOption = "";
                            ValveOption = "";
                            SFOption = "";
                            CmodeOption = "";
                        }
                    }
                    else
                    {
                        await messageService.ShowMessage(result.Message);
                        await messageService.CloseWithFade();
                    }
                }
            }
            else
            {
                DeviceNum = "";
                AlermAOption = "";
                AlermBOption = "";
                ADOption = "";
                ValveOption = "";
                SFOption = "";
                CmodeOption = "";
            }
        }

        // MFCコマンドタイプ1(W) 返信なし
        [RelayCommand]
        private async Task CommMFCAsyncType1(CancellationToken token)
        {
            if (mfcService.Port == null || !mfcService.Port.IsOpen)
            {
                return;
            }

            if (CommMFCType1Command == "")
            {
                return;
            }

            await mfcService.RequestType1Async(CommMFCType1Command, token);
        }

        public IAsyncRelayCommand commMFCAsyncType1WithCommand { get; }

        /// <summary>
        /// MFCコマンドタイプ1(W)　返信なし
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        [RelayCommand]
        private async Task CommMFCAsyncType1WithCommand(string? cmd, CancellationToken token)
        {
            if (mfcService.Port == null || !mfcService.Port.IsOpen)
            {
                return;
            }

            if (cmd == "")
            {
                return;
            }

            var result = await mfcService.RequestType1Async(cmd, token);
            if (result != null)
            {
                if (result.IsSuccess)
                {
                    await UpdateMFCStatus(token);
                }
                else
                {
                    await messageService.ShowMessage(result.Message);
                    await messageService.CloseWithFade();
                }
            }
        }

        /// <summary>
        /// MFCコマンドタイプ2(R)　返信あり
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task CommMFCAsyncType2(CancellationToken token)
        {
            if (mfcService.Port == null || !mfcService.Port.IsOpen)
            { 
                return;
            }

            if(CommMFCType2Command == "")
            {
                return;
            }

            var result = await mfcService.RequestType2Async(CommMFCType2Command, token);
            if (result != null)
            {
                if (result.IsSuccess)
                {
                    CommMFCType2Result = result.Message;
                }
                else
                {
                    await messageService.ShowMessage(result.Message);
                    await messageService.CloseWithFade();
                    return;
                }
            }
        }

        /// <summary>
        /// MFCコマンドタイプ3(W)　AK返信あり、エコーあり
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task CommMFCAsyncType3(CancellationToken token)
        {
            if (mfcService.Port == null || !mfcService.Port.IsOpen)
            {
                return;
            }

            if (CommMFCType3Command1 == "" || CommMFCType3Command2 == "")
            {
                return;
            }

            var result = await mfcService.RequestType3Async(CommMFCType3Command1, CommMFCType3Command2, token);
            if (result != null)
            {
                if (result.IsSuccess)
                {
                    CommMFCType3Result = result.Message;
                }
                else
                {
                    await messageService.ShowMessage(result.Message);
                    await messageService.CloseWithFade();
                    return;
                }
            }
        }

        [RelayCommand]
        /// <summary>
        /// 天秤のポートオープン
        /// </summary>
        private async void ControlBalancePort()
        {
            // ポート番号が設定されていなければ中断
            if (BalancePort?.PortName == null) return;

            vmCts = new CancellationTokenSource();
            var token = vmCts.Token;

            try
            {
                // ポートオープン
                if (!IsBalancePortOpened)
                {
                    if (BalancePort != null)
                    {
                        // ポートボタンを無効化する
                        IsBalancePortbtnEnable = false;

                        await BalanceSerialService.Instance.Connect(BalancePort, token);
                        // 天秤通信アイコン表示条件2：ポートオープン状態であること

                        // ダブルチェック
                        if(BalanceSerialService.Instance.Port != null &&BalanceSerialService.Instance.Port.IsOpen)
                        {
                            IsBalancePortOpened = true;

                            // ポート名とフレンドリ名のセット
                            BalancePortWithName = BalancePort.PortName + " - " + BalancePort.FriendlyName;

                            // ポート選択不可能にする
                            IsBalancePortListEnabled = false;

                            // サービスに通知
                            commStatusService.IsBalanceConnected = true;
                        }
                        else
                        {
                            await messageService.ShowMessage("Failed to open the port.");
                            IsBalancePortOpened = false;
                            await Task.Delay(3000);
                            await messageService.CloseWithFade();
                        }

                        // ポートボタンを有効化する
                        IsBalancePortbtnEnable = true;
                    }
                }
                // ポートクローズ
                else
                {
                    if (BalancePort != null)
                    {
                        // ポートボタンを無効化する
                        IsBalancePortbtnEnable = false;

                        BalanceSerialService.Instance.Disconnect();
                        IsBalancePortOpened = false;

                        // ポート名・フレンドリ名を初期化する
                        BalancePortWithName = "";

                        // ポート選択可能にする
                        IsBalancePortListEnabled = true;

                        // ポートボタンを有効化する
                        IsBalancePortbtnEnable = true;

                        // サービスに通知
                        commStatusService.IsBalanceConnected = false;
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// 天秤との通信
        /// </summary>
        /// <returns></returns>
        private async Task CommBalanceAsyncCommand(CancellationToken token)
        {
            if (BalanceSerialService.Instance.Port == null|| !BalanceSerialService.Instance.Port.IsOpen) return;

            var result = await BalanceSerialService.Instance.RequestWeightAsync(token);
            if (result != null)
            {
                if (result.IsSuccess)
                {
                    CommBalanceValue = result.Message;
                }
                else
                {
                    await messageService.ShowMessage(result.Message);
                    await messageService.CloseWithFade();
                }
            }
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
        private async void MessageShowAdded()
        {
            await messageService.ShowMessage("Port reloading...");
        }

        /// <summary>
        /// シリアルポートUSBケーブルが抜かれた場合のメッセージ表示処理
        /// </summary>
        private async void MessageShowRemoved()
        {
            await messageService.ShowMessage("Port disconnected...");
        }

        private void NoChangeDetected()
        {
            vmCts = new CancellationTokenSource();
            DeleteDialog(vmCts.Token);
        }

        /// <summary>
        /// ポート抜き差しするも変化が無い場合にダイアログを消去する
        /// </summary>
        private async void DeleteDialog(CancellationToken token)
        {
            // ポート選択可能にする
            IsMfcPortListEnabled = true;
            IsBalancePortListEnabled = true;

            MfcSerialService.Instance.Disconnect();
            IsMfcPortOpened = false;
            UpdateMfcPortOpenBtnText();
            OnMfcConnectedChanged();

            BalanceSerialService.Instance.Disconnect();
            IsBalancePortOpened = false;
            UpdateBalancePortOpenBtnText();
            OnBalanceConnectedChanged();
            await UpdateMFCStatus(token);
            await messageService.CloseWithFade();
        }

        /// <summary>
        /// ポート差し込みイベントハンドラ（非同期）
        /// </summary>
        private async void OnPortAdded(string portName)
        {
            // ポート選択可能にする
            IsMfcPortListEnabled = true;
            IsBalancePortListEnabled = true;

            // 接続対象名の削除
            MfcPortWithName = "";
            BalancePortWithName = "";

            // 安定化のため短時間待つ（WMI と併用する場合に有効）
            await Task.Delay(delayTime);

            App.Current.Dispatcher.Invoke(() =>
            {
                RefreshAvailablePorts();
            });

            vmCts = new CancellationTokenSource();
            DeleteDialog(vmCts.Token);

            // ポートボタンを有効化する
            IsMfcPortbtnEnable = true;
            IsBalancePortbtnEnable = true;

            // サービスに通知
            commStatusService.IsMfcConnected = false;
            commStatusService.IsBalanceConnected = false;
        }

        /// <summary>
        /// ポート抜き出しイベントハンドラ（非同期）
        /// </summary>
        private async void OnPortRemoved(string portName)
        {
            IsMfcPortSelected = false;
            IsMfcPortOpened = false;
            IsBalancePortSelected = false;
            IsBalancePortOpened = false;
            // ポート選択可能にする
            IsMfcPortListEnabled = true;
            IsBalancePortListEnabled = true;

            // 接続対象名の削除
            MfcPortWithName = "";
            BalancePortWithName = "";

            // 接続解除
            MfcSerialService.Instance.Disconnect();
            //IsMfcPortSelected = false;
            //IsMfcPortOpened = false;
            UpdateMfcPortOpenBtnText();
            OnMfcConnectedChanged();

            BalanceSerialService.Instance.Disconnect();
            //IsBalancePortSelected = false;
            //IsBalancePortOpened = false;
            UpdateBalancePortOpenBtnText();
            OnBalanceConnectedChanged();

            vmCts = new CancellationTokenSource();
            await UpdateMFCStatus(vmCts.Token);

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

            await messageService.CloseWithFade();

            // ポートボタンを有効化する
            IsMfcPortbtnEnable = true;
            IsBalancePortbtnEnable = true;

            // サービスに通知
            commStatusService.IsMfcConnected = false;
            commStatusService.IsBalanceConnected = false;
        }

        /// <summary>
        /// アプリケーション終了の検知
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppStatusService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ApplicationStatusService.IsQuit))
            {
                if (appStatusService.IsQuit)
                {
                    Dispose();
                }
            }
        }


        /// <summary>
        /// イベント削除
        /// </summary>
        public void StopPortWatcher()
        {
            portWatcher.noChangeDetected -= NoChangeDetected;
            portWatcher.messageShowAdded -= MessageShowAdded;
            portWatcher.messageShowRemoved -= MessageShowRemoved;
            portWatcher.PortAdded -= OnPortAdded;
            portWatcher.PortRemoved -= OnPortRemoved;
        }
    }
}
