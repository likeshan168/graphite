﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Bender.Extensions;
using Graphite.Extensibility;
using Graphite.Extensions;
using Tests.Common.Fakes;

namespace Tests.Common
{
    public class List<TKey, TValue> : List<KeyValuePair<TKey, TValue>>
    {
        public ILookup<TKey, TValue> ToLookup()
        {
            return this.ToLookup(x => x.Key, x => x.Value);
        }
    }

    public static class Extensions
    {
        public static void Add<TKey, TValue>(
            this List<KeyValuePair<TKey, TValue>> source, 
            TKey key, TValue value)
        {
            source.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public static string ReadAllText(this Stream stream)
        {
            using (stream)
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        public static void Clear(this HttpConfiguration configuration)
        {
            configuration.Routes.Clear();
            configuration.DependencyResolver = new EmptyResolver();
        }

        public static TimeSpan Elapsed(this Action action)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        public static void Times(this int iterations, Action action)
        {
            for (var i = 0; i < iterations; i++)
            {
                action();
            }
        }

        public static void TimesParallel(this int iterations, Action action)
        {
            1.To(iterations).AsParallel().ForEach(x => action());
        }

        public static Task<HttpResponseMessage> SendAsync(this HttpMessageHandler handler,
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return (Task<HttpResponseMessage>) handler.GetType()
                .GetMethod("SendAsync", BindingFlags.Instance | 
                        BindingFlags.NonPublic).Invoke(handler, 
                    new object[] { request, cancellationToken });
        }

        public static T WaitForResult2<T>(this Task<T> task)
        {
            task.Wait();
            return task.Result;
        }

        public static IEnumerable<T> Generate<T>(this T seed, Func<T, T> generator)
        {
            var item = seed;
            while (true)
            {
                yield return item;
                var nextItem = generator(item);
                if (nextItem == null) yield break;
                item = nextItem;
            }
        }

        public static IEnumerable<Exception> GetChain(this Exception exception)
        {
            return exception.Generate(x => x.InnerException);
        }

        public static void AddRange<T>(this List<T> source, params T[] items)
        {
            source.AddRange((IEnumerable<T>)items);
        }

        public static void Append<TPlugin, TContext>(this
            PluginDefinitions<TPlugin, TContext> definitions, Type type,
            Func<TContext, bool> predicate = null)
        {
            typeof(PluginDefinitions<TPlugin, TContext>)
                .GetMethods().FirstOrDefault(x => x.Name == nameof(
                    PluginDefinitions<TPlugin, TContext>.Append))
                .MakeGenericMethod(type)
                .Invoke(definitions, new object[] { predicate });
        }

        public static void WriteAllText(this Stream stream, string text)
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(text);
                writer.Flush();
            }
        }

        public static ILookup<string, string> GetHeaderValues(this string value)
        {
            return value.Split(';')
                .Select(x => x.Split('='))
                .ToLookup(x => x[0].Trim(),
                    x => x.Length > 1 ? x[1].Trim(' ', '"') : "");
        }

        public static HttpResponseMessage CreateTextResponse(this string content)
        {
            return content.CreateResponseTask("text/plain");
        }

        public static HttpResponseMessage CreateHtmlResponse(this string content)
        {
            return content.CreateResponseTask("text/html");
        }

        public static HttpResponseMessage CreateJsonResponse(this string content)
        {
            return content.CreateResponseTask("application/json");
        }

        public static HttpResponseMessage CreateResponseTask(
            this string content, string contentType)
        {
            return new HttpResponseMessage
            {
                Content = new StringContent(content, Encoding.UTF8, contentType)
            };
        }
    }
}
