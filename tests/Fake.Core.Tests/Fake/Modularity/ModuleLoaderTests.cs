using Fake.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Fake.Modularity;

public class ModuleLoaderTests
{
    [Fact]
    void 应该按照依赖顺序加载模块()
    {
        var loader = new FakeModuleLoader();
        var services = new ServiceCollection();
        services.AddSingleton<IInitLoggerFactory>(new FakeInitLoggerFactory());
        var modules = loader.LoadModules(services, typeof(StartupModule));
        
        modules.Length.ShouldBe(3);
        modules[0].Type.ShouldBe(typeof(FakeCoreModule));
        modules[1].Type.ShouldBe(typeof(CustomModule));
        modules[2].Type.ShouldBe(typeof(StartupModule));
    }
    
    [Fact]
    void 没有注册日志应该抛异常()
    {
        Should.Throw<InvalidOperationException>(() =>
        {
            var loader = new FakeModuleLoader();
            var services = new ServiceCollection();
            loader.LoadModules(services, typeof(StartupModule));
        });
    }
}

[DependsOn(typeof(CustomModule))]
public class StartupModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}

public class CustomModule : FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}