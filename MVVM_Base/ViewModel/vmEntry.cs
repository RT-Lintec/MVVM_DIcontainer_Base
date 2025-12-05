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
        BView,
        Help,
        End
    }

    public partial class vmEntry : ObservableObject
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
            set => SetProperty(ref isMainSelected, value);
        }

        /// <summary>
        /// A_Viewボタン選択
        /// </summary>
        private bool isLinearViewSelected;
        public bool IsLinearViewSelected
        {
            get => isLinearViewSelected;
            set => SetProperty(ref isLinearViewSelected, value);
        }

        /// <summary>
        /// B_Viewボタン選択
        /// </summary>
        private bool isBViewSelected;
        public bool IsBViewSelected
        {
            get => isBViewSelected;
            set => SetProperty(ref isBViewSelected, value);
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
            IsBViewSelected = type == ViewType.BView;
            IsHelpSelected = type == ViewType.Help;
            IsEndSelected = type == ViewType.End;
        }

        private readonly ThemeService themeService;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public vmEntry(ThemeService _themeService) 
        { 
            themeService = _themeService;
            themeService.PropertyChanged += ThemeService_PropertyChanged;
        }

        #region カラーテーマ変更通知関連
        private void ThemeService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ThemeService.CurrentTheme))
            {
                // ここで CurrentTheme 変化を検知可能
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

        private viewMain mainView;
        /// <summary>
        /// ビュー切り替え
        /// </summary>
        /// <param name="type"></param>
        [RelayCommand]
        public void ShowView(ViewType type)
        {
            // 選択状態を一括で更新
            SelectView(type);

            // CurrentView 切り替え
            CurrentView = type switch
            {
                ViewType.Main => mainView ??= diRoot.Instance.GetService<viewMain>(),
                ViewType.LinearView => diRoot.Instance.GetService<viewLinear>(),
                ViewType.BView => diRoot.Instance.GetService<viewB>(),
                _ => CurrentView
            };
        }

        [RelayCommand]
        /// <summary>
        /// 天秤のポートオープン
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

                if (themeService.CurrentTheme == "Dark")
                {
                    themeService.CurrentTheme = "Light";
                }
                else
                {
                    themeService.CurrentTheme = "Dark";
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
    }
}