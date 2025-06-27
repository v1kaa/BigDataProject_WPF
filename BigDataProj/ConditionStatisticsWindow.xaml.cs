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
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;


namespace BigDataProj
{
    

    /// <summary>
    /// Interaction logic for ConditionStatisticsWindow.xaml
    /// </summary>
    public partial class ConditionStatisticsWindow : Window
    {
        
        private List<ConditionStatistic> _statistics;

        public ConditionStatisticsWindow(List<ConditionStatistic> statistics, DateTime startDate, DateTime endDate)
        {
            InitializeComponent();
            _statistics = statistics;

            // Set date range in header
            DateRangeText.Text = $"{startDate:MMM dd, yyyy} - {endDate:MMM dd, yyyy}";

            LoadData();
            CreateVisualChart();
        }

        private void LoadData()
        {
            // Bind data to DataGrid
            StatsDataGrid.ItemsSource = _statistics;

            // Update summary footer
            var totalSales = _statistics.Sum(s => s.TotalSales);
            var totalTransactions = _statistics.Sum(s => s.SalesCount);

            TotalSalesText.Text = $"Total Sales: ${totalSales:N2}";
            TotalTransactionsText.Text = $"Total Transactions: {totalTransactions:N0}";
        }

        private void CreateVisualChart()
        {
            if (_statistics == null || !_statistics.Any()) return;

            var maxSales = _statistics.Max(s => s.TotalSales);
            var totalSales = _statistics.Sum(s => s.TotalSales);

            // Define colors for different conditions
            var colors = new Brush[]
            {
                new SolidColorBrush(Color.FromRgb(52, 152, 219)),   // Blue
                new SolidColorBrush(Color.FromRgb(46, 204, 113)),   // Green
                new SolidColorBrush(Color.FromRgb(241, 196, 15)),   // Yellow
                new SolidColorBrush(Color.FromRgb(231, 76, 60)),    // Red
                new SolidColorBrush(Color.FromRgb(155, 89, 182)),   // Purple
                new SolidColorBrush(Color.FromRgb(52, 73, 94)),     // Dark Blue
                new SolidColorBrush(Color.FromRgb(230, 126, 34)),   // Orange
                new SolidColorBrush(Color.FromRgb(149, 165, 166))   // Gray
            };

            // Calculate bar widths and assign colors
            for (int i = 0; i < _statistics.Count; i++)
            {
                var stat = _statistics[i];

                // Calculate bar width (max width 300px)
                stat.BarWidth = maxSales > 0 ? (double)(stat.TotalSales / maxSales) * 300 : 0;

                // Assign color
                stat.BarColor = colors[i % colors.Length];

                // Calculate percentage
                var percentage = totalSales > 0 ? (double)(stat.TotalSales / totalSales) * 100 : 0;
                stat.PercentageText = $"{percentage:F1}%";
            }

            // Bind to ItemsControl
            ChartItemsControl.ItemsSource = _statistics;
        }
    }
}
