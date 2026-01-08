using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MVVM_Base.ViewModel
{
    public partial class vmLinear : ObservableObject, IViewModel
    {
        #region サービスインスタンス
        private readonly IMFCSerialService mfcService;
        private readonly ThemeService themeService;
        private readonly CommStatusService commStatusService;
        private readonly ViewModelManagerService vmService;
        private readonly ApplicationStatusService appStatusService;
        private readonly HighPrecisionTimer precisionTimer;
        private readonly IMessageService messageService;
        #endregion

        #region 測定設定
        /// <summary>
        /// 設定流量
        /// </summary>
        private string flowValue = "200";
        public string FlowValue
        {
            get => flowValue;
            set
            {
                if (flowValue != value)
                {
                    flowValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 測定インターバル
        /// </summary>
        private string intervalValue = "5";
        public string IntervalValue
        {
            get => intervalValue;
            set
            {
                if (intervalValue != value)
                {
                    intervalValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 測定回数
        /// </summary>
        private string attemptsValue = "10";
        public string AttemptsValue
        {
            get => attemptsValue;
            set
            {
                if (attemptsValue != value)
                {
                    attemptsValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// オーバーシュート安定時間
        /// </summary>
        private string stableOSValue = "10";
        public string StableOSValue
        {
            get => stableOSValue;
            set
            {
                if (stableOSValue != value)
                {
                    stableOSValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// オーバーシュート後待ち時間
        /// </summary>
        private string waitOSValue = "5";
        public string WaitOSValue
        {
            get => waitOSValue;
            set
            {
                if (waitOSValue != value)
                {
                    waitOSValue = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region サービスからの通知対応プロパティ

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

        /// <summary>
        /// MFCシリアルナンバー
        /// </summary>
        private string serialNum;
        public string SerialNum
        {
            get => serialNum;
            set
            {
                if (serialNum != value)
                {
                    serialNum = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region サイズ変数
        /// <summary>
        /// グループボックス幅 100
        /// </summary>
        private double _groupBoxWidth100 = 100;
        public double GroupBoxWidth100
        {
            get => _groupBoxWidth100;
            set
            {
                _groupBoxWidth100 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// グループボックス幅 150
        /// </summary>
        private double _groupBoxWidth150 = 150;
        public double GroupBoxWidth150
        {
            get => _groupBoxWidth150;
            set
            {
                _groupBoxWidth150 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// グループボックス幅 200
        /// </summary>
        private double _groupBoxWidth200 = 235;
        public double GroupBoxWidth200
        {
            get => _groupBoxWidth200;
            set
            {
                _groupBoxWidth200 = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// 小グループボックス幅
        /// </summary>
        private double _smallGBWidth = 280;
        public double SmallGBWidth
        {
            get => _smallGBWidth;
            set
            {
                _smallGBWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// グループボックス幅 390
        /// </summary>
        private double _groupBoxWidth295 = 295;
        public double GroupBoxWidth295
        {
            get => _groupBoxWidth295;
            set
            {
                _groupBoxWidth295 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// グループボックス幅 390
        /// </summary>
        private double _groupBoxWidth325 = 325;
        public double GroupBoxWidth325
        {
            get => _groupBoxWidth325;
            set
            {
                _groupBoxWidth325 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// グループボックス幅 340
        /// </summary>
        private double _groupBoxWidth245 = 245;
        public double GroupBoxWidth245
        {
            get => _groupBoxWidth245;
            set
            {
                _groupBoxWidth245 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// グループボックス幅 390
        /// </summary>
        private double _groupBoxWidth390 = 440;
        public double GroupBoxWidth390
        {
            get => _groupBoxWidth390;
            set
            {
                _groupBoxWidth390 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// グループボックス幅 500
        /// </summary>
        private double _groupBoxWidth500 = 575;
        public double GroupBoxWidth500
        {
            get => _groupBoxWidth500;
            set
            {
                _groupBoxWidth500 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// グループボックス幅 750
        /// </summary>
        private double _groupBoxWidth700 = 730;
        public double GroupBoxWidth700
        {
            get => _groupBoxWidth700;
            set
            {
                _groupBoxWidth700 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// グループボックス幅 750
        /// </summary>
        private double _groupBoxHeight250 = 250;
        public double GroupBoxHeight250
        {
            get => _groupBoxHeight250;
            set
            {
                _groupBoxHeight250 = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 中グループボックス幅
        /// </summary>
        private double _middleGBWidth = 385;
        public double MiddleGBWidth
        {
            get => _middleGBWidth;
            set
            {
                _middleGBWidth = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 中グループボックス幅
        /// </summary>
        private double _largeGBWidth = 500;
        public double LargeGBWidth
        {
            get => _largeGBWidth;
            set
            {
                _largeGBWidth = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// 小グループボックス高さ
        /// </summary>
        private double _smallGBHeight = 200;
        public double SmallGBHeight
        {
            get => _smallGBHeight;
            set
            {
                _smallGBHeight = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 中グループボックス高さ
        /// </summary>
        private double _middleGBHeight = 200;
        public double MiddleGBHeight
        {
            get => _middleGBHeight;
            set
            {
                _middleGBHeight = value;
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
        /// 10点リニア補正グリッドフォントサイズ
        /// </summary>
        private double dataGridFontSize = 16;
        public double DataGridFontSize
        {
            get => dataGridFontSize;
            set
            {
                dataGridFontSize = value;
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
        /// MFMボタンのサイズ
        /// </summary>
        private int cmdBtnSIze = 100;
        public int CmdBtnSIze
        {
            get => cmdBtnSIze;
            set
            {
                if (cmdBtnSIze != value)
                {
                    cmdBtnSIze = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// MFMボタンのサイズ
        /// </summary>
        private int mfmBtnSIze = 80;
        public int MfmBtnSIze
        {
            get => mfmBtnSIze;
            set
            {
                if (mfmBtnSIze != value)
                {
                    mfmBtnSIze = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ZeroAdjustボタンのサイズ
        /// </summary>
        private int zaBtnSIze = 100;
        public int ZaBtnSIze
        {
            get => zaBtnSIze;
            set
            {
                if (zaBtnSIze != value)
                {
                    zaBtnSIze = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        ///　スパン調整ゲイン増減ボタンのサイズ
        /// </summary>
        private int spnaGainSIze = 40;
        public int SpnaGainSIze
        {
            get => spnaGainSIze;
            set
            {
                if (spnaGainSIze != value)
                {
                    spnaGainSIze = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region UIテキスト
        /// <summary>
        /// MFMボタンのテキスト
        /// </summary>
        private string mfmBtnText = "Execute";
        public string MfmBtnText
        {
            get => mfmBtnText;
            set
            {
                if (mfmBtnText != value)
                {
                    mfmBtnText = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ゼロ調整　ZS送信ボタンのテキスト
        /// </summary>
        private string zStext = "Zero Send";
        public string ZStext
        {
            get => zStext;
            set
            {
                if (zStext != value)
                {
                    zStext = value;
                }
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 流量出力値
        /// </summary>
        private string flowOut;
        public string FlowOut
        {
            get => flowOut;
            set
            {
                if (flowOut != value)
                {
                    flowOut = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Span合わせ スパン調整ゲイン下位バイトの値
        /// </summary>
        private string fb41Val;
        public string Fb41Val
        {
            get => fb41Val;
            set
            {
                if (fb41Val != value)
                {
                    fb41Val = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Span合わせ スパン調整ゲイン上位バイトの値
        /// </summary>
        private string fb42Val;
        public string Fb42Val
        {
            get => fb42Val;
            set
            {
                if (fb42Val != value)
                {
                    fb42Val = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region ボタン押下可否フラグ
        /// <summary>
        /// MFMボタンの押下可否
        /// </summary>
        private bool canMFM = true;
        public bool CanMFM
        {
            get => canMFM;
            set
            {
                if (canMFM != value)
                {
                    canMFM = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// MFM後に実行可能なコマンド
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class CanAfterMFMAttribute : Attribute
        {
            public string Code { get; }
            public CanAfterMFMAttribute(string code)
            {
                Code = code;
            }
        }

        /// <summary>
        /// Calculateボタンの押下可否
        /// </summary>
        private bool canCalculate = true;
        [CanAfterMFMAttribute("Calc")]
        public bool CanCalculate
        {
            get => canCalculate;
            set
            {
                if (canCalculate != value)
                {
                    canCalculate = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Calculateボタンの押下可否
        /// </summary>
        private bool canCalAndConf = true;
        [CanAfterMFMAttribute("CalConf")]
        public bool CanCalAndConf
        {
            get => canCalAndConf;
            set
            {
                if (canCalAndConf != value)
                {
                    canCalAndConf = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// MFMせずに実行可能なコマンド
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class CanBeforeMFMAttribute : Attribute
        {
            public string Code { get; }
            public CanBeforeMFMAttribute(string code)
            {
                Code = code;
            }
        }

        /// <summary>
        /// Calculateボタンの押下可否
        /// </summary>
        private bool canConform = true;
        [CanBeforeMFMAttribute("Conf")]
        public bool CanConform
        {
            get => canConform;
            set
            {
                if (canConform != value)
                {
                    canConform = value;
                    OnPropertyChanged();
                }
            }
        }



        /// <summary>
        /// Calculateボタンの押下可否
        /// </summary>
        private bool canManual = true;
        [CanBeforeMFMAttribute("Manual")]
        public bool CanManual
        {
            get => canManual;
            set
            {
                if (canManual != value)
                {
                    canManual = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ゼロ調整関連
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class CanZeroAttribute : Attribute
        {
            public string Code { get; }
            public CanZeroAttribute(string code)
            {
                Code = code;
            }
        }

        /// <summary>
        /// Zero Sendボタンの押下可否
        /// </summary>
        private bool canZeroSend = true;
        [CanZeroAttribute("ZeroSend")]
        public bool CanZeroSend
        {
            get => canZeroSend;
            set
            {
                if (canZeroSend != value)
                {
                    canZeroSend = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Zero OKボタンの押下可否
        /// </summary>
        private bool canZeroOK = true;
        [CanZeroAttribute("ZeroOK")]
        public bool CanZeroOK
        {
            get => canZeroOK;
            set
            {
                if (canZeroOK != value)
                {
                    canZeroOK = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Span合わせ関連
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class CanSpanAttribute : Attribute
        {
            public string Code { get; }
            public CanSpanAttribute(string code)
            {
                Code = code;
            }
        }
        /// <summary>
        /// Zero OKボタンの押下可否
        /// </summary>
        private bool canSpanAdjust = true;
        [CanSpanAttribute("CanSpanAdjust")]
        public bool CanSpanAdjust
        {
            get => canSpanAdjust;
            set
            {
                if (canSpanAdjust != value)
                {
                    canSpanAdjust = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region FBデータ

        [AttributeUsage(AttributeTargets.Property)]
        public class FbCodeAttribute : Attribute
        {
            public string Code { get; }
            public FbCodeAttribute(string code)
            {
                Code = code;
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb90;
        [FbCode("FB90")]
        public string Fb90
        {
            get => fb90;
            set
            {
                if (fb90 != value)
                {
                    fb90 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb91;
        [FbCode("FB91")]
        public string Fb91
        {
            get => fb91;
            set
            {
                if (fb91 != value)
                {
                    fb91 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb92;
        [FbCode("FB92")]
        public string Fb92
        {
            get => fb92;
            set
            {
                if (fb92 != value)
                {
                    fb92 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb93;
        [FbCode("FB93")]
        public string Fb93
        {
            get => fb93;
            set
            {
                if (fb93 != value)
                {
                    fb93 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb94;
        [FbCode("FB94")]
        public string Fb94
        {
            get => fb94;
            set
            {
                if (fb94 != value)
                {
                    fb94 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb95;
        [FbCode("FB95")]
        public string Fb95
        {
            get => fb95;
            set
            {
                if (fb95 != value)
                {
                    fb95 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb96;
        [FbCode("FB96")]
        public string Fb96
        {
            get => fb96;
            set
            {
                if (fb96 != value)
                {
                    fb96 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb97;
        [FbCode("FB97")]
        public string Fb97
        {
            get => fb97;
            set
            {
                if (fb97 != value)
                {
                    fb97 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb98;
        [FbCode("FB98")]
        public string Fb98
        {
            get => fb98;
            set
            {
                if (fb98 != value)
                {
                    fb98 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb99;
        [FbCode("FB99")]
        public string Fb99
        {
            get => fb99;
            set
            {
                if (fb99 != value)
                {
                    fb99 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb9a;
        [FbCode("FB9A")]
        public string Fb9a
        {
            get => fb9a;
            set
            {
                if (fb9a != value)
                {
                    fb9a = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb9b;
        [FbCode("FB9B")]
        public string Fb9b
        {
            get => fb9b;
            set
            {
                if (fb9b != value)
                {
                    fb9b = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb9c;
        [FbCode("FB9C")]
        public string Fb9c
        {
            get => fb9c;
            set
            {
                if (fb9c != value)
                {
                    fb9c = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb9d;
        [FbCode("FB9D")]
        public string Fb9d
        {
            get => fb9d;
            set
            {
                if (fb9d != value)
                {
                    fb9d = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb9e;
        [FbCode("FB9E")]
        public string Fb9e
        {
            get => fb9e;
            set
            {
                if (fb9e != value)
                {
                    fb9e = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb9f;
        [FbCode("FB9F")]
        public string Fb9f
        {
            get => fb9f;
            set
            {
                if (fb9f != value)
                {
                    fb9f = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fba0;
        [FbCode("FBA0")]
        public string Fba0
        {
            get => fba0;
            set
            {
                if (fba0 != value)
                {
                    fba0 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fba1;
        [FbCode("FBA1")]
        public string Fba1
        {
            get => fba1;
            set
            {
                if (fba1 != value)
                {
                    fba1 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fba2;
        [FbCode("FBA2")]
        public string Fba2
        {
            get => fba2;
            set
            {
                if (fba2 != value)
                {
                    fba2 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fba3;
        [FbCode("FBA3")]
        public string Fba3
        {
            get => fba3;
            set
            {
                if (fba3 != value)
                {
                    fba3 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb41;
        [FbCode("FB41")]
        public string Fb41
        {
            get => fb41;
            set
            {
                if (fb41 != value)
                {
                    fb41 = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb42;
        [FbCode("FB42")]
        public string Fb42
        {
            get => fb42;
            set
            {
                if (fb42 != value)
                {
                    fb42 = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Version, BP, 5%刻み設定
        /// <summary>
        /// バーション値
        /// </summary>
        public enum VersionType
        {
            Ver400,
            Ver298
        }
        private VersionType versionValue = VersionType.Ver400;
        public VersionType VersionValue
        {
            get => versionValue;
            set
            {
                if (versionValue != value)
                {
                    versionValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// BP値
        /// </summary>
        public enum BPType
        {
            Variable,
            Invariable
        }
        private BPType bPValue = BPType.Invariable;
        public BPType BPValue
        {
            get => bPValue;
            set
            {
                if (bPValue != value)
                {
                    bPValue = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Increment値
        /// </summary>
        public enum IncerementType
        {
            Normal,
            FivePercent
        }
        private IncerementType incrementValue = IncerementType.Normal;
        public IncerementType IncrementValue
        {
            get => incrementValue;
            set
            {
                if (incrementValue != value)
                {
                    incrementValue = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region ViewModelManagerServiceの管理プロパティ
        /// <summary>
        /// 終了可否
        /// </summary>
        public bool canQuit { get; set; }

        /// <summary>
        /// 画面遷移可否
        /// </summary>
        public bool canTransitOther { get; set; }
        #endregion

        #region UI操作可否フラグ

        /// <summary>
        /// 流量設定の変更可否
        /// </summary>
        private bool flowEnable = true;
        public bool FlowEnable
        {
            get => flowEnable;
            set
            {
                if (flowEnable != value)
                {
                    flowEnable = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 測定設定の変更可否
        /// </summary>
        private bool mSettingEnable = true;
        public bool MSettingEnable
        {
            get => mSettingEnable;
            set
            {
                if (mSettingEnable != value)
                {
                    mSettingEnable = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ラジオボタンの押下可否
        /// </summary>
        private bool rBtnEnable = true;
        public bool RBtnEnable
        {
            get => rBtnEnable;
            set
            {
                if (rBtnEnable != value)
                {
                    rBtnEnable = value;
                    OnPropertyChanged();
                }
            }
        }



        #endregion

        #region MFM関連フラグ
        /// <summary>
        /// MFM中か否か
        /// </summary>
        private bool isMfmStarted = false;
        public bool IsMfmStarted
        {
            get => isMfmStarted;
            set
            {
                if (isMfmStarted != value)
                {
                    isMfmStarted = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Zero Sendボタンが押下されたか
        /// </summary>
        private bool isZeroSend = false;

        /// <summary>
        /// Zero OKが押下されたか
        /// </summary>
        private bool isZeroOK = false;

        /// <summary>
        /// Span OKが押下されたか
        /// </summary>
        private bool isSpanOK = false;
        #endregion

        /// <summary>
        /// 拡縮回数
        /// </summary>
        private int tcnt = 0;
        public bool IsViewVisible { get; private set; }

        private int messageFadeTime = 2000;

        /// <summary>
        /// 計測結果の構成要素
        /// </summary>

        public ObservableCollection<MeasureResult> Column0 { get; set; }
        public ObservableCollection<MeasureResult> Column1 { get; set; }

        /// <summary>
        /// 天秤との通信回数
        /// </summary>
        private long cntBalCom = 0;

        /// <summary>
        /// 天秤からの最後の応答値
        /// </summary>
        private float lastBalanceVal = 0;

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

        public ObservableCollection<string> SetPointArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> TrueValueArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> ReadingValueArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> InitialVoArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> CorrectDataArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> VoutArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> VOArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> ConfirmArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        private CancellationTokenSource? _loadCts;
        private CancellationTokenSource? _mfmCts;
        private CancellationTokenSource? _calculateCts;

        private bool _isMfmProccessing;
        public bool IsMfmProccessing
        {
            get => _isMfmProccessing;
            set => SetProperty(ref _isMfmProccessing, value);
        }

        private bool _isCalculating;
        public bool IsCalculating
        {
            get => _isCalculating;
            set => SetProperty(ref _isCalculating, value);
        }

        public class MeasureResult : INotifyPropertyChanged
        {
            private string _value;
            public string Value
            {
                get => _value;
                set
                {
                    _value = value;
                    OnPropertyChanged();
                }
            }

            private bool isUpdate;
            public bool IsUpdate
            {
                get => isUpdate;
                set
                {
                    isUpdate = value;
                    OnPropertyChanged();
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string? name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
