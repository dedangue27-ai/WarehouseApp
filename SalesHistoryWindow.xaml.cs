using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WarehouseApp
{
    public partial class SalesHistoryWindow : Window
    {
        public SalesHistoryWindow()
        {
            InitializeComponent();
            LoadSalesHistory("");
        }

        private void LoadSalesHistory(string keyword)
        {
            try
            {
                // جلب البيانات مباشرة من الجدول دون معادلات معقدة تعيد الحساب بشكل خاطئ
                string query = @"
            SELECT 
                COALESCE(s.InvoiceNumber, 'N/A') AS InvoiceNumber, 
                COALESCE(s.SoldBy, 'Admin') AS SoldBy, 
                COALESCE(p.Name, 'Produit Inconnu') AS ProductName, 
                COALESCE(si.Quantity, 0) AS Quantity, 
                COALESCE(si.UnitPrice, 0) AS UnitPrice, 
                COALESCE(
                    CASE 
                        WHEN si.Quantity > 0 THEN (p.SellingPrice * si.Quantity) - si.TotalPrice
                        ELSE ABS(si.TotalPrice) -- في حالة الإرجاع
                    END, 0
                ) AS Discount, 
                COALESCE(si.TotalPrice, 0) AS TotalAmount, 
                CASE 
                    WHEN COALESCE(si.Quantity, 0) < 0 THEN 'Retour' 
                    ELSE 'Vendu' 
                END AS Status,
                COALESCE(s.SaleDate, '') AS SaleDate 
            FROM Sales s
            LEFT JOIN SaleItems si ON s.Id = si.SaleId
            LEFT JOIN Products p ON si.ProductId = p.Id
        ";

                Dictionary<string, object> parameters = null;

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    query += " WHERE s.InvoiceNumber LIKE @search OR s.SoldBy LIKE @search OR p.Name LIKE @search";
                    parameters = new Dictionary<string, object>
                    {
                        { "@search", $"%{keyword.Trim()}%" }
                    };
                }

                query += " ORDER BY s.Id DESC";

                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);

                if (SalesHistoryGrid != null)
                {
                    SalesHistoryGrid.ItemsSource = dt?.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadSalesHistory(TxtSearch.Text);
        }

        private void SalesHistoryGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (SalesHistoryGrid.SelectedItem is DataRowView rowView)
                {
                    string invoiceNumber = rowView["InvoiceNumber"]?.ToString();

                    if (!string.IsNullOrEmpty(invoiceNumber) && invoiceNumber != "N/A")
                    {
                        Clipboard.SetText(invoiceNumber);
                        MessageBox.Show($"Le numéro de facture [{invoiceNumber}] a été copié dans le presse-papiers.",
                                        "Copié", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la copie : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBackToMain_Click(object sender, RoutedEventArgs e)
        {
            // Récupère la fenêtre globale de SalesHistoryWindow et la ferme
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