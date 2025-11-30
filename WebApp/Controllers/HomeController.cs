using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

namespace WebApp.Controllers;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Connect(string url, string databaseName, string username, string password)
    {
        try
        {
            var (success, sessionId, userId, handler, errorMessage) = await AuthenticateOdoo(url, databaseName, username, password);
            
            if (success)
            {
                // Récupérer les produits après la connexion
                var (productsSuccess, products, productsError) = await GetProducts(url, databaseName, sessionId!, handler!);
                
                if (productsSuccess)
                {
                    ViewBag.Success = true;
                    ViewBag.Message = $"Connection successful! Session ID: {sessionId}, User ID: {userId}. Found {products?.Count ?? 0} products.";
                    ViewBag.SessionId = sessionId;
                    ViewBag.UserId = userId;
                    ViewBag.Products = products;
                }
                else
                {
                    ViewBag.Success = false;
                    ViewBag.Message = $"Connected but failed to fetch products: {productsError}";
                }
            }
            else
            {
                ViewBag.Success = false;
                ViewBag.Message = $"Connection failed: {errorMessage}";
            }
        }
        catch (Exception ex)
        {
            ViewBag.Success = false;
            ViewBag.Message = $"Error: {ex.Message}";
        }
        
        return View("Index");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<(bool success, string? sessionId, string? userId, HttpClientHandler? handler, string? errorMessage)> AuthenticateOdoo(
        string baseUrl, string databaseName, string username, string password)
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = new System.Net.CookieContainer()
        };
        
        var httpClient = new HttpClient(handler);
        
        // Ensure the base URL ends with a slash
        if (!baseUrl.EndsWith("/"))
        {
            baseUrl += "/";
        }
        
        var authUrl = $"{baseUrl}web/session/authenticate";
        
        var requestBody = new
        {
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                db = databaseName,
                login = username,
                password = password
            },
            id = new Random().Next(1, 1000000)
        };
        
        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync(authUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            return (false, null, null, null, $"HTTP Error: {response.StatusCode}");
        }
        
        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;
        
        // Check for JSON-RPC error
        if (root.TryGetProperty("error", out var error))
        {
            var errorMessage = error.TryGetProperty("data", out var errorData) && 
                             errorData.TryGetProperty("message", out var msg)
                ? msg.GetString()
                : "Authentication failed";
            return (false, null, null, null, errorMessage);
        }
        
        // Extract user ID from result
        string? userId = null;
        string? sessionId = null;
        
        if (root.TryGetProperty("result", out var result))
        {
            // Get UID from result
            if (result.TryGetProperty("uid", out var uid))
            {
                var uidValue = uid.GetInt32();
                if (uidValue > 0) // Valid user ID
                {
                    userId = uidValue.ToString();
                }
            }
            
            // Try to extract session_id from cookies
            var cookies = handler.CookieContainer.GetCookies(new Uri(baseUrl));
            foreach (System.Net.Cookie cookie in cookies)
            {
                if (cookie.Name == "session_id")
                {
                    sessionId = cookie.Value;
                    break;
                }
            }
            
            if (!string.IsNullOrEmpty(userId) && userId != "0")
            {
                return (true, sessionId ?? "Cookie not found", userId, handler, null);
            }
        }
        
        return (false, null, null, null, "Invalid credentials or authentication failed");
    }

    private async Task<(bool success, List<Dictionary<string, object>>? products, string? errorMessage)> GetProducts(
        string baseUrl, string databaseName, string sessionId, HttpClientHandler handler)
    {
        var httpClient = new HttpClient(handler);
        
        // Ensure the base URL ends with a slash
        if (!baseUrl.EndsWith("/"))
        {
            baseUrl += "/";
        }
        
        var callKwUrl = $"{baseUrl}web/dataset/call_kw";
        
        var requestBody = new
        {
            jsonrpc = "2.0",
            method = "call",
            @params = new
            {
                model = "product.product",
                method = "search_read",
                args = new object[] { },
                kwargs = new
                {
                    fields = new[] { "id", "name", "default_code", "list_price", "qty_available", "categ_id", "max_guests" },
                    limit = 50
                }
            },
            id = new Random().Next(1, 1000000)
        };
        
        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        
        var response = await httpClient.PostAsync(callKwUrl, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            return (false, null, $"HTTP Error: {response.StatusCode}");
        }
        
        using var doc = JsonDocument.Parse(responseContent);
        var root = doc.RootElement;
        
        // Check for JSON-RPC error
        if (root.TryGetProperty("error", out var error))
        {
            var errorMessage = error.TryGetProperty("data", out var errorData) && 
                             errorData.TryGetProperty("message", out var msg)
                ? msg.GetString()
                : "Failed to fetch products";
            return (false, null, errorMessage);
        }
        
        // Extract products from result
        if (root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Array)
        {
            var products = new List<Dictionary<string, object>>();
            
            foreach (var item in result.EnumerateArray())
            {
                var product = new Dictionary<string, object>();
                
                foreach (var property in item.EnumerateObject())
                {
                    product[property.Name] = property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString() ?? "",
                        JsonValueKind.Number => property.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.Null => null!,
                        _ => property.Value.ToString()
                    };
                }
                
                products.Add(product);
            }
            
            return (true, products, null);
        }
        
        return (false, null, "No products found in response");
    }
}
