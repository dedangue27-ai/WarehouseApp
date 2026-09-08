using System.Windows;

namespace WarehouseApp
{
    public partial class PasswordDialog : Window
    {
        public string PasswordResult { get; private set; } = string.Empty;

        public PasswordDialog()
        {
            InitializeComponent();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            PasswordResult = TxtConfirmPass.Password.Trim();
            DialogResult = true;
            this.Close();
        }
    }
}