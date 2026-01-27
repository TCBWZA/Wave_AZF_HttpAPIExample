# Understanding JSON and HTTPClient in Azure Functions vs Traditional APIs

## 📘 Overview

This document explains the key differences between working with JSON and HTTPClient in Azure Functions versus traditional ASP.NET Core APIs. Understanding these differences is crucial for your assignment.

---

## 🔄 JSON Serialization/Deserialization

### What is JSON Serialization?

**Serialization** = Converting C# objects → JSON text
**Deserialization** = Converting JSON text → C# objects

### Example Object
```csharp
var order = new OrderDto 
{ 
    Id = 123, 
    CustomerEmail = "test@example.com" 
};
```

### Serialized to JSON
```json
{
  "id": 123,
  "customerEmail": "test@example.com"
}
```

---

## 🆚 Traditional API vs Azure Functions

### 1. Receiving JSON Data (Request Body)

#### Traditional ASP.NET Core API
```csharp
[HttpPost]
[Route("api/orders")]
public IActionResult CreateOrder([FromBody] CreateOrderDto order)
{
    // 'order' is AUTOMATICALLY deserialized from JSON
    // ASP.NET Core's model binding does this for you
    
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    return Ok(order);
}
```

**What happens automatically:**
- ASP.NET Core reads the request body
- Deserializes JSON to `CreateOrderDto`
- Validates using data annotations
- Populates `ModelState`

#### Azure Functions
```csharp
[Function("CreateOrder")]
public async Task<IActionResult> CreateOrder(HttpRequest req)
{
    // YOU must EXPLICITLY deserialize the JSON
    var order = await req.ReadFromJsonAsync<CreateOrderDto>();
    
    // YOU must manually check if it's null
    if (order == null)
        return new BadRequestObjectResult("Invalid data");
    
    // YOU must manually validate (or use FluentValidation)
    if (order.SupplierId <= 0)
        return new BadRequestObjectResult("Invalid SupplierId");
    
    return new OkObjectResult(order);
}
```

**What you must do manually:**
- Read the request body
- Deserialize JSON yourself
- Validate the data yourself

### Why the Difference?

- **Traditional APIs** are built on ASP.NET Core MVC framework → lots of automatic features
- **Azure Functions** are lightweight and serverless → you control everything explicitly
- **Trade-off**: Functions are more flexible but require more manual work

---

## 📤 Sending JSON Data (Response Body)

### Both Handle This Similarly!

#### Traditional API
```csharp
public IActionResult GetOrder(int id)
{
    var order = new OrderDto { Id = id, CustomerEmail = "test@example.com" };
    return Ok(order); // Automatically serialized to JSON
}
```

#### Azure Function
```csharp
[Function("GetOrder")]
public IActionResult GetOrder(int id)
{
    var order = new OrderDto { Id = id, CustomerEmail = "test@example.com" };
    return new OkObjectResult(order); // Automatically serialized to JSON
}
```

**Both automatically serialize the object to JSON in the response!**

---

## 🌐 Making HTTP Calls to External APIs

### Using HttpClient

Both Traditional APIs and Azure Functions use `HttpClient` the same way, but there are best practices to follow:

### ❌ WRONG WAY (Don't do this!)

```csharp
[Function("GetData")]
public async Task<IActionResult> GetData(HttpRequest req)
{
    // BAD: Creating HttpClient every request causes socket exhaustion!
    using (var client = new HttpClient())
    {
        var response = await client.GetStringAsync("https://api.example.com/data");
        return new OkObjectResult(response);
    }
}
```

**Problems:**
- Each request creates a new HttpClient
- Sockets are not released quickly
- Can exhaust available sockets (port exhaustion)
- Performance degradation

### ✅ CORRECT WAY (Do this!)

#### Option 1: Static HttpClient (Simple, for demos)
```csharp
public class OrdersFunction
{
    // Static = shared across all function invocations
    private static readonly HttpClient _httpClient = new HttpClient();
    
    [Function("GetData")]
    public async Task<IActionResult> GetData(HttpRequest req)
    {
        var response = await _httpClient.GetStringAsync("https://api.example.com/data");
        return new OkObjectResult(response);
    }
}
```

**Pros:**
- Simple to implement
- No socket exhaustion
- Good for demos/learning

**Cons:**
- No automatic DNS refresh
- Hard to configure per-client
- Not ideal for production

#### Option 2: IHttpClientFactory (Production Best Practice)
```csharp
// In Program.cs
builder.Services.AddHttpClient("OrdersAPI", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// In your Function
public class OrdersFunction
{
    private readonly IHttpClientFactory _httpClientFactory;
    
    public OrdersFunction(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    [Function("GetData")]
    public async Task<IActionResult> GetData(HttpRequest req)
    {
        var client = _httpClientFactory.CreateClient("OrdersAPI");
        var response = await client.GetStringAsync("/data");
        return new OkObjectResult(response);
    }
}
```

**Pros:**
- Automatic DNS refresh
- Per-client configuration
- Built-in support for Polly (retry, circuit breaker)
- Best for production

**Cons:**
- More complex setup
- Requires DI understanding

---

## 🔍 JSON Deserialization with HttpClient

### The Full Flow

```csharp
// STEP 1: Make HTTP request
HttpResponseMessage response = await _httpClient.GetAsync("https://api.example.com/orders");

// STEP 2: Check if successful
if (!response.IsSuccessStatusCode)
{
    return new StatusCodeResult((int)response.StatusCode);
}

// STEP 3: Read response as string
string jsonString = await response.Content.ReadAsStringAsync();
// jsonString = "[{"id":1,"customerEmail":"test@example.com"}]"

// STEP 4: Deserialize JSON to C# objects
var orders = JsonSerializer.Deserialize<List<OrderDto>>(jsonString, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true // Important!
});

// STEP 5: Use the objects
return new OkObjectResult(orders);
```

### Why PropertyNameCaseInsensitive?

JSON from APIs often uses different casing than C#:

**API Response (camelCase):**
```json
{
  "id": 123,
  "customerEmail": "test@example.com"
}
```

**C# Class (PascalCase):**
```csharp
public class OrderDto
{
    public int Id { get; set; }
    public string CustomerEmail { get; set; }
}
```

Without `PropertyNameCaseInsensitive = true`, deserialization would fail!

### Alternative: Match the JSON exactly
```csharp
public class OrderDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("customerEmail")]
    public string CustomerEmail { get; set; }
}
```

---

## 📨 Sending JSON with HttpClient (POST/PUT)

### Complete Example

```csharp
// STEP 1: Create your C# object
var newOrder = new CreateOrderDto
{
    SupplierId = 1,
    CustomerEmail = "test@example.com",
    OrderItems = new List<CreateOrderItemDto>
    {
        new CreateOrderItemDto { ProductId = 5, Quantity = 2, Price = 29.99m }
    }
};

// STEP 2: Serialize to JSON string
string jsonContent = JsonSerializer.Serialize(newOrder, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Converts to camelCase
    WriteIndented = false // Compact JSON
});
// Result: {"supplierId":1,"customerEmail":"test@example.com",...}

// STEP 3: Create HTTP content
var httpContent = new StringContent(
    jsonContent,
    System.Text.Encoding.UTF8,
    "application/json" // Content-Type header
);

// STEP 4: Send POST request
HttpResponseMessage response = await _httpClient.PostAsync(
    "https://api.example.com/orders",
    httpContent
);

// STEP 5: Handle response
if (response.IsSuccessStatusCode)
{
    string responseJson = await response.Content.ReadAsStringAsync();
    var createdOrder = JsonSerializer.Deserialize<OrderDto>(responseJson);
    return new CreatedResult($"/api/orders/{createdOrder.Id}", createdOrder);
}
```

---

## 🎯 Common Mistakes Students Make

### 1. Forgetting to Deserialize in Functions
```csharp
// ❌ WRONG
[Function("CreateOrder")]
public IActionResult CreateOrder(HttpRequest req)
{
    // req doesn't give you the object automatically!
    // You must deserialize it!
}

// ✅ CORRECT
[Function("CreateOrder")]
public async Task<IActionResult> CreateOrder(HttpRequest req)
{
    var order = await req.ReadFromJsonAsync<CreateOrderDto>();
}
```

### 2. Creating HttpClient Every Time
```csharp
// ❌ WRONG - Socket exhaustion!
using (var client = new HttpClient()) { ... }

// ✅ CORRECT - Use static or IHttpClientFactory
private static readonly HttpClient _httpClient = new HttpClient();
```

### 3. Not Handling API Errors
```csharp
// ❌ WRONG - What if API returns 404 or 500?
string json = await _httpClient.GetStringAsync(url); // Throws exception!

// ✅ CORRECT - Check status first
HttpResponseMessage response = await _httpClient.GetAsync(url);
if (!response.IsSuccessStatusCode)
{
    return new StatusCodeResult((int)response.StatusCode);
}
```

### 4. Case Sensitivity Issues
```csharp
// ❌ WRONG - Will fail if JSON uses camelCase
JsonSerializer.Deserialize<OrderDto>(json);

// ✅ CORRECT - Handle case differences
JsonSerializer.Deserialize<OrderDto>(json, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});
```

### 5. Forgetting Content-Type
```csharp
// ❌ WRONG - API won't understand it's JSON
var content = new StringContent(jsonString);

// ✅ CORRECT - Specify content type
var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
```

---

## 📊 Quick Reference Table

| Task | Traditional API | Azure Function |
|------|----------------|----------------|
| **Read JSON from request** | `[FromBody] OrderDto order` | `await req.ReadFromJsonAsync<OrderDto>()` |
| **Validate request** | `ModelState.IsValid` | Manual validation |
| **Return JSON** | `Ok(data)` | `new OkObjectResult(data)` |
| **HttpClient creation** | Use `IHttpClientFactory` | Use static or `IHttpClientFactory` |
| **Deserialize API response** | `JsonSerializer.Deserialize<T>()` | `JsonSerializer.Deserialize<T>()` |
| **Serialize for POST** | `JsonSerializer.Serialize()` | `JsonSerializer.Serialize()` |

---

## 💡 Study Tips

1. **Understand the flow**: Request → Deserialize → Process → Serialize → Response
2. **Practice both**: Try examples in both Traditional API and Azure Functions
3. **Use logging**: `_logger.LogInformation()` helps you see what's happening
4. **Test error cases**: What if the JSON is invalid? What if the API is down?
5. **Read the comments**: The code examples have detailed explanations

---

## 🔗 Additional Resources

- [System.Text.Json Documentation](https://docs.microsoft.com/dotnet/standard/serialization/system-text-json-overview)
- [HttpClient Guidelines](https://docs.microsoft.com/dotnet/fundamentals/networking/http/httpclient-guidelines)
- [Azure Functions HTTP Triggers](https://docs.microsoft.com/azure/azure-functions/functions-bindings-http-webhook-trigger)

---

**Remember**: The key difference is that Azure Functions require **explicit** handling of JSON, while Traditional APIs do it **automatically**. Both approaches work, but you need to understand when and how to do things manually!
