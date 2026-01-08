using MVVM_Base.DiContainer;
using MVVM_Base.Model;
using MVVM_Base.View;
using MVVM_Base.ViewModel;
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
        private PortWatcherService portWatcher;

        private ViewModelManagerService vmManager;


        /// <summary>
        /// アプリ開始時の処理
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // viewEntryを作成
            var entry = diRoot.Instance.GetService<viewEntry>();

            // viewLinearを作成
            var linear = diRoot.Instance.GetService<viewLinear>();

            // PortWatcherサービス取得
            portWatcher = diRoot.Instance.GetService<PortWatcherService>();

            // vm管理クラス
            vmManager = diRoot.Instance.GetService<ViewModelManagerService>();

            // viewEntry のウィンドウハンドルが生成されるタイミングで PortWatcher を開始
            entry.SourceInitialized += (s, ev) =>
            {
                var hwnd = new WindowInteropHelper(entry).Handle;

                portWatcher.Initialize(hwnd);
                portWatcher.Start();
            };
            
            entry.Show();
        }

        /// <summary>
        /// アプリ終了時の処理
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e)
        {
            portWatcher.Stop();
            //vmManager.DisposeAll();
            base.OnExit(e);
        }
    }
}