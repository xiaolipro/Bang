﻿using Fake.Modularity;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Fake.Auditing;

public class FakeAuditingModule:FakeModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.OnRegistered(AuditingInterceptorRegistrar.RegisterIfNeeded);
    }
}