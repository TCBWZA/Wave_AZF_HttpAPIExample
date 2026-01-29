# Azure Functions HTTP API Example

This project demonstrates how to create Azure Functions that interact with external REST APIs. It's designed as a teaching example for students learning about serverless computing and API integration.

## 📚 What You'll Learn

1. **Azure Functions Basics**
   - How to create HTTP-triggered functions
   - Understanding serverless architecture
   - Function routing and parameters

2. **HTTP Communication**
   - Using HttpClient to call external APIs
   - Handling GET and POST requests
   - Error handling and status codes

3. **JSON Serialization**
   - Converting JSON to C# objects (deserialization)
   - Converting C# objects to JSON (serialization)
   - Using System.Text.Json
   - Differences between Azure Functions and traditional APIs

4. **Best Practices**
   - Logging and monitoring
   - Dependency injection
   - Error handling patterns

## 🏗️ Project Structure

```
AZF_HttpAPIExample/
│
├── Models/
│   ├── OrderDto.cs           # Data Transfer Objects for Orders
│   └── OrderStatus.cs        # Enum for order statuses
│
├── OrdersFunction.cs         # Main function with 3 examples
├── Program.cs                # Startup configuration
└── AZF_HttpAPIExample.csproj # Project dependencies
```

## 📖 The Three Functions

### 1. **GetAllOrders** (GET)

**URL:** `GET /api/orders`

**Purpose:** Retrieves all orders from an external API

**Key Concepts:**
- HTTP GET requests
- Deserializing JSON arrays
- Error handling

**Example:**
```bash
curl http://localhost:7071/api/orders
```

**Response:**
```json
[
  {
    "id": 1,
    "customerId": 100,
    "orderDate": "2024-01-15T10:30:00Z",
    "totalAmount": 59.98,
    ...
  }
]
```

### 2. **GetOrderById** (GET with Route Parameter)

**URL:** `GET /api/orders/{id}`

**Purpose:** Retrieves a specific order by ID

**Key Concepts:**
- Route parameters
- 404 Not Found handling
- Single object deserialization

**Example (PowerShell):**
```powershell
Invoke-RestMethod -Uri "http://localhost:7071/api/orders/123" -Method Get
```

**Response:**
```json
{
  "id": 123,
  "customerId": 100,
  "orderDate": "2024-01-15T10:30:00Z",
  "totalAmount": 59.98,
  ...
}
```

### 3. **CreateOrder** (POST)

**URL:** `POST /api/orders`

**Purpose:** Creates a new order by sending data to an external API

**Key Concepts:**
- Reading JSON request body
- Validation
- Serializing objects to JSON
- POST requests with HttpClient
- 201 Created responses

**Example (PowerShell):**
```powershell
$body = @{
    supplierId = 1
    customerId = 100
    customerEmail = "customer@example.com"
    orderDate = "2024-01-15T10:30:00Z"
    orderStatus = 0
    orderItems = @(
        @{
            productId = 5
            quantity = 2
            price = 29.99
        }
    )
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/orders" -Method Post -Body $body -ContentType "application/json"
```

### 4. **CreateSpeedyOrder** (POST)

**URL:** `POST /api/speedy-orders`

**Purpose:** Creates a new order from Speedy supplier format

**Key Concepts:**
- External supplier integration
- Data transformation using extension methods
- API enrichment (supplier lookup)

**Example (PowerShell):**
```powershell
$body = @{
    customerId = 123
    lineItems = @(@{ productId = 5; qty = 2; unitPrice = 29.99 })
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/speedy-orders" -Method Post -Body $body -ContentType "application/json"
```

### 5. **CreateVaultOrder** (POST)

**URL:** `POST /api/vault-orders`

**Purpose:** Creates a new order from Vault supplier format

**Key Concepts:**
- External supplier integration with email-based customer lookup
- Data transformation using extension methods
- API enrichment (customer ID lookup from email)
- HttpResponseData for response control

**Example (PowerShell):**
```powershell
$body = @{
    customerEmail = "customer@vault-supplier.com"
    placedAt = 1700000000
    items = @(@{ productCode = "550e8400-e29b-41d4-a716-446655440000"; quantityOrdered = 2; pricePerUnit = 29.99 })
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:7071/api/vault-orders" -Method Post -Body $body -ContentType "application/json"
```

## 💡 Extension Ideas

1. Add filtering to GetAllOrders (e.g., by status or date)
2. Implement PUT and DELETE functions
3. Add authentication/authorization
4. Implement retry logic for API calls
5. Add caching to improve performance
6. Create a function that calls multiple APIs and combines results
7. **Add more supplier integrations** (like the SpeedyOrder example)
8. **Implement webhook endpoints** for supplier notifications
9. Create a frontend app (e.g., React, Angular) to consume the API
10. Add API documentation using Swagger/OpenAPI

## 🔑 Key Differences: Azure Functions vs Traditional APIs

### JSON Serialization

**Traditional API Controller:**
```csharp
[HttpPost]
public IActionResult Create([FromBody] OrderDto order) // Automatic deserialization
{
    return Ok(order); // Automatic serialization
}
```

**Azure Function:**
```csharp
[Function("CreateOrder")]
public async Task<IActionResult> CreateOrder(HttpRequest req)
{
    var order = await req.ReadFromJsonAsync<OrderDto>(); // Explicit deserialization
    return new OkObjectResult(order); // Automatic serialization
}
```

### HttpClient Usage

**Traditional API (with DI):**
```csharp
public class OrdersController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public OrdersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<IActionResult> Get()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync("...");
        ...
    }
}
```

**Azure Function (simple example):**
```csharp
public class OrdersFunction
{
    private static readonly HttpClient _httpClient = new HttpClient();
    
    [Function("GetOrders")]
    public async Task<IActionResult> GetOrders(HttpRequest req)
    {
        var response = await _httpClient.GetAsync("...");
        ...
    }
}
```

### Execution Model

| Aspect | Traditional API | Azure Functions |
|--------|----------------|-----------------|
| **Cost** | Pay for uptime (always running) | Pay per execution |
| **Scaling** | Manual configuration | Automatic |
| **Cold Start** | No | Yes (can be mitigated) |
| **State** | Can be stateful | Should be stateless |
| **Best For** | Complex apps, consistent load | Event-driven, variable load |

## 🛠️ Setup Instructions

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022 or VS Code
- Azure Functions Core Tools (for local testing)

### Local Development

1. **Clone the repository**
   ```bash
   cd AZF_HttpAPIExample
   ```

2. **Configure the API URL**
   
> ⚠️ **IMPORTANT - Security Notice**: 
> The `local.settings.json` file contains configuration settings and may include sensitive information (API keys, connection strings, etc.). This file is **excluded from Git** (via `.gitignore`) and should **never be committed to your repository**.
   
If `local.settings.json` doesn't exist, you may need to:
- **Rename** `local.settings.json.sample` to `local.settings.json` (if a sample file is provided)
- **Create** a new `local.settings.json` file with the following content:
   
```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ApiBaseUrl": "http://localhost:5143/api"
  }
}
```
   
Replace `http://localhost:5143/api` with your actual API URL.
   
> 💡 **Best Practice**: Share a `local.settings.json.sample` file in your repository with placeholder values, not the actual `local.settings.json` file.

3. **Restore NuGet packages** (first time only)
   ```powershell
   dotnet restore
   ```
   
   > **Corporate Environment Note**: This project includes Newtonsoft.Json 13.0.4 explicitly to ensure compatibility with corporate security policies. If you encounter package restore issues, check with your IT department about approved package sources.

4. **Run the project**
   
In Visual Studio: Press **F5**
   
Or in PowerShell/Command Prompt:
```powershell
func start
```

5. **Test the endpoints**
   
   The functions will be available at:
   - `http://localhost:7071/api/orders` (GET - all orders)
   - `http://localhost:7071/api/orders/123` (GET - specific order)
   - `http://localhost:7071/api/orders` (POST - create order)

## 📝 Important Code Patterns

### 1. Reading Configuration Settings

```csharp
// In constructor - inject IConfiguration
public OrdersFunction(ILogger<OrdersFunction> logger, IConfiguration configuration)
{
    _logger = logger;
    _configuration = configuration;
    
    // Read API base URL from configuration
    _apiBaseUrl = _configuration["ApiBaseUrl"] ?? "http://localhost:5143/api";
}

// Configuration comes from:
// - Local: local.settings.json -> "Values": { "ApiBaseUrl": "..." }
// - Azure: Configuration > Application Settings > ApiBaseUrl
```

### 2. Reading JSON Request Body

```csharp
// Azure Functions way
var data = await req.ReadFromJsonAsync<CreateOrderDto>();

// Alternative (manual)
string body = await new StreamReader(req.Body).ReadToEndAsync();
var data = JsonSerializer.Deserialize<CreateOrderDto>(body);
```

### 3. Calling External API with GET

```csharp
// Simple way
string json = await _httpClient.GetStringAsync(url);
var data = JsonSerializer.Deserialize<OrderDto>(json);

// With error handling
HttpResponseMessage response = await _httpClient.GetAsync(url);
if (response.IsSuccessStatusCode)
{
    string json = await response.Content.ReadAsStringAsync();
    var data = JsonSerializer.Deserialize<OrderDto>(json);
}
```

### 4. Calling External API with POST

```csharp
// Serialize your object
string json = JsonSerializer.Serialize(newOrder);

// Create HTTP content
var content = new StringContent(json, Encoding.UTF8, "application/json");

// Send POST request
HttpResponseMessage response = await _httpClient.PostAsync(url, content);
```

### 5. Returning Different Status Codes

```csharp
// 200 OK
return new OkObjectResult(data);

// 201 Created
return new CreatedResult($"/api/orders/{id}", data);

// 400 Bad Request
return new BadRequestObjectResult(new { message = "Invalid data" });

// 404 Not Found
return new NotFoundObjectResult(new { message = "Order not found" });

// 500 Internal Server Error
return new StatusCodeResult(StatusCodes.Status500InternalServerError);
```

## 🎯 Assignment Tips

### Common Mistakes to Avoid

1. **Creating HttpClient per request**
   ```csharp
   // ❌ DON'T DO THIS
   [Function("GetData")]
   public async Task<IActionResult> GetData(HttpRequest req)
   {
       var client = new HttpClient(); // Creates new client every time!
       ...
   }
   
   // ✅ DO THIS INSTEAD
   private static readonly HttpClient _httpClient = new HttpClient();
   ```

2. **Forgetting PropertyNameCaseInsensitive**
   ```csharp
   // If API returns {"orderId": 1} but your class has "OrderId"
   var options = new JsonSerializerOptions
   {
       PropertyNameCaseInsensitive = true // This fixes it!
   };
   ```

3. **Not handling errors**
   ```csharp
   // ❌ BAD
   string json = await _httpClient.GetStringAsync(url); // Throws on error
   
   // ✅ GOOD
   try
   {
       string json = await _httpClient.GetStringAsync(url);
   }
   catch (HttpRequestException ex)
   {
       _logger.LogError($"API call failed: {ex.Message}");
       return new StatusCodeResult(503);
   }
   ```

### Testing Your Functions

1. **Use Postman or PowerShell** to test endpoints (examples below)
2. **Check the console output** for logging information
3. **Use breakpoints** in Visual Studio to debug
4. **Test error cases** (what if API is down? what if ID doesn't exist?)

**Available Endpoints:**
- `GET /api/orders` - Get all orders
- `GET /api/orders/{id}` - Get specific order
- `POST /api/orders` - Create standard order
- `POST /api/speedy-orders` - Create Speedy order
- `POST /api/vault-orders` - Create Vault order
