using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MVVM_Base.ViewModel.vmLinear;

namespace MVVM_Base.ViewModel
{
    public partial class vmBalw : ObservableObject, IViewModel
    {
        #region サービスインスタンス
        private readonly IMFCSerialService mfcService;
        private readonly ThemeService themeService;
        private readonly CommStatusService commStatusService;
        private readonly ViewModelManagerService vmService;
        private readonly ApplicationStatusService appStatusService;
        private readonly HighPrecisionTimer precisionTimer;
        private readonly IMessageService messageService;
        private readonly LanguageService languageService;
        private readonly IdentifierService identifierService;
        #endregion

        #region ViewModelManagerServiceの管理プロパティ
        /// <summary>
        /// 終了可否
        /// </summary>
        public bool canQuit { get; set; }

        #endregion

        #region 通知対応プロパティ

        /// <summary>
        /// MFC接続状態
        /// </summary>
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

        /// <summary>
        /// 天秤接続状態
        /// </summary>
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
                }
            }
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

        #region サイズ変数
        /// <summary>
        /// 通信可視化ボックスの幅サイズ
        /// </summary>
        private double commBoxWidth = 150;
        public double CommBoxWidth
        {
            get => commBoxWidth;
            set
            {
                commBoxWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 通信可視化ボックスの高さサイズ
        /// </summary>
        private double commBoxHeight = 130;
        public double CommBoxHeight
        {
            get => commBoxHeight;
            set
            {
                commBoxHeight = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// セルサイズ
        /// </summary>
        private double celWidth = 80;
        public double CelWidth
        {
            get => celWidth;
            set
            {
                celWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Markボックスサイズ
        /// </summary>
        private double markWidth = 70;
        public double MarkWidth
        {
            get => markWidth;
            set
            {
                markWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 読み取り値ボックスサイズ
        /// </summary>
        private double  readWidth = 70;
        public double ReadWidth
        {
            get => readWidth;
            set
            {
                readWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// タイマー設定ボックスの幅サイズ
        /// </summary>
        private double timerSettingBoxWidth = 450;
        public double TimerSettingBoxWidth
        {
            get => timerSettingBoxWidth;
            set
            {
                timerSettingBoxWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///　他設定高さ
        /// </summary>
        private float unitTextboxWidth = 100;
        public float UnitTextboxWidth
        {
            get => unitTextboxWidth;
            set
            {
                if (unitTextboxWidth != value)
                {
                    unitTextboxWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 出力ボックスの幅サイズ
        /// </summary>
        private double outputBoxWidth = 605;
        public double OutputBoxWidth
        {
            get => outputBoxWidth;
            set
            {
                outputBoxWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 出力ボックスの高さサイズ
        /// </summary>
        private double outputBoxHeight = 520;
        public double OutputBoxHeight
        {
            get => outputBoxHeight;
            set
            {
                outputBoxHeight = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 測定結果ボックスの幅サイズ
        /// </summary>
        private double measureBoxWidth = 200;
        public double MeasureBoxWidth
        {
            get => measureBoxWidth;
            set
            {
                measureBoxWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 測定結果ボックスの高さサイズ
        /// </summary>
        private double measureBoxHeight = 390;
        public double MeasureBoxHeight
        {
            get => measureBoxHeight;
            set
            {
                measureBoxHeight = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Logボックスの幅サイズ
        /// </summary>
        private double logBoxWidth = 350;
        public double LogBoxWidth
        {
            get => logBoxWidth;
            set
            {
                logBoxWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Logボックスの高さサイズ
        /// </summary>
        private double logBoxHeight = 390;
        public double LogBoxHeight
        {
            get => logBoxHeight;
            set
            {
                logBoxHeight = value;
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
        /// 単位フォントサイズ
        /// </summary>
        private double unitFontSize = 14;
        public double UnitFontSize
        {
            get => unitFontSize;
            set
            {
                unitFontSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ラベルサイズ
        /// </summary>
        private double labelSize = 12;
        public double LabelSize
        {
            get => labelSize;
            set
            {
                labelSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ログフォントサイズ
        /// </summary>
        private double logFontSize = 12;
        public double LogFontSize
        {
            get => logFontSize;
            set
            {
                logFontSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ボタンフォントサイズ
        /// </summary>
        private double buttonFontSize = 16;
        public double ButtonFontSize
        {
            get => buttonFontSize;
            set
            {
                buttonFontSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ボタン幅
        /// </summary>
        private int mesureBtnSize = 60;
        public int MesureBtnSize
        {
            get => mesureBtnSize;
            set
            {
                mesureBtnSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ボタン幅
        /// </summary>
        private int outputBtnSize = 100;
        public int OutputBtnSize
        {
            get => outputBtnSize;
            set
            {
                outputBtnSize = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Markとテキストボックス間のマージン
        /// </summary>
        private int marginMark = 15;
        public int MarginMark
        {
            get => marginMark;
            set
            {
                marginMark = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// start/stopマージン
        /// </summary>
        private int marginStartStop = 15;
        public int MarginStartStop
        {
            get => marginStartStop;
            set
            {
                marginStartStop = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 読み値マージン
        /// </summary>
        private int marginReading = 15;
        public int MarginReading
        {
            get => marginReading;
            set
            {
                marginReading = value;
                OnPropertyChanged();
            }
        }

        #endregion

        enum ProcessState
        {
            Initial,
            Measurement,
            Exporting
        }

        ProcessState curState = ProcessState.Initial;

        /// <summary>
        /// メッセージのフェードアウト開始までの時間
        /// </summary>
        private int messageFadeTime = 2000;

        /// <summary>
        /// 天秤との通信回数
        /// </summary>
        private long cntBalCom = 0;

        /// <summary>
        /// 天秤からの最後の応答値
        /// </summary>
        private float lastBalanceVal = 0;

        /// <summary>
        /// 測定インターバルの秒数
        /// </summary>
        private string intervalSecValue = "5";
        public string IntervalSecValue
        {
            get => intervalSecValue;
            set
            {
                if (intervalSecValue != value)
                {
                    intervalSecValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 測定インターバルの分数
        /// </summary>
        private string intervalMinValue = "0";
        public string IntervalMinValue
        {
            get => intervalMinValue;
            set
            {
                if (intervalMinValue != value)
                {
                    intervalMinValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Mark
        /// </summary>
        private TimeSpan markValue;
        public TimeSpan MarkValue
        {
            get => markValue;
            set
            {
                markValue = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Measure関連ボタンの押下可否
        /// </summary>
        private bool canMeasure;
        public bool CanMeasure
        {
            get => canMeasure;
            set
            {
                canMeasure = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Exportボタンの押下可否
        /// </summary>
        private bool canExport;
        public bool CanExport
        {
            get => canExport;
            set
            {
                canExport = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// タイマー設定の編集可否
        /// </summary>
        private bool settingEnable;
        public bool SettingEnable
        {
            get => settingEnable;
            set
            {
                settingEnable = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 現在のカラーテーマ
        /// </summary>
        private string colorTheme = "";
        public string ColorTheme
        {
            get => colorTheme;
            private set => colorTheme = value;
        }


        List<string> debugList = new List<string>();

        /// <summary>
        /// 天秤との最後の通信時刻
        /// </summary>
        private DateTime lastUTC;

        /// <summary>
        /// 天秤計測結果の生値[mg]
        /// </summary>
        float[] balNumList = new float[11];

        /// <summary>
        /// 計測タイミング(UTC)のリスト
        /// </summary>
        DateTime[] dateList = new DateTime[11];

        /// <summary>
        /// 計測結果の項目名格納先
        /// </summary>
        public ObservableCollection<MeasureResult> MesurementItems { get; set; }

        /// <summary>
        /// 計測結果の値格納先
        /// </summary>
        public ObservableCollection<MeasureResult> MeasurementValues { get; set; }

        /// <summary>
        /// Calc, Conf処理時
        /// </summary>
        private CancellationTokenSource? _calculateCts;

        /// <summary>
        /// ログ
        /// </summary>
        public ObservableCollection<string> Logs { get; } = new();

        /// <summary>
        /// Stopボタンが押下されたか
        /// </summary>
        private bool isStop = false;

        private string readValue = "0";
        public string ReadValue
        {
            get => readValue;
            set
            {
                if (readValue != value)
                {
                    readValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// UI拡縮回数
        /// </summary>
        private int tcnt = 0;
    }
}
