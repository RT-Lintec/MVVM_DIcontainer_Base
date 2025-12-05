using MVVM_Base.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MVVM_Base.View
{
    /// <summary>
    /// viewA.xaml の相互作用ロジック
    /// </summary>
    public partial class viewLinear : UserControl
    {
        public viewLinear(vmLinear _vm)
        {
            InitializeComponent();
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
