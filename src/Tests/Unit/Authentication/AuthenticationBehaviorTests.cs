﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Graphite;
using Graphite.Actions;
using Graphite.Authentication;
using Graphite.Behaviors;
using Graphite.Extensions;
using NSubstitute;
using NUnit.Framework;
using Should;
using Tests.Common;

namespace Tests.Unit.Authentication
{
    [TestFixture]
    public class AuthenticationBehaviorTests
    {
        private TestBasicAuthenticator _basicAuthenticator;
        private TestBearerTokenAuthenticator _bearerTokenAuthenticator;
        private ActionConfigurationContext _actionConfigurationContext;
        private Configuration _configuration;
        private HttpRequestMessage _requestMessage;
        private HttpResponseMessage _responseMessage;
        private IBehaviorChain _BehaviorChain;
        private List<IAuthenticator> _authenticators;
        private AuthenticationBehavior _behavior;

        [SetUp]
        public void Setup()
        {
            _authenticators = new List<IAuthenticator>();
            _basicAuthenticator = new TestBasicAuthenticator("fark", "farker");
            _bearerTokenAuthenticator = new TestBearerTokenAuthenticator("fark");
            _configuration = new Configuration();
            _actionConfigurationContext = new ActionConfigurationContext(
                new ConfigurationContext(_configuration, null), new ActionDescriptor(null, null));
            _requestMessage = new HttpRequestMessage();
            _responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            _BehaviorChain = Substitute.For<IBehaviorChain>();
            _BehaviorChain.InvokeNext().Returns(_responseMessage);
            _behavior = new AuthenticationBehavior(_BehaviorChain, _requestMessage, 
                _responseMessage, _authenticators, _configuration, _actionConfigurationContext);
        }

        [Test]
        public async Task Should_thow_exception_if_no_authenticators_found()
        {
            await _behavior.Should().Throw<GraphiteException>(async x => await x.Invoke());

            await _BehaviorChain.DidNotReceiveWithAnyArgs().InvokeNext();
        }

        [Test]
        public async Task Should_return_unauthorized_if_no_authorization_header_found()
        {
            _authenticators.Add(_basicAuthenticator);
            
            var result = await _behavior.Invoke();
            
            await _BehaviorChain.DidNotReceiveWithAnyArgs().InvokeNext();

            Should_be_unauthorized(result, authenticators: _basicAuthenticator);
        }

        [Test]
        public async Task Should_return_unauthorized_if_no_matching_authenticators_found()
        {
            _requestMessage.SetBasicAuthorizationHeader("fark", "farker");
            _authenticators.Add(_bearerTokenAuthenticator);

            var result = await _behavior.Invoke();
            
            await _BehaviorChain.DidNotReceiveWithAnyArgs().InvokeNext();

            Should_be_unauthorized(result, authenticators: _bearerTokenAuthenticator);
        }

        [Test]
        public async Task Should_return_unauthorized_if_authentication_fails()
        {
            _requestMessage.SetBasicAuthorizationHeader("fark", "wrong");
            _authenticators.Add(_basicAuthenticator);

            var result = await _behavior.Invoke();
            
            await _BehaviorChain.DidNotReceiveWithAnyArgs().InvokeNext();

            Should_be_unauthorized(result, authenticators: _basicAuthenticator);
        }

        [Test]
        public async Task Should_return_multiple_challenges_when_multiple_authenticators_defined()
        {
            _requestMessage.SetBasicAuthorizationHeader("fark", "wrong");
            _authenticators.Add(_basicAuthenticator);
            _authenticators.Add(_bearerTokenAuthenticator);

            var result = await _behavior.Invoke();

            await _BehaviorChain.DidNotReceiveWithAnyArgs().InvokeNext();

            Should_be_unauthorized(result, null, null, _basicAuthenticator, _bearerTokenAuthenticator);
        }

        [Test]
        public async Task Should_only_use_authenticators_that_apply()
        {
            _requestMessage.SetBasicAuthorizationHeader("fark", "farker");
            _authenticators.Add(_basicAuthenticator);
            _authenticators.Add(_bearerTokenAuthenticator);
            _configuration.Authenticators.Append(_basicAuthenticator, x => false);

            var result = await _behavior.Invoke();

            await _BehaviorChain.DidNotReceiveWithAnyArgs().InvokeNext();

            Should_be_unauthorized(result, null, null, _bearerTokenAuthenticator);
        }

        [Test]
        public async Task Should_call_inner_behavior_if_authentication_succeeds()
        {
            _requestMessage.SetBasicAuthorizationHeader("fark", "farker");
            _authenticators.Add(_basicAuthenticator);

            var result = await _behavior.Invoke();

            result.StatusCode.ShouldEqual(HttpStatusCode.OK);
            await _BehaviorChain.ReceivedWithAnyArgs().InvokeNext();
        }

        [TestCase(null, "config")]
        [TestCase("authenticator", "authenticator")]
        public async Task Should_set_unauthorized_status_message(
            string authenticatorStatusMessage, string expected)
        {
            _requestMessage.SetBasicAuthorizationHeader("fark", "wrong");
            _configuration.DefaultUnauthorizedStatusMessage = "config";
            _basicAuthenticator.StatusMessageOverride = authenticatorStatusMessage;
            _authenticators.Add(_basicAuthenticator);

            var result = await _behavior.Invoke();

            Should_be_unauthorized(result, null, expected, authenticators: _basicAuthenticator);
        }

        [TestCase(null, "config")]
        [TestCase("authenticator", "authenticator")]
        public async Task Should_set_realm(string authenticatorRealm, string expected)
        {
            _requestMessage.SetBasicAuthorizationHeader("fark", "wrong");
            _configuration.DefaultAuthenticationRealm = "config";
            _basicAuthenticator.RealmOverride = authenticatorRealm;
            _authenticators.Add(_basicAuthenticator);

            var result = await _behavior.Invoke();

            Should_be_unauthorized(result, expected, authenticators: _basicAuthenticator);
        }

        private void Should_be_unauthorized(HttpResponseMessage responseMessage, 
            string realm = null, string statusMessage = null, params IAuthenticator[] authenticators)
        {
            responseMessage.StatusCode.ShouldEqual(HttpStatusCode.Unauthorized);
            responseMessage.ReasonPhrase.ShouldEqual(statusMessage ?? 
                _configuration.DefaultUnauthorizedStatusMessage ?? "Unauthorized");

            responseMessage.Headers.WwwAuthenticate.Count.ShouldEqual(authenticators.Length);

            foreach (var authenticator in authenticators)
            {
                var header = responseMessage.Headers.WwwAuthenticate.FirstOrDefault(
                    x => x.Scheme.EqualsIgnoreCase(authenticator.Scheme));

                header.ShouldNotBeNull();
                realm = realm ?? authenticator.Realm;
                header.Parameter.ShouldEqual(realm.IsNotNullOrEmpty() ? 
                    $"realm=\"{realm ?? authenticator.Realm}\"" : null);
            }
        }

        public class TestBearerTokenAuthenticator : BearerTokenAuthenticatorBase
        {
            private readonly string _token;

            public TestBearerTokenAuthenticator(string token)
            {
                _token = token;
            }

            public override bool Authenticate(string credentials)
            {
                return credentials == _token;
            }
        }
    }
}