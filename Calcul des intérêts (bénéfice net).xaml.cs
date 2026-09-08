using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;

namespace WarehouseApp
{
    /// <summary>
    /// Interaction logic for Calcul_des_intérêts__bénéfice_net_.xaml
    /// </summary>
    public partial class Calcul_des_intérêts__bénéfice_net_ : Window
    {
        public SeriesCollection ChartSeries { get; set; }
        public List<string> ChartLabels { get; set; }

        public Calcul_des_intérêts__bénéfice_net_()
        {
            InitializeComponent();
            DpStartDate.SelectedDate = DateTime.Now.AddDays(-30);
            DpEndDate.SelectedDate = DateTime.Now;
            LoadFinancialData();
        }

        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadFinancialData();
        }

        private void LoadFinancialData()
        {
            try
            {
                DateTime startDate = DpStartDate.SelectedDate ?? DateTime.Now.AddDays(-30);
                DateTime endDate = DpEndDate.SelectedDate ?? DateTime.Now;

                string startStr = startDate.ToString("yyyy-MM-dd");
                string endStr = endDate.ToString("yyyy-MM-dd");

                string todayStr = DateTime.Now.ToString("yyyy-MM-dd");
                string monthStr = DateTime.Now.ToString("yyyy-MM");
                string yearStr = DateTime.Now.ToString("yyyy");

                TxtDailyInterest.Text = CalculateNetProfitForPeriod($"{todayStr} 00:00:00", $"{todayStr} 23:59:59").ToString("N2") + " DA";
                TxtMonthlyInterest.Text = CalculateNetProfitForPeriod($"{monthStr}-01 00:00:00", $"{monthStr}-31 23:59:59").ToString("N2") + " DA";
                TxtYearlyInterest.Text = CalculateNetProfitForPeriod($"{yearStr}-01-01 00:00:00", $"{yearStr}-12-31 23:59:59").ToString("N2") + " DA";

                string query = @"
                    SELECT SUBSTR(sl.SaleDate, 1, 10) as SaleDay,
                    (SUM(sl.FinalAmount) - SUM(p.PurchasePrice * s.Quantity)) as NetProfit
                    FROM Sales sl
                    JOIN SaleItems s ON sl.Id = s.SaleId
                    JOIN Products p ON s.ProductId = p.Id
                    WHERE sl.Status = 'Active' AND SUBSTR(sl.SaleDate, 1, 10) BETWEEN @Start AND @End
                    GROUP BY SUBSTR(sl.SaleDate, 1, 10)
                    ORDER BY SaleDay ASC;";

                var parameters = new Dictionary<string, object>
                {
                    { "@Start", startStr },
                    { "@End", endStr }
                };

                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);

                ChartValues<decimal> profitValues = new ChartValues<decimal>();
                List<string> labels = new List<string>();

                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        labels.Add(row["SaleDay"].ToString());
                        profitValues.Add(Convert.ToDecimal(row["NetProfit"]));
                    }
                }

                ProfitChart.Series = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Bénéfice Net (Intérêt Net)",
                        Values = profitValues,
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 8
                    }
                };

                ChartLabels = labels;
                ProfitChart.DataContext = this;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la récupération des données du graphique : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private decimal CalculateNetProfitForPeriod(string start, string end)
        {
            string query = @"
                SELECT (
                    SELECT COALESCE(SUM(sl.FinalAmount), 0) FROM Sales sl WHERE sl.SaleDate BETWEEN @Start AND @End AND sl.Status = 'Active'
                ) - (
                    SELECT COALESCE(SUM(p.PurchasePrice * s.Quantity), 0) 
                    FROM SaleItems s 
                    JOIN Products p ON s.ProductId = p.Id 
                    JOIN Sales sl ON s.SaleId = sl.Id 
                    WHERE sl.SaleDate BETWEEN @Start AND @End AND sl.Status = 'Active'
                )";

            var parameters = new Dictionary<string, object>
            {
                { "@Start", start },
                { "@End", end }
            };

            var result = DatabaseHelper.ExecuteScalar(query, parameters);
            return (result != null && result != DBNull.Value) ? Convert.ToDecimal(result) : 0;
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime startDate = DpStartDate.SelectedDate ?? DateTime.Now.AddDays(-30);
                DateTime endDate = DpEndDate.SelectedDate ?? DateTime.Now;

                string query = @"
            SELECT SUBSTR(sl.SaleDate, 1, 10) AS [Date],
                   SUM(sl.FinalAmount) AS [Total Ventes],
                   SUM(p.PurchasePrice * s.Quantity) AS [Total Coût],
                   (SUM(sl.FinalAmount) - SUM(p.PurchasePrice * s.Quantity)) AS [Bénéfice Net]
            FROM Sales sl
            JOIN SaleItems s ON sl.Id = s.SaleId
            JOIN Products p ON s.ProductId = p.Id
            WHERE sl.Status = 'Active' AND SUBSTR(sl.SaleDate, 1, 10) BETWEEN @Start AND @End
            GROUP BY SUBSTR(sl.SaleDate, 1, 10)";

                var parameters = new Dictionary<string, object>
                {
                    { "@Start", startDate.ToString("yyyy-MM-dd") },
                    { "@End", endDate.ToString("yyyy-MM-dd") }
                };

                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);

                Microsoft.Win32.SaveFileDialog saveDlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    FileName = $"Net_Profit_{startDate:yyyyMMdd}_to_{endDate:yyyyMMdd}.csv"
                };

                if (saveDlg.ShowDialog() == true)
                {
                    using (FileStream fs = new FileStream(saveDlg.FileName, FileMode.Create, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fs, new UTF8Encoding(true)))
                        {
                            List<string> columns = new List<string>();
                            foreach (DataColumn col in dt.Columns)
                            {
                                columns.Add($"\"{col.ColumnName}\"");
                            }
                            sw.WriteLine(string.Join(";", columns));

                            foreach (DataRow row in dt.Rows)
                            {
                                List<string> fields = new List<string>();
                                foreach (var item in row.ItemArray)
                                {
                                    string val = "";
                                    if (item != null && item != DBNull.Value)
                                    {
                                        if (item is decimal dec)
                                            val = dec.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                                        else if (item is double dbl)
                                            val = dbl.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                                        else
                                            val = item.ToString();
                                    }
                                    fields.Add($"\"{val}\"");
                                }
                                sw.WriteLine(string.Join(";", fields));
                            }
                        }
                    }

                    MessageBox.Show("Données exportées avec succès vers le fichier CSV !", "Exportation réussie", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Une erreur est survenue lors de l'exportation du fichier : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}