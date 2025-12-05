using MVVM_Base.DiContainer;
using MVVM_Base.Model;
using MVVM_Base.View;
using System.Windows;
using System.Windows.Interop;

namespace MVVM_Base
{
    /// <summary>
    /// エントリポイント
    /// </summary>
    public partial class App : Application
    {
        public App() { }
        private PortWatcherService _portWatcher;

        /// <summary>
        /// アプリ開始時の処理
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //// diコンテナの持つportWatcherを取得する
            //_portWatcher = diRoot.Instance.GetService<PortWatcherService>();
            //_portWatcher.Start();
            //diRoot.Instance.GetService<viewEntry>().Show();

            // viewEntry を作成して表示
            var entry = diRoot.Instance.GetService<viewEntry>();

            // PortWatcherサービス取得
            _portWatcher = diRoot.Instance.GetService<PortWatcherService>();

            // viewEntry のウィンドウハンドルが生成されるタイミングで PortWatcher を開始
            entry.SourceInitialized += (s, ev) =>
            {
                var hwnd = new WindowInteropHelper(entry).Handle;

                _portWatcher.Initialize(hwnd);
                _portWatcher.Start();
            };

            entry.Show();
        }

        /// <summary>
        /// アプリ終了時の処理
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e)
        {
            _portWatcher.Stop();
            base.OnExit(e);
        }
    }
}