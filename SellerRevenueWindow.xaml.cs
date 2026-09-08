using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;

namespace WarehouseApp
{
    public partial class SellersRevenueWindow : Window
    {
        public SellersRevenueWindow()
        {
            InitializeComponent();
            LoadSellersRevenue();
        }

        private void LoadSellersRevenue()
        {
            try
            {
                string todayPrefix = DateTime.Now.ToString("yyyy-MM-dd");
                string monthPrefix = DateTime.Now.ToString("yyyy-MM");

                // استعلام لجلب المستخدمين مع حساب مجموع مبيعاتهم اليومية والشهرية بعد الخصم
                string query = @"
    SELECT 
    u.Username, 
    u.Role,
    (SELECT COALESCE(SUM(FinalAmount), 0) FROM Sales s WHERE s.SoldBy = u.Username AND s.SaleDate LIKE @Today) AS DailyRevenue,
    (SELECT COALESCE(SUM(FinalAmount), 0) FROM Sales s WHERE s.SoldBy = u.Username AND s.SaleDate LIKE @Month) AS MonthlyRevenue
    FROM Users u 
    WHERE u.Username != 'DEV'";

                var parameters = new Dictionary<string, object>
                {
                    { "@Today", $"{todayPrefix}%" },
                    { "@Month", $"{monthPrefix}%" }
                };
                DataTable dt = DatabaseHelper.ExecuteQuery("SELECT Username, Role FROM Users WHERE Username != 'DEV'", null);

                DataTable resultTable = new DataTable();
                resultTable.Columns.Add("Username", typeof(string));
                resultTable.Columns.Add("Role", typeof(string));
                resultTable.Columns.Add("DailyRevenue", typeof(decimal));
                resultTable.Columns.Add("MonthlyRevenue", typeof(decimal));

                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string username = row["Username"].ToString();
                        string role = row["Role"].ToString();

                        // استخدام FinalAmount بدلاً من TotalAmount لضمان خصم قيمة التخفيض وتطابق الصندوق
                        string dailyQuery = "SELECT SUM(FinalAmount) FROM Sales WHERE SoldBy = @User AND SaleDate LIKE @Today";
                        var dailyParams = new Dictionary<string, object> { { "@User", username }, { "@Today", $"{todayPrefix}%" } };
                        var dailyRes = DatabaseHelper.ExecuteScalar(dailyQuery, dailyParams);
                        decimal dailyRev = (dailyRes != null && dailyRes != DBNull.Value) ? Convert.ToDecimal(dailyRes) : 0;

                        string monthlyQuery = "SELECT SUM(FinalAmount) FROM Sales WHERE SoldBy = @User AND SaleDate LIKE @Month";
                        var monthlyParams = new Dictionary<string, object> { { "@User", username }, { "@Month", $"{monthPrefix}%" } };
                        var monthlyRes = DatabaseHelper.ExecuteScalar(monthlyQuery, monthlyParams);
                        decimal monthlyRev = (monthlyRes != null && monthlyRes != DBNull.Value) ? Convert.ToDecimal(monthlyRes) : 0;

                        resultTable.Rows.Add(username, role, dailyRev, monthlyRev);
                    }
                }

                SellersRevenueGrid.ItemsSource = resultTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des revenus des vendeurs : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBackToMain_Click(object sender, RoutedEventArgs e)
        {

            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
            else
            {
                this.Close();
            }
        }
    }
    
}