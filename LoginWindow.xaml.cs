using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.Sqlite;

namespace WarehouseApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            DatabaseHelper.InitializeDatabase();
        }

        private void TxtPassword_Loaded(object sender, RoutedEventArgs e)
        {
            TxtPassword.Focus();
        }

        private void TxtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BtnLogin_Click(sender, e);
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string password = TxtPassword.Password.Trim();

            if (string.IsNullOrEmpty(password))
            {
                MessageBox.Show("يرجى إدخال كلمة المرور!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var con = new SqliteConnection(DatabaseHelper.ConnectionString))
                {
                    con.Open();
                    string query = "SELECT Username, Role FROM Users WHERE Password = @Password LIMIT 1";
                    using (var cmd = new SqliteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@Password", password);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string username = reader.GetString(0);
                                string role = reader.GetString(1);

                                // حفظ المستخدم والدور الحالي في الجلسة العامة هنا بالداخل
                                App.LoggedInUsername = username;
                                App.LoggedInRole = role;

                                // التحقق من الصلاحية وفتح النافذة المناسبة بناءً على الدور
                                if (role == "Admin" || role == "مدير")
                                {
                                    MainWindow mainWindow = new MainWindow(username, role);
                                    Application.Current.MainWindow = mainWindow;
                                    mainWindow.Show();
                                    this.Close();
                                }
                                else if (role == "Seller" || role == "موظف" || role == "بائع")
                                {
                                    SalesWindow salesWin = new SalesWindow();
                                    Application.Current.MainWindow = salesWin;
                                    salesWin.Show();
                                    this.Close();
                                }
                                else
                                {
                                    // دور افتراضي في حال لم يتطابق مع ما سبق
                                    MainWindow mainWindow = new MainWindow(username, role);
                                    Application.Current.MainWindow = mainWindow;
                                    mainWindow.Show();
                                    this.Close();
                                }
                            }
                            else
                            {
                                MessageBox.Show("كلمة المرور غير صحيحة!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                                TxtPassword.Clear();
                                TxtPassword.Focus();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ في الاتصال بقاعدة البيانات: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}