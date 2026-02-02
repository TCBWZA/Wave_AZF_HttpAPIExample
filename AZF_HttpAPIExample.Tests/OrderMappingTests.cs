using NUnit.Framework;
using NeoWarewholesale.API.DTOs.External;
using NeoWarewholesale.API.Mappings;
using NeoWarewholesale.API.DTOs;
using NeoWarewholesale.API.Models;
using AZF_HttpAPIExample.Models;
using AZF_HttpAPIExample.Extensions;
using System;
using System.Collections.Generic;

namespace AZF_HttpAPIExample.Tests
{
    public class OrderMappingTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void MappingExtensions_OrderToDto_MapsFieldsCorrectly()
        {
            var order = new Order
            {
                Id = 10,
                CustomerId = 5,
                SupplierId = 2,
                Supplier = new Supplier { Id = 2, Name = "Acme" },
                OrderDate = new DateTime(2024, 1, 1),
                CustomerEmail = "a@b.com",
                BillingAddress = new Address { Street = "1 A St", City = "Town", County = "County", PostalCode = "12345", Country = "GB" },
                DeliveryAddress = new Address { Street = "2 B St", City = "City", County = "County", PostalCode = "54321", Country = "GB" },
                OrderStatus = OrderStatus.Received,
                OrderItems = new List<OrderItem>
                {
                    new OrderItem { Id = 1, ProductId = 100, Quantity = 2, Price = 5.5m, Product = new Product { Name = "Widget", ProductCode = Guid.Parse("11111111-1111-1111-1111-111111111111") } }
                }
            };

            var dto = order.ToDto();

            Assert.That(dto.Id, Is.EqualTo(order.Id));
            Assert.That(dto.CustomerId, Is.EqualTo(order.CustomerId));
            Assert.That(dto.SupplierId, Is.EqualTo(order.SupplierId));
            Assert.That(dto.SupplierName, Is.EqualTo("Acme"));
            Assert.That(dto.OrderDate, Is.EqualTo(order.OrderDate));
            Assert.That(dto.CustomerEmail, Is.EqualTo(order.CustomerEmail));
            Assert.That(dto.BillingAddress, Is.Not.Null);
            Assert.That(dto.DeliveryAddress, Is.Not.Null);
            Assert.That(dto.OrderStatus, Is.EqualTo(order.OrderStatus));
            Assert.That(dto.OrderItems?.Count, Is.EqualTo(1));
            Assert.That(dto.TotalAmount, Is.EqualTo(11.0m));
        }

        [Test]
        public void VaultFunction_ConvertVaultdto_ConvertsCorrectly()
        {
            var vault = new VaultOrderDto
            {
                CustomerEmail = "cust@example.com",
                PlacedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                DeliveryDetails = new VaultDeliveryDetailsDto
                {
                    BillingLocation = new VaultLocationDto { AddressLine = "Bill St", CityName = "Btown", StateProvince = "Bcounty", ZipPostal = "11111", CountryCode = "GB" },
                    ShippingLocation = new VaultLocationDto { AddressLine = "Ship St", CityName = "Stown", StateProvince = "Scounty", ZipPostal = "22222", CountryCode = "GB" }
                },
                Items = new List<VaultItemDto>
                {
                    new VaultItemDto { ProductCode = Guid.NewGuid(), QuantityOrdered = 3, PricePerUnit = 2.5m }
                }
            };

            // Convert Vault DTO to internal CreateOrderDto using extension method
            // Simulate product id resolution by providing CreateOrderItemDto instances
            var createOrderItems = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 1, Quantity = vault.Items[0].QuantityOrdered, Price = vault.Items[0].PricePerUnit }
            };

            var result = vault.ToCreateOrderDto(99, createOrderItems);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.CustomerId, Is.EqualTo(99));
            // Vault mapping sets SupplierId = 2 in the extensions
            Assert.That(result.SupplierId, Is.EqualTo(2));
            Assert.That(result.BillingAddress, Is.Not.Null);
            Assert.That(result.DeliveryAddress, Is.Not.Null);
            Assert.That(result.OrderItems.Count, Is.EqualTo(1));
            Assert.That(result.OrderItems[0].Quantity, Is.EqualTo(3));
            Assert.That(result.OrderItems[0].Price, Is.EqualTo(2.5m));
        }
    }
}
