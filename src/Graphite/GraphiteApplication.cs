﻿using System;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using Graphite.DependencyInjection;
using Graphite.Extensibility;
using Graphite.Monitoring;
using Graphite.Reflection;

namespace Graphite
{
    public class GraphiteApplication : IDisposable
    {
        private readonly HttpConfiguration _httpConfiguration;

        public GraphiteApplication(HttpConfiguration httpConfiguration)
        {
            _httpConfiguration = httpConfiguration;
        }

        public IContainer Container { get; private set; }
        public bool Initialized => Container != null;
        public Metrics Metrics { get; private set; }

        public void Initialize(Action<ConfigurationDsl> configure, 
            params Assembly[] defaultAssemblies)
        {
            var configuration = new Configuration();

            configuration.Assemblies.AddRange(defaultAssemblies
                .Where(x => !x.IsSystemAssembly()));

            configure?.Invoke(new ConfigurationDsl(configuration, _httpConfiguration));

            Initialize(configuration);
        }

        public void Initialize(Configuration configuration)
        {
            if (Initialized) return;

            try
            {
                Metrics = new Metrics();
                Metrics.BeginStartup();
                
                Container = new TrackingContainer(configuration.Container);

                Container.Register(Container);
                Container.Register(Metrics);
                Container.Register(configuration);
                Container.Register(_httpConfiguration);
                configuration.Container.RegisterPlugin(configuration.TypeCache);
                Container.RegisterPlugins(configuration.ActionMethodSources);
                Container.RegisterPlugins(configuration.ActionSources);
                Container.RegisterPlugins(configuration.ActionDecorators);
                Container.RegisterPlugins(configuration.RouteConventions);
                Container.RegisterPlugin(configuration.InlineConstraintBuilder);
                Container.RegisterPlugin(configuration.ConstraintBuilder);
                Container.RegisterPlugins(configuration.UrlConventions);
                Container.RegisterPlugin(configuration.Initializer);
                Container.RegisterPlugin(configuration.HttpRouteMapper);
                Container.RegisterPlugin(configuration.RequestPropertiesProvider);
                Container.RegisterPlugin(configuration.PathProvider);
                Container.RegisterPlugins(configuration.HttpRouteDecorators);
                Container.RegisterPlugin(configuration.InlineConstraintResolver);
                Container.RegisterPlugin(configuration.UnhandledExceptionHandler);
                Container.RegisterPlugin(configuration.BehaviorChainInvoker);
                Container.RegisterPlugins(configuration.Authenticators);
                Container.RegisterPlugin(configuration.ActionInvoker);
                Container.RegisterPlugins(configuration.RequestBinders);
                Container.RegisterPlugins(configuration.RequestReaders);
                Container.RegisterPlugins(configuration.ValueMappers);
                Container.RegisterPlugins(configuration.ResponseWriters);
                Container.IncludeRegistry(configuration.Registry);

                Container.GetInstance<IInitializer>().Initialize();

                Metrics.StartupComplete();
            }
            catch (Exception exception)
            {
                throw new GraphiteInitializationException(exception);
            }
        }

        public void Dispose()
        {
            Container?.Dispose();
        }
    }
}
