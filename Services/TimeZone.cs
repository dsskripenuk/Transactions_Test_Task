using Newtonsoft.Json;
using System.Globalization;
using Transactions_test_task.IServices;

namespace Transactions_test_task.Services
{
    public class TimeZone : ITimeZone
    {
        private readonly HttpClient _httpClient;
        private readonly string _googleApiKey;

        public TimeZone(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _googleApiKey = configuration["GoogleApiKey"];
        }

        public DateTime ConvertToUserTimeZone(DateTime utcTime, string userTimeZoneId)
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(userTimeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZone);
        }

        private bool IsValidLatitude(double latitude) => latitude >= -90 && latitude <= 90;
        private bool IsValidLongitude(double longitude) => longitude >= -180 && longitude <= 180;

        public async Task<string?> GetTimeZoneFromCoordinatesAsync(string coordinates)
        {
            if (string.IsNullOrWhiteSpace(coordinates))
            {
                throw new ArgumentException("Coordinates cannot be null or empty", nameof(coordinates));
            }

            var parts = coordinates.Split(',');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Coordinates should contain exactly two values separated by a comma.");
            }

            if (!double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
                !double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
            {
                throw new ArgumentException("Invalid coordinate format.");
            }

            if (!IsValidLatitude(latitude) || !IsValidLongitude(longitude))
            {
                throw new ArgumentException("Coordinates are out of range.");
            }

            var requestUri = $"https://maps.googleapis.com/maps/api/timezone/json?location={latitude},{longitude}&timestamp={new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()}&key={_googleApiKey}";

            try
            {
                Console.WriteLine($"Request URI: {requestUri}");

                var response = await _httpClient.GetAsync(requestUri);
                Console.WriteLine($"Response status code: {response.StatusCode}");

                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Response content: {content}");

                var result = JsonConvert.DeserializeObject<GoogleTimeZoneApiResponse>(content);

                if (result == null)
                {
                    throw new Exception("API response is null.");
                }

                switch (result.Status)
                {
                    case "OK":
                        return result.TimeZoneId;
                    case "ZERO_RESULTS":
                        Console.WriteLine("No time zone found for the provided coordinates.");
                        return null;
                    default:
                        throw new Exception($"API returned an error status: {result.Status}");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Console.WriteLine($"HTTP Request failed: {httpEx.Message}");
                throw;
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"JSON deserialization failed: {jsonEx.Message}");
                throw;
            }
        }
    }
}

public class GoogleTimeZoneApiResponse
{
    [JsonProperty("timeZoneId")]
    public string TimeZoneId { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }
}
