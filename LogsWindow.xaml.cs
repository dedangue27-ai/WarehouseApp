using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;

namespace WarehouseApp
{
    public partial class LogsWindow : Window
    {
        public LogsWindow()
        {
            InitializeComponent();
            LoadLogs();
        }

        private void LoadLogs()
        {
            try
            {
                string query = "SELECT Id, Username, Action, LogDate FROM SystemLogs ORDER BY Id DESC";
                DataTable dt = DatabaseHelper.ExecuteQuery(query);
                LogsDataGrid.ItemsSource = dt.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء جلب السجلات: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void LogSystemAction(string username, string action)
        {
            try
            {
                string query = "INSERT INTO SystemLogs (Username, Action, LogDate) VALUES (@username, @action, @date)";
                var parameters = new Dictionary<string, object>
                {
                    { "@username", string.IsNullOrEmpty(username) ? "مستخدم النظام" : username },
                    { "@action", action },
                    { "@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                };
                DatabaseHelper.ExecuteNonQuery(query, parameters);
            }
            catch (Exception)
            {
                // تجاهل الخطأ حتى لا يتعطل النظام لو حدثت مشكلة في السجلات
            }
        }

       

        
    }
}