using NeoWarewholesale.API.DTOs;
using NeoWarewholesale.API.DTOs.External;
using AZF_HttpAPIExample.Models;

namespace AZF_HttpAPIExample.Extensions
{
    /// <summary>
    /// Extension methods for mapping between different DTO formats
    /// </summary>
    public static class MappingExtensions
    {
        /// <summary>
        /// Maps a SpeedyOrderDto to a CreateOrderDto for internal processing
        /// </summary>
        /// <param name="speedyOrder">The Speedy order to map</param>
        /// <param name="supplierName">The supplier name to include</param>
        /// <returns>A CreateOrderDto ready for internal processing</returns>
        public static CreateOrderDto ToCreateOrderDto(this SpeedyOrderDto speedyOrder, string supplierName)
        {
            return new CreateOrderDto
            {
                CustomerId = speedyOrder.CustomerId,
                SupplierId = 1, // Speedy is always supplier ID 1 in our system
                OrderDate = speedyOrder.OrderTimestamp,
                CustomerEmail = null, // Speedy doesn't provide email
                BillingAddress = speedyOrder.BillTo?.ToAddressDto(),
                DeliveryAddress = speedyOrder.ShipTo?.ToAddressDto(),
                OrderStatus = OrderStatus.Received,
                OrderItems = speedyOrder.LineItems.Select(item => new CreateOrderItemDto
                {
                    ProductId = item.ProductId,
                    Quantity = item.Qty,
                    Price = item.UnitPrice
                }).ToList()
            };
        }

        /// <summary>
        /// Maps a SpeedyAddressDto to an AddressDto
        /// </summary>
        private static AddressDto ToAddressDto(this SpeedyAddressDto speedyAddress)
        {
            return new AddressDto
            {
                Street = speedyAddress.StreetAddress,
                City = speedyAddress.City,
                County = speedyAddress.Region, // Speedy calls it Region, we call it County
                PostalCode = speedyAddress.PostCode,
                Country = speedyAddress.Country
            };
        }

        /// <summary>
        /// Maps a VaultOrderDto to a CreateOrderDto for internal processing
        /// Note: Product IDs should be looked up separately before calling this method
        /// </summary>
        /// <param name="vaultOrder">The Vault order to map</param>
        /// <param name="customerId">The customer ID looked up from email</param>
        /// <param name="orderItems">The order items with resolved product IDs</param>
        /// <returns>A CreateOrderDto ready for internal processing</returns>
        public static CreateOrderDto ToCreateOrderDto(this VaultOrderDto vaultOrder, long customerId, List<CreateOrderItemDto> orderItems)
        {
            // Convert Unix timestamp to DateTime
            var orderDate = DateTimeOffset.FromUnixTimeSeconds(vaultOrder.PlacedAt).UtcDateTime;

            return new CreateOrderDto
            {
                CustomerId = customerId,
                SupplierId = 2, // Vault is supplier ID 2 in our system
                OrderDate = orderDate,
                CustomerEmail = vaultOrder.CustomerEmail,
                BillingAddress = vaultOrder.DeliveryDetails?.BillingLocation?.ToAddressDto(),
                DeliveryAddress = vaultOrder.DeliveryDetails?.ShippingLocation?.ToAddressDto(),
                OrderStatus = OrderStatus.Received,
                OrderItems = orderItems
            };
        }

        /// <summary>
        /// Maps a VaultLocationDto to an AddressDto
        /// </summary>
        private static AddressDto ToAddressDto(this VaultLocationDto vaultLocation)
        {
            return new AddressDto
            {
                Street = vaultLocation.AddressLine,
                City = vaultLocation.CityName,
                County = vaultLocation.StateProvince,
                PostalCode = vaultLocation.ZipPostal,
                Country = vaultLocation.CountryCode
            };
        }
    }
}