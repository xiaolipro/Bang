﻿using System.Reflection;
using Fake.DependencyInjection;
using Fake.Extensions;
using Fake.Logging;
using Fake.Modularity;
using Microsoft.Extensions.Logging;

namespace Fake;

public class FakeApplication : IFakeApplicationInfo
{
    public IReadOnlyList<IModuleDescriptor> Modules { get; }
    public string ApplicationName { get; }
    public string ApplicationId { get; } = Guid.NewGuid().ToString();

    public Type StartupModuleType { get; }
    public IServiceCollection Services { get; }

    private IServiceProvider _serviceProvider;
    public IServiceProvider ServiceProvider
    {
        get
        {
            if (!_initializedModules) throw new FakeException($"模块初始化前，不能使用{nameof(ServiceProvider)}");
            return _serviceProvider;
        }
    }

    private bool _configuredServices;
    private bool _initializedModules;

    internal FakeApplication(Type startupModuleType, Action<FakeApplicationCreationOptions> optionsAction)
        : this(startupModuleType, new ServiceCollection(), optionsAction)
    {
    }

    internal FakeApplication(
        [NotNull] Type startupModuleType,
        [NotNull] IServiceCollection services,
        [CanBeNull] Action<FakeApplicationCreationOptions> optionsAction)
    {
        ThrowHelper.ThrowIfNull(startupModuleType, nameof(startupModuleType));
        ThrowHelper.ThrowIfNull(services, nameof(services));

        StartupModuleType = startupModuleType;
        Services = services;

        services.GetOrAddObjectAccessor<IServiceProvider>();

        var options = new FakeApplicationCreationOptions(services);
        optionsAction?.Invoke(options);

        ApplicationName = GetApplicationName(options);

        services.AddSingleton(this);
        services.AddSingleton<IModuleContainer>(this);

        services.AddLogging();
        services.AddFakeCoreServices(this, options);

        Modules = LoadModules(services);

        ConfigureServices();
    }


    public void ConfigureServices()
    {
        if (_configuredServices)
        {
            throw new FakeInitializationException($"{nameof(ConfigureServices)}已经调用过了，不要重复调用");
        }

        var context = new ServiceConfigurationContext(Services);

        // PreConfigureServices
        foreach (var module in Modules)
        {
            try
            {
                module.Instance.PreConfigureServices(context);
            }
            catch (Exception ex)
            {
                throw new FakeInitializationException(
                    $"模块{module.Type.AssemblyQualifiedName}在{nameof(IConfigureServicesLifecycle.PreConfigureServices)} 阶段发生异常。有关详细信息，请参阅内部异常。",
                    ex);
            }
        }

        // ConfigureServices
        var assemblies = new HashSet<Assembly>();
        foreach (var module in Modules)
        {
            if (module.Instance is FakeModule FakeModule)
            {
                if (!FakeModule.SkipAutoServiceRegistration)
                {
                    var assembly = module.Type.Assembly;
                    if (!assemblies.Contains(assembly))
                    {
                        Services.AddAssembly(assembly);
                        assemblies.Add(assembly);
                    }
                }
            }
            
            try
            {
                module.Instance.ConfigureServices(context);
            }
            catch (Exception ex)
            {
                throw new FakeInitializationException(
                    $"模块{module.Type.AssemblyQualifiedName}在{nameof(IConfigureServicesLifecycle.ConfigureServices)}阶段发生异常。有关详细信息，请参阅内部异常。",
                    ex);
            }
        }

        // PostConfigureServices
        foreach (var module in Modules)
        {
            try
            {
                module.Instance.PostConfigureServices(context);
            }
            catch (Exception ex)
            {
                throw new FakeInitializationException(
                    $"模块{module.Type.AssemblyQualifiedName}在{nameof(IConfigureServicesLifecycle.PostConfigureServices)}阶段发生异常。有关详细信息，请参阅内部异常。",
                    ex);
            }
        }

        _configuredServices = true;
    }

    

    public virtual void Shutdown()
    {
        // Shutdown
        var context = new ApplicationShutdownContext(_serviceProvider);
        foreach (var module in Modules)
        {
            try
            {
                module.Instance.Shutdown(context);
            }
            catch (Exception ex)
            {
                throw new FakeInitializationException(
                    $"模块{module.Type.AssemblyQualifiedName}在{nameof(IShutdownLifecycle.Shutdown)}阶段发生异常。有关详细信息，请参阅内部异常。",
                    ex);
            }
        }
    }

    public virtual void Dispose()
    {
        // 应该在这里销毁ServiceProvider，但Shutdown可能还没被调用
    }

    public virtual void InitializeModules([CanBeNull]IServiceProvider serviceProvider = null)
    {
        if (_initializedModules)
        {
            throw new FakeInitializationException($"{nameof(InitializeModules)}已经调用过了，不要重复调用");
        }
        SetServiceProvider(serviceProvider);
        
        // log
        var logger = _serviceProvider.GetRequiredService<ILogger<FakeApplication>>();

        var initLogger = _serviceProvider.GetRequiredService<IFakeLoggerFactory>().Create<FakeApplication>();

        foreach (var entry in initLogger.Entries)
        {
            logger.Log(entry.LogLevel, entry.EventId, entry.State, entry.Exception, entry.Formatter);
        }

        initLogger.Entries.Clear();

        var context = new ApplicationConfigureContext(_serviceProvider);

        // PreConfigure
        foreach (var module in Modules)
        {
            try
            {
                module.Instance.PreConfigure(context);
            }
            catch (Exception ex)
            {
                throw new FakeInitializationException(
                    $"模块{module.Type.AssemblyQualifiedName}在{nameof(IConfigureLifecycle.PreConfigure)}阶段发生异常。有关详细信息，请参阅内部异常。",
                    ex);
            }
        }

        // Configure
        foreach (var module in Modules)
        {
            try
            {
                module.Instance.Configure(context);
            }
            catch (Exception ex)
            {
                throw new FakeInitializationException(
                    $"模块{module.Type.AssemblyQualifiedName}在{nameof(IConfigureLifecycle.Configure)}阶段发生异常。有关详细信息，请参阅内部异常。",
                    ex);
            }
        }

        // PostConfigure
        foreach (var module in Modules)
        {
            try
            {
                module.Instance.PostConfigure(context);
            }
            catch (Exception ex)
            {
                throw new FakeInitializationException(
                    $"模块{module.Type.AssemblyQualifiedName}在{nameof(IConfigureLifecycle.PostConfigure)}阶段发生异常。有关详细信息，请参阅内部异常。",
                    ex);
            }
        }
        _initializedModules = true;
    }
    
    protected virtual void SetServiceProvider([CanBeNull]IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider?? Services.BuildServiceProvider();
        _serviceProvider.GetRequiredService<ObjectAccessor<IServiceProvider>>().Value = serviceProvider;
    }
    
    private IReadOnlyList<IModuleDescriptor> LoadModules(IServiceCollection services)
    {
        return services.GetSingletonInstance<IModuleLoader>().LoadModules(services, StartupModuleType);
    }

    private static string GetApplicationName(FakeApplicationCreationOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ApplicationName))
        {
            return options.ApplicationName;
        }

        var configuration = options.Services.GetConfigurationOrDefault();

        if (configuration == default) return Assembly.GetEntryAssembly()?.GetName().Name;

        return configuration[nameof(IApplicationInfo.ApplicationName)];
    }
    
    private void RegisterService(IModuleDescriptor module)
    {
        if (module.Instance is not FakeModule FakeModule) return;
        
        if (FakeModule.SkipAutoServiceRegistration) return;

        Services.AddAssembly(module.Assembly);
    }
}