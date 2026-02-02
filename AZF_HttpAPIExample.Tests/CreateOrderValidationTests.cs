using NUnit.Framework;
using NeoWarewholesale.API.DTOs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AZF_HttpAPIExample.Tests
{
    public class CreateOrderValidationTests
    {
        [Test]
        public void CreateOrderDto_MissingSupplierId_IsInvalid()
        {
            var dto = new CreateOrderDto
            {
                SupplierId = 0,
                OrderDate = System.DateTime.UtcNow,
                OrderItems = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = 1, Quantity = 1, Price = 10m }
                }
            };

            var results = Validate(dto);

            Assert.That(results, Has.Exactly(1).Items);
            Assert.That(results[0].ErrorMessage, Does.Contain("SupplierId"));
        }

        [Test]
        public void CreateOrderDto_MissingOrderItems_IsInvalid()
        {
            var dto = new CreateOrderDto
            {
                SupplierId = 1,
                OrderDate = System.DateTime.UtcNow,
                OrderItems = new List<CreateOrderItemDto>()
            };

            var results = Validate(dto);

            Assert.That(results, Has.Exactly(1).Items);
            Assert.That(results[0].ErrorMessage, Does.Contain("Order must contain at least one item."));
        }

        [Test]
        public void CreateOrderDto_ValidData_IsValid()
        {
            var dto = new CreateOrderDto
            {
                SupplierId = 1,
                OrderDate = System.DateTime.UtcNow,
                OrderItems = new List<CreateOrderItemDto>
                {
                    new CreateOrderItemDto { ProductId = 1, Quantity = 1, Price = 10m }
                }
            };

            var results = Validate(dto);

            Assert.That(results, Is.Empty);
        }

        private static List<ValidationResult> Validate(object instance)
        {
            var context = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(instance, context, results, true);
            return results;
        }
    }
}
