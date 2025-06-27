using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Text.Json.Serialization;
using BigDataProj.classes;

namespace BigDataProj
{
    public class ConditionStatistic
    {
        public string Condition { get; set; }
        public decimal TotalSales { get; set; }
        public int SalesCount { get; set; }
        public decimal AverageSale { get; set; }
        public string FormattedTotalSales => $"${TotalSales:N2}";
        public string FormattedAverageSale => $"${AverageSale:N2}";

        // Properties for visual chart
        public double BarWidth { get; set; }
        public Brush BarColor { get; set; }
        public string PercentageText { get; set; }
    }
    
    public partial class Analyze : Window

    {

        public Analyze()
        {
            _ = CheckAndUpdateWeatherDataAsync();
            InitializeComponent();
            LoadAvailableDates();

        }
        string DbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, System.IO.Path.Combine("Database", "cafee_weather_db.db"));
        private const string ConnectionString = "Data Source=localhost;Initial Catalog=YourDatabaseName;Integrated Security=True;";
       

        public async Task CheckAndUpdateWeatherDataAsync()
        {
            string connectionString = $"Data Source={DbPath}";
            {
                try
                {
                    var weatherManager = new WeatherFetcher(connectionString);
                    await weatherManager.CheckAndUpdateWeatherDataAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Weather data update error: {ex.Message}",
                        "error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                
            }
        }

        public List<DateTime> GetAvailableSalesDates(string dbPath)
        {
            var availableDates = new List<DateTime>();

            using (var connection = new SqliteConnection($"Data Source={dbPath}"))
            {
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT DISTINCT Date FROM Sales ORDER BY Date
                ";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (DateTime.TryParse(reader.GetString(0), out DateTime date))
                        {
                            availableDates.Add(date);
                        }
                    }
                }
            }

            return availableDates;
        }

        // Load available dates into the DatePickers (Start and End)
        private void LoadAvailableDates()
        {
            var availableDates = GetAvailableSalesDates(DbPath);

            if (availableDates.Count > 0)
            {
                // Set the minimum date of StartDatePicker to the first available date
                StartDatePicker.DisplayDateStart = availableDates.First();
                EndDatePicker.DisplayDateStart = availableDates.First();

                // Set the maximum date of EndDatePicker to the last available date
                StartDatePicker.DisplayDateEnd = availableDates.Last();
                EndDatePicker.DisplayDateEnd = availableDates.Last();

                // Set the default selected date to the first available date
                StartDatePicker.SelectedDate = availableDates.First();
                EndDatePicker.SelectedDate = availableDates.Last();
            }
        }

        // Event handler for the Analyze button click
        private void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected start and end dates
            DateTime? startDate = StartDatePicker.SelectedDate;
            DateTime? endDate = EndDatePicker.SelectedDate;

            if (!startDate.HasValue || !endDate.HasValue)
            {
                MessageBox.Show("Please select both start and end dates.");
                return;
            }

            if (startDate > endDate)
            {
                MessageBox.Show("The start date cannot be later than the end date.");
                return;
            }

            // Get the sales data for the selected date range
            List<classes.SalesData> salesData = GetSalesDataForDateRange(startDate.Value, endDate.Value);

            if (salesData.Count == 0)
            {
                MessageBox.Show("No sales data found for the selected date range.");
                return;
            }

            // Display the sales data in the ListView
            SalesDataListView.ItemsSource = salesData;

            // Analyze the data (e.g., calculate total sales, average sales)
            AnalyzeSalesData(salesData);
        }

        private List<classes.SalesData> GetSalesDataForDateRange(DateTime startDate, DateTime endDate)
        {
            // Connect to the database and fetch the sales data for the selected date range
            // Replace this with your actual database fetching logic
            List<classes.SalesData> salesData = new List<classes.SalesData>();

            using (var connection = new SqliteConnection($"Data Source={DbPath}"))
            {
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"
                    SELECT Date, Product, Quantity, Total
                    FROM Sales
                    WHERE Date BETWEEN $start AND $end
                ";
                cmd.Parameters.AddWithValue("$start", startDate.ToString("yyyy-MM-dd"));
                cmd.Parameters.AddWithValue("$end", endDate.ToString("yyyy-MM-dd"));

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        salesData.Add(new classes.SalesData
                        {
                            Date = reader.GetString(0),
                            Product = reader.GetString(1),
                            Quantity = reader.GetInt32(2),
                            Total = reader.GetDouble(3)
                        });
                    }
                }
            }

            return salesData;
        }

        private void AnalyzeSalesData(List<classes.SalesData> salesData)
        {
            // Calculate total sales
            double totalSales = salesData.Sum(s => s.Total);

            // Calculate average sales per day
            double averageSalesPerDay = salesData.Average(s => s.Total);
           ;

            // Display the analysis results
            AnalysisResult.Text = $"Total Sales: {totalSales:C}\nAverage Sales per Day: {averageSalesPerDay:C}";

        }


        public void ConditionStatiscticsButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate date selection
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select both start and end dates.", "Date Selection Required",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime startDate = StartDatePicker.SelectedDate.Value;
            DateTime endDate = EndDatePicker.SelectedDate.Value;

            if (startDate > endDate)
            {
                MessageBox.Show("Start date cannot be later than end date.", "Invalid Date Range",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get statistics data
            var conditionStats = GetConditionStatistics(startDate, endDate);

            if (conditionStats == null || !conditionStats.Any())
            {
                MessageBox.Show("No data found for the selected date range.", "No Data",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Open statistics window
            var statsWindow = new ConditionStatisticsWindow(conditionStats, startDate, endDate);
            statsWindow.Show();
        }

        // Method to calculate condition statistics from database
        private List<ConditionStatistic> GetConditionStatistics(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DbPath}"))
                {
                    connection.Open();

                    string query = @"
                SELECT 
                    w.Conditions,
                    SUM(s.Total) as TotalSales,
                    COUNT(*) as SalesCount,
                    AVG(s.Total) as AverageSale
                FROM Sales s
                INNER JOIN Weather w ON date(s.Date) = date(w.Date) AND s.City = w.City
                WHERE s.Date >= $StartDate AND s.Date <= $EndDate
                GROUP BY w.Conditions
                ORDER BY SUM(s.Total) DESC";

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        command.Parameters.AddWithValue("$StartDate", startDate.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("$EndDate", endDate.ToString("yyyy-MM-dd"));

                        var conditionStats = new List<ConditionStatistic>();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                conditionStats.Add(new ConditionStatistic
                                {
                                    Condition = reader["Conditions"].ToString(),
                                    TotalSales = Convert.ToDecimal(reader["TotalSales"]),
                                    SalesCount = Convert.ToInt32(reader["SalesCount"]),
                                    AverageSale = Convert.ToDecimal(reader["AverageSale"])
                                });
                            }
                        }

                        return conditionStats;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating statistics: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<ConditionStatistic>();
            }
        }
        public void CleanData_Click(object sender, EventArgs e)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DbPath}"))
                {
                    connection.Open();

                    int nullRowsCount = 0;
                    int duplicateRowsCount = 0;

                    // Count null/invalid rows
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                    SELECT COUNT(*) FROM Sales
                    WHERE 
                        Product IS NULL OR TRIM(Product) = '' OR
                        Quantity IS NULL OR
                        Total IS NULL
                ";
                        nullRowsCount = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Count duplicate rows
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                    SELECT COUNT(*) FROM Sales
                    WHERE rowid NOT IN (
                        SELECT MIN(rowid)
                        FROM Sales
                        GROUP BY Date, Product, Quantity, Total
                    )
                ";
                        duplicateRowsCount = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    // Delete null/invalid rows
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                    DELETE FROM Sales
                    WHERE 
                        Product IS NULL OR TRIM(Product) = '' OR
                        Quantity IS NULL OR
                        Total IS NULL
                ";
                        cmd.ExecuteNonQuery();
                    }

                    // Delete duplicate rows
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = @"
                    DELETE FROM Sales
                    WHERE rowid NOT IN (
                        SELECT MIN(rowid)
                        FROM Sales
                        GROUP BY Date, Product, Quantity, Total
                    )
                ";
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show(
                        $"Data cleaned successfully!\n" +
                        $"{nullRowsCount} rows removed due to null or invalid values.\n" +
                        $"{duplicateRowsCount} duplicate rows removed.",
                        "Cleanup Summary", MessageBoxButton.OK, MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cleaning data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void TemperatureSales_Click(object sender, EventArgs e)
        {
            // Validate date selection
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select both start and end dates.", "Date Selection Required",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime startDate = StartDatePicker.SelectedDate.Value;
            DateTime endDate = EndDatePicker.SelectedDate.Value;

            if (startDate > endDate)
            {
                MessageBox.Show("Start date cannot be later than end date.", "Invalid Date Range",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get temperature-sales correlation data
            var temperatureData = GetTemperatureSalesData(startDate, endDate);

            if (temperatureData == null || !temperatureData.Any())
            {
                MessageBox.Show("No data found for the selected date range.", "No Data",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Open temperature sales analysis window
            var tempSalesWindow = new TemperatureSalesWindow(temperatureData, startDate, endDate);
            tempSalesWindow.Show();
        }

        // Method to get temperature and sales correlation data
        private List<TemperatureSalesData> GetTemperatureSalesData(DateTime startDate, DateTime endDate)
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DbPath}"))
                {
                    connection.Open();

                    string query = @"
                 SELECT 
        s.Date,
        w.TempMax,
        w.TempMin,
        (w.TempMax + w.TempMin) / 2.0 as AvgTemperature,
        SUM(s.Total) as DailySales,
        COUNT(*) as TransactionCount,  
        AVG(s.Total) as AverageTransaction,
        w.Conditions,
        w.Humidity,
        w.Precip
    FROM Sales s
    INNER JOIN Weather w ON date(s.Date) = date(w.Date) AND s.City = w.City
    WHERE s.Date >= $StartDate AND s.Date <= $EndDate
    GROUP BY s.Date, w.TempMax, w.TempMin, w.Conditions, w.Humidity, w.Precip
    ORDER BY s.Date"; 

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        command.Parameters.AddWithValue("$StartDate", startDate.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("$EndDate", endDate.ToString("yyyy-MM-dd"));

                        var temperatureData = new List<TemperatureSalesData>();

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                temperatureData.Add(new TemperatureSalesData
                                {
                                    Date = reader["Date"].ToString(),
                                    Temperature = Convert.ToDouble(reader["AvgTemperature"]),
                                    TempMax = Convert.ToDouble(reader["TempMax"]),
                                    TempMin = Convert.ToDouble(reader["TempMin"]),
                                    Sales = Convert.ToDecimal(reader["DailySales"]),
                                    TransactionCount = Convert.ToInt32(reader["TransactionCount"]),
                                    AverageTransaction = Convert.ToDecimal(reader["AverageTransaction"]),
                                    Conditions = reader["Conditions"].ToString(),
                                    Humidity = reader["Humidity"] != DBNull.Value ? Convert.ToDouble(reader["Humidity"]) : 0,
                                    Precip = reader["Precip"] != DBNull.Value ? Convert.ToDouble(reader["Precip"]) : 0
                                });
                            }
                        }

                        return temperatureData;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving temperature-sales data: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<TemperatureSalesData>();
            }
        }
        public void SalesOverTime_Click(object sender, EventArgs e)
        {
            // Validate date selection
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Please select both start and end dates.", "Date Selection Required",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DateTime startDate = StartDatePicker.SelectedDate.Value;
            DateTime endDate = EndDatePicker.SelectedDate.Value;

            if (startDate > endDate)
            {
                MessageBox.Show("Start date cannot be later than end date.", "Invalid Date Range",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Get temperature-sales correlation data
            var temperatureData = GetTemperatureSalesData(startDate, endDate);

            if (temperatureData == null || !temperatureData.Any())
            {
                MessageBox.Show("No data found for the selected date range.", "No Data",
                               MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Open temperature sales analysis window
            var tempSalesWindow = new SalesOverTimeWindow(temperatureData);
            tempSalesWindow.Show();
        }




    }
}