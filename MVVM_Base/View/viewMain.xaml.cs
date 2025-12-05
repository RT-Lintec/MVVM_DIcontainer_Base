using MVVM_Base.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace MVVM_Base.View
{
    /// <summary>
    /// viewMain.xaml の相互作用ロジック
    /// </summary>
    public partial class viewMain : UserControl
    {
        /// <summary>
        /// vmからのイベント通知を受け取るために保持
        /// </summary>
        private vmMain? vm;

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
        private bool isContinueMfcIconAnim = false;

        private bool isContinueBalanceIconAnim = false;

        public viewMain(vmMain m_vmMainView)
        {
            InitializeComponent();
            DataContext = m_vmMainView;
        }

        /// <summary>
        /// view表示時のイベント登録処理 xaml最上部に依存処理記述
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // 初期化　一回のみ
            if (vm == null && DataContext is vmMain _vmMain)
            {
                vm = _vmMain;
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(_vmMain.IsMfcConnected) || 
                        e.PropertyName == nameof(_vmMain.IsBalanceConnected) || 
                        e.PropertyName == nameof(vmMain.IsDarkTheme))
                        Vm_PropertyChanged(s,e);
                };
            }

            // 画面表示時に拡縮対応させる
            this.Focusable = true;
            this.Focus();
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

            // MFCポート接続状態の通知に対する処理
            if (e.PropertyName == nameof(vmMain.IsMfcConnected))
            {
                if (((vmMain)sender).IsMfcConnected)
                {
                    isContinueMfcIconAnim = true;
                    AnimateCommIconColor(nameof(MfcCommIconColor), icMfcStoryboard);
                }
                else
                {
                    isContinueMfcIconAnim = false;
                    StopTitleBarAnimation(icMfcStoryboard);
                    if (transitionMfcComm != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            transitionMfcComm.Completed += (s, e) =>
                            {
                                StopCommIconAnimationAndReset(MfcCommIconColor, ((vmMain)sender).IsDarkTheme, isContinueMfcIconAnim);
                            };
                        });
                    }
                }
            }
            // 天秤ポート接続状態の通知に対する処理
            else if (e.PropertyName == nameof(vmMain.IsBalanceConnected))
            {
                if (((vmMain)sender).IsBalanceConnected)
                {
                    isContinueBalanceIconAnim = true;
                    AnimateCommIconColor(nameof(BalanceCommIconColor), icBalanceStoryboard);
                }
                else
                {
                    isContinueBalanceIconAnim = false;
                    StopTitleBarAnimation(icBalanceStoryboard);
                    if (transitionBalanceComm != null)
                    {
                        //transitionBalanceComm.Completed += (s, e) =>
                        //{
                        //    StopCommIconAnimationAndReset(BalanceCommIconColor, ((vmMain)sender).IsDarkTheme, isContinueBalanceIconAnim);
                        //};
                        Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            StopCommIconAnimationAndReset(BalanceCommIconColor, ((vmMain)sender).IsDarkTheme, isContinueBalanceIconAnim);
                        });
                    }
                }
            }
            // カラーテーマ変更通知に対する処理
            else if (e.PropertyName == nameof(vmMain.IsDarkTheme))
            {
                if(((vmMain)sender).IsMfcConnected && ((vmMain)sender).IsBalanceConnected)
                {
                    ThemeChangeCommIcon(MfcCommIconColor, nameof(MfcCommIconColor), transitionMfcComm, icMfcStoryboard, ((vmMain)sender).IsDarkTheme, isContinueMfcIconAnim);
                    ThemeChangeCommIcon(BalanceCommIconColor, nameof(BalanceCommIconColor), transitionBalanceComm, icBalanceStoryboard, ((vmMain)sender).IsDarkTheme, isContinueBalanceIconAnim);
                }
                else if (((vmMain)sender).IsMfcConnected)
                {
                    ThemeChangeCommIcon(MfcCommIconColor, nameof(MfcCommIconColor), transitionMfcComm, icMfcStoryboard,((vmMain)sender).IsDarkTheme, isContinueMfcIconAnim);
                }
                else if(((vmMain)sender).IsBalanceConnected)
                {
                    ThemeChangeCommIcon(BalanceCommIconColor, nameof(BalanceCommIconColor), transitionBalanceComm, icBalanceStoryboard, ((vmMain)sender).IsDarkTheme, isContinueBalanceIconAnim);
                }
            }
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
            if (res is string s && ColorConverter.ConvertFromString(s) is Color parsed)
                return parsed;
            return fallback;
        }

        /// <summary>
        /// 通信アイコンのカラー遷移アニメーション
        /// </summary>
        /// <param name="gs1"></param>
        /// <param name="gs2"></param>
        /// <param name="newColor1"></param>
        /// <param name="newColor2"></param>
        private void ThemeChangeCommIcon(GradientStop targetGS, string target, Storyboard animSb, Storyboard orgSb, bool isDark, bool isContinueAnimation)
        {
            string theme = isDark ? "Dark" : "Light";
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
            animSb.Completed += (s, e) =>
            {
                if (isContinueAnimation)
                {
                    AnimateCommIconColor(target, orgSb);
                }
            };

            animSb.Begin(this);
        }

        /// <summary>
        /// ポートクローズ時にアイコンのカラー遷移アニメーションを停止させる
        /// </summary>
        /// <param name="isDark"></param>
        private void StopCommIconAnimationAndReset(GradientStop target, bool isDark, bool isContinue)
        {
            isContinue = false;

            string theme = isDark ? "Dark" : "Light";
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
