using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigDataProj
{
    public class WeatherDay
    {

        public string datetime { get; set; }
        public double tempmax { get; set; }
        public double tempmin { get; set; }
        public double humidity { get; set; }
        public double precip { get; set; }
        public string conditions { get; set; }
        public string city { get; set; }
        public double temp => (tempmax + tempmin) / 2.0;
    }
    public class WeatherResponse
    {
        public string address { get; set; }
        public List<WeatherDay> days { get; set; }
    }
}
