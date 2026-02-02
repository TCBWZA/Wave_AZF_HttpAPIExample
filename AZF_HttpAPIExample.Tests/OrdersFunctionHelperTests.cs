using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NeoWarewholesale.API.DTOs;
using AZF_HttpAPIExample;
using NUnit.Framework;

namespace AZF_HttpAPIExample.Tests
{
    public class OrdersFunctionHelperTests
    {
        private const string ApiBaseUrl = "https://api.test";
        private IConfiguration _configuration;

        [SetUp]
        public void Setup()
        {
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[] { new KeyValuePair<string, string>("ApiBaseUrl", ApiBaseUrl) })
                .Build();
        }

        [Test]
        public async Task GetCustomerIdAsync_ReturnsValue_WhenApiSucceeds()
        {
            const string email = "user@example.com";
            var handler = new StubHttpMessageHandler(request =>
            {
                if (request.RequestUri?.AbsoluteUri == $"{ApiBaseUrl}/customers/by-email/{Uri.EscapeDataString(email)}")
                {
                    var customer = new CustomerDto { Id = 42 };
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(customer), Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var function = CreateFunction(handler);

            var result = await function.GetCustomerIdAsync(email);

            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public async Task GetCustomerIdAsync_ReturnsNull_WhenApiFails()
        {
            var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
            var function = CreateFunction(handler);

            var result = await function.GetCustomerIdAsync("missing@example.com");

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GetProductIdAsync_ReturnsValue_WhenApiSucceeds()
        {
            var productCode = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
            var handler = new StubHttpMessageHandler(request =>
            {
                if (request.RequestUri?.AbsoluteUri == $"{ApiBaseUrl}/products/by-code/{productCode}")
                {
                    var product = new ProductDto { Id = 99 };
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(product), Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var function = CreateFunction(handler);

            var result = await function.GetProductIdAsync(productCode);

            Assert.That(result, Is.EqualTo(99));
        }

        [Test]
        public async Task GetProductIdAsync_ReturnsNull_WhenApiReturnsBadRequest()
        {
            var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest));
            var function = CreateFunction(handler);

            var result = await function.GetProductIdAsync(Guid.NewGuid());

            Assert.That(result, Is.Null);
        }

        private OrdersFunction CreateFunction(HttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            var factory = new StubHttpClientFactory(httpClient);
            return new OrdersFunction(NullLogger<OrdersFunction>.Instance, _configuration, factory);
        }

        private class StubHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient _client;

            public StubHttpClientFactory(HttpClient client)
            {
                _client = client;
            }

            public HttpClient CreateClient(string name) => _client;
        }

        private class StubHttpMessageHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

            public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
            {
                _responder = responder;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(_responder(request));
            }
        }
    }
}
