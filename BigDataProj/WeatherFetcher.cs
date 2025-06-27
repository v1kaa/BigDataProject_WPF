using System;
using System.Collections.Generic;
using System.Data;
//using System.Data.SqlClient;
using Microsoft.Data.Sqlite;

using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Windows.Documents;

namespace BigDataProj
{
    public class WeatherFetcher
    {
        private readonly string _connectionString;
        private static readonly HttpClient _httpClient = new HttpClient();

        public WeatherFetcher(string connectionString)
        {
            _connectionString = connectionString;
        }

      
        public async Task CheckAndUpdateWeatherDataAsync()
        {
            try
            {
                // 1. Get the date range and unique cities from the Sales table
                var salesData = await GetSalesDateRangeAndCitiesAsync();

                if (salesData.Cities == null || !salesData.Cities.Any())
                {
                    MessageBox.Show("Не знайдено міст у таблиці Sales", "Інформація",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Show information about the found data
                string info = $"Date range found: {salesData.StartDate:yyyy-MM-dd} - {salesData.EndDate:yyyy-MM-dd}\n" +
                             $"City: {string.Join(", ", salesData.Cities)}\n\n" +
                             $"⚠️ NOTE: To avoid API limitations, there will be a delay between requests.";

                var result = MessageBox.Show($"{info}\n\nStart downloading weather data?",
                    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // 2. For each city, we check and download weather data
                int totalCities = salesData.Cities.Count;
                int processedCities = 0;
                int newRecords = 0;

                foreach (string city in salesData.Cities)
                {
                    //Console.WriteLine($"City processing {processedCities + 1}/{totalCities}: {city}");

                    // Add a delay between cities to avoid rate limiting
                    if (processedCities > 0)
                    {
                        //Console.WriteLine("Затримка 1 секунда між містами...");
                        await Task.Delay(1000); // 1 секунда між містами
                    }

                    var cityRecords = await ProcessCityWeatherDataAsync(city, salesData.StartDate, salesData.EndDate);
                    newRecords += cityRecords;
                    processedCities++;
                }

                MessageBox.Show($"Synchronization complete!\n" +
                              $"city processed: {processedCities}\n" +
                              $"New entries added: {newRecords}",
                              "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error while syncing weather data:\n{ex.Message}",
                    "error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        

        

        #region methods

        private async Task<(DateTime StartDate, DateTime EndDate, List<string> Cities)> GetSalesDateRangeAndCitiesAsync()
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Get all dates as strings and process them in C#
                string query = @"
            SELECT Date
            FROM Sales
            WHERE Date IS NOT NULL AND Date != ''
            ORDER BY Date";

                var dates = new List<DateTime>();

                using (var command = new SqliteCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string dateString = reader["Date"].ToString();

                            // Trying different date formats
                            if (TryParseDate(dateString, out DateTime parsedDate))
                            {
                                dates.Add(parsedDate);
                            }
                        }
                    }
                }

                if (!dates.Any())
                {
                    throw new InvalidOperationException("No valid dates found in the Sales table");
                }

                DateTime startDate = dates.Min();
                DateTime endDate = dates.Max();

                // Get unique cities
                string cityQuery = @"
            SELECT DISTINCT City 
            FROM Sales 
            WHERE City IS NOT NULL 
            AND LTRIM(RTRIM(City)) != ''";

                var cities = new List<string>();

                using (var command = new SqliteCommand(cityQuery, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            cities.Add(reader.GetString("City").Trim());
                        }
                    }
                }

                return (startDate, endDate, cities);
            }
        }

        /// <summary>
        /// Helper method for parsing dates from different formats
        /// </summary>
        private bool TryParseDate(string dateString, out DateTime date)
        {
            date = default;

            if (string.IsNullOrWhiteSpace(dateString))
                return false;

            // List of possible date formats
            string[] formats = {
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "dd.MM.yyyy",
        "dd/MM/yyyy",
        "MM/dd/yyyy",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy/MM/dd HH:mm:ss",
        "dd.MM.yyyy HH:mm:ss",
        "dd/MM/yyyy HH:mm:ss",
        "MM/dd/yyyy HH:mm:ss"
    };

            // standard parsing
            if (DateTime.TryParse(dateString, out date))
            {
                return true;
            }

            // specific formats
            foreach (string format in formats)
            {
                if (DateTime.TryParseExact(dateString, format, null, System.Globalization.DateTimeStyles.None, out date))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<int> ProcessCityWeatherDataAsync(string city, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Add logging for debugging
                Console.WriteLine($"🏙️  City processing: {city}");
                Console.WriteLine($"📅 Perion: {startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}");

                // Check if there is already data for this city and period
                bool hasData = await HasWeatherDataAsync(city, startDate, endDate);
                Console.WriteLine($"📊 data already exist: {hasData}");

                if (hasData)
                {
                    Console.WriteLine($"⏭️  city {city} - data already exist");
                    return 0; 
                }

                
                Console.WriteLine($"🌐 Loading data from the API for {city}...");
                var weatherData = await GetWeatherDataFromApiAsync(city, startDate, endDate);

                if (weatherData != null && weatherData.days != null && weatherData.days.Any())
                {
                    Console.WriteLine($"✅ Received {weatherData.days.Count} days of data for {city}");

                   
                    await SaveWeatherDataAsync(city, weatherData.days);
                    Console.WriteLine($"💾 Saved the data to the database {city}");
                    return weatherData.days.Count;
                }
                else
                {
                    Console.WriteLine($"❌ No data received from API for {city}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                
                Console.WriteLine($"❌ Error processing city {city}: {ex.Message}");
                Console.WriteLine($"📍 Stack trace: {ex.StackTrace}");

                
                MessageBox.Show($"Error processing city {city}:\n{ex.Message}",
                    "error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return 0;
            }
        }

        private async Task<bool> HasWeatherDataAsync(string city, DateTime startDate, DateTime endDate)
        {
            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Get all dates for a city and check them
                string query = @"
            SELECT Date
            FROM Weather 
            WHERE city = @city";

                var weatherDates = new List<DateTime>();

                using (var command = new SqliteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@city", city);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string dateString = reader["Date"].ToString();
                            if (TryParseDate(dateString, out DateTime parsedDate))
                            {
                                weatherDates.Add(parsedDate.Date); 
                            }
                        }
                    }
                }

                // Checking date coverage
                int expectedDays = (endDate.Date - startDate.Date).Days + 1;
                int actualDays = 0;

                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    if (weatherDates.Contains(date))
                    {
                        actualDays++;
                    }
                }

                Console.WriteLine($"📊 for city {city}: waiting {expectedDays} days, found {actualDays} days");

               
                return actualDays >= (expectedDays * 0.9);
            }
        }

        private async Task<WeatherResponse> GetWeatherDataFromApiAsync(string city, DateTime startDate, DateTime endDate)
        {
            // First, we need to get latitude and longitude for the city
            var coordinates = await GetCityCoordinatesAsync(city);
            if (coordinates == null)
            {
                throw new Exception($"Could not find coordinates for city: {city}");
            }

            string url = $"https://archive-api.open-meteo.com/v1/archive?" +
                         $"latitude={coordinates.Latitude}&longitude={coordinates.Longitude}" +
                         $"&start_date={startDate:yyyy-MM-dd}&end_date={endDate:yyyy-MM-dd}" +
                         "&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,relative_humidity_2m_mean,weather_code" +
                         "&timezone=auto";

            int maxRetries = 3;
            int baseDelay = 2000;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Console.WriteLine($"🌐 API request #{attempt + 1} for {city}");

                    if (attempt > 0)
                    {
                        int delay = baseDelay * (int)Math.Pow(2, attempt - 1);
                        Console.WriteLine($"⏳ Waiting {delay}ms before retry...");
                        await Task.Delay(delay);
                    }

                    using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        request.Headers.Add("User-Agent", "WeatherApp/1.0");

                        using (var response = await _httpClient.SendAsync(request))
                        {
                            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                Console.WriteLine($"⚠️ Received 429 (Too Many Requests) for {city}");
                                continue;
                            }

                            response.EnsureSuccessStatusCode();

                            var responseContent = await response.Content.ReadAsStringAsync();
                            Console.WriteLine($"📡 API Response length: {responseContent.Length} characters");

                            // Convert Open-Meteo response to our WeatherResponse format
                            var openMeteoResponse = JsonConvert.DeserializeObject<OpenMeteoResponse>(responseContent);
                            var weatherResponse = ConvertToWeatherResponse(openMeteoResponse, city);

                            Console.WriteLine($"🔄 Converted response: {weatherResponse?.days?.Count ?? 0} days");
                            return weatherResponse;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error in attempt {attempt + 1}: {ex.Message}");
                    if (attempt == maxRetries)
                    {
                        throw new Exception($"Failed to get data for city {city} after {maxRetries + 1} attempts: {ex.Message}");
                    }
                }
            }

            throw new Exception($"Failed to get data for city {city} after {maxRetries + 1} attempts");
        }

        private async Task<classes.CityCoordinates> GetCityCoordinatesAsync(string city)
        {
            // You might want to implement a local cache of city coordinates
            // For now, we'll use Open-Meteo's geocoding API
            string geocodingUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1";

            try
            {
                var response = await _httpClient.GetAsync(geocodingUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var geocodingResponse = JsonConvert.DeserializeObject<GeocodingResponse>(content);

                if (geocodingResponse?.results?.Count > 0)
                {
                    return new classes.CityCoordinates
                    {
                        Latitude = geocodingResponse.results[0].latitude,
                        Longitude = geocodingResponse.results[0].longitude
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting coordinates for {city}: {ex.Message}");
            }

            return null;
        }

        private WeatherResponse ConvertToWeatherResponse(OpenMeteoResponse openMeteoResponse, string city)
        {
            if (openMeteoResponse?.daily == null)
                return null;

            var weatherResponse = new WeatherResponse
            {
                days = new List<WeatherDay>()
            };

            // Assuming all daily arrays have the same length
            for (int i = 0; i < openMeteoResponse.daily.time.Count; i++)
            {
                weatherResponse.days.Add(new WeatherDay
                {
                    datetime = openMeteoResponse.daily.time[i],
                    tempmax = openMeteoResponse.daily.temperature_2m_max[i],
                    tempmin = openMeteoResponse.daily.temperature_2m_min[i],
                    precip = openMeteoResponse.daily.precipitation_sum[i],
                    humidity = openMeteoResponse.daily.relative_humidity_2m_mean[i],
                    conditions = ConvertWeatherCodeToString(openMeteoResponse.daily.weather_code[i])
                });
            }

            return weatherResponse;
        }

        private string ConvertWeatherCodeToString(int weatherCode)
        {
            // Convert WMO weather code to human-readable string
            // Full list: https://open-meteo.com/en/docs
            switch (weatherCode)
            {
                case 0: return "Clear";
                case 1: return "Mainly clear";
                case 2: return "Partly cloudy";
                case 3: return "Overcast";
                case 45: return "Fog";
                case 48: return "Depositing rime fog";
                case 51: return "Light drizzle";
                case 53: return "Moderate drizzle";
                case 55: return "Dense drizzle";
                case 56: return "Light freezing drizzle";
                case 57: return "Dense freezing drizzle";
                case 61: return "Slight rain";
                case 63: return "Moderate rain";
                case 65: return "Heavy rain";
                case 66: return "Light freezing rain";
                case 67: return "Heavy freezing rain";
                case 71: return "Slight snow fall";
                case 73: return "Moderate snow fall";
                case 75: return "Heavy snow fall";
                case 77: return "Snow grains";
                case 80: return "Slight rain showers";
                case 81: return "Moderate rain showers";
                case 82: return "Violent rain showers";
                case 85: return "Slight snow showers";
                case 86: return "Heavy snow showers";
                case 95: return "Thunderstorm";
                case 96: return "Thunderstorm with slight hail";
                case 99: return "Thunderstorm with heavy hail";
                default: return "Unknown";
            }
        }
        private async Task SaveWeatherDataAsync(string city, List<WeatherDay> days)
        {
            Console.WriteLine($"💾 start saving {days.Count} days for city {city}");

            // first five records
            foreach (var day in days.Take(5))
            {
                //Console.WriteLine($"Date: {day.datetime}, " +
                //                  $"TempMax: {day.tempmax}, TempMin: {day.tempmin}, " +
                //                  $"Precip: {day.precip}, Conditions: {day.conditions}, " +
                //                  $"Humidity: {day.humidity}");
            }

            using (var connection = new SqliteConnection(_connectionString))
            {
                await connection.OpenAsync();
                //Console.WriteLine("З'єднання з базою відкрито");

                // transaction
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {

                        string insertQuery = @"
    INSERT OR IGNORE INTO Weather (Date, TempMax, TempMin, Precip, Conditions, Humidity, city)
    VALUES (@Date, @TempMax, @TempMin, @Precip, @Conditions, @Humidity, @City)";

                        int savedCount = 0;

                        foreach (var day in days)
                        {
                            using (var command = new SqliteCommand(insertQuery, connection, transaction))
                            {
                                
                                command.Parameters.AddWithValue("@Date", day.datetime);
                                command.Parameters.AddWithValue("@TempMax", day.tempmax);
                                command.Parameters.AddWithValue("@TempMin", day.tempmin);
                                command.Parameters.AddWithValue("@Precip", day.precip);
                                command.Parameters.AddWithValue("@Conditions", day.conditions ?? (object)DBNull.Value);
                                command.Parameters.AddWithValue("@Humidity", day.humidity);
                                command.Parameters.AddWithValue("@City", city);

                                int rowsAffected = await command.ExecuteNonQueryAsync();
                                if (rowsAffected > 0)
                                {
                                    savedCount++;
                                    Console.WriteLine($" Saved data for {city} on {day.datetime}");
                                }
                                else
                                {
                                    Console.WriteLine($" record for {city} on {day.datetime} already exist");
                                }
                            }
                        }

                        transaction.Commit();
                        Console.WriteLine($" Transaction completed successfully. Saved {savedCount} records for {city}");

                       
                        MessageBox.Show($"saved {savedCount} records for city {city}",
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Save error, transaction canceled: {ex.Message}");
                        
                        throw new Exception($"Error saving weather data for the city {city}: {ex.Message}", ex);
                    }
                }
            }
        }


        public class OpenMeteoResponse
        {
            public classes.DailyData daily { get; set; }
        }

        public class GeocodingResponse
        {
            public List<classes.GeocodingResult> results { get; set; }
        }
      
        #endregion
    }

 
}