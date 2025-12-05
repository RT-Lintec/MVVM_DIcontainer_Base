using MVVM_Base.View;
using System.Windows;
using System.Windows.Media.Animation;

namespace MVVM_Base.Model
{
    public class MessageBlocker : IMessageService
    {
        /// <summary>
        /// 排他制御オブジェクト
        /// </summary>
        private readonly object lockObj = new();

        /// <summary>
        /// カスタムメッセージボックス
        /// </summary>
        private CustomMessageBox? dialog;

        /// <summary>
        /// クローズ中フラグ
        /// </summary>
        private bool isClosing = false;

        /// <summary>
        /// メッセージ表示
        /// </summary>
        /// <param name="message"></param>
        public Task ShowMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // クリティカルセクション
                lock (lockObj)
                {
                    // すでに同じ内容なら開き直さない
                    if (dialog != null && dialog.MessageText.Text == message)
                    {
                        return;
                    }

                    // 既存を即閉じる（フェード中なら強制）
                    if (dialog != null)
                    {
                        dialog.Close();
                        dialog = null;
                        isClosing = false;
                    }

                    // 新規作成
                    dialog = new CustomMessageBox(message, Application.Current.MainWindow);
                    dialog.Opacity = 1.0;
                    dialog.Show();
                }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// メッセージ消去
        /// </summary>
        public Task CloseWithFade()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CustomMessageBox? dialogToClose;

                // クリティカルセクション
                lock (lockObj)
                {
                    // 対象無し or クローズ中なら処理中断
                    if (dialog == null || isClosing)
                    {
                        return;
                    }

                    // クローズ中フラグオンして対象をキャプチャ
                    isClosing = true;
                    dialogToClose = dialog;
                }

                // fade-out アニメーション
                var fadeOut = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromSeconds(2),
                    FillBehavior = FillBehavior.Stop
                };

                fadeOut.Completed += (_, __) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // クリティカルセクション
                        lock (lockObj)
                        {
                            dialogToClose.Close();

                            // 今閉じたウィンドウが最新のものであれば null にする
                            if (dialog == dialogToClose)
                            {
                                dialog = null;
                            }

                            isClosing = false;
                        }
                    });
                };

                dialogToClose.BeginAnimation(Window.OpacityProperty, fadeOut);
            });

            return Task.CompletedTask;
        }
    }
}
