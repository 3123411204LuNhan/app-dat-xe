using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RideHailingApp.Services
{
    public class GoogleMapsService
    {
        public const string MapsApiKey = "AIzaSyCCYa-4drXqi7-uTnLKKzprYFiJZlSzXss";
        private readonly HttpClient _routesClient;
        private readonly HttpClient _placesClient;
        private readonly HttpClient _geoClient;

        public GoogleMapsService()
        {
            _routesClient = new HttpClient
            {
                BaseAddress = new Uri("https://routes.googleapis.com/"),
                Timeout = TimeSpan.FromSeconds(10)
            };
            _routesClient.DefaultRequestHeaders.Add("X-Goog-Api-Key", MapsApiKey);
            _routesClient.DefaultRequestHeaders.Add(
                "X-Goog-FieldMask",
                "routes.duration,routes.distanceMeters,routes.polyline.encodedPolyline");

            _placesClient = new HttpClient
            {
                BaseAddress = new Uri("https://places.googleapis.com/"),
                Timeout = TimeSpan.FromSeconds(10)
            };
            _placesClient.DefaultRequestHeaders.Add("X-Goog-Api-Key", MapsApiKey);
            _placesClient.DefaultRequestHeaders.Add(
                "X-Goog-FieldMask",
                "suggestions.placePrediction.text,suggestions.placePrediction.placeId");

            _geoClient = new HttpClient
            {
                BaseAddress = new Uri("https://maps.googleapis.com/"),
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        // ─── Routes API ───

        public async Task<RouteResult> GetRouteAsync(string originAddress, string destinationAddress)
        {
            var body = new RouteRequest
            {
                Origin      = new RouteWaypoint { Address = originAddress },
                Destination = new RouteWaypoint { Address = destinationAddress }
            };
            return await CallRoutesApiAsync(body);
        }

        public async Task<RouteResult> GetRouteByCoordinatesAsync(
            double originLat, double originLon,
            double destLat, double destLon)
        {
            var body = new RouteRequest
            {
                Origin = new RouteWaypoint
                {
                    Location = new RouteLocation { LatLng = new LatLng { Latitude = originLat, Longitude = originLon } }
                },
                Destination = new RouteWaypoint
                {
                    Location = new RouteLocation { LatLng = new LatLng { Latitude = destLat, Longitude = destLon } }
                }
            };
            return await CallRoutesApiAsync(body);
        }

        private async Task<RouteResult> CallRoutesApiAsync(RouteRequest body)
        {
            try
            {
                var resp = await _routesClient.PostAsJsonAsync(
                    "directions/v2:computeRoutes", body);

                if (!resp.IsSuccessStatusCode)
                {
                    var err = await resp.Content.ReadAsStringAsync();
                    return new RouteResult { IsSuccess = false, ErrorMessage = $"Routes API {(int)resp.StatusCode}: {err}" };
                }

                var data = await resp.Content.ReadFromJsonAsync<RouteResponse>();
                if (data == null || data.Routes.Count == 0)
                    return new RouteResult { IsSuccess = false, ErrorMessage = "Không tìm thấy tuyến đường." };

                var route = data.Routes[0];
                var decoded = DecodePolyline(route.Polyline?.EncodedPolyline ?? "");

                return new RouteResult
                {
                    IsSuccess       = true,
                    DistanceKm      = route.DistanceKm,
                    DurationMinutes = route.DurationMinutes,
                    EncodedPolyline = route.Polyline?.EncodedPolyline ?? "",
                    DecodedPoints   = decoded
                };
            }
            catch (Exception ex)
            {
                return new RouteResult { IsSuccess = false, ErrorMessage = $"Lỗi mạng: {ex.Message}" };
            }
        }

        // ─── Places Autocomplete API ───

        public async Task<List<PlaceSuggestion>> GetAutocompleteAsync(
            string input, double biasLat = 10.7769, double biasLon = 106.7009)
        {
            if (string.IsNullOrWhiteSpace(input) || input.Length < 2)
                return new List<PlaceSuggestion>();

            var body = new AutocompleteRequest
            {
                Input = input,
                LocationBias = new LocationBias
                {
                    Circle = new LocationCircle
                    {
                        Center = new LatLng { Latitude = biasLat, Longitude = biasLon },
                        Radius = 50000
                    }
                }
            };

            try
            {
                var resp = await _placesClient.PostAsJsonAsync("v1/places:autocomplete", body);
                if (!resp.IsSuccessStatusCode) return new List<PlaceSuggestion>();

                var data = await resp.Content.ReadFromJsonAsync<AutocompleteResponse>();
                return data?.Suggestions ?? new List<PlaceSuggestion>();
            }
            catch
            {
                return new List<PlaceSuggestion>();
            }
        }

        // ─── Geocoding API ───

        public async Task<(double Lat, double Lon, string Address)?> GeocodeAsync(string address)
        {
            try
            {
                var url = $"maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={MapsApiKey}&language=vi&region=vn";
                var resp = await _geoClient.GetFromJsonAsync<GeocodingResponse>(url);
                if (resp?.Status != "OK" || resp.Results.Count == 0) return null;

                var loc = resp.Results[0].Geometry?.Location;
                if (loc == null) return null;
                return (loc.Latitude, loc.Longitude, resp.Results[0].FormattedAddress);
            }
            catch
            {
                return null;
            }
        }

        public async Task<string?> ReverseGeocodeAsync(double lat, double lon)
        {
            try
            {
                var url = $"maps/api/geocode/json?latlng={lat},{lon}&key={MapsApiKey}&language=vi";
                var resp = await _geoClient.GetFromJsonAsync<GeocodingResponse>(url);
                if (resp?.Status != "OK" || resp.Results.Count == 0) return null;
                return resp.Results[0].FormattedAddress;
            }
            catch
            {
                return null;
            }
        }

        // ─── Polyline Decoder ───

        public static List<(double Lat, double Lon)> DecodePolyline(string encoded)
        {
            var points = new List<(double, double)>();
            if (string.IsNullOrEmpty(encoded)) return points;

            int index = 0, len = encoded.Length;
            int lat = 0, lng = 0;

            while (index < len)
            {
                int result = 0, shift = 0, b;
                do
                {
                    b = encoded[index++] - 63;
                    result |= (b & 0x1F) << shift;
                    shift += 5;
                } while (b >= 0x20);
                lat += (result & 1) != 0 ? ~(result >> 1) : result >> 1;

                result = 0; shift = 0;
                do
                {
                    b = encoded[index++] - 63;
                    result |= (b & 0x1F) << shift;
                    shift += 5;
                } while (b >= 0x20);
                lng += (result & 1) != 0 ? ~(result >> 1) : result >> 1;

                points.Add((lat / 1e5, lng / 1e5));
            }
            return points;
        }
    }
}
