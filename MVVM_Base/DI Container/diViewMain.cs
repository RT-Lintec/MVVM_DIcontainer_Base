using LM_V3_V4.View;
using LM_V3_V4.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace LM_V3_V4.DiContainer
{
    private static class diViewMain
    {
        private static void Configure(IServiceCollection services)
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

