using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using AZF_HttpAPIExample.Models;

/*
 * ============================================================================
 * STUDENT EXAMPLE: Azure Functions with HTTP Triggers and API Integration
 * ============================================================================
 * 
 * This class demonstrates how to:
 * 1. Create Azure Functions with HTTP triggers (GET and POST)
 * 2. Use HttpClient to call external REST APIs
 * 3. Handle JSON serialization/deserialization in Azure Functions
 * 4. Process query parameters and route data
 * 5. Return different HTTP status codes
 * 6. Use IConfiguration to read settings from local.settings.json or Azure configuration
 * 
 * KEY CONCEPTS:
 * 
 * AZURE FUNCTIONS vs TRADITIONAL API:
 * - Azure Functions are serverless - they only run when triggered (pay per execution)
 * - Traditional APIs run continuously on a server (pay for uptime)
 * - Functions are event-driven and scale automatically
 * 
 * JSON SERIALIZATION IN AZURE FUNCTIONS:
 * - Azure Functions use System.Text.Json by default (modern, fast)
 * - When calling external APIs with HttpClient, YOU must handle serialization
 * - For responses, Azure Functions can automatically serialize objects to JSON
 * - System.Text.Json is case-sensitive by default (use JsonSerializerOptions to configure)
 * 
 * HTTPCLIENT BEST PRACTICES:
 * - NEVER create a new HttpClient for each request (causes socket exhaustion)
 * - Use IHttpClientFactory or static HttpClient
 * - In this example, we use a static HttpClient for simplicity
 * - In production, use dependency injection with IHttpClientFactory
 * 
 * CONFIGURATION:
 * - IConfiguration reads settings from local.settings.json (local) or Azure App Settings (deployed)
 * - Access settings using configuration["SettingName"] or configuration["SectionName:SettingName"]
 * - Settings in local.settings.json go under "Values" section
 * - In Azure Portal, add settings in Configuration > Application Settings
 */

namespace AZF_HttpAPIExample;

public class OrdersFunction
{
    private readonly ILogger<OrdersFunction> _logger;
    private readonly IConfiguration _configuration;
    
    // Static HttpClient - shared across all function invocations
    // In production, use IHttpClientFactory via dependency injection
    private static readonly HttpClient _httpClient = new HttpClient();
    
    // Base URL of the API we're calling - now read from configuration
    // Configured in local.settings.json (local) or Azure App Settings (deployed)
    private readonly string _apiBaseUrl;

    public OrdersFunction(ILogger<OrdersFunction> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        
        // Read API base URL from configuration
        // In local.settings.json, this is under "Values": { "ApiBaseUrl": "https://..." }
        // In Azure Portal, this is in Configuration > Application Settings
        _apiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5143/api";
        
        _logger.LogInformation($"API Base URL configured as: {_apiBaseUrl}");
    }

    /*
     * ========================================================================
     * FUNCTION 1: GET ALL ORDERS
     * ========================================================================
     * 
     * HTTP Trigger: GET request
     * Route: /api/GetAllOrders
     * Authorization: Function level (requires a function key)
     * 
     * PURPOSE:
     * This function demonstrates how to:
     * 1. Accept an HTTP GET request
     * 2. Call an external API using HttpClient
     * 3. Deserialize JSON response from the API
     * 4. Return the data to the caller
     * 
     * FLOW:
     * Client ? Azure Function ? External API ? Azure Function ? Client
     * 
     * JSON HANDLING:
     * - HttpClient.GetStringAsync() returns raw JSON string
     * - JsonSerializer.Deserialize<T>() converts JSON string to C# objects
     * - OkObjectResult automatically serializes C# objects back to JSON for response
     */
    [Function("GetAllOrders")]
    public async Task<IActionResult> GetAllOrders(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders")] HttpRequest req)
    {
        _logger.LogInformation("GetAllOrders function triggered.");

        try
        {
            // STEP 1: Build the API URL
            // Read from configuration instead of hardcoded constant
            string apiUrl = $"{_apiBaseUrl}/orders";
            
            _logger.LogInformation($"Calling external API: {apiUrl}");

            // STEP 2: Make HTTP GET request to the external API
            // GetStringAsync sends a GET request and returns the response body as a string
            string jsonResponse = await _httpClient.GetStringAsync(apiUrl);
            
            // STEP 3: Deserialize JSON string into C# objects
            // JsonSerializer.Deserialize converts JSON text into strongly-typed objects
            // JsonSerializerOptions configures how to handle JSON (e.g., case sensitivity)
            var orders = JsonSerializer.Deserialize<List<OrderDto>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Allows matching "orderId" with "OrderId"
            });

            // STEP 4: Log success
            _logger.LogInformation($"Successfully retrieved {orders?.Count ?? 0} orders.");

            // STEP 5: Return the data
            // OkObjectResult returns HTTP 200 (OK) with the data
            // Azure Functions automatically serializes the orders list to JSON
            return new OkObjectResult(orders);
        }
        catch (HttpRequestException ex)
        {
            // HTTP-specific errors (network issues, API not responding, etc.)
            _logger.LogError($"Error calling external API: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
        }
        catch (JsonException ex)
        {
            // JSON parsing errors (invalid JSON format, type mismatch, etc.)
            _logger.LogError($"Error deserializing JSON response: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            // Catch-all for any other unexpected errors
            _logger.LogError($"Unexpected error: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /*
     * ========================================================================
     * FUNCTION 2: GET SPECIFIC ORDER BY ID
     * ========================================================================
     * 
     * HTTP Trigger: GET request
     * Route: /api/orders/{id}
     * Authorization: Function level
     * 
     * PURPOSE:
     * This function demonstrates how to:
     * 1. Accept route parameters (order ID from URL)
     * 2. Call an external API with dynamic URL
     * 3. Handle cases where the resource is not found (404)
     * 
     * EXAMPLE USAGE:
     * GET https://your-function-app.azurewebsites.net/api/orders/123
     * 
     * ROUTE PARAMETERS:
     * - {id} in the route becomes a parameter in the function
     * - Azure Functions automatically extracts it from the URL
     * - The parameter name must match the route template
     */
    [Function("GetOrderById")]
    public async Task<IActionResult> GetOrderById(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{id}")] HttpRequest req,
        long id) // Route parameter - automatically populated from URL
    {
        _logger.LogInformation($"GetOrderById function triggered for Order ID: {id}");

        try
        {
            // STEP 1: Build the API URL with the order ID
            string apiUrl = $"{_apiBaseUrl}/orders/{id}";
            
            _logger.LogInformation($"Calling external API: {apiUrl}");

            // STEP 2: Make HTTP GET request to the external API
            HttpResponseMessage response = await _httpClient.GetAsync(apiUrl);

            // STEP 3: Check if the API request was successful
            if (!response.IsSuccessStatusCode)
            {
                // If the API returns 404, the order doesn't exist
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning($"Order with ID {id} not found.");
                    return new NotFoundObjectResult(new { message = $"Order with ID {id} not found." });
                }

                // For other error status codes, log and return appropriate error
                _logger.LogError($"API returned error: {response.StatusCode}");
                return new StatusCodeResult((int)response.StatusCode);
            }

            // STEP 4: Read the response content as a string
            string jsonResponse = await response.Content.ReadAsStringAsync();

            // STEP 5: Deserialize JSON into a single OrderDto object
            var order = JsonSerializer.Deserialize<OrderDto>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // STEP 6: Log success and return the order
            _logger.LogInformation($"Successfully retrieved order {id}.");
            return new OkObjectResult(order);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"Error calling external API: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Error deserializing JSON response: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    /*
     * ========================================================================
     * FUNCTION 3: CREATE NEW ORDER (POST)
     * ========================================================================
     * 
     * HTTP Trigger: POST request
     * Route: /api/orders
     * Authorization: Function level
     * 
     * PURPOSE:
     * This function demonstrates how to:
     * 1. Accept POST requests with JSON body
     * 2. Deserialize incoming JSON data
     * 3. Validate the data
     * 4. Send POST request to external API with JSON payload
     * 5. Return created resource with 201 status code
     * 
     * JSON HANDLING IN POST REQUESTS:
     * - Read request body using await req.ReadFromJsonAsync<T>()
     * - This automatically deserializes the JSON into your DTO
     * - To send JSON to external API, serialize your object with JsonSerializer.Serialize()
     * - Use StringContent with "application/json" media type
     * 
     * EXAMPLE REQUEST BODY:
     * {
     *   "supplierId": 1,
     *   "customerId": 100,
     *   "customerEmail": "customer@example.com",
     *   "orderDate": "2024-01-15T10:30:00Z",
     *   "orderStatus": 0,
     *   "orderItems": [
     *     {
     *       "productId": 5,
     *       "quantity": 2,
     *       "price": 29.99
     *     }
     *   ]
     * }
     */
    [Function("CreateOrder")]
    public async Task<IActionResult> CreateOrder(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequest req)
    {
        _logger.LogInformation("CreateOrder function triggered.");

        try
        {
            // STEP 1: Read and deserialize the request body
            // ReadFromJsonAsync automatically deserializes JSON from request body
            var newOrder = await req.ReadFromJsonAsync<CreateOrderDto>();

            // STEP 2: Validate the input
            if (newOrder == null)
            {
                _logger.LogWarning("Request body is empty or invalid.");
                return new BadRequestObjectResult(new { message = "Request body is required." });
            }

            // Additional validation - check required fields
            if (newOrder.SupplierId <= 0)
            {
                return new BadRequestObjectResult(new { message = "Invalid SupplierId." });
            }

            if (newOrder.OrderItems == null || !newOrder.OrderItems.Any())
            {
                return new BadRequestObjectResult(new { message = "Order must contain at least one item." });
            }

            // STEP 3: Serialize the object to JSON for sending to API
            string jsonContent = JsonSerializer.Serialize(newOrder, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Converts to camelCase for JSON
                WriteIndented = false // Compact JSON (no extra whitespace)
            });

            _logger.LogInformation($"Sending order to API: {jsonContent}");

            // STEP 4: Create HTTP content with JSON
            var httpContent = new StringContent(
                jsonContent,
                System.Text.Encoding.UTF8,
                "application/json"); // Content-Type header

            // STEP 5: Send POST request to external API
            string apiUrl = $"{_apiBaseUrl}/orders";
            HttpResponseMessage response = await _httpClient.PostAsync(apiUrl, httpContent);

            // STEP 6: Check if the API request was successful
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API returned error: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                return new ObjectResult(new { message = "Failed to create order.", details = errorContent })
                {
                    StatusCode = (int)response.StatusCode
                };
            }

            // STEP 7: Read the created order from response
            string responseJson = await response.Content.ReadAsStringAsync();
            var createdOrder = JsonSerializer.Deserialize<OrderDto>(responseJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // STEP 8: Return 201 Created with the new order
            _logger.LogInformation($"Successfully created order with ID: {createdOrder?.Id}");
            
            // CreatedResult returns HTTP 201 with Location header pointing to the new resource
            return new CreatedResult($"/api/orders/{createdOrder?.Id}", createdOrder);
        }
        catch (JsonException ex)
        {
            _logger.LogError($"Error with JSON processing: {ex.Message}");
            return new BadRequestObjectResult(new { message = "Invalid JSON format.", error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"Error calling external API: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status503ServiceUnavailable);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error: {ex.Message}");
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }
}

/*
 * ============================================================================
 * KEY DIFFERENCES: AZURE FUNCTIONS vs TRADITIONAL HTTPCLIENT APPS
 * ============================================================================
 * 
 * 1. JSON SERIALIZATION:
 *    
 *    Traditional API Controller (ASP.NET Core):
 *    - [FromBody] attribute automatically deserializes JSON
 *    - Returning objects automatically serializes to JSON
 *    - Model validation happens automatically
 *    
 *    Azure Functions:
 *    - Must explicitly call req.ReadFromJsonAsync<T>() to deserialize
 *    - Returning objects in IActionResult handles serialization
 *    - Manual validation required (or use validation attributes)
 * 
 * 2. DEPENDENCY INJECTION:
 *    
 *    Traditional API:
 *    - Services injected via constructor
 *    - Lifetime scopes: Transient, Scoped, Singleton
 *    
 *    Azure Functions:
 *    - Similar DI support via constructor
 *    - Configure in Program.cs using builder.Services
 *    - HttpClient should use IHttpClientFactory
 * 
 * 3. ROUTING:
 *    
 *    Traditional API:
 *    - [Route] attribute on controller
 *    - [HttpGet], [HttpPost] attributes on methods
 *    
 *    Azure Functions:
 *    - [Function] attribute with name
 *    - [HttpTrigger] with route template
 *    - More flexible routing per function
 * 
 * 4. EXECUTION MODEL:
 *    
 *    Traditional API:
 *    - Always running (pays for VM/App Service)
 *    - Consistent response times
 *    - Stateful (can use memory caching)
 *    
 *    Azure Functions:
 *    - Runs on-demand (pay per execution)
 *    - May have "cold start" delay
 *    - Stateless by design (use external storage for state)
 * 
 * 5. SCALING:
 *    
 *    Traditional API:
 *    - Scale up (bigger VM) or scale out (more instances)
 *    - Manual configuration
 *    
 *    Azure Functions:
 *    - Automatic scaling based on load
 *    - Consumption plan scales to zero when idle
 * 
 * ============================================================================
 * TESTING THESE FUNCTIONS
 * ============================================================================
 * 
 * 1. Using Postman or curl:
 *    
 *    GET All Orders:
 *    GET http://localhost:7071/api/orders
 *    
 *    GET Specific Order:
 *    GET http://localhost:7071/api/orders/123
 *    
 *    POST New Order:
 *    POST http://localhost:7071/api/orders
 *    Content-Type: application/json
 *    Body: { "supplierId": 1, "orderItems": [...] }
 * 
 * 2. Function Keys:
 *    - In local development, keys are optional
 *    - In Azure, get function key from portal
 *    - Add to query string: ?code=YOUR_FUNCTION_KEY
 * 
 * ============================================================================
 */