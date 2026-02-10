using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM_Base.DiContainer;
using MVVM_Base.Model;
using MVVM_Base.View;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MVVM_Base.ViewModel
{
    public enum ViewType
    {
        Main,
        LinearView,
        Balw,
        Help,
        End
    }

    public partial class vmEntry : ObservableObject, IViewModel
    {

        [ObservableProperty]
        private UserControl currentView;

        /// <summary>
        /// Mainボタン選択
        /// </summary>
        private bool isMainSelected;
        public bool IsMainSelected
        {
            get => isMainSelected;
            set
            {
                if (vmService.CanTransit)
                {
                    SetProperty(ref isMainSelected, value);
                }
            }
        }

        /// <summary>
        /// A_Viewボタン選択
        /// </summary>
        private bool isLinearViewSelected;
        public bool IsLinearViewSelected
        {
            get => isLinearViewSelected;
            set
            {
                if (vmService.CanTransit)
                {
                    SetProperty(ref isLinearViewSelected, value);
                }
            }

        }

        /// <summary>
        /// B_Viewボタン選択
        /// </summary>
        private bool isBViewSelected;
        public bool IsBalwSelected
        {
            get => isBViewSelected;
            set
            {
                if (vmService.CanTransit)
                {
                    SetProperty(ref isBViewSelected, value);
                }
            }
        }

        /// <summary>
        /// Helpボタン選択
        /// </summary>
        private bool isHelpSelected;
        public bool IsHelpSelected
        {
            get => isHelpSelected;
            set => SetProperty(ref isHelpSelected, value);
        }

        /// <summary>
        /// Endボタン選択
        /// </summary>
        private bool isEndSelected;
        public bool IsEndSelected
        {
            get => isEndSelected;
            set => SetProperty(ref isEndSelected, value);
        }

        /// <summary>
        /// 選択されたViewタイプと一致するbooleanをtrueにする→Viewに通知がいく
        /// </summary>
        /// <param name="type"></param>
        public void SelectView(ViewType type)
        {
            IsMainSelected = type == ViewType.Main;
            IsLinearViewSelected = type == ViewType.LinearView;
            IsBalwSelected = type == ViewType.Balw;
            IsHelpSelected = type == ViewType.Help;
            IsEndSelected = type == ViewType.End;
        }

        // service
        private readonly ThemeService themeService;
        private readonly ViewModelManagerService vmService;
        private readonly ApplicationStatusService appStatusService;
        private readonly IMessageService messageService;
        private readonly LanguageService languageService;
        private readonly IdentifierService identifierService;

        /// <summary>
        /// 終了可否
        /// </summary>
        public bool canQuit { get; set; }

        /// <summary>
        /// 画面遷移可否
        /// </summary>
        public bool canTransitOther { get; set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public vmEntry(ThemeService _themeService, ViewModelManagerService _vmService, ApplicationStatusService _appStatusService,
            IMessageService _messageService, LanguageService _languageService, IdentifierService _identifierService) 
        { 
            themeService = _themeService;
            themeService.PropertyChanged += ThemeService_PropertyChanged;
            ColorTheme = themeService.Dark;

            vmService = _vmService;
            vmService.Register(this);
            vmService.PropertyChanged += VmService_PropertyChanged;

            appStatusService = _appStatusService;
            messageService = _messageService;

            languageService = _languageService;
            languageService.PropertyChanged += LanguageService_PropertyChanged;

            identifierService = _identifierService;

            canTransitOther = true;
        }

        public void Dispose()
        {
            // 終了可否判断
            canQuit = true;

            // 終了可否チェック
            vmService.CheckCanQuit();

            canTransitOther = true;
        }


        #region 変更通知関連

        /// <summary>
        /// 言語変更通知の検知
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LanguageService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 言語
            if (e.PropertyName == nameof(LanguageService.CurrentLanguage))
            {
                OnLanguageChanged(languageService.CurrentLanguage);
            }
        }

        /// <summary>
        /// 言語変更通知の発行　日本語ベース
        /// </summary>
        /// <param name="newTheme"></param>
        private void OnLanguageChanged(LanguageType languageType)
        {
            // View に依存せず ViewModel 内で処理可能
            // 例：内部フラグ更新や別プロパティ更新など
            IsJapanese = languageType == LanguageType.Japanese; // フラグ例
                                                     // 必要であれば PropertyChanged 通知も出す
            OnPropertyChanged(nameof(IsJapanese));
        }

        private void ThemeService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // カラーテーマ
            if (e.PropertyName == nameof(ThemeService.CurrentTheme))
            {
                OnThemeChanged(themeService.CurrentTheme);
            }
        }

        /// <summary>
        /// LanguageServiceにイベント通知を委任しているので、プロパティ変化の通知は行わない
        /// 行うと二重発火となり、viewEntryでのプロパティ変更イベントが二回発生する。
        /// </summary>
        private bool isJapanese;
        public bool IsJapanese
        {
            get => isJapanese;
            set
            {
                if (isJapanese != value)
                {
                    isJapanese = value;
                }
            }
        }

        /// <summary>
        /// カラーテーマ変更通知発行
        /// </summary>
        /// <param name="newTheme"></param>
        private void OnThemeChanged(string newTheme)
        {
            // View に依存せず ViewModel 内で処理可能
            // 例：内部フラグ更新や別プロパティ更新など
            IsDarkTheme = newTheme == themeService.Dark;
            ColorTheme = newTheme;
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

        /// <summary>
        /// 現在のカラーテーマ
        /// </summary>
        private string colorTheme = "";
        public string ColorTheme
        {
            get => colorTheme;
            private set => colorTheme = value;
        }

        private void VmService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(vmService.CanTransit))
            {
                // ここで CurrentTheme 変化を検知可能
                OnTransitChanged(vmService.CanTransit);
            }
        }

        private void OnTransitChanged(bool _canTransit)
        {
            // View に依存せず ViewModel 内で処理可能
            // 例：内部フラグ更新や別プロパティ更新など
            CanTransit = _canTransit; // フラグ例
                                              // 必要であれば PropertyChanged 通知も出す
            OnPropertyChanged(nameof(CanTransit));
        }

        private bool canTransit = true;
        public bool CanTransit
        {
            get => canTransit;
            set
            {
                if (canTransit != value)
                {
                    canTransit = value;
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        private viewMain mainView;
        /// <summary>
        /// ビュー切り替え
        /// </summary>
        /// <param name="type"></param>
        [RelayCommand]
        public async void ShowView(ViewType type)
        {
            // 選択状態を一括で更新
            SelectView(type);

            // 各画面で未保存の出力結果が存在する場合
            if (vmService.HasNonsavedOutput)
            {
                CurrentView = null;

                while (vmService.HasNonsavedOutput)
                {
                    await Task.Delay(10);
                }
            }

            if (vmService.CanTransit)
            {
                // CurrentView 切り替え
                CurrentView = type switch
                {
                    ViewType.Main => mainView ??= diRoot.Instance.GetService<viewMain>(),
                    ViewType.LinearView => diRoot.Instance.GetService<viewLinear>(),
                    ViewType.Balw => diRoot.Instance.GetService<viewBalw>(),
                    _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
                };
            }
        }

        [RelayCommand]
        /// <summary>
        /// 言語変更
        /// </summary>
        private void ChangeLanguage()
        {
            try
            {
                // サービスに変更通知出させる
                if (languageService.CurrentLanguage == LanguageType.Japanese)
                {
                    languageService.CurrentLanguage = LanguageType.English;
                }
                else
                {
                    languageService.CurrentLanguage = LanguageType.Japanese;
                }
            }
            catch (Exception ex)
            {

            }
        }

        [RelayCommand]
        /// <summary>
        /// カラーテーマ変更
        /// </summary>
        private void ChangeColorTheme()
        {
            try
            {
                // 現在色の取得
                var resources = Application.Current.Resources;
                Color oldThemeColor = (Color)resources["ButtonThemeColor"];
                Color oldAcsColor = (Color)resources["AccentColor"];
                Color oldWcColor1 = (Color)resources["LeftWindowColor1"];
                Color oldWcColor2 = (Color)resources["LeftWindowColor2"];
                Color oldCtColor1 = (Color)resources["CheckToggleColor1"];
                Color oldCtColor2 = (Color)resources["CheckToggleColor2"];
                Color oldTextColor = (Color)resources["TextColor"];
                Color oldTagColor = (Color)resources["TagColor"];

                // サービスに変更通知出させる
                if (themeService.CurrentTheme == themeService.Dark)
                {
                    themeService.CurrentTheme = themeService.Light;
                }
                else
                {
                    themeService.CurrentTheme = themeService.Dark;
                }

                // 変更後カラーの取得
                var newDict = new ResourceDictionary { Source = new Uri($"/Theme/{themeService.CurrentTheme}Theme.xaml", UriKind.Relative) };

                Color newTbarColor1 = (Color)newDict["TitleBarAnimFrom1"];
                Color newTbarColor2 = (Color)newDict["TitleBarAnimFrom2"];
                Color newThemeColor = (Color)newDict["ButtonThemeColor"];
                Color newAcsColor = (Color)newDict["AccentColor"];
                Color newWcColor1 = (Color)newDict["LeftWindowColor1"];
                Color newWcColor2 = (Color)newDict["LeftWindowColor2"];
                Color newCtColor1 = (Color)newDict["CheckToggleColor1"];
                Color newCtColor2 = (Color)newDict["CheckToggleColor2"];
                Color newTextColor = (Color)newDict["TextColor"];
                Color newTagColor = (Color)newDict["TagColor"];

                // SolidColorBrush参照ブラシのアニメーション
                ThemeChangeSCB("AccentBrush", oldAcsColor, newAcsColor);
                ThemeChangeSCB("LeftWindowColor1Brush", oldWcColor1, newWcColor1);
                ThemeChangeSCB("LeftWindowColor2Brush", oldWcColor2, newWcColor2);
                ThemeChangeSCB("TextColorBrush", oldTextColor, newTextColor);
                ThemeChangeSCB("TagColorBrush", oldTagColor, newTagColor);

                // LinearGradientBrush参照ブラシのアニメーション
                ThemeChangeLGB(newDict, "CheckToggleBrush", "CheckToggleColor1", "CheckToggleColor2", oldCtColor1, oldCtColor2, newCtColor1, newCtColor2);

                //// カラーテーマ変更ボタンのアニメーション
                //ThemeChangeButton(ThemeToggleButton, oldThemeColor, newThemeColor);
            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// SolidColorBrush参照UIのテーマ変化アニメーション
        /// </summary>
        /// <param name="target"></param>
        /// <param name="oldColor"></param>
        /// <param name="newColor"></param>
        private void ThemeChangeSCB(string target, Color oldColor, Color newColor)
        {
            var brush = Application.Current.Resources[target] as SolidColorBrush;

            if (brush != null)
            {
                // 凍結されている場合はクローンして再登録
                if (brush.IsFrozen)
                {
                    brush = brush.Clone();
                    Application.Current.Resources[target] = brush;
                }

                // アニメーション作成
                var anim = new ColorAnimation
                {
                    From = oldColor,
                    To = newColor,
                    Duration = TimeSpan.FromSeconds(1),
                    AutoReverse = false
                };

                // アニメーション開始
                brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
            }
        }

        /// <summary>
        /// LinearGradientBrush参照UIのテーマ変化アニメーション
        /// </summary>
        /// <param name="newDict"></param>
        /// <param name="targetBrush"></param>
        /// <param name="targetColor1"></param>
        /// <param name="targetColor2"></param>
        /// <param name="oldColor1"></param>
        /// <param name="oldColor2"></param>
        /// <param name="newColor1"></param>
        /// <param name="newColor2"></param>
        private void ThemeChangeLGB(ResourceDictionary newDict, string targetBrush, string targetColor1, string targetColor2, Color oldColor1, Color oldColor2, Color newColor1, Color newColor2)
        {
            var resources = Application.Current.Resources;

            // Brush を取得
            var brushCheckToggle = Application.Current.Resources[targetBrush] as LinearGradientBrush;
            if (brushCheckToggle != null)
            {
                if (brushCheckToggle.IsFrozen)
                {
                    brushCheckToggle = brushCheckToggle.Clone();
                    Application.Current.Resources[targetBrush] = brushCheckToggle;
                }

                // GradientStop を取得
                var gs1 = brushCheckToggle.GradientStops[0];
                var gs2 = brushCheckToggle.GradientStops[1];

                // アニメーション作成
                var anim1 = new ColorAnimation(oldColor1, newColor1, TimeSpan.FromSeconds(1));
                var anim2 = new ColorAnimation(oldColor2, newColor2, TimeSpan.FromSeconds(1));

                anim2.Completed += (o, e) =>
                {
                    resources[targetColor1] = newDict[targetColor1];
                    resources[targetColor2] = newDict[targetColor2];
                };
                // GradientStop にアニメーションを適用
                gs1.BeginAnimation(GradientStop.ColorProperty, anim1);
                gs2.BeginAnimation(GradientStop.ColorProperty, anim2);
            }
        }

        /// <summary>
        /// 終了ボタンの終了側スライド完了
        /// </summary>
        public async void OnThumbSlideCompleted()
        {
            // 未保存のリニア調整データがあるときの確認
            if (vmService.HasNonsavedOutput)
            {
                var confirm = await messageService.ShowModalAsync(languageService.FirstConfirmBeforeQuit);
                if (confirm.Value)
                {
                    confirm = await messageService.ShowModalAsync(languageService.SecondConfirmBeforeQuit);
                    if (!confirm.Value)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            // trueにして各vmの終了イベント発火
            appStatusService.IsQuit = true;
            Dispose();

            // 各vmの終了チェックが完了したので、サービスに全vmの終了状態を調べさせる
            // 全vmが終了OKならアプリ終了
            vmService.CheckCanQuit();

            // アプリ終了中断の場合はfalseに戻す
            appStatusService.IsQuit = false;
        }
    }
}