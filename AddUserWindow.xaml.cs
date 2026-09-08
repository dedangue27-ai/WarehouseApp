using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.Sqlite;

namespace WarehouseApp
{
    public partial class AddUserWindow : Window
    {
        public AddUserWindow()
        {
            InitializeComponent();
        }

        // دالة الفحص عبر F12 (موجودة داخل الكلاس حصرياً)
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == Key.F12)
            {
                // التحقق الصارم من أن الحساب الحالي هو DEV فقط
                if (App.LoggedInUsername == "DEV" || App.LoggedInRole == "DEV")
                {
                    DevAuditWindow auditWindow = new DevAuditWindow();
                    auditWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Commande inconnue.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                e.Handled = true;
            }
        }

        private void BtnSaveUser_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtNewUser.Text.Trim();
            string password = TxtNewPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("يرجى ملء جميع الحقول المطلوبة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string selectedRole = (RoleComboBox.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "Seller";

            try
            {
                using (var con = new SqliteConnection(DatabaseHelper.ConnectionString))
                {
                    con.Open();
                    string query = "INSERT INTO Users (Username, Password, Role) VALUES (@Username, @Password, @Role)";

                    using (var cmd = new SqliteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Password", password);
                        cmd.Parameters.AddWithValue("@Role", selectedRole);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("تم حفظ المستخدم بنجاح!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ: اسم المستخدم موجود مسبقاً أو حدثت مشكلة. " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}