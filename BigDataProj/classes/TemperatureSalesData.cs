using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigDataProj.classes
{
    public class TemperatureSalesData
    {
        public string Date { get; set; }
        public double Temperature { get; set; }  // This will be the average of TempMax and TempMin
        public double TempMax { get; set; }
        public double TempMin { get; set; }
        public decimal Sales { get; set; }
        public int TransactionCount { get; set; }
        public decimal AverageTransaction { get; set; }
        public string Conditions { get; set; }
        public double Humidity { get; set; }
        public double Precip { get; set; }
        public DateTime DateParsed => DateTime.Parse(Date);
        public string WeatherCondition => Conditions;
        public string FormattedSales => $"${Sales:N2}";
        public string FormattedAverage => $"${AverageTransaction:N2}";
        public string TemperatureDisplay => $"{Temperature:F1}°C";
        public string TemperatureRangeDisplay => $"{TempMin:F1}°C - {TempMax:F1}°C";
        public string HumidityDisplay => $"{Humidity:F1}%";
        public string PrecipDisplay => $"{Precip:F1}mm";
    }
}
