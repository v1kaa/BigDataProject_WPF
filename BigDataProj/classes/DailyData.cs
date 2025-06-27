using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigDataProj.classes
{
    public class DailyData
    {
        public List<string> time { get; set; }
        public List<double> temperature_2m_max { get; set; }
        public List<double> temperature_2m_min { get; set; }
        public List<double> precipitation_sum { get; set; }
        public List<double> relative_humidity_2m_mean { get; set; }
        public List<int> weather_code { get; set; }
    }
}
