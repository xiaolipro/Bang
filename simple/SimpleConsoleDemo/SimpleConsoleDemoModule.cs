using Fake.Auditing;
using Fake.Autofac;
using Fake.Identity;
using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleConsoleDemo;

[DependsOn(typeof(FakeAuditingModule),typeof(FakeAutofacModule),typeof(FakeIdentityModule))]
public class SimpleConsoleDemoModule:FakeModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<FakeAuditingOptions>(options =>
        {
            // options.IsEnabledActionLog = false;
            // options.IsEnabledExceptionLog = false;
        });
    }
}