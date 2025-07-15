using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AionCoreBot.Infrastructure.Interfaces;

namespace AionCoreBot.Infrastructure.Comms.Clients
{
    public class BinanceRestClient : IBinanceRestClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private const string BaseUrl = "https://api.binance.com";

        public BinanceRestClient(string apiKey, string apiSecret)
        {
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _apiSecret = apiSecret ?? throw new ArgumentNullException(nameof(apiSecret));

            _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            _httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);
        }

        /* ----------  account ---------- */

        public async Task<string> GetAccountInfoAsync(CancellationToken ct = default)
        {
            var endpoint = "/api/v3/account";

            var qs = $"timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var url = BuildSignedUrl(endpoint, qs);

            using var resp = await _httpClient.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync(ct);
        }

        /* ----------  public helper (geen signature) ---------- */

        public async Task<string> GetRawAsync(string url, CancellationToken ct = default)
        {
            using var resp = await _httpClient.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync(ct);
        }

        /* ----------  orders ---------- */

        public async Task<string> PlaceOrderAsync(
            string symbol,
            string side,
            string type,
            decimal quantity,
            decimal? price = null,
            CancellationToken ct = default)
        {
            var endpoint = "/api/v3/order";

            var q = HttpUtility.ParseQueryString(string.Empty);
            q["symbol"] = symbol.ToUpperInvariant();
            q["side"] = side.ToUpperInvariant();
            q["type"] = type.ToUpperInvariant();
            q["quantity"] = quantity.ToString(System.Globalization.CultureInfo.InvariantCulture);
            q["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            if (price.HasValue)
            {
                q["price"] = price.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                q["timeInForce"] = "GTC";
            }

            var url = BuildSignedUrl(endpoint, q.ToString()!);

            using var resp = await _httpClient.PostAsync(url, new StringContent(string.Empty), ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync(ct);
        }

        public async Task<string> GetOrderStatusAsync(string symbol, long orderId, CancellationToken ct = default)
        {
            var endpoint = "/api/v3/order";

            var q = HttpUtility.ParseQueryString(string.Empty);
            q["symbol"] = symbol.ToUpperInvariant();
            q["orderId"] = orderId.ToString();
            q["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            var url = BuildSignedUrl(endpoint, q.ToString()!);

            using var resp = await _httpClient.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync(ct);
        }

        public async Task<string> CancelOrderAsync(string symbol, long orderId, CancellationToken ct = default)
        {
            var endpoint = "/api/v3/order";

            var q = HttpUtility.ParseQueryString(string.Empty);
            q["symbol"] = symbol.ToUpperInvariant();
            q["orderId"] = orderId.ToString();
            q["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

            var url = BuildSignedUrl(endpoint, q.ToString()!);

            using var req = new HttpRequestMessage(HttpMethod.Delete, url);
            using var resp = await _httpClient.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync(ct);
        }

        /* ----------  helpers ---------- */

        private string BuildSignedUrl(string endpoint, string queryString)
        {
            // queryString kan leeg zijn voor sommige (publieke) endpoints
            var sig = CreateSignature(queryString);
            return string.IsNullOrEmpty(queryString)
                ? $"{endpoint}?signature={sig}"
                : $"{endpoint}?{queryString}&signature={sig}";
        }

        private string CreateSignature(string queryString)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public void Dispose() => _httpClient?.Dispose();
    }
}


//using System;
//using System.Net.Http;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading.Tasks;
//using System.Web;
//using AionCoreBot.Infrastructure.Interfaces;

//namespace AionCoreBot.Infrastructure.Comms.Clients
//{    
//    public class BinanceRestClient : IBinanceRestClient, IDisposable
//    {
//        private readonly HttpClient _httpClient;
//        private readonly string _apiKey;
//        private readonly string _apiSecret;
//        private const string BaseUrl = "https://api.binance.com";

//        public BinanceRestClient(string apiKey, string apiSecret)
//        {
//            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
//            _apiSecret = apiSecret ?? throw new ArgumentNullException(nameof(apiSecret));

//            _httpClient = new HttpClient
//            {
//                BaseAddress = new Uri(BaseUrl)
//            };

//            _httpClient.DefaultRequestHeaders.Add("X-MBX-APIKEY", _apiKey);
//        }

//        public async Task<string> GetAccountInfoAsync(CancellationToken ct = default)
//        {
//            var endpoint = "/api/v3/account";
//            var qs = $"timestamp={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
//            var url = BuildSignedUrl(endpoint, qs);
//            var response = await _httpClient.GetAsync(url, ct);
//            response.EnsureSuccessStatusCode();
//            return await response.Content.ReadAsStringAsync();
//        }

//        public async Task<string> GetRawAsync(string url, CancellationToken ct = default)
//        {
//            var response = await _httpClient.GetAsync(url);
//            response.EnsureSuccessStatusCode();
//            return await response.Content.ReadAsStringAsync();
//        }

//        public async Task<string> PlaceOrderAsync(string symbol, string side, string type, decimal quantity, decimal? price = null)
//        {
//            var endpoint = "/api/v3/order";
//            var query = HttpUtility.ParseQueryString(string.Empty);
//            query["symbol"] = symbol.ToUpperInvariant();
//            query["side"] = side.ToUpperInvariant();
//            query["type"] = type.ToUpperInvariant();
//            query["quantity"] = quantity.ToString(System.Globalization.CultureInfo.InvariantCulture);
//            query["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

//            if (price.HasValue)
//            {
//                query["price"] = price.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
//                // Voor limit orders is vaak ook nodig: timeInForce = GTC
//                query["timeInForce"] = "GTC";
//            }

//            var queryString = query.ToString();
//            var url = BuildSignedUrl(endpoint, queryString);

//            var content = new StringContent(string.Empty); // POST zonder body, alles in URL querystring

//            var response = await _httpClient.PostAsync(url, content);
//            response.EnsureSuccessStatusCode();
//            return await response.Content.ReadAsStringAsync();
//        }

//        public async Task<string> GetOrderStatusAsync(string symbol, long orderId)
//        {
//            var endpoint = "/api/v3/order";
//            var query = HttpUtility.ParseQueryString(string.Empty);
//            query["symbol"] = symbol.ToUpperInvariant();
//            query["orderId"] = orderId.ToString();
//            query["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

//            var queryString = query.ToString();
//            var url = BuildSignedUrl(endpoint, queryString);

//            var response = await _httpClient.GetAsync(url);
//            response.EnsureSuccessStatusCode();
//            return await response.Content.ReadAsStringAsync();
//        }

//        public async Task<string> CancelOrderAsync(string symbol, long orderId)
//        {
//            var endpoint = "/api/v3/order";
//            var query = HttpUtility.ParseQueryString(string.Empty);
//            query["symbol"] = symbol.ToUpperInvariant();
//            query["orderId"] = orderId.ToString();
//            query["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

//            var queryString = query.ToString();
//            var url = BuildSignedUrl(endpoint, queryString);

//            var request = new HttpRequestMessage(HttpMethod.Delete, url);
//            var response = await _httpClient.SendAsync(request);
//            response.EnsureSuccessStatusCode();
//            return await response.Content.ReadAsStringAsync();
//        }

//        private string BuildSignedUrl(string endpoint, string queryString)
//        {
//            var signature = CreateSignature(queryString);
//            return $"{endpoint}?{queryString}&signature={signature}";
//        }

//        private string CreateSignature(string queryString)
//        {
//            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_apiSecret));
//            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(queryString));
//            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
//        }

//        public void Dispose()
//        {
//            _httpClient?.Dispose();
//        }
//    }
//}
