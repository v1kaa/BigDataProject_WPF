using System;
using System.Collections.Generic;
using System.Linq;
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
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
using OxyPlot.Annotations;


namespace BigDataProj
{
    /// <summary>
    /// Interaction logic for TemperatureSalesWindow.xaml
    /// </summary>
    public partial class TemperatureSalesWindow : Window
    {
        private List<classes.TemperatureSalesData> _data;
        private PlotModel _plotModel;
        private ScatterSeries _scatterSeries;
        private LineSeries _trendSeries;

        public TemperatureSalesWindow(List<classes.TemperatureSalesData> data, DateTime startDate, DateTime endDate)
        {
            InitializeComponent();
            _data = data?.OrderBy(d => d.Temperature).ToList() ?? new List<classes.TemperatureSalesData>();

            // Set date range in header
            DateRangeText.Text = $"{startDate:MMM dd, yyyy} - {endDate:MMM dd, yyyy}";

            InitializeChart();
            LoadStatistics();
        }

        private void InitializeChart()
        {
            _plotModel = new PlotModel
            {
                Title = "",
                Background = OxyColors.White,
                PlotAreaBorderColor = OxyColors.LightGray,
                PlotAreaBorderThickness = new OxyThickness(1),
                Padding = new OxyThickness(10)
            };

            // X-axis (Temperature)
            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Temperature (°C)",
                TitleFontSize = 14,
                FontSize = 12,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.FromArgb(50, 0, 0, 0),
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColor.FromArgb(30, 0, 0, 0)
            };

            // Y-axis (Sales)
            var yAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Sales ($)",
                TitleFontSize = 14,
                FontSize = 12,
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = OxyColor.FromArgb(50, 0, 0, 0),
                MinorGridlineStyle = LineStyle.Dot,
                MinorGridlineColor = OxyColor.FromArgb(30, 0, 0, 0),
                StringFormat = "C0"
            };

            _plotModel.Axes.Add(xAxis);
            _plotModel.Axes.Add(yAxis);

            // Scatter series for data points
            _scatterSeries = new ScatterSeries
            {
                Title = "Sales Data",
                MarkerType = MarkerType.Circle,
                MarkerSize = 6,
                MarkerFill = OxyColor.FromRgb(52, 152, 219),
                MarkerStroke = OxyColor.FromRgb(41, 128, 185),
                MarkerStrokeThickness = 1
            };

            // Add data points
            foreach (var item in _data)
            {
                _scatterSeries.Points.Add(new ScatterPoint(item.Temperature, (double)item.Sales));
            }

            _plotModel.Series.Add(_scatterSeries);

            // Create trend line
            CreateTrendLine();

            TemperatureChart.Model = _plotModel;
        }

        private void CreateTrendLine()
        {
            if (_data.Count < 2) return;

            // Calculate linear regression
            var n = _data.Count;
            var sumX = _data.Sum(d => d.Temperature);
            var sumY = _data.Sum(d => (double)d.Sales);
            var sumXY = _data.Sum(d => d.Temperature * (double)d.Sales);
            var sumX2 = _data.Sum(d => d.Temperature * d.Temperature);

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var intercept = (sumY - slope * sumX) / n;

            // Calculate R² (coefficient of determination)
            var yMean = sumY / n;
            var ssTot = _data.Sum(d => Math.Pow((double)d.Sales - yMean, 2));
            var ssRes = _data.Sum(d => Math.Pow((double)d.Sales - (slope * d.Temperature + intercept), 2));
            var rSquared = ssTot == 0 ? 0 : 1 - (ssRes / ssTot);


            // Create trend line series
            _trendSeries = new LineSeries
            {
                Title = "Trend Line",
                Color = OxyColor.FromRgb(231, 76, 60),
                StrokeThickness = 2,
                LineStyle = LineStyle.Dash,
                IsVisible = false
            };

            var minTemp = _data.Min(d => d.Temperature);
            var maxTemp = _data.Max(d => d.Temperature);

            _trendSeries.Points.Add(new DataPoint(minTemp, slope * minTemp + intercept));
            _trendSeries.Points.Add(new DataPoint(maxTemp, slope * maxTemp + intercept));

            _plotModel.Series.Add(_trendSeries);
            var annotationText = $"y = {slope:F2}x + {intercept:F2}\nR² = {rSquared:F3}";
            var annotation = new TextAnnotation
            {
                Text = annotationText,
                TextPosition = new DataPoint(minTemp, slope * minTemp + intercept),
                Stroke = OxyColors.Transparent,
                TextColor = OxyColors.Black,
                FontSize = 12,
                Background = OxyColor.FromAColor(200, OxyColors.White),
                Padding = new OxyThickness(4),
                Layer = AnnotationLayer.AboveSeries
            };

            _plotModel.Annotations.Add(annotation);

        }

        private void LoadStatistics()
        {
            if (!_data.Any()) return;

            // Calculate statistics
            var totalSales = _data.Sum(d => d.Sales);
            var totalTransactions = _data.Sum(d => d.TransactionCount);
            var averageSale = totalSales / totalTransactions;

            // Find optimal temperature (highest sales)
            var optimalData = _data.OrderByDescending(d => d.Sales).First();
            OptimalTempText.Text = $"{optimalData.Temperature:F1}°C";
            PeakSalesText.Text = $"${optimalData.Sales:N2}";

            // Calculate correlation coefficient
            var correlation = CalculateCorrelation();
            CorrelationText.Text = $"{correlation:F3}";

            var rSquared = correlation * correlation;
            if (rSquared < 0.001)
            {
                R2Text.Text = $"R² < 0.001 (słabe dopasowanie)";
                R2Text.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
            else
            {
                R2Text.Text = $"R² = {rSquared:F3}";
                R2Text.Foreground = new SolidColorBrush(Colors.Black);
            }


            // Update footer
            TotalSalesText.Text = $"Total Sales: ${totalSales:N2}";
            TotalTransactionsText.Text = $"Total Transactions: {totalTransactions:N0}";
            AverageSaleText.Text = $"${averageSale:N2}";

            var minTemp = _data.Min(d => d.Temperature);
            var maxTemp = _data.Max(d => d.Temperature);
            TemperatureRangeText.Text = $"{minTemp:F1}°C - {maxTemp:F1}°C";

            // Create temperature ranges
            CreateTemperatureRanges();
        }

        private double CalculateCorrelation()
        {
            if (_data.Count < 2) return 0;

            var n = _data.Count;
            var sumX = _data.Sum(d => d.Temperature);
            var sumY = _data.Sum(d => (double)d.Sales);
            var sumXY = _data.Sum(d => d.Temperature * (double)d.Sales);
            var sumX2 = _data.Sum(d => d.Temperature * d.Temperature);
            var sumY2 = _data.Sum(d => (double)d.Sales * (double)d.Sales);

            var numerator = n * sumXY - sumX * sumY;
            var denominator = Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));

            return denominator == 0 ? 0 : numerator / denominator;
        }

        private void CreateTemperatureRanges()
        {
            var ranges = new List<TemperatureRangeData>();

            // Define temperature ranges
            var rangeDefinitions = new List<TemperatureRangeDefinition>
            {
                new TemperatureRangeDefinition { Label = "Freezing", Min = double.MinValue, Max = 0 },
                new TemperatureRangeDefinition { Label = "Cold", Min = 0, Max = 10 },
                new TemperatureRangeDefinition { Label = "Cool", Min = 10, Max = 20 },
                new TemperatureRangeDefinition { Label = "Warm", Min = 20, Max = 30 },
                new TemperatureRangeDefinition { Label = "Hot", Min = 30, Max = double.MaxValue }
            };

            foreach (var rangeDef in rangeDefinitions)
            {
                var rangeData = _data.Where(d => d.Temperature > rangeDef.Min && d.Temperature <= rangeDef.Max).ToList();

                if (rangeData.Any())
                {
                    var totalSales = rangeData.Sum(d => d.Sales);
                    var transactionCount = rangeData.Sum(d => d.TransactionCount);
                    var count = rangeData.Count;
                    var minTemp = rangeData.Min(d => d.Temperature);
                    var maxTemp = rangeData.Max(d => d.Temperature);

                    ranges.Add(new TemperatureRangeData
                    {
                        TransactionCount = $"{transactionCount} transactions",
                        RangeLabel = rangeDef.Label,
                        TemperatureRange = $"{minTemp:F1}°C - {maxTemp:F1}°C",
                        FormattedSales = $"${totalSales:N2}"
                        
                    });
                }
            }

            TemperatureRangesControl.ItemsSource = ranges;
        }

        private void ShowTrendLineButton_Click(object sender, RoutedEventArgs e)
        {
            if (_trendSeries != null)
            {
                _trendSeries.IsVisible = !_trendSeries.IsVisible;
                ShowTrendLineButton.Content = _trendSeries.IsVisible ? "Hide Trend Line" : "Show Trend Line";
                _plotModel.InvalidatePlot(true);
            }
        }

        private void ShowDataPointsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_scatterSeries != null)
            {
                _scatterSeries.IsVisible = !_scatterSeries.IsVisible;
                ShowDataPointsButton.Content = _scatterSeries.IsVisible ? "Hide Data Points" : "Show Data Points";
                _plotModel.InvalidatePlot(true);
            }
        }
        private void ShowAnalysisButton_Click(object sender, RoutedEventArgs e)
        {
            if (_data.Count < 2)
            {
                MessageBox.Show("Too little data for regression analysis.");
                return;
            }

            // Regression calculations
            var n = _data.Count;
            var sumX = _data.Sum(d => d.Temperature);
            var sumY = _data.Sum(d => (double)d.Sales);
            var sumXY = _data.Sum(d => d.Temperature * (double)d.Sales);
            var sumX2 = _data.Sum(d => d.Temperature * d.Temperature);
            var sumY2 = _data.Sum(d => (double)d.Sales * (double)d.Sales);

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var intercept = (sumY - slope * sumX) / n;

            var numerator = n * sumXY - sumX * sumY;
            var denominator = Math.Sqrt((n * sumX2 - sumX * sumX) * (n * sumY2 - sumY * sumY));
            var r = (denominator == 0) ? 0 : numerator / denominator;
            var rSquared = r * r;

            // Predictions
            var temperatures = new List<double> { -10, -5, 0, 5, 10, 15, 20, 25 };
            var predictions = temperatures.Select(t =>
                $"For {t}°C: predicted sales: {(slope * t + intercept):C2}"
            );

            // Insights
            var insights = "- Sales increase with temperature (if slope coefficient is positive).\n" +
                           "- Highest sales occurred at " + _data.OrderByDescending(d => d.Sales).First().Temperature.ToString("F1") + "°C.\n" +
                           "- R² = " + rSquared.ToString("F3") + " – indicates " +
                           (rSquared < 0.1 ? "very weak" :
                            rSquared < 0.4 ? "weak" :
                            rSquared < 0.7 ? "moderate" :
                            rSquared < 0.9 ? "strong" : "very strong") +
                           " relationship between temperature and sales.";

            // Message box
            var message = $"Regression equation:\n  y = {slope:F2} * x + {intercept:F2}\n" +
                          $"R² = {rSquared:F3}\n\n" +
                          "Predictions:\n" +
                          string.Join("\n", predictions) + "\n\n" +
                          "Insights:\n" + insights;

            MessageBox.Show(message, "Regression Analysis", MessageBoxButton.OK, MessageBoxImage.Information);
        }

    }

    // Data classes

    public class TemperatureRangeData
    {
        public string RangeLabel { get; set; }
        public string TemperatureRange { get; set; }
        public string FormattedSales { get; set; }
        public string TransactionCount { get; set; }
    }

    public class TemperatureRangeDefinition
    {
        public string Label { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
    }
}