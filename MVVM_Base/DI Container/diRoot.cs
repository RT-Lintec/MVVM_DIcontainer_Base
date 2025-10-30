using MVVM_Base.Services;
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

        private readonly IServiceProvider _provider;

        public diRoot()
        {
            var services = new ServiceCollection();

            // 共通サービス登録
            services.AddSingleton<IAnimalService, AnimalService>();

            // 各画面DIにサービス登録させる
            diEntry.Configure(services);
            diViewMain.Configure(services);

            _provider = services.BuildServiceProvider();
        }

        public T GetService<T>() where T : notnull => _provider.GetRequiredService<T>();

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
                services.AddTransient<vmMain>();

                // view
                services.AddTransient<viewMain>();
                services.AddTransient<viewA>();
                services.AddTransient<viewB>();
            }
        }
    }


}