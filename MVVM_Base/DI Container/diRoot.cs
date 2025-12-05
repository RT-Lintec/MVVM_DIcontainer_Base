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
            //services.AddSingleton<IAnimalService, AnimalService>();
            services.AddSingleton<ThemeService>();

            // 各画面DIにサービス登録させる
            diEntry.Configure(services);
            diViewMain.Configure(services);
            diViewLinear.Configure(services);

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
                services.AddTransient<vmEntry>();

                // view
                services.AddTransient<viewEntry>();
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
                services.AddSingleton<PortWatcherService>();
                services.AddTransient<IMessageService, MessageBlocker>();

                // view
                services.AddTransient<viewMain>();
                services.AddTransient<viewLinear>();
                services.AddTransient<viewB>();
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
                services.AddSingleton<PortWatcherService>();
                services.AddTransient<IMessageService, MessageBlocker>();

                // view
                services.AddTransient<viewLinear>();
            }
        }
    }


}