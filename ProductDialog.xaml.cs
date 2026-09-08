using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace WarehouseApp
{
    public class InvoiceItemModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int Quantity { get; set; }
        public string ExpiryDate { get; set; }
    }

    public partial class AddProductWindow : Window
    {
        public ObservableCollection<InvoiceItemModel> InvoiceItems { get; set; } = new ObservableCollection<InvoiceItemModel>();

        public AddProductWindow()
        {
            InitializeComponent();
            GridInvoiceProducts.ItemsSource = InvoiceItems;
            Loaded += (s, e) =>
            {
                TxtInvoiceNumber.Focus();
                TxtCode.Text = GenerateBarcode();
            };
        }

        private string GenerateBarcode()
        {
            return DateTime.Now.ToString("yyMMddHHmmssfff");
        }

        public AddProductWindow(string defaultCode) : this()
        {
            if (TxtCode != null && !string.IsNullOrEmpty(defaultCode)) TxtCode.Text = defaultCode;
        }

        public AddProductWindow(string defaultCode, string defaultName) : this()
        {
            if (TxtCode != null && !string.IsNullOrEmpty(defaultCode)) TxtCode.Text = defaultCode;
            if (TxtName != null) TxtName.Text = defaultName;
        }

        public AddProductWindow(int productId, string code, string name, decimal purchasePrice, decimal sellingPrice, int quantity, string expiryDate) : this()
        {
            if (TxtCode != null) TxtCode.Text = code;
            if (TxtName != null) TxtName.Text = name;
            if (TxtPurchasePrice != null) TxtPurchasePrice.Text = purchasePrice.ToString("0.##");
            if (TxtSellingPrice != null) TxtSellingPrice.Text = sellingPrice.ToString("0.##");
            if (TxtQuantity != null) TxtQuantity.Text = quantity.ToString();
            if (TxtExpiryDate != null) TxtExpiryDate.Text = expiryDate;

            InvoiceItems.Add(new InvoiceItemModel
            {
                Code = code,
                Name = name,
                PurchasePrice = purchasePrice,
                SellingPrice = sellingPrice,
                Quantity = quantity,
                ExpiryDate = expiryDate
            });

            CalculateTotalInvoice();
        }

        private void CalculateTotalInvoice()
        {
            decimal total = InvoiceItems.Sum(item => item.PurchasePrice * item.Quantity);
            if (TxtTotalInvoicePrice != null)
            {
                TxtTotalInvoicePrice.Text = total.ToString("0.##");
            }
        }

        private void BtnAddToList_Click(object sender, RoutedEventArgs e)
        {
            string code = TxtCode.Text.Trim();
            string name = TxtName.Text.Trim();

            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Veuillez saisir au moins le code-barres et le nom du produit.", "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(TxtPurchasePrice.Text.Trim(), out decimal purchasePrice)) purchasePrice = 0;
            if (!decimal.TryParse(TxtSellingPrice.Text.Trim(), out decimal sellingPrice)) sellingPrice = 0;
            if (!int.TryParse(TxtQuantity.Text.Trim(), out int quantity)) quantity = 1;
            string expiryDate = TxtExpiryDate.Text.Trim();

            InvoiceItems.Add(new InvoiceItemModel
            {
                Code = code,
                Name = name,
                PurchasePrice = purchasePrice,
                SellingPrice = sellingPrice,
                Quantity = quantity,
                ExpiryDate = expiryDate
            });

            CalculateTotalInvoice();

            // توليد باركود افتراضي جديد بالوقت والتاريخ للمنتج التالي وتفريغ الحقول
            TxtCode.Text = GenerateBarcode();
            TxtName.Clear();
            TxtPurchasePrice.Clear();
            TxtSellingPrice.Clear();
            TxtQuantity.Clear();
            TxtExpiryDate.Clear();

            // الانتقال المباشر لخانة اسم المنتج
            TxtName.Focus();
        }

        private void BtnSaveInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (InvoiceItems.Count == 0)
                {
                    MessageBox.Show("Veuillez ajouter au moins un produit à la liste.", "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string invoiceNumber = TxtInvoiceNumber.Text.Trim();
                string supplier = TxtSupplier.Text.Trim();

                if (!decimal.TryParse(TxtTotalInvoicePrice.Text.Trim(), out decimal totalInvoicePrice)) totalInvoicePrice = 0;

                foreach (var item in InvoiceItems)
                {
                    string query = "INSERT INTO Products (Code, Name, PurchasePrice, SellingPrice, Quantity, ExpiryDate) VALUES (@Code, @Name, @PurchasePrice, @SellingPrice, @Quantity, @ExpiryDate)";

                    var parameters = new Dictionary<string, object>
                    {
                        { "@Code", item.Code },
                        { "@Name", item.Name },
                        { "@PurchasePrice", item.PurchasePrice },
                        { "@SellingPrice", item.SellingPrice },
                        { "@Quantity", item.Quantity },
                        { "@ExpiryDate", string.IsNullOrEmpty(item.ExpiryDate) ? (object)DBNull.Value : item.ExpiryDate },
                        { "@InvoiceNumber", string.IsNullOrEmpty(invoiceNumber) ? (object)DBNull.Value : invoiceNumber },
                        { "@Supplier", string.IsNullOrEmpty(supplier) ? (object)DBNull.Value : supplier },
                        { "@TotalInvoicePrice", totalInvoicePrice }
                    };

                    DatabaseHelper.ExecuteNonQuery(query, parameters);
                }

                MessageBox.Show("La facture et tous les produits ont été enregistrés avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}