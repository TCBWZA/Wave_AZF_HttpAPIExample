using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NeoWarewholesale.API.DTOs;
using NeoWarewholesale.API.DTOs.External;
using AZF_HttpAPIExample;
using AZF_HttpAPIExample.Models;
using NUnit.Framework;

namespace AZF_HttpAPIExample.Tests
{
    public class OrdersFunctionIntegrationTests
    {
        private IConfiguration _configuration;
        private const string ApiBaseUrl = "https://api.test";

        [SetUp]
        public void Setup()
        {
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>{{"ApiBaseUrl", ApiBaseUrl}})
                .Build();
        }

        [Test]
        public async Task CreateOrder_ReturnsCreatedResult_WithExternalApiSuccess()
        {
            var createdOrder = new OrderDto { Id = 101, SupplierId = 1, CustomerId = 10 };
            var handler = new StubHttpMessageHandler(request =>
            {
                if (request.Method == HttpMethod.Post && request.RequestUri?.AbsoluteUri == $"{ApiBaseUrl}/orders")
                {
                    var response = new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(createdOrder), Encoding.UTF8, "application/json")
                    };
                    return response;
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var function = CreateFunction(handler);

            var dto = new CreateOrderDto
            {
                SupplierId = 1,
                OrderDate = DateTime.UtcNow,
                OrderItems = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = 1, Quantity = 1, Price = 10m }
                }
            };

            var request = BuildJsonRequest(JsonSerializer.Serialize(dto));
            var result = await function.CreateOrder(request);

            Assert.That(result, Is.TypeOf<Microsoft.AspNetCore.Mvc.CreatedResult>());
            var createdResult = (Microsoft.AspNetCore.Mvc.CreatedResult)result;
            Assert.That(createdResult.Value, Is.InstanceOf<OrderDto>());
            Assert.That(((OrderDto)createdResult.Value).Id, Is.EqualTo(101));
        }

        [Test]
        public async Task CreateOrder_ReturnsBadRequest_WhenSupplierIdMissing()
        {
            var dto = new CreateOrderDto { SupplierId = 0, OrderDate = DateTime.UtcNow, OrderItems = new List<CreateOrderItemDto>() };
            var request = BuildJsonRequest(JsonSerializer.Serialize(dto));
            var function = CreateFunction(new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));

            var result = await function.CreateOrder(request);

            Assert.That(result, Is.TypeOf<Microsoft.AspNetCore.Mvc.BadRequestObjectResult>());
        }

        [Test]
        public async Task CreateSpeedyOrder_ReturnsCreatedResult_WithSupplierLookup()
        {
            var handler = new StubHttpMessageHandler(request =>
            {
                if (request.Method == HttpMethod.Get && request.RequestUri?.AbsoluteUri == $"{ApiBaseUrl}/suppliers/1")
                {
                    var supplier = new { id = 1L, name = "Speedy" };
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(supplier), Encoding.UTF8, "application/json")
                    };
                }

                if (request.Method == HttpMethod.Post && request.RequestUri?.AbsoluteUri == $"{ApiBaseUrl}/orders")
                {
                    var order = new OrderDto { Id = 202, SupplierId = 1, CustomerId = 321 };
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent(JsonSerializer.Serialize(order), Encoding.UTF8, "application/json")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var function = CreateFunction(handler);

            var speedyOrder = new SpeedyOrderDto
            {
                CustomerId = 5,
                OrderTimestamp = DateTime.UtcNow,
                LineItems = new List<SpeedyLineItemDto>
                {
                    new SpeedyLineItemDto { ProductId = 11, Qty = 1, UnitPrice = 15m }
                }
            };

            var request = BuildJsonRequest(JsonSerializer.Serialize(speedyOrder));
            var result = await function.CreateSpeedyOrder(request);

            Assert.That(result, Is.TypeOf<Microsoft.AspNetCore.Mvc.CreatedResult>());
            var created = (Microsoft.AspNetCore.Mvc.CreatedResult)result;
            Assert.That(created.Value, Is.InstanceOf<OrderDto>());
            Assert.That(((OrderDto)created.Value).Id, Is.EqualTo(202));
        }

        private static HttpRequest BuildJsonRequest(string payload)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.ContentType = "application/json";
            var bytes = Encoding.UTF8.GetBytes(payload);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
            context.Request.Body.Position = 0;
            return context.Request;
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
