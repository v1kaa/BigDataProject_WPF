using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Data.Sqlite;



namespace BigDataProj
{
    public partial class MainWindow : Window
    {

        internal readonly string DbPath;
        string city = "Warsaw";

        public MainWindow()
        {
            InitializeComponent();
            string relativePath = System.IO.Path.Combine("Database", "cafee_weather_db.db");
            DbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            //UpdateAvailableDates();

        }

        //private async void ChooseDataFile_Click(object sender, RoutedEventArgs e)
        //{
        //    if (WeatherFetcher.WeatherDataAlreadyExists(start, end, DbPath))
        //    {
        //        MessageBox.Show("Dane pogodowe dla wybranego okresu są już zapisane w bazie.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
        //        return;
        //    }
        //        List<WeatherDay> weather = await WeatherFetcher.GetWeatherDataAsync(location, start, end);
        //    WeatherFetcher.SaveToCsv(weather, outputFile);
        //    WeatherFetcher.SaveWeatherToDatabase(weather, DbPath);

        //}

        private void CityTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlaceholderText.Visibility = string.IsNullOrWhiteSpace(CityTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private async void ImportCsvButton_Click(object sender, RoutedEventArgs e)
        {
            city = CityTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(city) || city.Equals("Enter city name...", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Please provide a city name before importing data.", "City Required",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                CityTextBox.Focus();
                return;
            }

            ClearDatabase();

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                await ImportSalesFromCsv(openFileDialog.FileName, DbPath);
            }
        }



        private void ClearDatabase()
        {
            try
            {
                using (var connection = new SqliteConnection($"Data Source={DbPath}"))
                {
                    connection.Open();

                    
                    var clearSalesCmd = connection.CreateCommand();
                    clearSalesCmd.CommandText = "DELETE FROM Sales";
                    clearSalesCmd.ExecuteNonQuery();

                    
                    var clearWeatherCmd = connection.CreateCommand();
                    clearWeatherCmd.CommandText = "DELETE FROM Weather";
                    clearWeatherCmd.ExecuteNonQuery();

                    // Dodaj inne tabele, które wymagają czyszczenia, w razie potrzeby
                }

                MessageBox.Show("Database has been cleared successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while clearing the database: {ex.Message}");
            }
        }



        public async Task ImportSalesFromCsv(string csvFilePath, string DbPath)
        {
            if (!File.Exists(csvFilePath))
            {
                MessageBox.Show("CSV file does not exist.", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var lines = File.ReadAllLines(csvFilePath);
            if (lines.Length < 2)
            {
                MessageBox.Show("CSV file is empty or only contains headers.", "File Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // UI: Setup progress window
            var progressWindow = CreateProgressWindow(out ProgressBar progressBar, out TextBlock statusLabel, out TextBlock percentLabel);
            progressWindow.Show();

            try
            {
                await Task.Run(async () =>
                {
                    UpdateProgress("Parsing CSV headers...", 10);

                    var header = lines[0].Split(',');
                    var columnMap = header
                        .Select((col, index) => new { Name = col.Trim().ToLower(), Index = index })
                        .ToDictionary(x => x.Name, x => x.Index);

                    string[] requiredCols = { "date", "product", "quantity", "total" };
                    if (requiredCols.Any(col => !columnMap.ContainsKey(col)))
                    {
                        CloseProgressWithMessage(progressWindow, "CSV must contain: Date, Product, Quantity, Total", "Missing Columns");
                        return;
                    }

                    UpdateProgress("Connecting to database...", 20);

                    using var connection = new SqliteConnection($"Data Source={DbPath}");
                    connection.Open();

                    UpdateProgress("Clearing existing data...", 30);

                    using (var clearCmd = connection.CreateCommand())
                    {
                        clearCmd.CommandText = "DELETE FROM Sales";
                        clearCmd.ExecuteNonQuery();
                    }

                    bool hasCityColumn = columnMap.ContainsKey("city");
                    int totalRows = lines.Length - 1;
                    int processed = 0;
                    int imported = 0;

                    UpdateProgress("Processing rows...", 35);

                    for (int i = 1; i < lines.Length; i++)
                    {
                        var columns = lines[i].Split(',');
                        if (columns.Length < header.Length) { processed++; continue; }

                        string dateStr = columns[columnMap["date"]].Trim();
                        string product = columns[columnMap["product"]].Trim();

                        if (!DateTime.TryParse(dateStr, out DateTime parsedDate) ||
                            string.IsNullOrWhiteSpace(product) ||
                            !int.TryParse(columns[columnMap["quantity"]].Trim(), out int quantity) ||
                            !double.TryParse(columns[columnMap["total"]].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double total))
                        {
                            processed++; continue;
                        }

                        string cityValue = city;

                        

                        using (var insertCmd = connection.CreateCommand())
                        {
                            insertCmd.CommandText = @"
                        INSERT INTO Sales (Date, Product, Quantity, Total,City)
                        VALUES ($date, $product, $quantity, $total,$city)";
                            insertCmd.Parameters.AddWithValue("$date", parsedDate.ToString("yyyy-MM-dd"));
                            insertCmd.Parameters.AddWithValue("$product", product);
                            insertCmd.Parameters.AddWithValue("$quantity", quantity);
                            insertCmd.Parameters.AddWithValue("$total", total);
                            insertCmd.Parameters.AddWithValue("$city", cityValue);
                            insertCmd.ExecuteNonQuery();
                        }

                        processed++; imported++;

                        if (processed % 10 == 0 || processed == totalRows)
                        {
                            int percent = 35 + (int)((processed / (double)totalRows) * 60);
                            UpdateProgress($"Imported {imported} rows...", percent);
                        }
                    }

                    UpdateProgress("Finalizing import...", 100);
                    await Task.Delay(500);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        progressWindow.Close();
                        MessageBox.Show("Import Complete", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                });
            }
            catch (Exception ex)
            {
                progressWindow.Close();
                MessageBox.Show($"Error: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // ---------------- Local Helpers ----------------

            void UpdateProgress(string message, int percent)
            {
                Dispatcher.Invoke(() =>
                {
                    statusLabel.Text = message;
                    progressBar.Value = percent;
                    percentLabel.Text = $"{percent}%";
                });
            }

            void CloseProgressWithMessage(Window window, string message, string caption)
            {
                Dispatcher.Invoke(() =>
                {
                    window.Close();
                    MessageBox.Show(message, caption, MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }

            Window CreateProgressWindow(out ProgressBar bar, out TextBlock status, out TextBlock percent)
            {
                var win = new Window
                {
                    Title = "Importing Data",
                    Width = 400,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    ResizeMode = ResizeMode.NoResize
                };

                var grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                status = new TextBlock { Text = "Starting...", FontSize = 14, Margin = new Thickness(0, 0, 0, 10) };
                bar = new ProgressBar { Height = 20, Minimum = 0, Maximum = 100, Margin = new Thickness(0, 0, 0, 10) };
                percent = new TextBlock { Text = "0%", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Center };

                Grid.SetRow(status, 0);
                Grid.SetRow(bar, 1);
                Grid.SetRow(percent, 2);

                grid.Children.Add(status);
                grid.Children.Add(bar);
                grid.Children.Add(percent);

                win.Content = grid;
                return win;
            }
        }




        private void ShowTableWithData_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of the NewWindow and show it
            DataTable newWindow = new DataTable();
            newWindow.Show();
        }
        private void ShowAnalyzedData_Click(object sender, RoutedEventArgs e)
        {
            // Create an instance of the NewWindow and show it
            Analyze newAnalyze = new Analyze();
            newAnalyze.Show();
        }


    } 
}