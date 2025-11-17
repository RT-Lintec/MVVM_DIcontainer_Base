using System;
using System.Windows;
using System.Windows.Media.Animation;

namespace MVVM_Base.View
{
    public partial class CustomMessageBox : Window
    {
        /// <summary>
        /// アプリ中央に配置される、見た目がカスタマイズされたメッセージボックス
        /// </summary>
        /// <param name="message"></param>
        /// <param name="owner"></param>
        public CustomMessageBox(string message, Window? owner = null)
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
    }
}
