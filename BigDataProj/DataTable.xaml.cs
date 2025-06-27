using System;
using System.Collections.Generic;
using System.Data.SQLite;
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

namespace BigDataProj
{
    /// <summary>
    /// Interaction logic for DataTable.xaml
    /// </summary>
    public partial class DataTable : Window
    {
        public DataTable()
        {
            InitializeComponent();
            LoadSalesData();
        }

        private void LoadSalesData()
        {
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, System.IO.Path.Combine("Database", "cafee_weather_db.db"));  // Replace with your actual DB path

            // Create a list to store the sales data
            List<SalesRecord> salesData = new List<SalesRecord>();

            // Connect to the SQLite database and fetch the data
            using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
            {
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT Date, Product, Quantity, Total, City FROM Sales";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        salesData.Add(new SalesRecord
                        {
                            Date = reader.GetDateTime(0),
                            Product = reader.GetString(1),
                            Quantity = reader.GetInt32(2),
                            Total = reader.GetDouble(3),
                            City = reader.GetString(4)
                        });
                    }
                }
            }

            // Bind the data to the DataGrid
            SalesDataGrid.ItemsSource = salesData;
        }
    }

    // Class to represent a record from the Sales table
    public class SalesRecord
    {
        public DateTime Date { get; set; }
        public string Product { get; set; }
        public int Quantity { get; set; }
        public double Total { get; set; }
        public string City { get; set; }
    }
}
    

