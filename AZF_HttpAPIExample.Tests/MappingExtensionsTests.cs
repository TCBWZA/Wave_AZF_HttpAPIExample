using NUnit.Framework;
using NeoWarewholesale.API.DTOs.External;
using NeoWarewholesale.API.DTOs;
using AZF_HttpAPIExample.Extensions;
using System;
using System.Collections.Generic;

namespace AZF_HttpAPIExample.Tests
{
    public class MappingExtensionsTests
    {
        [Test]
        public void ToCreateOrderDto_FromSpeedyOrder_MapsFieldsCorrectly()
        {
            // Arrange
            var speedy = new SpeedyOrderDto
            {
                CustomerId = 123,
                OrderTimestamp = new DateTime(2024, 01, 02, 3, 4, 5, DateTimeKind.Utc),
                ShipTo = new SpeedyAddressDto { StreetAddress = "1 Ship St", City = "ShipCity", Region = "CountyX", PostCode = "11111", Country = "GB" },
                BillTo = new SpeedyAddressDto { StreetAddress = "1 Bill St", City = "BillCity", Region = "CountyY", PostCode = "22222", Country = "GB" },
                LineItems = new List<SpeedyLineItemDto>
                {
                    new SpeedyLineItemDto { ProductId = 10, Qty = 2, UnitPrice = 5.00m }
                }
            };

            // Act
            var result = speedy.ToCreateOrderDto("Speedy Ltd");

            // Assert (NUnit Assert.That syntax)
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result.CustomerId, Is.EqualTo(123), "CustomerId should be 123");
            Assert.That(result.SupplierId, Is.EqualTo(1), "SupplierId should be 1"); // mapping sets Speedy supplier id to 1
            Assert.That(result.OrderDate, Is.EqualTo(speedy.OrderTimestamp), "OrderDate should match the source OrderTimestamp");
            Assert.That(result.DeliveryAddress, Is.Not.Null, "DeliveryAddress should not be null");
            Assert.That(result.BillingAddress, Is.Not.Null, "BillingAddress should not be null");
            Assert.That(result.OrderItems, Is.Not.Null, "OrderItems should not be null");
            Assert.That(result.OrderItems, Has.Count.EqualTo(1), "OrderItems should contain exactly one item");
            Assert.That(result.OrderItems[0].ProductId, Is.EqualTo(10), "First OrderItem ProductId should be 10");
            Assert.That(result.OrderItems[0].Quantity, Is.EqualTo(2), "First OrderItem Quantity should be 2");
            Assert.That(result.OrderItems[0].Price, Is.EqualTo(5.00m), "First OrderItem Price should be 5.00");
        }

        [Test]
        public void ToCreateOrderDto_FromVaultOrder_UsesProvidedOrderItems()
        {
            // Arrange
            var vault = new VaultOrderDto
            {
                CustomerEmail = "c@d.com",
                PlacedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                DeliveryDetails = new VaultDeliveryDetailsDto
                {
                    BillingLocation = new VaultLocationDto { AddressLine = "Bill St", CityName = "Btown", StateProvince = "Bcounty", ZipPostal = "11111", CountryCode = "GB" },
                    ShippingLocation = new VaultLocationDto { AddressLine = "Ship St", CityName = "Stown", StateProvince = "Scounty", ZipPostal = "22222", CountryCode = "GB" }
                }
            };

            var items = new List<CreateOrderItemDto>
            {
                new CreateOrderItemDto { ProductId = 50, Quantity = 3, Price = 2.5m }
            };

            // Act
            var result = vault.ToCreateOrderDto(77, items);

            // Assert (NUnit Assert.That syntax)
            Assert.That(result, Is.Not.Null, "Result should not be null");
            Assert.That(result.CustomerId, Is.EqualTo(77), "CustomerId should be 77");
            Assert.That(result.SupplierId, Is.EqualTo(2), "SupplierId should be 2");
            Assert.That(result.OrderItems, Is.SameAs(items), "OrderItems should reference the same list instance provided");
            Assert.That(result.BillingAddress, Is.Not.Null, "BillingAddress should not be null");
            Assert.That(result.DeliveryAddress, Is.Not.Null, "DeliveryAddress should not be null");
        }
    }
}
