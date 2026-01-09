using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Model;
using System.ComponentModel;

namespace MVVM_Base.ViewModel
{
    public partial class vmLinear : ObservableObject, IViewModel
    {
        public vmLinear(ThemeService _themeService, CommStatusService _commStatusService, IMessageService _messageService,
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

            Column0 = new System.Collections.ObjectModel.ObservableCollection<MeasureResult>();
            Column1 = new System.Collections.ObjectModel.ObservableCollection<MeasureResult>();

            // 計測結果の表を形成
            ResetMeasureResult();
            ResetOutputResult();
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
        /// 操作状態に応じてUIを管理
        /// </summary>
        /// <param name="state"></param>
        private void ChangeState(ProcessState state)
        {
            SwitchAllbtn(false);
            UIAllFalse();

            switch (state)
            {
                case ProcessState.Initial:
                    {
                        InitialUI();
                        SwitchBeforeMFMBtn(true);
                        break;
                    };
                case ProcessState.MFMStarted:
                    {
                        MfmStart();
                        break;
                    };
                case ProcessState.ZeroAdjust:
                    {
                        CanZeroSend = true;
                        CanZeroOK = true;
                        break;
                    };
                case ProcessState.Span:
                    {
                        CanSpanAdjust = true;
                        break;
                    };
                case ProcessState.AfterMFM:
                    {
                        IsMfmStarted = false;
                        CanMFM = true;
                        FlowEnable = true;
                        MSettingEnable = true;
                        RBtnEnable = true;
                        SwitchBeforeMFMBtn(true);
                        SwitchAfterMFMBtn(true);
                        break;
                    };
                case ProcessState.Measurement:
                    {
                        break;
                    };
                case ProcessState.Transit:
                    {
                        break;
                    };

            }
        }

        /// <summary>
        /// MFM処理開始時のフラグ処理
        /// </summary>
        private void MfmStart()
        {
            CanMFM = false;
            IsMfmStarted = true;
            FlowEnable = false;
            MSettingEnable = false;
            RBtnEnable = false;

            isZeroSend = false;
            isZeroOK = false;
            isSpanOK = false;
        }

        /// <summary>
        /// MFM処理開始時のフラグ処理
        /// </summary>
        private void InitialUI()
        {
            CanMFM = true;
            FlowEnable = true;
            MSettingEnable = true;
            RBtnEnable = true;
        }

        /// <summary>
        /// MFM処理開始時のフラグ処理
        /// </summary>
        private void UIAllFalse()
        {
            CanMFM = false;
            FlowEnable = false;
            MSettingEnable = false;
            RBtnEnable = false;

            isZeroSend = false;
            isZeroOK = false;
            isSpanOK = false;
        }
    }
}
