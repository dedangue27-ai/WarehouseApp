using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace WarehouseApp
{
    public partial class AddProductWindow : Window
    {
        // تعريف متغير لتحديد ما إذا كنا في وضع التعديل أم الإضافة الجديدة
        private int editingProductId = 0;

        public AddProductWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => TxtCode.Focus();
        }

        public AddProductWindow(string defaultCode) : this()
        {
            if (TxtCode != null)
            {
                TxtCode.Text = defaultCode;
            }
        }

        public AddProductWindow(string defaultCode, string defaultName) : this()
        {
            if (TxtCode != null) TxtCode.Text = defaultCode;
            if (TxtName != null) TxtName.Text = defaultName;
        }

        // المُنشئ الخاص بفتح النافذة لوضع التعديل عند النقر المزدوج
        public AddProductWindow(int productId, string code, string name, decimal purchasePrice, decimal sellingPrice, int quantity, string expiryDate) : this()
        {
            editingProductId = productId; // تخزين معرف المنتج لتمييز عملية التعديل

            if (TxtCode != null) TxtCode.Text = code;
            if (TxtName != null) TxtName.Text = name;
            if (TxtPurchasePrice != null) TxtPurchasePrice.Text = purchasePrice.ToString("0.##");
            if (TxtSellingPrice != null) TxtSellingPrice.Text = sellingPrice.ToString("0.##");
            if (TxtQuantity != null) TxtQuantity.Text = quantity.ToString();
            if (TxtExpiryDate != null) TxtExpiryDate.Text = expiryDate;

            // تغيير عنوان النافذة ونص الزر ليناسب وضع التعديل
            this.Title = "Modifier le produit";

            // البحث عن زر الحفظ وتغيير نصه إن أمكن، أو الاعتماد على التعديل المباشر
            // (تأكد من إعطاء اسم x:Name="BtnSaveProduct" للزر في ملف الـ XAML إن لم يكن موجوداً)
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (sender == TxtCode) TxtName.Focus();
                else if (sender == TxtName) TxtPurchasePrice.Focus();
                else if (sender == TxtPurchasePrice) TxtSellingPrice.Focus();
                else if (sender == TxtSellingPrice) TxtQuantity.Focus();
                else if (sender == TxtQuantity) TxtExpiryDate.Focus();
                else if (sender == TxtExpiryDate) BtnSaveProduct_Click(sender, e);
            }
        }

        private void BtnSaveProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string code = TxtCode.Text.Trim();
                string name = TxtName.Text.Trim();
                string purchasePriceStr = TxtPurchasePrice.Text.Trim();
                string sellingPriceStr = TxtSellingPrice.Text.Trim();
                string quantityStr = TxtQuantity.Text.Trim();
                string expiryDate = TxtExpiryDate.Text.Trim();

                if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Veuillez remplir au moins le code et le nom du produit.", "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtCode.Focus();
                    return;
                }

                if (!decimal.TryParse(purchasePriceStr, out decimal purchasePrice)) purchasePrice = 0;
                if (!decimal.TryParse(sellingPriceStr, out decimal sellingPrice)) sellingPrice = 0;
                if (!int.TryParse(quantityStr, out int quantity)) quantity = 1;

                string query;
                var parameters = new Dictionary<string, object>
                {
                    { "@Code", code },
                    { "@Name", name },
                    { "@PurchasePrice", purchasePrice },
                    { "@SellingPrice", sellingPrice },
                    { "@Quantity", quantity },
                    { "@ExpiryDate", string.IsNullOrEmpty(expiryDate) ? (object)DBNull.Value : expiryDate }
                };

                // إذا كان editingProductId أكبر من صفر، فهذا يعني أننا نقوم بتعديل منتج موجود مسبقاً
                if (editingProductId > 0)
                {
                    query = "UPDATE Products SET Code = @Code, Name = @Name, PurchasePrice = @PurchasePrice, SellingPrice = @SellingPrice, Quantity = @Quantity, ExpiryDate = @ExpiryDate WHERE Id = @Id";
                    parameters.Add("@Id", editingProductId);
                }
                else
                {
                    // خلاف ذلك، عملية إضافة منتج جديد
                    query = "INSERT INTO Products (Code, Name, PurchasePrice, SellingPrice, Quantity, ExpiryDate) VALUES (@Code, @Name, @PurchasePrice, @SellingPrice, @Quantity, @ExpiryDate)";
                }

                DatabaseHelper.ExecuteNonQuery(query, parameters);

                MessageBox.Show("Le produit a été enregistré avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Une erreur s'est produite lors de l'enregistrement : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}