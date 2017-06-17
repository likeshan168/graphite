﻿using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using Graphite.Actions;
using Graphite.Extensibility;
using Graphite.Extensions;

namespace Graphite.Authentication
{
    public static class Extensions
    {
        public static HttpRequestMessage SetBasicAuthorizationHeader(
            this HttpRequestMessage request, string username, string password)
        {
            request.Headers.SetBasicAuthorizationHeader(username, password);
            return request;
        }

        public static HttpRequestHeaders SetBasicAuthorizationHeader(
            this HttpRequestHeaders headers, string username, string password)
        {
            return headers.SetAuthorizationHeader(BasicAuthenticatorBase
                .BasicScheme, $"{username}:{password}");
        }

        public static HttpRequestMessage SetBearerTokenAuthorizationHeader(
            this HttpRequestMessage request, string token)
        {
            request.Headers.SetBearerTokenAuthorizationHeader(token);
            return request;
        }

        public static HttpRequestHeaders SetBearerTokenAuthorizationHeader(
            this HttpRequestHeaders request, string token)
        {
            return request.SetAuthorizationHeader(BearerTokenAuthenticatorBase.BearerTokenScheme, token);
        }

        public static HttpRequestHeaders SetAuthorizationHeader(this HttpRequestHeaders headers,
            string authorizationType, string credentials)
        {
            headers.Authorization = new AuthenticationHeaderValue(authorizationType, credentials);
            return headers;
        }

        public static HttpResponseMessage AddAuthenticateHeader(
            this HttpResponseMessage response, string authorizationType, string realm)
        {
            response.Headers.WwwAuthenticate.Add(realm.IsNotNullOrEmpty() 
                ? new AuthenticationHeaderValue(authorizationType, $"realm=\"{realm}\"")
                : new AuthenticationHeaderValue(authorizationType));
            return response;
        }

        public static IEnumerable<IAuthenticator> ThatApplyTo(
            this IEnumerable<IAuthenticator> authenticators,
            ActionConfigurationContext actionConfigurationContext)
        {
            return actionConfigurationContext.Configuration.Authenticators
                .ThatApplyTo(authenticators, actionConfigurationContext);
        }
    }
}