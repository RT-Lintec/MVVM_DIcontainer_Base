using CommunityToolkit.Mvvm.ComponentModel;
using MVVM_Base.Model;

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

            canTransitOther = true;

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
    }
}
