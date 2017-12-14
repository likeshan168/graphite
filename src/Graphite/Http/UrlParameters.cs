using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Graphite.Extensions;
using Graphite.Linq;
using Graphite.Routing;

namespace Graphite.Http
{
    public interface IUrlParameters : ILookup<string, object> { }

    public class UrlParameters : IUrlParameters
    {
        private readonly IEnumerable<KeyValuePair<string, object>> _parameters;
        private readonly RouteDescriptor _routeDescriptor;

        public UrlParameters(HttpRequestMessage request, RouteDescriptor routeDescriptor)
        {
            _routeDescriptor = routeDescriptor;
            var parameters = request.GetRequestContext().RouteData.Values;
            _parameters = _routeDescriptor.UrlParameters.Any(x => x.IsWildcard)
                ? parameters.SelectMany(ExpandWildcardParameters)
                : parameters;

        }

        public int Count => _parameters.Count();
        
        public bool Contains(string key) => _parameters.Any(x => x.Key.EqualsUncase(key));
        
        public IEnumerable<object> this[string key] => _parameters
            .Where(x => x.Key.EqualsUncase(key)).Select(x => x.Value);

        public IEnumerator<IGrouping<string, object>> GetEnumerator()
        {
            foreach (var parameter in _parameters)
            {
                yield return new ValueGrouping<string, object>(parameter.Key, parameter.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private IEnumerable<KeyValuePair<string, object>> ExpandWildcardParameters
            (KeyValuePair<string, object> value)
        {
            var wildcard = _routeDescriptor.UrlParameters.FirstOrDefault(x => 
                x.IsWildcard && x.Name.EqualsUncase(value.Key));
            return wildcard == null || !(wildcard.TypeDescriptor.IsArray || 
                    wildcard.TypeDescriptor.IsGenericListCastable)
                ? new[] { value }
                : value.Value.Split('/').ToKeyValuePairs(value.Key);
        }
    }
}