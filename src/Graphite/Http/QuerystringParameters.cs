using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Graphite.Extensions;

namespace Graphite.Http
{
    public interface IQuerystringParameters : ILookup<string, object> { }

    public class QuerystringParameters : IQuerystringParameters
    {
        private readonly IEnumerable<KeyValuePair<string, string>> _parameters;

        public QuerystringParameters(HttpRequestMessage request)
        {
            _parameters = request.GetQueryNameValuePairs();
        }

        public bool Contains(string key) => _parameters.Any(x => x.Key.EqualsUncase(key));

        public int Count => _parameters.Count();

        public IEnumerable<object> this[string key] => _parameters
            .Where(x => x.Key.EqualsUncase(key)).Select(x => x.Value);

        public IEnumerator<IGrouping<string, object>> GetEnumerator()
        {
            return _parameters.GroupBy(x => x.Key, x => x.Value)
                .Cast<IGrouping<string, object>>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}