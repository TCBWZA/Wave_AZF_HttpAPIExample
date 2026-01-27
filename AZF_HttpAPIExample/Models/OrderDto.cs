using System.ComponentModel.DataAnnotations;

namespace AZF_HttpAPIExample.Models
{
    /// <summary>
    /// Represents an address (billing or delivery).
    /// This is a simple DTO (Data Transfer Object) used to pass address information.
    /// </summary>
    public class AddressDto
    {
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? County { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for Order information.
    /// DTOs are used to transfer data between systems (like from an API to an Azure Function).
    /// They contain only the data needed, without business logic.
    /// </summary>
    public class OrderDto
    {
        public long Id { get; set; }
        public long? CustomerId { get; set; }
        public long SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string? CustomerEmail { get; set; }
        public AddressDto? BillingAddress { get; set; }
        public AddressDto? DeliveryAddress { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemDto>? OrderItems { get; set; }
    }

    /// <summary>
    /// Data Transfer Object for Order Item information.
    /// Represents a single line item within an order.
    /// </summary>
    public class OrderItemDto
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public Guid ProductCode { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        
        /// <summary>
        /// Calculated property - shows the total for this line item
        /// </summary>
        public decimal LineTotal => Quantity * Price;
    }

    /// <summary>
    /// DTO used when creating a new order via POST request.
    /// Contains validation attributes to ensure data quality.
    /// </summary>
    public class CreateOrderDto
    {
        public long? CustomerId { get; set; }

        [Required(ErrorMessage = "SupplierId is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "SupplierId must be greater than zero.")]
        public long SupplierId { get; set; }

        [Required(ErrorMessage = "OrderDate is required.")]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [StringLength(200, ErrorMessage = "CustomerEmail cannot exceed 200 characters.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string? CustomerEmail { get; set; }

        public AddressDto? BillingAddress { get; set; }
        public AddressDto? DeliveryAddress { get; set; }

        [Required(ErrorMessage = "OrderStatus is required.")]
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Received;

        [Required(ErrorMessage = "OrderItems is required.")]
        [MinLength(1, ErrorMessage = "Order must contain at least one item.")]
        public List<CreateOrderItemDto> OrderItems { get; set; } = new List<CreateOrderItemDto>();
    }

    /// <summary>
    /// DTO for creating a new order item.
    /// </summary>
    public class CreateOrderItemDto
    {
        [Required(ErrorMessage = "ProductId is required.")]
        [Range(1, long.MaxValue, ErrorMessage = "ProductId must be greater than zero.")]
        public long ProductId { get; set; }

        [Required(ErrorMessage = "Quantity is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Price is required.")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0.")]
        public decimal Price { get; set; }
    }
}
