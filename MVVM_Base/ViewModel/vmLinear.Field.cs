using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Common;
using MVVM_Base.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

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
        private readonly HighPrecisionTimer precisionTimer2;
        private readonly IMessageService messageService;
        private readonly LanguageService languageService;
        //private readonly HexCheckBehavior hexCheckBehavior;

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

        #region 通知対応プロパティ

        private bool isMfcConnected = false;
        /// <summary>
        /// MFC接続状態
        /// </summary>
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

        private bool isBalanceConnected = false;
        /// <summary>
        /// 天秤接続状態
        /// </summary>
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

        private bool isDarkTheme;
        /// <summary>
        /// ThemeServiceにイベント通知を委任しているので、プロパティ変化の通知は行わない
        /// 行うと二重発火となり、viewEntryでのプロパティ変更イベントが二回発生する。
        /// </summary>
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

        private string serialNum;
        /// <summary>
        /// MFCシリアルナンバー
        /// </summary>
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

        private int confIndex = -1;
        /// <summary>
        /// 押下したConfirmボタンのインデクス
        /// </summary>
        public int ConfIndex
        {
            get => confIndex;
            private set
            {
                if (confIndex != value)
                {
                    confIndex = value;
                    OnPropertyChanged(); // CallerMemberName を使っている場合
                }
            }
        }

        private bool isMapGenerated = false;
        /// <summary>
        /// FBラベルマップが生成されたかどうか。
        /// </summary>
        public bool IsMapGenerated
        {
            get => isMapGenerated;
            set
            {
                if (isMapGenerated != value)
                {
                    isMapGenerated = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool isHexChanged = false;
        /// <summary>
        /// ゲイン値が変更されたか
        /// </summary>
        public bool IsHexChanged
        {
            get => isHexChanged;
            set
            {
                if (isHexChanged != value)
                {
                    isHexChanged = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool isModified;
        /// <summary>
        /// ゲイン値を直接変更したかどうか
        /// </summary>
        public bool IsModified
        {
            get => isModified;
            set
            {
                if (isModified != value)
                {
                    isModified = value;
                    if (!isGainDirectChanged)
                    {
                        isGainDirectChanged = true;
                    }
                    OnPropertyChanged();
                }
            }
        }

        private bool isGainDirectChanged = false;
        /// <summary>
        /// ゲイン値が直接変更された場合はtrue
        /// </summary>
        //public bool IsGainDirectChanged
        //{
        //    get => isGainDirectChanged;
        //    set
        //    {
        //        if (isGainDirectChanged != value)
        //        {
        //            isGainDirectChanged = value;
        //            OnPropertyChanged();
        //        }
        //    }
        //}

        #endregion

        #region サイズ変数
        /// <summary>
        /// グループボックス幅 100
        /// </summary>
        private double _groupBoxWidth90 = 90;
        public double GroupBoxWidth90
        {
            get => _groupBoxWidth90;
            set
            {
                _groupBoxWidth90 = value;
                OnPropertyChanged();
            }
        }

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
        private double _smallGBWidth = 290;
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
        private double _groupBoxWidth245 = 320;
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
        private double _groupBoxHeight250 = 280;
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
        /// Vtemp Intervalフォントサイズ
        /// </summary>
        private float vtFontSize = 13;
        public float VtFontSize
        {
            get => vtFontSize;
            set
            {
                vtFontSize = value;
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
        public int SpanGainSIze
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

        /// <summary>
        ///　出力関連のボタンサイズ
        /// </summary>
        private int outputBtnSIze = 120;
        public int OutputBtnSIze
        {
            get => outputBtnSIze;
            set
            {
                if (outputBtnSIze != value)
                {
                    outputBtnSIze = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///　出力関連のボタンサイズ
        /// </summary>
        private float logFontSize = 12;
        public float LogFontSize
        {
            get => logFontSize;
            set
            {
                if (logFontSize != value)
                {
                    logFontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///　confirmボタン高さ
        /// </summary>
        private float confBtnFontSize = 10;
        public float ConfBtnFontSize
        {
            get => confBtnFontSize;
            set
            {
                if (confBtnFontSize != value)
                {
                    confBtnFontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///　confirmボタン高さ
        /// </summary>
        private float confHeightSize = 18;
        public float ConfHeightSize
        {
            get => confHeightSize;
            set
            {
                if (confHeightSize != value)
                {
                    confHeightSize = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///　ログ高さ
        /// </summary>
        private float logHeightSize = 528;
        public float LogHeightSize
        {
            get => logHeightSize;
            set
            {
                if (logHeightSize != value)
                {
                    logHeightSize = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///　5%刻み高さ
        /// </summary>
        private float fivePerHeightSize = 280;
        public float FivePerHeightSize
        {
            get => fivePerHeightSize;
            set
            {
                if (fivePerHeightSize != value)
                {
                    fivePerHeightSize = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///　他設定高さ
        /// </summary>
        private float otherSettingFontSize = 16;
        public float OtherSettingFontSize
        {
            get => otherSettingFontSize;
            set
            {
                if (otherSettingFontSize != value)
                {
                    otherSettingFontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///　他設定高さ
        /// </summary>
        private float unitTextboxWidth = 120;
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
        ///　他設定高さ
        /// </summary>
        private float measureColumNameWidth = 60;
        public float MeasureColumNameWidth
        {
            get => measureColumNameWidth;
            set
            {
                if (measureColumNameWidth != value)
                {
                    measureColumNameWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///　5%幅
        /// </summary>
        private float fivePerMatrixWidth = 70;
        public float FivePerMatrixWidth
        {
            get => fivePerMatrixWidth;
            set
            {
                if (fivePerMatrixWidth != value)
                {
                    fivePerMatrixWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///　他設定高さ
        /// </summary>
        private float spanInputWidth = 30;
        public float SpanInputWidth
        {
            get => spanInputWidth;
            set
            {
                if (spanInputWidth != value)
                {
                    spanInputWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///　flow出力ボックス幅
        /// </summary>
        private int flowOutputBoxWidth = 100;
        public int FlowOutputBoxWidth
        {
            get => flowOutputBoxWidth;
            set
            {
                if (flowOutputBoxWidth != value)
                {
                    flowOutputBoxWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///　Vtemp出力ボックス幅
        /// </summary>
        private int vTempOutputBoxWidth = 60;
        public int VTempOutputBoxWidth
        {
            get => vTempOutputBoxWidth;
            set
            {
                if (vTempOutputBoxWidth != value)
                {
                    vTempOutputBoxWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        ///　Vtemp出力ボックス幅
        /// </summary>
        private int measureSettingTextBoxWidth = 50;
        public int MeasureSettingTextBoxWidth
        {
            get => measureSettingTextBoxWidth;
            set
            {
                if (measureSettingTextBoxWidth != value)
                {
                    measureSettingTextBoxWidth = value;
                    OnPropertyChanged();
                }
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

        #endregion

        #region UIテキスト
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
        /// MFMボタンの押下可否
        /// </summary>        
        private bool canMFM = true;
        [CanBeforeMFMAttribute("MFM")]
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
        /// Calculateボタンの押下可否
        /// </summary>
        private bool canConfirm = true;
        [CanBeforeMFMAttribute("Conf")]
        public bool CanConfirm
        {
            get => canConfirm;
            set
            {
                if (canConfirm != value)
                {
                    canConfirm = value;
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
        /// 単独Confボタンの押下可否
        /// </summary>
        private bool canConfAlone = true;
        //[CanBeforeMFMAttribute("ConfAlone")]
        public bool CanConfAlone
        {
            get => canConfAlone;
            set
            {
                if (canConfAlone != value)
                {
                    canConfAlone = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// 単独Confボタンの押下可否
        /// </summary>
        private bool canReadWriteGain = true;
        //[CanBeforeMFMAttribute("ConfAlone")]
        public bool CanReadWriteGain
        {
            get => canReadWriteGain;
            set
            {
                if (canReadWriteGain != value)
                {
                    canReadWriteGain = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// csvエクスポートボタンの押下可否
        /// </summary>
        private bool canExport = false;
        //[CanBeforeMFMAttribute("ConfAlone")]
        public bool CanExport
        {
            get => canExport;
            set
            {
                if (canExport != value)
                {
                    canExport = value;
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

        /// <summary>
        /// Calculate Gainボタンの押下可否
        /// </summary>
        private bool canCalcGain = false;
        [CanBeforeMFMAttribute("CanCalcGain")]
        public bool CanCalcGain
        {
            get => canCalcGain;
            set
            {
                if (canCalcGain != value)
                {
                    canCalcGain = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region UI編集可否フラグ
        /// <summary>
        /// Reading Listの編集可否
        /// </summary>
        private bool canEditReadingList = false;

        public bool CanEditReadingList
        {
            get => canEditReadingList;
            set
            {
                if (canEditReadingList != value)
                {
                    canEditReadingList = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gainデータの編集可否
        /// </summary>
        private bool canEditGainData = true;

        public bool CanEditGainData
        {
            get => canEditGainData;
            set
            {
                if (canEditGainData != value)
                {
                    canEditGainData = value;
                    OnPropertyChanged();
                }
            }
        }
        #endregion

        #region FBデータ値保持

        /// <summary>
        /// upper, lower
        /// </summary>
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
        /// upper
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class FbUpperCodeAttribute : Attribute
        {
            public string Code { get; }
            public FbUpperCodeAttribute(string code)
            {
                Code = code;
            }
        }
        /// <summary>
        /// low
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class FbLowerCodeAttribute : Attribute
        {
            public string Code { get; }
            public FbLowerCodeAttribute(string code)
            {
                Code = code;
            }
        }

        /// <summary>
        /// FB90
        /// </summary>
        private string fb90;
        [FbCode("FB90")]
        [FbLowerCode("FB90")]
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
        [FbUpperCode("FB91")]
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
        [FbLowerCode("FB92")]
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
        [FbUpperCode("FB93")]
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
        [FbLowerCode("FB94")]
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
        [FbUpperCode("FB95")]
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
        [FbLowerCode("FB96")]
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
        [FbUpperCode("FB97")]
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
        [FbLowerCode("FB98")]
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
        [FbUpperCode("FB99")]
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
        [FbLowerCode("FB9A")]
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
        [FbUpperCode("FB9B")]
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
        [FbLowerCode("FB9C")]
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
        [FbUpperCode("FB9D")]
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
        [FbLowerCode("FB9E")]
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
        [FbUpperCode("FB9F")]
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
        [FbLowerCode("FBA0")]
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
        [FbUpperCode("FBA1")]
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
        [FbLowerCode("FBA2")]
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
        [FbUpperCode("FBA3")]
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
        [FbLowerCode("FB41")]
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
        [FbUpperCode("FB42")]
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

        #region データラベル

        ///// <summary>
        ///// FBゲイン
        ///// </summary>
        //private readonly Dictionary<string, string> fbMap = new();
        //public Dictionary<string, string> FbMap => fbMap;
        //public string this[string logicalFb]
        //{
        //    get => fbMap.TryGetValue(logicalFb, out var v) ? v : string.Empty;
        //    set
        //    {
        //        fbMap[logicalFb] = value;
        //        OnPropertyChanged($"Item[{logicalFb}]");
        //    }
        //}

        ///// <summary>
        ///// 閾値
        ///// </summary>
        //private readonly Dictionary<string, string> thresholdMap = new();
        //public Dictionary<string, string> ThresholdMap => thresholdMap;
        //public string this[string logicalFb]
        //{
        //    get => thresholdMap.TryGetValue(logicalFb, out var v) ? v : string.Empty;
        //    set
        //    {
        //        thresholdMap[logicalFb] = value;
        //        OnPropertyChanged($"Item[{logicalFb}]");
        //    }
        //}

        /// <summary>
        /// ゲイン・閾値のマップクラス
        /// </summary>
        public class IndexedStringMap : ObservableObject
        {
            private readonly Dictionary<string, string> map = new();

            public int Count => map.Count;
            public IEnumerable<string> Keys => map.Keys;

            public string this[string key]
            {
                get => map.TryGetValue(key, out var v) ? v : string.Empty;
                set
                {
                    map[key] = value;
                    OnPropertyChanged($"Item[{key}]");
                }
            }

            public bool TryGetValue(string key, out string value)
            {
                return map.TryGetValue(key, out value);
            }
        }

        /// <summary>
        /// ゲイン
        /// </summary>
        public IndexedStringMap FbMap { get; } = new();

        /// <summary>
        /// 閾値
        /// </summary>
        public IndexedStringMap ThresholdMap { get; } = new();
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
        /// MFMが完了したかどうか。初期値はfalse
        /// MFMが完了したらtrueに、アプリ起動時およびゲインのCalc後にfalseに変化する。
        /// 解釈としては、既存のゲイン値を初期化して調整・計算し直すための事前処理(初期化)がMFM
        /// </summary>
        private bool isFinishedMFM = false;

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

        enum ProcessState
        {
            Initial,
            MFMStarted,
            ZeroAdjust,
            Span,
            AfterMFM,
            AfterCalc,
            AfterCalcAndConf,
            Measurement,
            Manual,
            FiveperConf,
            Transit
        }

        ProcessState curState = ProcessState.Initial;
        ProcessState lastState = ProcessState.Initial;
        private bool noNeedConfirmUnsaved = false;
        private bool isSavedOutput = false;

        /// <summary>
        /// UI拡縮回数
        /// </summary>
        private int tcnt = 0;

        //public bool IsViewVisible { get; private set; }

        /// <summary>
        /// メッセージのフェードアウト開始までの時間
        /// </summary>
        private int messageFadeTime = 2000;

        /// <summary>
        /// 計測結果の項目名格納先
        /// </summary>
        public ObservableCollection<MeasureResult> MesurementItems { get; set; }

        /// <summary>
        /// 計測結果の値格納先
        /// </summary>
        public ObservableCollection<MeasureResult> MeasurementValues { get; set; }

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

        #region 出力関連

        public ObservableCollection<string> SetPointArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));
        public ObservableCollection<string> SetPointBelow50PercentArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));
        public ObservableCollection<string> SetPointAbove50PercentArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> TrueValueArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public class ReadingValue : INotifyPropertyChanged
        {
            private string? _value;
            public string? Value
            {
                get => _value;
                set
                {
                    if (_value == value) return;
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }

            public event PropertyChangedEventHandler? PropertyChanged;
        }

        public ObservableCollection<ReadingValue> ReadingValueArray { get; }
            = new ObservableCollection<ReadingValue>(Enumerable.Range(0, 11).Select(_ => new ReadingValue()));

        public ObservableCollection<string> ReadingValueBelow50Array { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> ReadingValueAbove50Array { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> InitialVoArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> CorrectDataArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> VoutArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> VOArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

        public ObservableCollection<string> ConfirmArray { get; } = new ObservableCollection<string>(Enumerable.Repeat("", 11));

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


        /// <summary>
        /// vtemp Cal
        /// </summary>
        private string vtempValueCal = "";
        public string VtempValueCal
        {
            get => vtempValueCal;
            set
            {
                if (vtempValueCal != value)
                {
                    vtempValueCal = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// vtemp Conf
        /// </summary>
        private string vtempValueConf = "";
        public string VtempValueConf
        {
            get => vtempValueConf;
            set
            {
                if (vtempValueConf != value)
                {
                    vtempValueConf = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// vtempインターバル
        /// </summary>
        private string vtempInterval = "5";
        public string VtempInterval
        {
            get => vtempInterval;
            set
            {
                if (vtempInterval != value)
                {
                    vtempInterval = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region キャンセレーショントークン
        /// <summary>
        /// ロード時
        /// </summary>
        private CancellationTokenSource? _loadCts;

        /// <summary>
        /// ゲインR/W時
        /// </summary>
        private CancellationTokenSource? _fbRWCts;

        /// <summary>
        /// MFM処理時
        /// </summary>
        private CancellationTokenSource? _mfmCts;

        /// <summary>
        /// Calc, Conf処理時
        /// </summary>
        private CancellationTokenSource? _calculateCts;
        #endregion

        /// <summary>
        /// 現在のカラーテーマ
        /// </summary>
        private string colorTheme = "";
        public string ColorTheme
        {
            get => colorTheme;
            private set => colorTheme = value;
        }

        /// <summary>
        /// ログ
        /// </summary>
        public ObservableCollection<string> Logs { get; } = new();

        /// <summary>
        /// ステータスのラベルフォントサイズ
        /// </summary>
        private double _statusFontSize = 12;
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
        /// ゲイン表セルの幅サイズ
        /// </summary>
        private double gainMatrixWidth = 55;
        public double GainMatrixWidth
        {
            get => gainMatrixWidth;
            set
            {
                gainMatrixWidth = value;
                OnPropertyChanged();
            }
        }

        private double gainMatrixTotalWidth = 607;
        public double GainMatrixTotalWidth
        {
            get => gainMatrixTotalWidth;
            set
            {
                gainMatrixTotalWidth = value;
                OnPropertyChanged();
            }
        }
    }
}
