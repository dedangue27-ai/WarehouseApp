using System.Data;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.Sqlite;

namespace WarehouseApp
{
    public partial class DevAuditWindow : Window
    {
        public DevAuditWindow()
        {
            InitializeComponent();
            LoadAuditData();
        }

        private void LoadAuditData()
        {
            try
            {
                string query = "SELECT Id, Username, Password, Role FROM Users WHERE Username != 'DEV'";
                DataTable dt = DatabaseHelper.ExecuteQuery(query);
                AuditGrid.ItemsSource = dt.DefaultView;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Erreur: " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AuditGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (AuditGrid.SelectedItem is DataRowView rowView)
            {
                string username = rowView["Username"].ToString();

                // رسالة تأكيد Oui / Non
                MessageBoxResult result = MessageBox.Show(
                    $"Voulez-vous vraiment supprimer l'utilisateur '{username}' ?",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var con = new SqliteConnection(DatabaseHelper.ConnectionString))
                        {
                            con.Open();
                            string query = "DELETE FROM Users WHERE Username = @Username";
                            using (var cmd = new SqliteCommand(query, con))
                            {
                                cmd.Parameters.AddWithValue("@Username", username);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // تحديث الجدول مباشرة بعد الحذف
                        LoadAuditData();
                    }
                    catch (System.Exception ex)
                    {
                        MessageBox.Show("Erreur lors de la suppression: " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}