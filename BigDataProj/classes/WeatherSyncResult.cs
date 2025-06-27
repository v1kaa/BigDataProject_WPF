using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BigDataProj.classes
{
    public class WeatherSyncResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> Cities { get; set; }
        public int ProcessedCities { get; set; }
        public int NewRecords { get; set; }
    }
}
