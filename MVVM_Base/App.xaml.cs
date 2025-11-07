using System.Windows;
using MVVM_Base.DiContainer;
using MVVM_Base.View;

namespace MVVM_Base
{
    /// <summary>
    /// エントリポイント
    /// </summary>
    public partial class App : Application
    {
        public App() { }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            diRoot.Instance.GetService<viewEntry>().Show();
        }
    }
}