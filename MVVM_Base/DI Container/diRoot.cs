using MVVM_Base.Common;
using MVVM_Base.Model;
using MVVM_Base.View;
using MVVM_Base.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace MVVM_Base.DiContainer
{
    /// <summary>
    /// DIコンテナのルートクラス
    /// 各画面ごとにDIコンテナ用意してコンストラクタでサービス登録
    /// </summary>
    public class diRoot
    {
        private static readonly Lazy<diRoot> _instance =
            new Lazy<diRoot>(() => new diRoot());

        private readonly IServiceProvider provider;

        public diRoot()
        {
            var services = new ServiceCollection();

            // 共通サービス登録
            services.AddSingleton<ThemeService>();
            services.AddSingleton<CommStatusService>();
            services.AddSingleton<ApplicationStatusService>();
            services.AddSingleton<ViewModelManagerService>();
            services.AddSingleton<HighPrecisionTimer>();
            services.AddSingleton<LanguageService>();
            services.AddSingleton<IdentifierService>();
            services.AddSingleton<MfcCommand>();
            //services.AddSingleton<HexCheckBehavior>();

            // 各画面DIにサービス登録させる
            diEntry.Configure(services);
            diViewMain.Configure(services);
            diViewLinear.Configure(services);
            diViewBalw.Configure(services);

            provider = services.BuildServiceProvider();
        }

        public T GetService<T>() where T : notnull => provider.GetRequiredService<T>();

        public static diRoot Instance => _instance.Value;

        /// <summary>
        /// エントリー画面のDIコンテナ
        /// </summary>
        private static class diEntry
        {
            public static void Configure(IServiceCollection services)
            {
                // vm
                services.AddSingleton<vmEntry>();

                // view
                services.AddSingleton<viewEntry>();

                // service
                services.AddSingleton<ViewModelManagerService>();
                services.AddTransient<IMessageService, MessageBlocker>();
            }
        }

        /// <summary>
        /// メインビュー画面のDIコンテナ
        /// </summary>
        private static class diViewMain
        {
            public static void Configure(IServiceCollection services)
            {
                // vm
                services.AddSingleton<vmMain>();

                // service
                services.AddSingleton<PortWatcherService>();
                services.AddTransient<IMessageService, MessageBlocker>();
                services.AddSingleton<ViewModelManagerService>();

                // view
                services.AddSingleton<viewMain>();
                services.AddSingleton<viewLinear>();
                services.AddTransient<viewBalw>();
            }
        }

        /// <summary>
        /// リニア調整ビュー画面のDIコンテナ
        /// </summary>
        private static class diViewLinear
        {
            public static void Configure(IServiceCollection services)
            {
                // vm
                services.AddSingleton<vmLinear>();

                // service
                services.AddSingleton<PortWatcherService>();
                services.AddTransient<IMessageService, MessageBlocker>();
                services.AddSingleton<ViewModelManagerService>();

                // view
                services.AddSingleton<viewLinear>();
            }
        }

        /// <summary>
        /// Balw画面のDIコンテナ
        /// </summary>
        private static class diViewBalw
        {
            public static void Configure(IServiceCollection services)
            {
                // vm
                services.AddSingleton<vmBalw>();

                // service
                services.AddSingleton<PortWatcherService>();
                services.AddTransient<IMessageService, MessageBlocker>();
                services.AddSingleton<ViewModelManagerService>();

                // view
                services.AddSingleton<viewBalw>();
            }
        }
    }


}