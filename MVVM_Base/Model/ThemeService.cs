using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace MVVM_Base.Model
{
    public class ThemeService : INotifyPropertyChanged
    {
        private string _currentTheme = "Dark"; // 初期テーマ

        public string CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    OnPropertyChanged();

                    // 現在色の取得
                    var resources = Application.Current.Resources;

                    // 変更後カラーの取得
                    var newDict = new ResourceDictionary { Source = new Uri($"/Theme/{_currentTheme}Theme.xaml", UriKind.Relative) };

                    // 既存キーの書き換え
                    resources["AccentColor"] = newDict["AccentColor"];
                    resources["AccentBrush"] = newDict["AccentBrush"];
                    resources["ButtonThemeColor"] = newDict["ButtonThemeColor"];
                    resources["ButtonThemeColorBrush"] = newDict["ButtonThemeColorBrush"];
                    resources["ButtonThemeColor2"] = newDict["ButtonThemeColor2"];
                    resources["ButtonThemeColorBrush2"] = newDict["ButtonThemeColorBrush2"];
                    resources["ThemeIconKind"] = newDict["ThemeIconKind"];
                    resources["LeftWindowColor1"] = newDict["LeftWindowColor1"];
                    resources["LeftWindowColor1Brush"] = newDict["LeftWindowColor1Brush"];
                    resources["LeftWindowColor2"] = newDict["LeftWindowColor2"];
                    resources["LeftWindowColor2Brush"] = newDict["LeftWindowColor2Brush"];
                    resources["TextColor"] = newDict["TextColor"];
                    resources["TagColor"] = newDict["TagColor"];
                    resources["RippleColor"] = newDict["RippleColor"];
                    resources["RippleColorBrush"] = newDict["RippleColorBrush"];
                    resources["CommIconColorFrom"] = newDict["CommIconColorFrom"];
                    resources["CommIconColorTo"] = newDict["CommIconColorTo"];
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
