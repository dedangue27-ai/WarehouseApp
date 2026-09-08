using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;

namespace WarehouseApp
{
    public partial class App : Application
    {
        // تعريف متغيرات عامة لحفظ معلومات المستخدم الحالي
        public static string LoggedInUsername { get; set; } = string.Empty;
        public static string LoggedInRole { get; set; } = string.Empty;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string windowsLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            string targetLang = "fr";
            if (windowsLang == "ar" || windowsLang == "en" || windowsLang == "fr")
            {
                targetLang = windowsLang;
            }

            LocalizationManager.ChangeLanguage(targetLang);

            SplashScreen splashScreen = new SplashScreen();
            splashScreen.Show();

            await Task.Delay(2000);

            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();

            splashScreen.Close();
        }
    }

    public static class MessageHelper
    {
        public static void Show(string key, MessageBoxImage icon = MessageBoxImage.Information)
        {
            string message = GetMessage(key);
            string title = GetMessage(key + "Title") != (key + "Title") ? GetMessage(key + "Title") : GetMessage("DefaultTitle");

            if (title == GetMessage("DefaultTitle"))
            {
                title = icon == MessageBoxImage.Error ? GetMessage("ErrorKey") : "Information";
            }

            MessageBox.Show(message, title, MessageBoxButton.OK, icon);
        }

        public static string GetMessage(string key)
        {
            string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

            if (lang == "ar")
            {
                if (key == "ErrorPassword") return "كلمة المرور غير صحيحة";
                if (key == "SuccessSale") return "تمت البيعة بنجاح";
                if (key == "ErrorKey") return "خطأ";
            }
            else if (lang == "en")
            {
                if (key == "ErrorPassword") return "Incorrect password";
                if (key == "SuccessSale") return "Sale completed successfully";
                if (key == "ErrorKey") return "Error";
            }
            else
            {
                if (key == "ErrorPassword") return "Mot de passe incorrect";
                if (key == "SuccessSale") return "Vente enregistrée avec succès";
                if (key == "ErrorKey") return "Erreur";
            }
            return key;
        }
    }

    public static class LocalizationManager
    {
        public static void ChangeLanguage(string langCode)
        {
            ResourceDictionary dict = new ResourceDictionary();
            switch (langCode)
            {
                case "en":
                    dict.Source = new Uri("pack://application:,,,/Languages/en.xaml", UriKind.Absolute);
                    break;
                case "ar":
                    dict.Source = new Uri("pack://application:,,,/Languages/ar.xaml", UriKind.Absolute);
                    break;
                default:
                    dict.Source = new Uri("pack://application:,,,/Languages/fr.xaml", UriKind.Absolute);
                    break;
            }

            App.Current.Resources.MergedDictionaries.Clear();
            App.Current.Resources.MergedDictionaries.Add(dict);
        }
    }
}