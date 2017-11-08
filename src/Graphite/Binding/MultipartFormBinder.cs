using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Graphite.Actions;
using Graphite.Extensibility;
using Graphite.Extensions;
using Graphite.Http;
using Graphite.Readers;
using Graphite.Reflection;
using Graphite.Routing;

namespace Graphite.Binding
{
    public class MultipartFormBinder : IRequestBinder
    {
        private readonly HttpRequestMessage _requestMessage;
        private readonly ActionDescriptor _actionDescriptor;
        private readonly IEnumerable<IRequestReader> _readers;
        private readonly ArgumentBinder _argumentBinder;
        private readonly ParameterBinder<BindResult> _parameterBinder;
        private readonly Configuration _configuration;

        public MultipartFormBinder(HttpRequestMessage requestMessage,
            ActionDescriptor actionDescriptor, IEnumerable<IRequestReader> readers,
            ArgumentBinder argumentBinder, ParameterBinder<BindResult> parameterBinder, 
            Configuration configuration)
        {
            _requestMessage = requestMessage;
            _actionDescriptor = actionDescriptor;
            _readers = readers;
            _argumentBinder = argumentBinder;
            _parameterBinder = parameterBinder;
            _configuration = configuration;
        }

        public bool AppliesTo(RequestBinderContext context)
        {
            return _requestMessage.ContentTypeIs(MimeTypes.MultipartFormData);
        }

        public async Task<BindResult> Bind(RequestBinderContext context)
        {
            var route = _actionDescriptor.Route;
            var requestParameter = route.RequestParameter;
            var stream = await _requestMessage.Content.ReadAsStreamAsync();
            var multipartContent = new MultipartContent(stream, _requestMessage.Content, _configuration);
            var values = new List<KeyValuePair<string, object>>();

            while (true)
            {
                var content = multipartContent.Peek();

                if (route.HasRequest)
                {
                    var result = await Read(content, requestParameter, context);
                    if (result.Status == BindingStatus.Failure) return result;
                    if (result.Status == BindingStatus.Success)
                    {
                        multipartContent.Pop();
                        continue;
                    }
                }

                if (route.HasParameterNamed(content.Name) ||
                    requestParameter.HasPropertyNamed(content.Name))
                {
                    values.Add(content.Name, await content.ReadAsStringAsync());
                    multipartContent.Pop();
                    continue;
                }

                break;
            }

            var bindResult = BindValues(values, context);
            if (bindResult.Status == BindingStatus.Failure) return bindResult;
            
            BindMultipartParameters(context, multipartContent);
            BindMultipartProperties(requestParameter, context, multipartContent);

            return BindResult.Success();
        }

        private async Task<BindResult> Read(HttpContent content,
            ParameterDescriptor requestParameter,
            RequestBinderContext context)
        {
            var readContext = content.CreateReaderContext(_actionDescriptor);
            var reader = _actionDescriptor.RequestReaders
                .ThatApplyToOrDefault(_readers, readContext)
                .FirstOrDefault();

            if (reader != null)
            {
                var result = await reader.Read(readContext);
                if (result.Status == ReadStatus.Failure)
                    return BindResult.Failure(result.ErrorMessage);
                context.ActionArguments[requestParameter.Position] = result.Value;
                return BindResult.Success();
            }
            return BindResult.NoReader();
        }

        private BindResult BindValues(List<KeyValuePair<string, object>> values, 
            RequestBinderContext context)
        {
            if (values.Any())
            {
                var valueLookup = values.ToLookup();
                if (_actionDescriptor.Route.Parameters.Any())
                {
                    return _argumentBinder.Bind(valueLookup,
                        context.ActionArguments, _actionDescriptor.Route.Parameters);
                }

                if (_actionDescriptor.Route.HasRequest)
                {
                    var requestParameter = _actionDescriptor.Route.RequestParameter;
                    var instance = EnsureRequestValue(context, requestParameter);

                    var actionParameters = requestParameter.ParameterType.Properties
                        .Select(x => new ActionParameter(_actionDescriptor.Action, requestParameter, x));

                    return _parameterBinder.Bind(valueLookup, actionParameters,
                        (p, v) => p.BindProperty(instance, v), 
                        BindResult.Success, BindResult.Failure);
                }
            }
            return BindResult.Success();
        }

        private void BindMultipartParameters(RequestBinderContext context, 
            IEnumerable<InputStream> multipartParts)
        {
            var route = _actionDescriptor.Route;
            var parameter = route.RequestParameter?.ParameterType.Type ==
                    typeof(IEnumerable<InputStream>)
                ? route.RequestParameter
                : route.Parameters.FirstOrDefault(x => x
                    .TypeDescriptor.Type == typeof(IEnumerable<InputStream>))?
                    .ParameterDescriptor;

            if (parameter != null)
                context.ActionArguments[parameter.Position] = multipartParts;
        }

        private void BindMultipartProperties(ParameterDescriptor requestParameter,
            RequestBinderContext context, IEnumerable<InputStream> multipartParts)
        {
            if (!_actionDescriptor.Route.HasRequest) return;

            var property = requestParameter?.ParameterType.Properties.FirstOrDefault(x => x
                .PropertyType.Type == typeof(IEnumerable<InputStream>));

            if (property == null) return;

            var instance = EnsureRequestValue(context, requestParameter);

            property.SetValue(instance, multipartParts);
        }

        private object EnsureRequestValue(RequestBinderContext context,
            ParameterDescriptor requestParameter)
        {
            var instance = context.ActionArguments.EnsureValue(
                requestParameter.Position,
                requestParameter.ParameterType.TryCreate);

            if (instance == null)
                throw new RequestTypeCreationException(requestParameter
                    .ParameterType, _actionDescriptor.Action);

            return instance;
        }
    }
}