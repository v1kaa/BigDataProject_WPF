using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BigDataProj
{
    public class GroupedSalesData
    {
        public DateTime? Date { get; set; }
        public decimal Sales { get; set; }
        public int? DayOfWeek { get; set; }
        public string DayName { get; set; }
    }

    public partial class SalesOverTimeWindow : Window
    {
        private List<classes.TemperatureSalesData> _originalData;

        public SalesOverTimeWindow(List<classes.TemperatureSalesData> data)
        {
            InitializeComponent();
            _originalData = data ?? new List<classes.TemperatureSalesData>();

            // Opóźnienie aktualizacji wykresu do momentu pełnego załadowania okna
            this.Loaded += (s, e) => UpdateChart();
        }

        private void GroupingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SalesPlot != null) // Sprawdzenie czy kontrolka została zainicjalizowana
                UpdateChart();
        }

        private void AggregationComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SalesPlot != null) // Sprawdzenie czy kontrolka została zainicjalizowana
                UpdateChart();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateChart();
        }

        private void UpdateChart()
        {
            if (SalesPlot == null || GroupingComboBox?.SelectedItem == null || AggregationComboBox?.SelectedItem == null)
                return;

            var groupingType = ((ComboBoxItem)GroupingComboBox.SelectedItem).Tag.ToString();
            var aggregationType = ((ComboBoxItem)AggregationComboBox.SelectedItem).Tag.ToString();

            var plotModel = new PlotModel { Title = GetChartTitle(groupingType, aggregationType) };

            // Przygotowanie danych
            var validData = _originalData
                ?.Where(d => d != null && DateTime.TryParse(d.Date, out _))
                .Select(d => new
                {
                    Date = DateTime.Parse(d.Date),
                    Sales = d.Sales
                })
                .OrderBy(d => d.Date)
                .Cast<dynamic>()
                .ToList();

            if (validData == null || !validData.Any())
            {
                SalesPlot.Model = plotModel;
                return;
            }

            // Grupowanie danych
            var groupedData = GroupData(validData, groupingType, aggregationType);

            // Konfiguracja osi
            ConfigureAxes(plotModel, groupingType);

            // Tworzenie serii
            var series = new LineSeries
            {
                Title = $"Sales ({GetAggregationName(aggregationType)})",
                MarkerType = MarkerType.Circle,
                MarkerSize = 4,
                MarkerStroke = OxyColors.DarkBlue,
                Color = OxyColors.Blue,
                StrokeThickness = 2
            };

            // Dodawanie punktów
            if (groupingType == "DaysOfWeek")
            {
                AddDaysOfWeekPoints(series, groupedData);
            }
            else
            {
                AddDatePoints(series, groupedData);
            }

            plotModel.Series.Add(series);
            SalesPlot.Model = plotModel;
        }

        private List<GroupedSalesData> GroupData(List<dynamic> data, string groupingType, string aggregationType)
        {
            switch (groupingType)
            {
                case "Daily":
                    return data.GroupBy(d => ((DateTime)d.Date).Date)
                        .Select(g => new GroupedSalesData
                        {
                            Date = g.Key,
                            Sales = ApplyAggregation(g.Select(x => (decimal)x.Sales), aggregationType),
                            DayOfWeek = (int)g.Key.DayOfWeek
                        })
                        .ToList();

                case "Weekly":
                    return data.GroupBy(d => GetWeekStart((DateTime)d.Date))
                        .Select(g => new GroupedSalesData
                        {
                            Date = g.Key,
                            Sales = ApplyAggregation(g.Select(x => (decimal)x.Sales), aggregationType)
                        })
                        .ToList();

                case "Monthly":
                    return data.GroupBy(d => new DateTime(((DateTime)d.Date).Year, ((DateTime)d.Date).Month, 1))
                        .Select(g => new GroupedSalesData
                        {
                            Date = g.Key,
                            Sales = ApplyAggregation(g.Select(x => (decimal)x.Sales), aggregationType)
                        })
                        .ToList();

                case "DaysOfWeek":
                    return data.GroupBy(d => ((DateTime)d.Date).DayOfWeek)
                        .Select(g => new GroupedSalesData
                        {
                            DayOfWeek = (int)g.Key,
                            DayName = GetPolishDayName(g.Key),
                            Sales = ApplyAggregation(g.Select(x => (decimal)x.Sales), aggregationType)
                        })
                        .OrderBy(x => x.DayOfWeek == 0 ? 7 : x.DayOfWeek) // Niedziela na końcu
                        .ToList();

                default:
                    return data.Select(d => new GroupedSalesData
                    {
                        Date = (DateTime)d.Date,
                        Sales = (decimal)d.Sales
                    }).ToList();
            }
        }

        private decimal ApplyAggregation(IEnumerable<decimal> values, string aggregationType)
        {
            switch (aggregationType)
            {
                case "Sum":
                    return values.Sum();
                case "Average":
                    return values.Average();
                case "Max":
                    return values.Max();
                case "Min":
                    return values.Min();
                default:
                    return values.Sum();
            }
        }

        private DateTime GetWeekStart(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        private string GetPolishDayName(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "Monday",
                DayOfWeek.Tuesday => "Tuesday",
                DayOfWeek.Wednesday => "Wednesday",
                DayOfWeek.Thursday => "Thursday",
                DayOfWeek.Friday => "Friday",
                DayOfWeek.Saturday => "Saturday",
                DayOfWeek.Sunday => "Sunday",
                _ => dayOfWeek.ToString()
            };
        }

        private void ConfigureAxes(PlotModel plotModel, string groupingType)
        {
            if (groupingType == "DaysOfWeek")
            {
                // Oś X - dni tygodnia
                var categoryAxis = new CategoryAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "Day of Week",
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                };
                plotModel.Axes.Add(categoryAxis);
            }
            else
            {
                // Oś X - czas (Data)
                var dateAxis = new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "Data",
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot,
                    IntervalType = GetDateIntervalType(groupingType),
                    StringFormat = GetDateFormat(groupingType)
                };
                plotModel.Axes.Add(dateAxis);
            }

            // Oś Y - sprzedaż
            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Sales",
                MajorGridlineStyle = LineStyle.Solid,
                MinorGridlineStyle = LineStyle.Dot
            };
            plotModel.Axes.Add(valueAxis);
        }

        private DateTimeIntervalType GetDateIntervalType(string groupingType)
        {
            return groupingType switch
            {
                "Daily" => DateTimeIntervalType.Days,
                "Weekly" => DateTimeIntervalType.Weeks,
                "Monthly" => DateTimeIntervalType.Months,
                _ => DateTimeIntervalType.Days
            };
        }

        private string GetDateFormat(string groupingType)
        {
            return groupingType switch
            {
                "Daily" => "dd MMM",
                "Weekly" => "dd MMM",
                "Monthly" => "MMM yyyy",
                _ => "dd MMM"
            };
        }

        private void AddDaysOfWeekPoints(LineSeries series, List<GroupedSalesData> groupedData)
        {
            if (SalesPlot?.Model == null) return;

            var categoryAxis = SalesPlot.Model.Axes.OfType<CategoryAxis>().FirstOrDefault();
            if (categoryAxis != null)
            {
                categoryAxis.Labels.Clear();
                for (int i = 0; i < groupedData.Count; i++)
                {
                    var item = groupedData[i];
                    categoryAxis.Labels.Add(item.DayName);
                    series.Points.Add(new DataPoint(i, (double)item.Sales));
                }
            }
        }

        private void AddDatePoints(LineSeries series, List<GroupedSalesData> groupedData)
        {
            foreach (var item in groupedData)
            {
                if (item.Date.HasValue)
                {
                    series.Points.Add(DateTimeAxis.CreateDataPoint(item.Date.Value, (double)item.Sales));
                }
            }
        }

        private string GetChartTitle(string groupingType, string aggregationType)
        {
            var groupingName = groupingType switch
            {
                "Daily" => "daily",
                "Weekly" => "weekly",
                "Monthly" => "monthly",
                "DaysOfWeek" => "by day of week",
                _ => "unknown"
            };


            var aggregationName = GetAggregationName(aggregationType);

            return $"Sprzedaż {groupingName} ({aggregationName})";
        }

        private string GetAggregationName(string aggregationType)
        {
            return aggregationType switch
            {
                "Sum" => "sum",
                "Average" => "average",
                "Max" => "maximum",
                "Min" => "minimum",
                _ => "sum"
            };

        }
    }
}