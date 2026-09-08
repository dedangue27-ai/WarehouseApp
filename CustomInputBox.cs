using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WarehouseApp
{
    public class CustomInputBox : Window
    {
        public string ResponseText { get; private set; }

        private CustomInputBox(string prompt, string title, string defaultValue, bool isPassword)
        {
            Title = title;
            Width = 380;
            Height = 190;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = Brushes.White;

            StackPanel panel = new StackPanel { Margin = new Thickness(15) };

            TextBlock lblPrompt = new TextBlock
            {
                Text = prompt,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 13
            };
            panel.Children.Add(lblPrompt);

            TextBox txtInput = null;
            PasswordBox pwdInput = null;

            if (isPassword)
            {
                pwdInput = new PasswordBox
                {
                    Height = 30,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 15),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                panel.Children.Add(pwdInput);
            }
            else
            {
                txtInput = new TextBox
                {
                    Text = defaultValue,
                    Height = 30,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 0, 15),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                panel.Children.Add(txtInput);
            }

            StackPanel btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Button btnCancel = new Button
            {
                Content = "إلغاء / Annuler",
                Width = 90,
                Height = 30,
                Margin = new Thickness(0, 0, 5, 0),
                IsCancel = true
            };
            btnCancel.Click += (s, e) => { DialogResult = false; Close(); };

            Button btnOk = new Button
            {
                Content = "موافق / OK",
                Width = 80,
                Height = 30,
                IsDefault = true
            };
            btnOk.Click += (s, e) => {
                ResponseText = isPassword ? pwdInput.Password : txtInput.Text;
                DialogResult = true;
                Close();
            };

            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnOk);
            panel.Children.Add(btnPanel);

            Content = panel;

            Loaded += (s, e) => {
                if (isPassword) pwdInput.Focus();
                else { txtInput.Focus(); txtInput.SelectAll(); }
            };
        }

        public static string Show(string prompt, string title, string defaultValue = "")
        {
            var box = new CustomInputBox(prompt, title, defaultValue, false);
            return box.ShowDialog() == true ? box.ResponseText : null;
        }

        public static string ShowPassword(string prompt, string title)
        {
            var box = new CustomInputBox(prompt, title, "", true);
            return box.ShowDialog() == true ? box.ResponseText : string.Empty;
        }
    }
}