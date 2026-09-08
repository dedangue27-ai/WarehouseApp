using System.Windows;
using Microsoft.Data.Sqlite;

namespace WarehouseApp
{
    public partial class PasswordPromptWindow : Window
    {
        public bool IsAuthenticated { get; private set; } = false;
        private string targetRole; // لتحديد ما إذا كنت تشترط دور معين مثل المشرف

        public PasswordPromptWindow(string requiredRole = "Admin")
        {
            InitializeComponent();
            targetRole = requiredRole;
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            string enteredPassword = TxtPasswordBox.Password;

            if (string.IsNullOrWhiteSpace(enteredPassword))
            {
                MessageBox.Show("الرجاء إدخال كلمة المرور.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (VerifyPasswordFromDatabase(enteredPassword))
            {
                IsAuthenticated = true;
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show("كلمة المرور غير صحيحة!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool VerifyPasswordFromDatabase(string password)
        {
            try
            {
                using (var con = new SqliteConnection(DatabaseHelper.ConnectionString))
                {
                    con.Open();
                    // البحث في جدول المستخدمين عن مستدور بالدور المطلوب أو مطابقة كلمة المرور مباشرة
                    string query = "SELECT COUNT(1) FROM Users WHERE Role = @role AND Password = @password";
                    using (var cmd = new SqliteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@role", targetRole);
                        cmd.Parameters.AddWithValue("@password", password); // ملاحظة: يفضل استخدام التشفير إذا كنت تطبقه في مشروعك

                        long count = (long)cmd.ExecuteScalar();
                        return count > 0;
                    }
                }
            }
            catch
            {
                // كحل بديل إذا أردت كلمة مرور عامة ثابتة للتأكيد مثل "1234"
                return password == "1234";
            }
        }
    }
}