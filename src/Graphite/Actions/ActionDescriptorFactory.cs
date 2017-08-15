﻿using System.Web.Http;
using Graphite.Routing;

namespace Graphite.Actions
{
    public class ActionDescriptorFactory
    {
        private readonly Configuration _configuration;
        private readonly HttpConfiguration _httpConfiguration;

        public ActionDescriptorFactory(Configuration configuration,
            HttpConfiguration httpConfiguration)
        {
            _configuration = configuration;
            _httpConfiguration = httpConfiguration;
        }

        public ActionDescriptor CreateDescriptor(ActionMethod actionMethod, 
            RouteDescriptor routeDescriptor)
        {
            var actionConfigurationContext = new ActionConfigurationContext(
                _configuration, _httpConfiguration, actionMethod, routeDescriptor);
            return new ActionDescriptor(actionMethod, routeDescriptor,
                _configuration.Authenticators.CloneAllThatApplyTo(actionConfigurationContext),
                _configuration.RequestBinders.CloneAllThatApplyTo(actionConfigurationContext),
                _configuration.RequestReaders.CloneAllThatApplyTo(actionConfigurationContext),
                _configuration.ResponseWriters.CloneAllThatApplyTo(actionConfigurationContext),
                _configuration.Behaviors.CloneAllThatApplyTo(actionConfigurationContext));
        }
    }
}