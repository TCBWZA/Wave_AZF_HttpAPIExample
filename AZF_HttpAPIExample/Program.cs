using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

/*
* ============================================================================
* AZURE FUNCTIONS PROGRAM.CS - Startup Configuration
* ============================================================================
* 
* This file configures the Azure Functions application.
* Similar to Startup.cs or Program.cs in ASP.NET Core apps.
* 
* KEY CONCEPTS:
* 
* 1. FUNCTIONS APPLICATION BUILDER:
*    - Creates and configures the Functions host
*    - Similar to WebApplication.CreateBuilder() in ASP.NET Core
* 
* 2. DEPENDENCY INJECTION:
*    - Register services using builder.Services
*    - Services are injected into Function constructors
*    - Examples: ILogger, IConfiguration, HttpClient, custom services
* 
* 3. CONFIGURATION:
*    - IConfiguration is automatically available for dependency injection
*    - Reads from local.settings.json (local) or Azure App Settings (deployed)
*    - Access in functions via constructor injection
* 
* 4. APPLICATION INSIGHTS:
*    - Telemetry and monitoring for Azure Functions
*    - Tracks requests, dependencies, exceptions
*    - View data in Azure Portal
*/

var builder = FunctionsApplication.CreateBuilder(args);

// Configure the Functions Web Application middleware
// This enables HTTP trigger support and ASP.NET Core integration
builder.ConfigureFunctionsWebApplication();

// Add Application Insights for monitoring and diagnostics
// This tracks all function executions, dependencies (like HTTP calls), and errors
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

/*
 * BEST PRACTICE: Configure HttpClient with IHttpClientFactory
 * 
 * Instead of using a static HttpClient (as shown in the simple example),
 * production code should use IHttpClientFactory to:
 * - Avoid socket exhaustion
 * - Enable proper DNS refresh
 * - Support named/typed clients
 * - Allow for configuration and policies (retry, timeout, etc.)
 * 
 * Uncomment the following to use IHttpClientFactory:
 */

// Register HttpClient for calling external APIs
builder.Services.AddHttpClient("OrdersAPI", client =>
{
    // Configure base address for your API
    // TODO: Replace with your actual API URL
    client.BaseAddress = new Uri("https://your-api-url.com/");
    
    // Set default request headers
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    
    // Set timeout for requests
    client.Timeout = TimeSpan.FromSeconds(30);
});

/*
 * OTHER COMMON SERVICES YOU MIGHT ADD:
 * 
 * 1. Custom Services:
 *    builder.Services.AddScoped<IOrderService, OrderService>();
 * 
 * 2. Database Context (if using Entity Framework):
 *    builder.Services.AddDbContext<AppDbContext>(options =>
 *        options.UseSqlServer(connectionString));
 * 
 * 3. Configuration:
 *    var config = builder.Configuration;
 *    var apiKey = config["ApiKey"];
 * 
 * 4. CORS (if needed for browser access):
 *    builder.Services.AddCors(options => { ... });
 * 
 * 5. Authentication:
 *    builder.Services.AddAuthentication(...)
 */

// Build and run the application
builder.Build().Run();

/*
 * ============================================================================
 * CONFIGURATION FILES
 * ============================================================================
 * 
 * Azure Functions uses these configuration files:
 * 
 * 1. local.settings.json (local development only, not deployed):
 *    {
 *      "IsEncrypted": false,
 *      "Values": {
 *        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
 *        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
 *        "ApiBaseUrl": "https://your-api.com",
 *        "ApiKey": "your-secret-key"
 *      }
 *    }
 * 
 * 2. host.json (function host configuration):
 *    {
 *      "version": "2.0",
 *      "logging": {
 *        "applicationInsights": {
 *          "samplingSettings": {
 *            "isEnabled": true
 *          }
 *        }
 *      }
 *    }
 * 
 * 3. In Azure Portal:
 *    - Configuration → Application Settings
 *    - Add key-value pairs for production settings
 *    - Access via Environment.GetEnvironmentVariable("KeyName")
 * 
 * ============================================================================
 */
