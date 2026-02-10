using MVVM_Base.Common;
using MVVM_Base.ViewModel;
using Microsoft.VisualBasic.Logging;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static MVVM_Base.ViewModel.vmLinear;
using Color = System.Windows.Media.Color;
using Point = System.Windows.Point;
namespace MVVM_Base.View
{
    /// <summary>
    /// viewA.xaml の相互作用ロジック
    /// </summary>
    public partial class viewLinear : UserControl
    {
        /// <summary>
        /// vmからのイベント通知を受け取るために保持
        /// </summary>
        private vmLinear? vm;

        /// <summary>
        /// 通信時のアイコンのアニメーションSB
        /// </summary>
        private Storyboard? icMfcStoryboard = new Storyboard()
        {
            RepeatBehavior = RepeatBehavior.Forever,
            AutoReverse = true
        };

        private Storyboard? icBalanceStoryboard = new Storyboard()
        {
            RepeatBehavior = RepeatBehavior.Forever,
            AutoReverse = true
        };

        /// <summary>
        /// カラーテーマ変更時のアイコンのアニメーションSB
        /// </summary>
        Storyboard transitionMfcComm = new Storyboard();

        Storyboard transitionBalanceComm = new Storyboard();

        /// <summary>
        /// アニメーション間隔
        /// </summary>
        private int animInterval = 1;

        /// <summary>
        /// ポートオープンでアニメーションする
        /// ポートクローズでアニメーションしない
        /// </summary>
        static private bool isContinueMfcIconAnim = false;
        static private bool isContinueBalanceIconAnim = false;

        public viewLinear(vmLinear _vm)
        {
            InitializeComponent();
            
            this.DataContextChanged += View_DataContextChanged;
            DataContext = _vm;

            // ログ更新で最下部を表示する
            _vm.Logs.CollectionChanged += (_, __) =>
            {
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (LogListBox.Items.Count > 0)
                    {
                        LogListBox.ScrollIntoView(LogListBox.Items[^1]);
                    }
                }, System.Windows.Threading.DispatcherPriority.Background);
            };
        }

        private void View_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (vm != null) return;  // 1回だけ実行

            if (e.NewValue is vmLinear _vm)
            {
                vm = _vm;

                this.Loaded += (s, e) => vm.OnViewLoaded();
                this.Unloaded += (s, e) => vm.OnViewUnloaded();

                vm.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(vm.IsMfcConnected) ||
                        args.PropertyName == nameof(vm.IsBalanceConnected) ||
                        args.PropertyName == nameof(vm.IsDarkTheme) ||
                        args.PropertyName == nameof(vm.IsMfmStarted) ||
                        args.PropertyName == nameof(vm.ConfIndex) ||
                        args.PropertyName == nameof(vm.CanEditGainData) ||
                        args.PropertyName == nameof(vm.IsMapGenerated))
                    {
                        Vm_PropertyChanged(s, args);
                    }
                };
            }
        }

        /// <summary>
        /// view表示時のイベント登録処理 xaml最上部に依存処理記述
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // MFC通信状態をアイコンに反映
            if (isContinueMfcIconAnim)
            {
                AnimateCommIconColor(nameof(MfcCommIconColor), icMfcStoryboard);
            }
            else
            {
                StopTitleBarAnimation(icMfcStoryboard);
            }

            // Balance通信状態をアイコンに反映
            if (isContinueBalanceIconAnim)
            {
                AnimateCommIconColor(nameof(BalanceCommIconColor), icBalanceStoryboard);
            }
            else
            {
                StopTitleBarAnimation(icBalanceStoryboard);
            }

            // 画面表示時に拡縮対応させる
            this.Focusable = true;
            this.Focus();

            // FBデータ取得・設定
            if (isContinueMfcIconAnim)
            {
                UpperFBData.ItemsSource = new List<vmLinear> { vm };

                lowerFBData.ItemsSource = new List<vmLinear> { vm };
            }
            else
            {
                UpperFBData.ItemsSource = new List<UpperFBModel>
                {
                    new UpperFBModel {
                        Fb90="00", Fb92="00", Fb94="00", Fb96="00", Fb98="00",
                        Fb9a="00", Fb9c="00", Fb9e="00", Fba0="00", Fba2="00", Fb41="00"
                    }
                };

                lowerFBData.ItemsSource = new List<LowerFBModel>
                {
                    new LowerFBModel {
                        Fb91="00", Fb93="00", Fb95="00", Fb97="00", Fb99="00",
                        Fb9b="00", Fb9d="00", Fb9f="00", Fba1="00", Fba3="00", Fb42="00"
                    }
                };
            }
        }

        /// <summary>
        /// 上位FBデータ
        /// </summary>
        private class UpperFBModel
        {
            public string Fb90 { get; set; }
            public string Fb92 { get; set; }
            public string Fb94 { get; set; }
            public string Fb96 { get; set; }
            public string Fb98 { get; set; }
            public string Fb9a { get; set; }
            public string Fb9c { get; set; }
            public string Fb9e { get; set; }
            public string Fba0 { get; set; }
            public string Fba2 { get; set; }
            public string Fb41 { get; set; }
        }

        /// <summary>
        /// 下位FBデータ
        /// </summary>
        private class LowerFBModel
        {
            public string Fb91 { get; set; }
            public string Fb93 { get; set; }
            public string Fb95 { get; set; }
            public string Fb97 { get; set; }
            public string Fb99 { get; set; }
            public string Fb9b { get; set; }
            public string Fb9d { get; set; }
            public string Fb9f { get; set; }
            public string Fba1 { get; set; }
            public string Fba3 { get; set; }
            public string Fb42 { get; set; }
        }
        /// <summary>
        /// vmからのプロパティ値変更イベント通知時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            if (e.PropertyName == nameof(vmLinear.IsMapGenerated))
            {
                string[] lowerFbs =
                {
                    "FB90","FB92","FB94","FB96","FB98",
                    "FB9A","FB9C","FB9E","FBA0","FBA2","FB41"
                };

                string[] upperFbs =
                {
                    "FB91","FB93","FB95","FB97","FB99",
                    "FB9B","FB9D","FB9F","FBA1","FBA3","FB42"
                };

                int lowerCnt = 0;
                lowerFBData.Columns.Clear();

                var fbProps = typeof(vmLinear)
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.GetCustomAttribute<FbLowerCodeAttribute>() != null);

                foreach (var prop in fbProps)
                {
                    lowerFBData.Columns.Add(CreateFbColumn(vm, lowerFbs[lowerCnt], prop));
                    lowerCnt++;
                }

                int upperCnt = 0;
                UpperFBData.Columns.Clear();

                fbProps = typeof(vmLinear)
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(p => p.GetCustomAttribute<FbUpperCodeAttribute>() != null);

                foreach (var prop in fbProps)
                {
                    UpperFBData.Columns.Add(CreateFbColumn(vm, upperFbs[upperCnt], prop));
                    upperCnt++;
                }
            }

            if (e.PropertyName == nameof(vmLinear.CanEditGainData))
            {
                if (((vmLinear)sender).CanEditGainData)
                {
                    lowerFBData.IsEnabled = true;
                    UpperFBData.IsEnabled = true;
                }
                else
                {
                    lowerFBData.IsEnabled = false;
                    UpperFBData.IsEnabled = false;
                }
            }

            // MFCポート接続状態の通知に対する処理
            if (e.PropertyName == nameof(vmLinear.IsMfcConnected))
            {
                if (((vmLinear)sender).IsMfcConnected)
                {
                    isContinueMfcIconAnim = true;
                }
                else
                {
                    isContinueMfcIconAnim = false;
                    StopTitleBarAnimation(icMfcStoryboard);

                    Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        UpperFBData.ItemsSource = new List<UpperFBModel>
                        {
                            new UpperFBModel {
                                Fb90="00", Fb92="00", Fb94="00", Fb96="00", Fb98="00",
                                Fb9a="00", Fb9c="00", Fb9e="00", Fba0="00", Fba2="00", Fb41="00"
                            }
                        };

                        lowerFBData.ItemsSource = new List<LowerFBModel>
                        {
                            new LowerFBModel {
                                Fb91="00", Fb93="00", Fb95="00", Fb97="00", Fb99="00",
                                Fb9b="00", Fb9d="00", Fb9f="00", Fba1="00", Fba3="00", Fb42="00"
                            }
                        };
                    });

                }
            }
            // 天秤ポート接続状態の通知に対する処理
            else if (e.PropertyName == nameof(vmLinear.IsBalanceConnected))
            {
                if (((vmLinear)sender).IsBalanceConnected)
                {
                    isContinueBalanceIconAnim = true;
                }
                else
                {
                    isContinueBalanceIconAnim = false;
                    StopTitleBarAnimation(icBalanceStoryboard);
                }
            }
            // カラーテーマ変更通知に対する処理
            else if (e.PropertyName == nameof(vmLinear.IsDarkTheme))
            {
                if (((vmLinear)sender).IsMfcConnected && ((vmLinear)sender).IsBalanceConnected)
                {
                    ThemeChangeCommIcon(MfcCommIconColor, nameof(MfcCommIconColor), transitionMfcComm, icMfcStoryboard, isContinueMfcIconAnim);
                    ThemeChangeCommIcon(BalanceCommIconColor, nameof(BalanceCommIconColor), transitionBalanceComm, icBalanceStoryboard, isContinueBalanceIconAnim);
                }
                else if (((vmLinear)sender).IsMfcConnected)
                {
                    ThemeChangeCommIcon(MfcCommIconColor, nameof(MfcCommIconColor), transitionMfcComm, icMfcStoryboard, isContinueMfcIconAnim);
                }
                else if (((vmLinear)sender).IsBalanceConnected)
                {
                    ThemeChangeCommIcon(BalanceCommIconColor, nameof(BalanceCommIconColor), transitionBalanceComm, icBalanceStoryboard, isContinueBalanceIconAnim);
                }
                ThemeChangeCommConfButton();
            }
            // MFMコマンドが開始されたかどうか
            else if(e.PropertyName == nameof(vmLinear.IsMfmStarted))
            {
                if(((vmLinear)sender).IsMfmStarted)
                {
                    UpperFBData.IsEnabled = false;
                    lowerFBData.IsEnabled = false;
                }
                else
                {
                    UpperFBData.IsEnabled = true;
                    lowerFBData.IsEnabled = true;
                }
            }
            // Fix me：使わないなら消す
            // confインデクスが変更された場合
            if (e.PropertyName == nameof(vmLinear.ConfIndex))
            {
                // conf1
                if (((vmLinear)sender).ConfIndex == 1)
                {

                }
                else if (((vmLinear)sender).ConfIndex == 2)
                {

                }
                else if (((vmLinear)sender).ConfIndex == 3)
                {

                }
                else if (((vmLinear)sender).ConfIndex == 4)
                {

                }
                else if (((vmLinear)sender).ConfIndex == 5)
                {

                }
                else if (((vmLinear)sender).ConfIndex == 6)
                {

                }
                else if (((vmLinear)sender).ConfIndex == 7)
                {

                }
                else if (((vmLinear)sender).ConfIndex == 8)
                {

                }
                else if (((vmLinear)sender).ConfIndex == 9)
                {

                }
                else if (((vmLinear)sender).ConfIndex == 10)
                {

                }
            }
        }
               
        /// <summary>
        /// ゲイン表の各セルにStyleを適用し、
        /// 値のバインド先はPropertyInfo、アドレス自体(ラベルの参照先)は外部キー
        /// </summary>
        /// <param name="vm"></param>
        /// <param name="key"></param>
        /// <param name="prop"></param>
        /// <returns></returns>
        private static DataGridTextColumn CreateFbColumn(vmLinear vm, string key, PropertyInfo prop)
        {
            // Header 用 TextBlock（ここが重要）
            var headerText = new TextBlock
            {
                Text = vm.FbMap[key].ToString(),
                TextAlignment = TextAlignment.Center
            };

            // ベースとなるスタイルを取得
            var baseStyle = (System.Windows.Style)Application.Current.FindResource("EditingTextBoxStyle");

            // スタイルを拡張する（新しいStyleインスタンスを作成）
            var dynamicStyle = new System.Windows.Style(typeof(TextBox), baseStyle);

            // VM（行データ）の特定のプロパティ（例：IsModified）にバインドするセッターを追加
            // もしプロパティ名が固定ならこれだけでOK
            // EditingTextBoxStyleスタイルのxamlタグで書くべき以下内容をコードビハインドで、同様に処理している
            // <Setter Property="local:HexCheckAssist.ChangedTrigger" Value="{Binding IsModified, Mode=TwoWay}"/>
            dynamicStyle.Setters.Add(new Setter
            {
                Property = HexCheckAssist.ChangedTriggerProperty,
                Value = new Binding("IsModified")
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.Explicit // 自動更新しない➡明示的に更新する
                }
            });

            // 次のxamlをコードビハインドで処理。処理はどちらか一方でOKで、こういうことも出来るというモデルケース
            // <Setter Property="local:HexCheckAssist.Enable" Value="True"/>
            dynamicStyle.Setters.Add(new Setter
            {
                Property = HexCheckAssist.EnableProperty,
                Value = true
            });

            return new DataGridTextColumn
            {
                Header = headerText,
                EditingElementStyle = dynamicStyle, // 作成したスタイルを適用
                Binding = new Binding(prop.Name)
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged // 値変更を更新トリガーとする
                }
            };
        }

        /// <summary>
        /// アイコンアニメーション開始
        /// </summary>
        private void AnimateCommIconColor(string target, Storyboard sb)
        {
            // 既にアニメーション中なら停止
            sb?.Stop(this);

            var anim1 = new ColorAnimation
            {
                From = GetThemeColor("CommIconColorFrom", Colors.LightGray),
                To = GetThemeColor("CommIconColorTo", Colors.DarkGray),
                Duration = TimeSpan.FromSeconds(animInterval),
                AutoReverse = true
            };
            Storyboard.SetTargetName(anim1, target.ToString());
            Storyboard.SetTargetProperty(anim1, new PropertyPath("Color"));
            sb.Children.Add(anim1);
            sb.Begin(this, true);
        }

        /// <summary>
        /// ポートオープンによるアイコンアニメーションを停止させる
        /// </summary>
        private void StopTitleBarAnimation(Storyboard target)
        {
            target?.Stop(this);
        }

        /// <summary>
        /// テーマカラーを取得する
        /// </summary>
        /// <param name="key"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        private Color GetThemeColor(string key, Color fallback)
        {
            // ThemeResourcesスロットを確認
            if (Application.Current.Resources["ThemeResources"] is ResourceDictionary themeSlot)
            {
                // 直接そのスロット内にキーがある場合
                if (themeSlot.Contains(key))
                {
                    return ConvertToColor(themeSlot[key], fallback);
                }

                // スロットのMergedDictionariesを順に検索
                foreach (var md in themeSlot.MergedDictionaries)
                {
                    if (md != null && md.Contains(key))
                    {
                        return ConvertToColor(md[key], fallback);
                    }
                }
            }

            // Applicationのtop-level MergedDictionariesを探索
            foreach (var md in Application.Current.Resources.MergedDictionaries)
            {
                if (md != null && md.Contains(key))
                {
                    return ConvertToColor(md[key], fallback);
                }
            }

            // 最終手段：TryFindResource（Application レベルの探索）
            var obj = Application.Current.TryFindResource(key);
            if (obj != null)
            {
                return ConvertToColor(obj, fallback);
            }

            // null
            return fallback;
        }

        /// <summary>
        /// 色変換
        /// </summary>
        /// <param name="res"></param>
        /// <param name="fallback"></param>
        /// <returns></returns>
        private Color ConvertToColor(object res, Color fallback)
        {
            if (res is Color c) return c;
            if (res is SolidColorBrush b) return b.Color;
            // 文字列等で定義しているケースがあれば TryParse してみる
            if (res is string s && System.Windows.Media.ColorConverter.ConvertFromString(s) is Color parsed)
                return parsed;
            return fallback;
        }

        /// <summary>
        /// カラーテーマ変更時、Confirmボタンのカラー遷移アニメーション
        /// </summary>
        private void ThemeChangeCommConfButton()
        {
            string theme = vm.ColorTheme;

            // 変更後カラーの取得
            var newDict = new ResourceDictionary { Source = new Uri($"/Theme/{theme}Theme.xaml", UriKind.Relative) };
            Color newAccentColor = (Color)newDict["TagColor"];
            Color newCoffButtonGColor = (Color)newDict["CoffButtonGColor"];

            // ブラシの取得
            var brush1 = (SolidColorBrush)RootGrid.Resources["GlowColorReference1"];
            var brush2 = (SolidColorBrush)RootGrid.Resources["GlowColorReference2"];

            // 変更前カラーの取得
            Color oldAccentColor = brush1.Color;
            Color oldCoffButtonGColor = brush2.Color;

            var anim1 = new ColorAnimation
            {
                From = oldAccentColor,
                To = newAccentColor,
                Duration = TimeSpan.FromSeconds(animInterval),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var anim2 = new ColorAnimation
            {
                From = oldCoffButtonGColor,
                To = newCoffButtonGColor,
                Duration = TimeSpan.FromSeconds(animInterval),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            brush1.BeginAnimation(SolidColorBrush.ColorProperty, anim1);
            brush2.BeginAnimation(SolidColorBrush.ColorProperty, anim2);
        }

        /// <summary>
        /// 通信アイコンのカラー遷移アニメーション
        /// </summary>
        /// <param name="gs1"></param>
        /// <param name="gs2"></param>
        /// <param name="newColor1"></param>
        /// <param name="newColor2"></param>
        private void ThemeChangeCommIcon(GradientStop targetGS, string target, Storyboard animSb, Storyboard orgSb, bool isContinueAnimation)
        {
            string theme = vm.ColorTheme;
            // 現在色の取得
            var resources = Application.Current.Resources;

            // 変更後カラーの取得
            var newDict = new ResourceDictionary { Source = new Uri($"/Theme/{theme}Theme.xaml", UriKind.Relative) };

            Color newColor = (Color)newDict["CommIconColorTo"];

            var anim = new ColorAnimation
            {
                From = targetGS.Color,
                To = newColor,
                Duration = TimeSpan.FromSeconds(animInterval),
                AutoReverse = false
            };

            Storyboard.SetTargetName(anim, target);
            Storyboard.SetTargetProperty(anim, new PropertyPath("Color"));
            animSb.Children.Add(anim);

            // テーマ移行アニメーション完了と同時にテーマアニメーション開始
            // 利用しているStoryboardはクラス変数のため、イベント毎にイベントハンドラを削除することで
            // イベント多重登録を防ぐ
            EventHandler handler = null;
            handler = (s, e) =>
            {
                animSb.Completed -= handler;   // ★自分自身を解除
                if (isContinueAnimation)
                {
                    AnimateCommIconColor(target, orgSb);
                }
            };

            animSb.Completed += handler;

            animSb.Begin(this);
        }

        /// <summary>
        /// ポートクローズ時にアイコンのカラー遷移アニメーションを停止させる
        /// </summary>
        private void StopCommIconAnimationAndReset(GradientStop target, bool isContinue)
        {
            isContinue = false;

            string theme = vm.ColorTheme;
            // 現在色の取得
            var resources = Application.Current.Resources;

            // 変更後カラーの取得
            var newDict = new ResourceDictionary { Source = new Uri($"/Theme/{theme}Theme.xaml", UriKind.Relative) };

            Color orgColor = (Color)newDict["CommIconColorFrom"];

            // アニメーションを完全に除去
            target.BeginAnimation(GradientStop.ColorProperty, null);

            // 色を戻す
            target.Color = orgColor;
        }

        private void ScrollViewer_ScrollChanged(object sender, EventArgs e)
        {
            var sv = (ScrollViewer)sender;

            if (sv != null)
            {
                bool isVerticalVisible = sv.ComputedVerticalScrollBarVisibility == Visibility.Visible;
                bool isHorizontalVisible = sv.ComputedHorizontalScrollBarVisibility == Visibility.Visible;

                // ここで矩形の表示/非表示を行う
                ScrollCorner.Visibility =
                    (isVerticalVisible || isHorizontalVisible)
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // 垂直方向のスクロールバーを初期化
                if (!isVerticalVisible)
                {
                    MyScrollViewer.ScrollToVerticalOffset(0);
                }

                // 水平方向のスクロールバーを初期化
                if (!isHorizontalVisible)
                {
                    MyScrollViewer.ScrollToHorizontalOffset(0);
                }
            }
        }

        /// <summary>
        /// マウスホイール押下状態ならtrue
        /// </summary>
        private bool isMiddleButtonDown = false;

        /// <summary>
        /// マウスホイールが押下された位置
        /// </summary>
        private Point middleButtonStart;

        /// <summary>
        /// マウスホイール押下時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Released && isMiddleButtonDown && sender is ScrollViewer sv)
            {
                isMiddleButtonDown = false;
                sv.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        /// <summary>
        /// マウスホイール解放時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.MiddleButton == MouseButtonState.Pressed && sender is ScrollViewer sv)
            {
                isMiddleButtonDown = true;
                middleButtonStart = e.GetPosition(sv);
                sv.CaptureMouse();
                e.Handled = true;
            }
        }

        /// <summary>
        /// マウス移動時の処理
        /// マウスホイール押下状態ならスクロールバーを移動させる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isMiddleButtonDown && sender is ScrollViewer sv)
            {
                Point currentPos = e.GetPosition(sv);
                Vector delta = currentPos - middleButtonStart;

                sv.ScrollToVerticalOffset(sv.VerticalOffset - delta.Y);
                sv.ScrollToHorizontalOffset(sv.HorizontalOffset - delta.X);

                middleButtonStart = currentPos;
                e.Handled = true;
            }
        }

        /// <summary>
        /// ScrollViewer側でCtrl + ホイールを判定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                // Ctrl 押下時だけ Handled = true でスクロール阻害
                e.Handled = true;
            }
            else
            {
                // Ctrl 未押下は通常のスクロール
                e.Handled = false;
            }
        }
    }
}
