# 🚀 Quick Reference Card - Azure Functions & JSON

## 📥 Reading JSON from Request (Azure Function)

```csharp
// Method 1: Automatic deserialization (easiest)
var order = await req.ReadFromJsonAsync<CreateOrderDto>();

// Method 2: Manual
string body = await new StreamReader(req.Body).ReadToEndAsync();
var order = JsonSerializer.Deserialize<CreateOrderDto>(body);
```

---

## 📤 Returning JSON Response

```csharp
// 200 OK
return new OkObjectResult(data);

// 201 Created
return new CreatedResult($"/api/orders/{id}", data);

// 400 Bad Request
return new BadRequestObjectResult(new { message = "Error" });

// 404 Not Found
return new NotFoundObjectResult(new { message = "Not found" });

// 500 Internal Server Error
return new StatusCodeResult(StatusCodes.Status500InternalServerError);
```

---

## 🌐 HttpClient - GET Request

```csharp
// Simple version
string json = await _httpClient.GetStringAsync(url);
var data = JsonSerializer.Deserialize<OrderDto>(json);

// With error handling (better)
HttpResponseMessage response = await _httpClient.GetAsync(url);
if (!response.IsSuccessStatusCode)
{
    return new StatusCodeResult((int)response.StatusCode);
}
string json = await response.Content.ReadAsStringAsync();
var data = JsonSerializer.Deserialize<OrderDto>(json, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
});
```

---

## 📮 HttpClient - POST Request

```csharp
// 1. Serialize object to JSON
string json = JsonSerializer.Serialize(newOrder, new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});

// 2. Create HTTP content
var content = new StringContent(json, Encoding.UTF8, "application/json");

// 3. Send POST
HttpResponseMessage response = await _httpClient.PostAsync(url, content);

// 4. Handle response
if (response.IsSuccessStatusCode)
{
    string responseJson = await response.Content.ReadAsStringAsync();
    var result = JsonSerializer.Deserialize<OrderDto>(responseJson);
}
```

---

## ⚙️ JsonSerializerOptions

```csharp
var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,      // "orderId" matches "OrderId"
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,  // Output as camelCase
    WriteIndented = false                    // Compact JSON (no spaces)
};
```

---

## 🔧 HttpClient Setup

```csharp
// ❌ WRONG - Creates new instance each time!
using (var client = new HttpClient()) { }

// ✅ CORRECT - Static instance (simple)
private static readonly HttpClient _httpClient = new HttpClient();

// ✅ BEST - IHttpClientFactory (production)
// In Program.cs:
builder.Services.AddHttpClient();

// In Function:
private readonly IHttpClientFactory _factory;
public MyFunction(IHttpClientFactory factory) 
{ 
    _factory = factory; 
}
```

---

## 🎯 Function Anatomy

```csharp
[Function("FunctionName")]  // Function name in Azure
public async Task<IActionResult> MethodName(
    [HttpTrigger(
        AuthorizationLevel.Function,  // Requires function key
        "get", "post",               // Allowed methods
        Route = "orders/{id}"        // URL route
    )] HttpRequest req,
    int id)                          // Route parameter
{
    // Your code here
}
```

---

## 🔍 Error Handling Pattern

```csharp
try
{
    // Your code
}
catch (HttpRequestException ex)
{
    _logger.LogError($"HTTP error: {ex.Message}");
    return new StatusCodeResult(503);
}
catch (JsonException ex)
{
    _logger.LogError($"JSON error: {ex.Message}");
    return new BadRequestObjectResult("Invalid JSON");
}
catch (Exception ex)
{
    _logger.LogError($"Error: {ex.Message}");
    return new StatusCodeResult(500);
}
```

---

## 📋 Common HTTP Status Codes

| Code | Name | When to Use |
|------|------|-------------|
| 200 | OK | Successful GET/PUT/DELETE |
| 201 | Created | Successful POST (resource created) |
| 204 | No Content | Successful DELETE (no body) |
| 400 | Bad Request | Invalid input/validation failed |
| 404 | Not Found | Resource doesn't exist |
| 500 | Internal Server Error | Unexpected server error |
| 503 | Service Unavailable | External API is down |

---

## 🎨 DTO Example

```csharp
public class OrderDto
{
    public long Id { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public List<OrderItemDto>? OrderItems { get; set; }
}

public class CreateOrderDto
{
    [Required]
    [Range(1, long.MaxValue)]
    public long SupplierId { get; set; }
    
    [EmailAddress]
    public string? CustomerEmail { get; set; }
    
    [MinLength(1)]
    public List<CreateOrderItemDto> OrderItems { get; set; } = new();
}
```

---

## 🧪 Testing Commands

```powershell
# GET All
Invoke-RestMethod -Uri "http://localhost:7071/api/orders" -Method Get

# GET By ID
Invoke-RestMethod -Uri "http://localhost:7071/api/orders/123" -Method Get

# POST Standard Order
$body = @{ supplierId = 1; orderItems = @(@{ productId = 5; quantity = 2; price = 29.99 }) } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:7071/api/orders" -Method Post -Body $body -ContentType "application/json"

# POST Speedy Order
$body = @{ customerId = 123; lineItems = @(@{ productId = 5; qty = 2; unitPrice = 29.99 }) } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:7071/api/speedy-orders" -Method Post -Body $body -ContentType "application/json"

# POST Vault Order
$body = @{ customerEmail = "customer@vault.com"; placedAt = 1700000000; items = @(@{ productCode = "550e8400-e29b-41d4-a716-446655440000"; quantityOrdered = 2; pricePerUnit = 29.99 }) } | ConvertTo-Json
Invoke-RestMethod -Uri "http://localhost:7071/api/vault-orders" -Method Post -Body $body -ContentType="application/json"
```

---

## 💡 Key Differences: Function vs API

| Aspect | Azure Function | Traditional API |
|--------|----------------|-----------------|
| Read JSON | `await req.ReadFromJsonAsync<T>()` | `[FromBody] T obj` |
| Validation | Manual | `ModelState.IsValid` |
| Routing | `[HttpTrigger(Route = "...")]` | `[Route("...")]` |
| Cost | Pay per execution | Pay for uptime |

## 📨 HttpResponseData vs IActionResult

```csharp
// IActionResult (automatic JSON serialization)
[Function("CreateOrder")]
public async Task<IActionResult> CreateOrder([HttpTrigger] HttpRequest req)
{
    var data = await req.ReadFromJsonAsync<OrderDto>();
    return new OkObjectResult(data); // Automatic JSON response
}

// HttpResponseData (manual control)
[Function("CreateVaultOrder")]
public async Task<HttpResponseData> CreateVaultOrder([HttpTrigger] HttpRequestData req)
{
    var response = req.CreateResponse();
    var data = await req.ReadFromJsonAsync<VaultOrderDto>();
    
    await response.WriteAsJsonAsync(data);
    response.StatusCode = System.Net.HttpStatusCode.Created;
    return response; // Manual JSON response
}
```

**When to use HttpResponseData:**
- Need fine-grained control over response
- Custom headers, status codes, or content types
- Streaming responses
- Advanced response manipulation

---

## 🧩 Extension Methods

```csharp
// In Extensions/MappingExtensions.cs
public static class MappingExtensions
{
    public static CreateOrderDto ToCreateOrderDto(this SpeedyOrderDto speedyOrder, string supplierName)
    {
        return new CreateOrderDto
        {
            SupplierId = 1, // Speedy is always supplier ID 1
            CustomerId = speedyOrder.CustomerId,
            OrderDate = speedyOrder.OrderTimestamp,
            // ... map other properties
        };
    }
}

// Usage in function
var createOrder = speedyOrder.ToCreateOrderDto(supplierName);
```

---

## 🐛 Common Mistakes

1. ❌ Creating new HttpClient each time → Use static
2. ❌ Not checking API response status → Check `IsSuccessStatusCode`
3. ❌ Forgetting case sensitivity → Use `PropertyNameCaseInsensitive`
4. ❌ Not handling errors → Use try-catch
5. ❌ Missing Content-Type → Specify "application/json"
6. ❌ **Hardcoding supplier IDs** → Use configuration or lookups
7. ❌ **Not using extension methods** → Keep mapping logic centralized
8. ❌ **Forgetting product ID lookups** → Always resolve external identifiers

---

## 📚 Files to Read

1. **README.md** - Overview and setup
2. **OrdersFunction.cs** - Complete examples with comments
3. **JSON_AND_HTTPCLIENT_GUIDE.md** - Deep dive explanations
4. **Extensions/MappingExtensions.cs** - Extension method examples

---

**Print this and keep it handy while coding!** 📄
