using System.Reflection;
using Fake;

namespace Microsoft.Extensions.DependencyInjection;

public static class FakeServiceCollectionCommonExtensions
{
    public static bool IsAdded<T>(this IServiceCollection services)
    {
        return services.IsAdded(typeof(T));
    }

    public static bool IsAdded(this IServiceCollection services, Type type)
    {
        return services.Any(d => d.ServiceType == type);
    }
    
    public static T GetSingletonInstanceOrNull<T>(this IServiceCollection services)
    {
        return (T)services
            .FirstOrDefault(d => d.ServiceType == typeof(T))
            ?.ImplementationInstance;
    }
    
    public static T GetSingletonInstance<T>(this IServiceCollection services)
    {
        var service = services.GetSingletonInstanceOrNull<T>();
        if (service == null)
        {
            throw new InvalidOperationException("找不到单例服务: " + typeof(T).AssemblyQualifiedName);
        }

        return service;
    }

    public static IServiceProvider BuildServiceProviderFromFactory([NotNull] this IServiceCollection services)
    {
        ThrowHelper.ThrowIfNull(services, nameof(services));

        // 通过服务商工厂创建服务商
        foreach (var service in services)
        {
            var factoryInterfaceType = service.ImplementationInstance?
                .GetType()
                .GetTypeInfo()
                .GetInterfaces()
                .FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IServiceProviderFactory<>));
            
            if (factoryInterfaceType == null) continue;

            var containerBuilderType = factoryInterfaceType.GenericTypeArguments[0];
            var serviceProvider = typeof(FakeServiceCollectionCommonExtensions)
                .GetMethods()
                .Single(m => m.Name == nameof(BuildServiceProviderFromFactory) && m.IsGenericMethod)
                .MakeGenericMethod(containerBuilderType)
                .Invoke(null, new object[] { services, null });

            return serviceProvider as IServiceProvider;
        }

        // 如果没有第三方IOC容器则使用默认的
        return services.BuildServiceProvider();
    }
    
    public static IServiceProvider BuildServiceProviderFromFactory<TContainerBuilder>(
        [NotNull] this ServiceCollection services, Action<TContainerBuilder> containerBuildAction)
        where TContainerBuilder : notnull
    {
        ThrowHelper.ThrowIfNull(services, nameof(services));

        
        // 这里默认是拿第一个实现，也就是第一个工厂
        // 一般来说我们要不就是使用默认的service provider，即aspnetcore通过BuildServiceProvider new出来的那个
        // 要不就是用类似autofac这种第三方provider，一个项目一般不会同时出现两个第三方service provider
        var serviceProviderFactory = services.GetSingletonInstanceOrNull<IServiceProviderFactory<TContainerBuilder>>();
        if (serviceProviderFactory == null)
        {
            throw new FakeException($"找不到{typeof(IServiceProviderFactory<TContainerBuilder>).FullName}的实现");
        }

        var builder = serviceProviderFactory.CreateBuilder(services);
        containerBuildAction?.Invoke(builder);

        return serviceProviderFactory.CreateServiceProvider(builder);
    }
}