﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Graphite.Actions;
using Graphite.DependencyInjection;
using Graphite.Exceptions;
using Graphite.Http;

namespace Graphite.Behaviors
{
    public class BehaviorChainInvoker : IBehaviorChainInvoker
    {
        private readonly Configuration _configuration;
        private readonly IContainer _container;
        private readonly ActionDescriptor _actionDescriptor;
        private readonly IExceptionHandler _exceptionHandler;

        public BehaviorChainInvoker(Configuration configuration, IContainer container,
            ActionDescriptor actionDescriptor, IExceptionHandler exceptionHandler)
        {
            _configuration = configuration;
            _container = container;
            _actionDescriptor = actionDescriptor;
            _exceptionHandler = exceptionHandler;
        }

        public virtual async Task<HttpResponseMessage> Invoke(ActionDescriptor actionDescriptor, 
            HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        {
            var requestContainer = _container.CreateScopedContainer();

            try
            {
                requestMessage.RegisterForDispose(requestContainer);

                Register(requestContainer, actionDescriptor, requestMessage, cancellationToken);

                return await requestContainer.GetInstance<IBehaviorChain>(
                    _configuration.BehaviorChain).InvokeNext();
            }
            catch (Exception exception)
            {
                return _exceptionHandler.HandleException(exception,
                    _actionDescriptor, requestMessage, _container);
            }
        }

        public virtual void Register(IContainer container, ActionDescriptor actionDescriptor,
            HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        {
            var httpRequestContext = requestMessage.GetRequestContext();
            var responseMessage = requestMessage.CreateResponse();
            container.IncludeRegistry(actionDescriptor.Registry);

            container.Register(container);
            container.Register(requestMessage);
            container.Register(responseMessage);
            container.Register(responseMessage.Headers);
            container.Register(actionDescriptor);
            container.Register(actionDescriptor.Action);
            container.Register(actionDescriptor.Route);
            container.Register(httpRequestContext);
            container.Register(new RequestCancellation(cancellationToken));
            container.Register(requestMessage.UrlParameters());
            container.Register(requestMessage.Querystring());
        }
    }
}
