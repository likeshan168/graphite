﻿using System;
using System.Collections.Generic;

namespace Graphite.DependencyInjection
{
    public interface ITrackingContainer
    {
        Registry ParentRegistry { get; }
        Registry Registry { get; }
    }

    public class TrackingContainer : IContainer, ITrackingContainer
    {
        private readonly IContainer _container;

        public TrackingContainer(IContainer container)
        {
            _container = container;
            ParentRegistry = new Registry();
            Registry = new Registry();
        }

        public TrackingContainer(IContainer container, Registry registry)
        {
            _container = container;
            ParentRegistry = new Registry(registry);
            Registry = new Registry();
        }

        public Registry ParentRegistry { get; }
        public Registry Registry { get; }

        public void Register(Type plugin, Type concrete, bool singleton)
        {
            Registry.Register(plugin, concrete, singleton);
            _container.Register(plugin, concrete, singleton);
        }

        public void Register(Type plugin, object instance, bool dispose)
        {
            Registry.Register(plugin, instance, dispose);
            _container.Register(plugin, instance, dispose);
        }

        public string GetConfiguration()
        {
            return _container.GetConfiguration();
        }

        public IContainer CreateScopedContainer()
        {
            return new TrackingContainer(_container.CreateScopedContainer(), Registry);
        }

        public object GetInstance(Type type, params Dependency[] dependencies)
        {
            return _container.GetInstance(type, dependencies);
        }

        public IEnumerable<object> GetInstances(Type type)
        {
            return _container.GetInstances(type);
        }

        public void Dispose()
        {
            _container.Dispose();
        }
    }
}
