namespace AZF_HttpAPIExample.Models
{
    /// <summary>
    /// Represents the status of an order in the fulfillment process.
    /// This enum is used to track where an order is in the delivery pipeline.
    /// </summary>
    public enum OrderStatus
    {
        /// <summary>
        /// Order has been received and is awaiting processing.
        /// </summary>
        Received = 0,

        /// <summary>
        /// Order is being picked from the warehouse.
        /// </summary>
        Picking = 1,

        /// <summary>
        /// Order has been dispatched for delivery.
        /// </summary>
        Dispatched = 2,

        /// <summary>
        /// Order has been delivered to the customer.
        /// </summary>
        Delivered = 3
    }
}
