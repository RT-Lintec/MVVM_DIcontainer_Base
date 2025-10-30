using LM_V3_V4.View;
using LM_V3_V4.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace LM_V3_V4.DiContainer
{
    private static class diEntry
    {
        private static void Configure(IServiceCollection services)
        {
            // vm
            services.AddTransient<vmEntry>();

            // view
            services.AddTransient<viewEntry>();
        }
    }

}

