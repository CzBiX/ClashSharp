using Microsoft.Extensions.DependencyInjection;

namespace ClashSharp.DI
{
    public static class ServiceCollectionExtensions
    {
        public static bool RemoveService<TService, TImplementation>(this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            // DI will use last found by default, so we loop it from end to start
            var index = services.Count;
            while (index-- > 0)
            {
                var item = services[index];
                if (item.ServiceType == typeof(TService) && item.ImplementationType == typeof(TImplementation))
                {
                    break;
                }
            }

            if (index < 0)
            {
                return false;
            }

            services.RemoveAt(index);
            return true;
        }
    }
}
