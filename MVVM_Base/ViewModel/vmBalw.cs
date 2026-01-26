using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using static MVVM_Base.ViewModel.vmBalw;
using static MVVM_Base.ViewModel.vmLinear;

namespace MVVM_Base.ViewModel
{
    public class vmBalw : ObservableObject, IViewModel
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
        /// タイマー設定ボックスの幅サイズ
        /// </summary>
        private double timerSettingBoxWidth = 500;
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
        private double outputBoxWidth = 655;
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
        private double outputBoxHeight = 500;
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
        private double measureBoxHeight = 350;
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
        private double logBoxWidth = 400;
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
        private double logBoxHeight = 350;
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
        /// 計測結果の項目名格納先
        /// </summary>
        public ObservableCollection<MeasureResult> MesurementItems { get; set; }

        /// <summary>
        /// 計測結果の値格納先
        /// </summary>
        public ObservableCollection<MeasureResult> MesurementValues { get; set; }

        public vmBalw(ThemeService _themeService, CommStatusService _commStatusService, IMessageService _messageService,
                        ViewModelManagerService _vmService, ApplicationStatusService _appStatusService, HighPrecisionTimer _precisionTimer)
        {
            //FlowValue = "200";
            mfcService = MfcSerialService.Instance;

            themeService = _themeService;
            themeService.PropertyChanged += ThemeService_PropertyChanged;

            commStatusService = _commStatusService;
            commStatusService.PropertyChanged += CommStatusService_PropertyChanged;

            messageService = _messageService;

            vmService = _vmService;
            vmService.Register(this);

            appStatusService = _appStatusService;
            appStatusService.PropertyChanged += AppStatusService_PropertyChanged;

            precisionTimer = _precisionTimer;

            MesurementItems = new ObservableCollection<MeasureResult>();
            MesurementValues = new ObservableCollection<MeasureResult>();

            // 計測結果の表を形成
            ResetMeasureResult();
        }

        public void Dispose()
        {
            // 終了可否判断
            canQuit = true;

            // 終了可否チェック
            vmService.CheckCanQuit();

            // 
            vmService.CanTransit = true;
        }

        #region 状態変更通知に対応する処理
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
            // View に依存せず ViewModel 内で処理可能
            // 例：内部フラグ更新や別プロパティ更新など
            IsDarkTheme = newTheme == "Dark"; // フラグ例
                                              // 必要であれば PropertyChanged 通知も出す
            OnPropertyChanged(nameof(IsDarkTheme));
        }

        /// <summary>
        /// 通信状態
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommStatusService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CommStatusService.IsMfcConnected))
            {
                // ここで CurrentTheme 変化を検知可能
                OnMfcCommChanged(commStatusService.IsMfcConnected);
            }

            if (e.PropertyName == nameof(CommStatusService.IsBalanceConnected))
            {
                // ここで CurrentTheme 変化を検知可能
                OnBalanceCommChanged(commStatusService.IsBalanceConnected);
            }
        }

        private void OnMfcCommChanged(bool isConnected)
        {
            IsMfcConnected = isConnected;
        }
        private void OnBalanceCommChanged(bool isConnected)
        {
            IsBalanceConnected = isConnected;
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

        #endregion

        /// <summary>
        /// viewロード時に呼ばれる。要イベント登録
        /// </summary>
        public async void OnViewLoaded()
        {

        }

        /// <summary>
        /// viewアンロード時に呼ばれる。要イベント登録
        /// </summary>
        public async void OnViewUnloaded()
        {

        }

        /// <summary>
        /// 計測結果の表をリセットする
        /// </summary>
        private void ResetMeasureResult()
        {
            // 計測結果の表を新規形成
            if (MesurementItems.Count == 0)
            {
                for (int i = 0; i < 11; i++)
                {
                    if (i == 0)
                    {
                        MeasureResult temp1 = new MeasureResult();
                        temp1.Value = "gn5-gn";
                        MesurementItems.Add(temp1);

                        MeasureResult temp2 = new MeasureResult();
                        temp2.Value = ($"");
                        MesurementValues.Add(temp2);

                        continue;
                    }

                    MeasureResult di = new MeasureResult();
                    di.Value = ($"d{i}");
                    MesurementItems.Add(di);

                    MeasureResult m = new MeasureResult();
                    m.Value = ($"");
                    MesurementValues.Add(m);
                }
            }
            // 値を全て初期化
            else
            {
                for (int i = 0; i < 11; i++)
                {
                    MesurementValues[i].Value = "";
                }
            }
        }
    }
}
