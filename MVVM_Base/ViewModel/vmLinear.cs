using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Common;
using MVVM_Base.Model;
using System.ComponentModel;
using System.IO;

namespace MVVM_Base.ViewModel
{
    public partial class vmLinear : ObservableObject, IViewModel
    {
        public vmLinear(ThemeService _themeService, CommStatusService _commStatusService, IMessageService _messageService,
            ViewModelManagerService _vmService, ApplicationStatusService _appStatusService, HighPrecisionTimer _precisionTimer, HighPrecisionTimer _precisionTimer2,
            LanguageService _languageService)
        {
            PropertyChanged += OnPropertyChanged;
            
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
            precisionTimer2 = _precisionTimer2;

            MesurementItems = new System.Collections.ObjectModel.ObservableCollection<MeasureResult>();
            MeasurementValues = new System.Collections.ObjectModel.ObservableCollection<MeasureResult>();

            languageService = _languageService;

            // 計測結果の表を形成
            ResetMeasureResult();
            ResetOutputResult(false);
        }

        public void Dispose()
        {
            // 終了可否判断
            canQuit = true;

            // 終了可否チェック
            vmService.CheckCanQuit();

            // 画面遷移可否
            vmService.CanTransit = true;
        }

        #region 状態変更通知に対応する処理

        /// <summary>
        /// ゲイン表にフォーカスが当たった時の処理
        /// 未保存出力データが存在する場合は、保存確認→未保存フラグ破棄
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IsModified))
            {
                if (vmService.HasNonsavedOutput && isGainDirectChanged)
                {
                    var confirm = await messageService.ShowModalAsync(languageService.FirstConfirmBeforeTransit);
                    if (confirm.Value)
                    {
                        await ExportParamsToCsv();
                    }
                    else
                    {
                        confirm = await messageService.ShowModalAsync(languageService.SecondConfirmBeforeTransit);
                        if (confirm.Value)
                        {
                            await ExportParamsToCsv();
                        }
                    }
                    // 未保存データを破棄したもののとして扱う
                    vmService.HasNonsavedOutput = false;
                }

                isGainDirectChanged = false;
            }
        }

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
            IsDarkTheme = newTheme == themeService.Dark;
            ColorTheme = newTheme;
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
        private async Task ChangeState(ProcessState state)
        {
            if (!noNeedConfirmUnsaved)
            {
                // 状態遷移時、最終出力が失われるケースは保存確認入れる
                if (CanExport && vmService.HasNonsavedOutput && state != ProcessState.Manual)
                {
                    if (!isSavedOutput)
                    {
                        var confirm = await messageService.ShowModalAsync(languageService.FirstConfirmBeforeTransit);
                        if (confirm.Value)
                        {
                            await ExportParamsToCsv();
                        }
                        else
                        {
                            confirm = await messageService.ShowModalAsync(languageService.SecondConfirmBeforeTransit);
                            if (confirm.Value)
                            {
                                await ExportParamsToCsv();
                            }
                        }
                        // 画面遷移を許すとデータを破棄したもののとして扱う
                        // アプリ終了もモーダル確認はでなくなる
                        vmService.HasNonsavedOutput = false;
                    }
                }
            }

            SwitchAllbtn(false);
            UIAllFalse();

            switch (state)
            {
                case ProcessState.Initial:
                    {
                        InitialUI();
                        SwitchBeforeMFMBtn(true);
                        CanEditGainData = true;
                        CanReadWriteGain = true;
                        break;
                    }
                    ;
                case ProcessState.MFMStarted:
                    {
                        MfmStart();
                        break;
                    }
                    ;
                case ProcessState.ZeroAdjust:
                    {
                        CanZeroSend = true;
                        CanZeroOK = true;
                        break;
                    }
                    ;
                case ProcessState.Span:
                    {
                        CanSpanAdjust = true;
                        break;
                    }
                    ;
                case ProcessState.AfterMFM:
                    {
                        CanReadWriteGain = true;
                        IsMfmStarted = false;
                        isFinishedMFM = true;
                        CanMFM = true;
                        CanEditGainData = true;
                        FlowEnable = true;
                        MSettingEnable = true;
                        RBtnEnable = true;
                        SwitchBeforeMFMBtn(true);
                        SwitchAfterMFMBtn(true);
                        break;
                    }
                    ;
                case ProcessState.AfterCalc:
                    {
                        CanReadWriteGain = true;
                        CanMFM = true;
                        CanEditGainData = true;
                        isFinishedMFM = false;
                        FlowEnable = true;
                        MSettingEnable = true;
                        RBtnEnable = true;
                        SwitchBeforeMFMBtn(true);
                        SwitchAfterMFMBtn(true);
                        break;
                    }
                    ;
                case ProcessState.AfterCalcAndConf:
                    {
                        CanReadWriteGain = true;
                        CanMFM = true;
                        CanEditGainData = true;
                        CanExport = true;
                        isFinishedMFM = false;
                        FlowEnable = true;
                        MSettingEnable = true;
                        RBtnEnable = true;
                        SwitchBeforeMFMBtn(true);
                        SwitchAfterMFMBtn(true);
                        break;
                    }
                    ;
                case ProcessState.Measurement:
                    {
                        break;
                    }
                    ;
                case ProcessState.FiveperConf:
                    {
                        break;
                    }
                    ;
                case ProcessState.Manual:
                    {
                        CanConfAlone = true;
                        CanEditReadingList = true;
                        break;
                    }
                    ;
                case ProcessState.Transit:
                    {
                        break;
                    }
                    ;
            }

            curState = state;
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
            CanEditReadingList = false;
            CanCalcGain = false;
            CanConfAlone = false;
            CanEditGainData = false;
            CanReadWriteGain = false;
            FlowEnable = false;
            MSettingEnable = false;
            RBtnEnable = false;

            isZeroSend = false;
            isZeroOK = false;
            isSpanOK = false;
        }
    }
}
