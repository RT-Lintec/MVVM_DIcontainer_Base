using System.Windows;

namespace MVVM_Base.View
{
    /// <summary>
    /// CustomModal.xaml の相互作用ロジック
    /// </summary>
    public partial class CustomModal : Window
    {
        /// <summary>
        /// アプリ中央に配置される、見た目がカスタマイズされたメッセージボックス
        /// </summary>
        /// <param name="message"></param>
        /// <param name="owner"></param>
        public CustomModal(string message, Window? owner = null)
        {
            InitializeComponent();
            MessageText.Text = message;

            // 親ウィンドウ指定
            this.Owner = owner ?? Application.Current.MainWindow;

            // Loaded イベントで中央に配置
            Loaded += (s, e) =>
            {
                if (this.Owner != null)
                {
                    this.Left = this.Owner.Left + (this.Owner.Width - this.Width) / 2;
                    this.Top = this.Owner.Top + (this.Owner.Height - this.Height) / 2;
                }
            };
        }

        /// <summary>
        /// OKボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOK(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Cancelボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
