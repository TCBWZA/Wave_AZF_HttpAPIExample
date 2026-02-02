using NUnit.Framework;
using NeoWarewholesale.API.DTOs.External;
using NeoWarewholesale.API.Mappings;
using NeoWarewholesale.API.DTOs;
using NeoWarewholesale.API.Models;
using System;
using System.Collections.Generic;

namespace AZF_HttpAPIExample.Tests
{
    public class Tests
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

            Assert.AreEqual(order.Id, dto.Id);
            Assert.AreEqual(order.CustomerId, dto.CustomerId);
            Assert.AreEqual(order.SupplierId, dto.SupplierId);
            Assert.AreEqual("Acme", dto.SupplierName);
            Assert.AreEqual(order.OrderDate, dto.OrderDate);
            Assert.AreEqual(order.CustomerEmail, dto.CustomerEmail);
            Assert.IsNotNull(dto.BillingAddress);
            Assert.IsNotNull(dto.DeliveryAddress);
            Assert.AreEqual(order.OrderStatus, dto.OrderStatus);
            Assert.AreEqual(1, dto.OrderItems?.Count);
            Assert.AreEqual(11.0m, dto.TotalAmount);
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

            // Call static converter with a sample customer id
            var result = AZF_HttpAPIExample.VaultFunction.ConvertVaultdto(vault, 99).Result;

            Assert.IsNotNull(result);
            Assert.AreEqual(99, result.CustomerId);
            Assert.AreEqual(1, result.SupplierId);
            Assert.IsNotNull(result.BillingAddress);
            Assert.IsNotNull(result.DeliveryAddress);
            Assert.AreEqual(1, result.OrderItems.Count);
            Assert.AreEqual(3, result.OrderItems[0].Quantity);
            Assert.AreEqual(2.5m, result.OrderItems[0].Price);
        }
    }
}
