using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.Data.Sqlite;

namespace WarehouseApp
{
    public partial class SalesWindow : Window
    {
        private List<SaleItem> cart = new List<SaleItem>();
        private DataTable productsData;

        public SalesWindow()
        {
            InitializeComponent();

           

            if (Application.Current.MainWindow != null)
            {
                this.FlowDirection = Application.Current.MainWindow.FlowDirection;
            }

            LoadProductsForSuggestions();

          
        }

        // تفعيل اختصارات لوحة المفاتيح من F3 إلى F8
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // F3: التركيز على مربع البحث
            if (e.Key == Key.F3)
            {
                TxtSearchInput.Focus();
                TxtSearchInput.SelectAll();
                e.Handled = true;
            }
            // F4: فتح نافذة الإرجاع
            else if (e.Key == Key.F4)
            {
                BtnReturnProduct_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // F5: إتمام البيع
            else if (e.Key == Key.F5)
            {
                BtnCompleteSale_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // F6: تفريغ وإلغاء السلة
            else if (e.Key == Key.F6)
            {
                BtnClearCart_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // F7: فتح سجل المبيعات
            else if (e.Key == Key.F7)
            {
                BtnOpenSalesHistory_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            // F8: التركيز على مربع التخفيض وتحديده
            else if (e.Key == Key.F8)
            {
                if (TxtDiscountInput != null)
                {
                    TxtDiscountInput.Focus();
                    TxtDiscountInput.SelectAll();
                }
                e.Handled = true;
            }
        }

        public class SaleItem
        {
            public int ProductId { get; set; }
            public string Barcode { get; set; }
            public string ProductName { get; set; }
            public decimal Price { get; set; }
            public int Quantity { get; set; }
            public int StockQuantity { get; set; }
            public decimal Total => Price * Quantity;
        }


        private void LoadProductsForSuggestions()
        {
            try
            {
                string query = "SELECT Id, Code, Name, SellingPrice, Quantity FROM Products WHERE Quantity > 0 ORDER BY Name";
                productsData = DatabaseHelper.ExecuteQuery(query);
                LstSuggestions.ItemsSource = productsData?.DefaultView;
            }
            catch (Exception)
            {
                MessageBox.Show("خطأ في تحميل المنتجات", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearchInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = TxtSearchInput.Text.Trim();

            if (string.IsNullOrEmpty(keyword))
            {
                PopupSuggestions.IsOpen = false;
                return;
            }

            try
            {
                string query = @"
                    SELECT Id, Code, Name, SellingPrice, Quantity 
                    FROM Products 
                    WHERE (Name LIKE @keyword OR Code LIKE @keyword) AND Quantity > 0
                    ORDER BY Name
                    LIMIT 10
                ";
                var parameters = new Dictionary<string, object>
                {
                    { "@keyword", $"%{keyword}%" }
                };

                var data = DatabaseHelper.ExecuteQuery(query, parameters);
                if (data != null)
                {
                    LstSuggestions.ItemsSource = data.DefaultView;
                    PopupSuggestions.IsOpen = data.Rows.Count > 0;
                }
            }
            catch (Exception)
            {
                // Ignore error during search typing
            }
        }

        private void LstSuggestions_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AddSelectedProduct();
        }

        private void LstSuggestions_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                AddSelectedProduct();
            }
        }

        private void TxtSearchInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // التحقق إذا كانت القائمة مفتوحة وفيهات عناصر
            if (PopupSuggestions.IsOpen && LstSuggestions.Items.Count > 0)
            {
                if (e.Key == System.Windows.Input.Key.Down)
                {
                    // النزول للأسفل في القائمة
                    if (LstSuggestions.SelectedIndex < LstSuggestions.Items.Count - 1)
                    {
                        LstSuggestions.SelectedIndex++;
                    }
                    else
                    {
                        LstSuggestions.SelectedIndex = 0; // العودة لأول عنصر إذا وصلنا للنهاية
                    }
                    e.Handled = true; // منع المؤشر من التحرك داخل خانة النص
                }
                else if (e.Key == System.Windows.Input.Key.Up)
                {
                    // الصعود للأعلى في القائمة
                    if (LstSuggestions.SelectedIndex > 0)
                    {
                        LstSuggestions.SelectedIndex--;
                    }
                    else
                    {
                        LstSuggestions.SelectedIndex = LstSuggestions.Items.Count - 1; // الذهاب لآخر عنصر
                    }
                    e.Handled = true; // منع المؤشر من التحرك داخل خانة النص
                }
            }

            // التعامل مع زر Enter للإضافة
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (PopupSuggestions.IsOpen && LstSuggestions.SelectedItem != null)
                {
                    AddSelectedProduct();
                }
                else
                {
                    BtnAddProduct_Click(null, null);
                }
                e.Handled = true;
            }
        }

        private void AddSelectedProduct()
        {
            if (LstSuggestions.SelectedItem == null) return;

            if (LstSuggestions.SelectedItem is DataRowView rowView)
            {
                DataRow row = rowView.Row;

                int productId = Convert.ToInt32(row["Id"]);
                string barcode = row["Code"].ToString();
                string productName = row["Name"].ToString();
                decimal price = Convert.ToDecimal(row["SellingPrice"]);
                int stock = Convert.ToInt32(row["Quantity"]);

                if (stock <= 0)
                {
                    MessageBox.Show("المنتج غير متوفر في المخزون!", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existing = cart.FirstOrDefault(c => c.ProductId == productId);
                if (existing != null)
                {
                    if (existing.Quantity + 1 > existing.StockQuantity)
                    {
                        MessageBox.Show($"الكمية المتوفرة: {existing.StockQuantity}", "تنبيه",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    existing.Quantity++;
                }
                else
                {
                    cart.Add(new SaleItem
                    {
                        ProductId = productId,
                        Barcode = barcode,
                        ProductName = productName,
                        Price = price,
                        Quantity = 1,
                        StockQuantity = stock
                    });
                }

                UpdateCart();
                PopupSuggestions.IsOpen = false;
                TxtSearchInput.Clear();
                TxtSearchInput.Focus();
            }
        }

        private void BtnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            string keyword = TxtSearchInput.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("يرجى إدخال اسم أو باركود المنتج", "تنبيه",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string query = @"
                    SELECT Id, Code, Name, SellingPrice, Quantity 
                    FROM Products 
                    WHERE (Name LIKE @keyword OR Code LIKE @keyword) AND Quantity > 0
                    ORDER BY Name
                    LIMIT 1
                ";
                var parameters = new Dictionary<string, object>
                {
                    { "@keyword", $"%{keyword}%" }
                };

                var data = DatabaseHelper.ExecuteQuery(query, parameters);

                if (data == null || data.Rows.Count == 0)
                {
                    MessageBox.Show("المنتج غير موجود أو غير متوفر في المخزون!", "تنبيه",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DataRow row = data.Rows[0];
                int productId = Convert.ToInt32(row["Id"]);
                string barcode = row["Code"].ToString();
                string productName = row["Name"].ToString();
                decimal price = Convert.ToDecimal(row["SellingPrice"]);
                int stock = Convert.ToInt32(row["Quantity"]);

                var existing = cart.FirstOrDefault(c => c.ProductId == productId);
                if (existing != null)
                {
                    if (existing.Quantity + 1 > existing.StockQuantity)
                    {
                        MessageBox.Show($"الكمية المتوفرة: {existing.StockQuantity}", "تنبيه",
                                        MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    existing.Quantity++;
                }
                else
                {
                    cart.Add(new SaleItem
                    {
                        ProductId = productId,
                        Barcode = barcode,
                        ProductName = productName,
                        Price = price,
                        Quantity = 1,
                        StockQuantity = stock
                    });
                }

                UpdateCart();
                TxtSearchInput.Clear();
                TxtSearchInput.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ: {ex.Message}", "خطأ",
                            MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCart()
        {
            SalesDataGrid.ItemsSource = null;
            SalesDataGrid.ItemsSource = cart;
            CalculateTotals();
        }

        private void CalculateTotals()
        {
            if (TxtSubTotal == null || TxtGrandTotal == null) return;

            decimal subTotal = cart.Sum(item => item.Total);
            TxtSubTotal.Text = subTotal.ToString("N2");

            decimal discountValue = 0;
            if (TxtDiscountInput != null && !string.IsNullOrWhiteSpace(TxtDiscountInput.Text))
            {
                string cleanText = TxtDiscountInput.Text.Trim().Replace(',', '.');
                if (decimal.TryParse(cleanText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedDiscount))
                {
                    discountValue = parsedDiscount;
                }
            }

            decimal finalTotal = subTotal;
            bool isPercentage = false;

            if (CmbDiscountType != null)
            {
                if (CmbDiscountType.SelectedIndex == 1)
                {
                    isPercentage = true;
                }
                else if (CmbDiscountType.SelectedItem is ComboBoxItem cbItem && cbItem.Content != null)
                {
                    string selectedType = cbItem.Content.ToString().Trim();
                    if (selectedType == "%" || selectedType.Contains("نسبة") || selectedType.Contains("Pourcentage"))
                    {
                        isPercentage = true;
                    }
                }
            }

            if (isPercentage)
            {
                if (discountValue > 100) discountValue = 100;
                decimal discountAmount = (subTotal * discountValue) / 100;
                finalTotal = subTotal - discountAmount;
            }
            else
            {
                if (discountValue > subTotal) discountValue = subTotal;
                finalTotal = subTotal - discountValue;
            }

            if (finalTotal < 0) finalTotal = 0;

            TxtGrandTotal.Text = finalTotal.ToString("N2");
        }

        private void TxtDiscountInput_TextChanged(object sender, TextChangedEventArgs e) => CalculateTotals();
        private void CmbDiscountType_SelectionChanged(object sender, SelectionChangedEventArgs e) => CalculateTotals();

        private void BtnIncreaseQty_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SaleItem item)
            {
                if (item.Quantity + 1 > item.StockQuantity)
                {
                    MessageBox.Show($"الكمية المتوفرة: {item.StockQuantity}", "تنبيه",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                item.Quantity++;
                UpdateCart();
            }
        }

        private void BtnDecreaseQty_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SaleItem item)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                }
                else
                {
                    cart.Remove(item);
                }
                UpdateCart();
            }
        }

        private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is SaleItem item)
            {
                cart.Remove(item);
                UpdateCart();
            }
        }

        private void SalesDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Row.Item is SaleItem item && e.EditingElement is TextBox textBox)
                {
                    if (int.TryParse(textBox.Text, out int newQty))
                    {
                        if (newQty > 0 && newQty <= item.StockQuantity)
                        {
                            item.Quantity = newQty;
                        }
                        else
                        {
                            MessageBox.Show($"الكمية يجب أن تكون بين 1 و {item.StockQuantity}", "تنبيه",
                                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        Dispatcher.BeginInvoke(new Action(UpdateCart));
                    }
                }
            }
        }

        private string PromptForInput(string title, string promptText)
        {
            // استخدام الكلاس المخصص الجديد بدلاً من النافذة اليدوية القديمة
            return CustomInputBox.Show(promptText, title);
        }

        private string PromptForPassword(string title, string promptText)
        {
            return CustomInputBox.ShowPassword(promptText, title);
        }

        private void BtnClearCart_Click(object sender, RoutedEventArgs e)
        {
            if (cart.Count == 0) return;

            var result = MessageBox.Show("Êtes-vous sûr de vouloir vider le panier ?", "Confirmation de suppression", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                cart.Clear();
                UpdateCart();
                TxtSearchInput.Clear();
                TxtSearchInput.Focus();
            }
        }

        private void BtnOpenSalesHistory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SalesHistoryWindow historyWindow = new SalesHistoryWindow();
                historyWindow.Owner = this;
                historyWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح سجل المبيعات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCompleteSale_Click(object sender, RoutedEventArgs e)
        {
            if (cart.Count == 0)
            {
                MessageBox.Show("Le panier est vide ! Ajoutez d'abord des produits.", "Attention",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Demande du mot de passe
            string sellerPassword = PromptForPassword("Confirmation de vente", "Veuillez entrer votre mot de passe pour confirmer la vente :");

            if (string.IsNullOrEmpty(sellerPassword))
            {
                return;
            }

            try
            {
                using (var con = new SqliteConnection(DatabaseHelper.ConnectionString))
                {
                    con.Open();

                    string getUsernameQuery = "SELECT Username FROM Users WHERE Password = @Password";
                    string loggedInSeller = string.Empty;

                    using (var cmd = new SqliteCommand(getUsernameQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@Password", sellerPassword);
                        var result = cmd.ExecuteScalar();

                        if (result == null || result == DBNull.Value)
                        {
                            MessageBox.Show("Mot de passe incorrect ! La vente a été annulée.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        loggedInSeller = result.ToString();
                    }

                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            string invoiceNumber = $"INV-{DateTime.Now:yyyyMMdd-HHmmss}";

                            decimal originalTotal = cart.Sum(c => c.Total);

                            decimal discountValue = 0;
                            if (TxtDiscountInput != null && !string.IsNullOrWhiteSpace(TxtDiscountInput.Text))
                            {
                                string cleanText = TxtDiscountInput.Text.Trim().Replace(',', '.');
                                if (decimal.TryParse(cleanText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedDiscount))
                                {
                                    discountValue = parsedDiscount;
                                }
                            }

                            bool isPercentage = false;
                            if (CmbDiscountType != null)
                            {
                                if (CmbDiscountType.SelectedIndex == 1)
                                {
                                    isPercentage = true;
                                }
                                else if (CmbDiscountType.SelectedItem is ComboBoxItem cbItem && cbItem.Content != null)
                                {
                                    string selectedType = cbItem.Content.ToString().Trim();
                                    if (selectedType == "%" || selectedType.Contains("نسبة") || selectedType.Contains("Pourcentage"))
                                    {
                                        isPercentage = true;
                                    }
                                }
                            }

                            decimal totalDiscount = 0;
                            if (isPercentage)
                            {
                                if (discountValue > 100) discountValue = 100;
                                totalDiscount = (originalTotal * discountValue) / 100;
                            }
                            else
                            {
                                if (discountValue > originalTotal) discountValue = originalTotal;
                                totalDiscount = discountValue;
                            }

                            decimal finalAmount = originalTotal - totalDiscount;
                            if (finalAmount < 0) finalAmount = 0;
                            if (totalDiscount < 0) totalDiscount = 0;

                            string saleQuery = @"
                INSERT INTO Sales (InvoiceNumber, SaleDate, TotalAmount, Discount, FinalAmount, PaymentMethod, SoldBy)
                VALUES (@invoice, @date, @total, @discount, @final, @payment, @soldBy);
                SELECT last_insert_rowid();
            ";

                            long saleId;
                            using (var saleCmd = new SqliteCommand(saleQuery, con, transaction))
                            {
                                saleCmd.Parameters.AddWithValue("@invoice", invoiceNumber);
                                saleCmd.Parameters.AddWithValue("@date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                                saleCmd.Parameters.AddWithValue("@total", originalTotal);
                                saleCmd.Parameters.AddWithValue("@discount", totalDiscount);
                                saleCmd.Parameters.AddWithValue("@final", finalAmount);
                                saleCmd.Parameters.AddWithValue("@payment", "Espèces");
                                saleCmd.Parameters.AddWithValue("@soldBy", loggedInSeller);

                                saleId = Convert.ToInt64(saleCmd.ExecuteScalar());
                            }

                            int totalUnitsInCart = cart.Sum(c => c.Quantity);
                            decimal discountPerUnit = totalUnitsInCart > 0 ? totalDiscount / totalUnitsInCart : 0;

                            foreach (var item in cart)
                            {
                                decimal itemTotalDiscount = Math.Round(discountPerUnit * item.Quantity, 2);

                                if (itemTotalDiscount > item.Total) itemTotalDiscount = item.Total;
                                if (itemTotalDiscount < 0) itemTotalDiscount = 0;

                                decimal itemFinalTotal = item.Total - itemTotalDiscount;
                                decimal itemEffectiveUnitPrice = item.Quantity > 0 ? itemFinalTotal / item.Quantity : item.Price;

                                string itemQuery = @"
                    INSERT INTO SaleItems (SaleId, ProductId, Quantity, UnitPrice, TotalPrice)
                    VALUES (@saleId, @productId, @quantity, @unitPrice, @total)
                ";
                                using (var itemCmd = new SqliteCommand(itemQuery, con, transaction))
                                {
                                    itemCmd.Parameters.AddWithValue("@saleId", saleId);
                                    itemCmd.Parameters.AddWithValue("@productId", item.ProductId);
                                    itemCmd.Parameters.AddWithValue("@quantity", item.Quantity);
                                    itemCmd.Parameters.AddWithValue("@unitPrice", itemEffectiveUnitPrice);
                                    itemCmd.Parameters.AddWithValue("@total", itemFinalTotal);
                                    itemCmd.ExecuteNonQuery();
                                }

                                string updateStock = "UPDATE Products SET Quantity = Quantity - @qty WHERE Id = @id";
                                using (var stockCmd = new SqliteCommand(updateStock, con, transaction))
                                {
                                    stockCmd.Parameters.AddWithValue("@qty", item.Quantity);
                                    stockCmd.Parameters.AddWithValue("@id", item.ProductId);
                                    stockCmd.ExecuteNonQuery();
                                }
                            }

                            transaction.Commit();

                            // --- Code d'impression de la facture ---
                            PrintDialog printDlg = new PrintDialog();
                            FlowDocument doc = new FlowDocument();
                            doc.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
                            doc.FlowDirection = FlowDirection.LeftToRight;
                            doc.PagePadding = new Thickness(30);
                            doc.ColumnWidth = double.PositiveInfinity;

                            Paragraph shopTitle = new Paragraph(new Run("Système de Gestion d'Entrepôt - Point de Vente"))
                            {
                                FontSize = 18,
                                FontWeight = FontWeights.Bold,
                                TextAlignment = TextAlignment.Center,
                                Margin = new Thickness(0, 0, 0, 5)
                            };
                            doc.Blocks.Add(shopTitle);

                            Paragraph title = new Paragraph(new Run("Facture de Vente"))
                            {
                                FontSize = 14,
                                FontWeight = FontWeights.Bold,
                                Foreground = System.Windows.Media.Brushes.DarkBlue,
                                TextAlignment = TextAlignment.Center,
                                Margin = new Thickness(0, 0, 0, 15)
                            };
                            doc.Blocks.Add(title);

                            Paragraph info = new Paragraph(new Run($"N° de Facture : {invoiceNumber}\nDate : {DateTime.Now:yyyy-MM-dd HH:mm}\nCaissier : {loggedInSeller}"))
                            {
                                FontSize = 11,
                                TextAlignment = TextAlignment.Left,
                                Margin = new Thickness(0, 0, 0, 15)
                            };
                            doc.Blocks.Add(info);

                            Table table = new Table();
                            table.CellSpacing = 0;
                            table.BorderBrush = System.Windows.Media.Brushes.Gray;
                            table.BorderThickness = new Thickness(1, 1, 0, 0);

                            table.Columns.Add(new TableColumn() { Width = new GridLength(2.5, GridUnitType.Star) });
                            table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
                            table.Columns.Add(new TableColumn() { Width = new GridLength(1.2, GridUnitType.Star) });
                            table.Columns.Add(new TableColumn() { Width = new GridLength(1.3, GridUnitType.Star) });

                            TableRowGroup rowGroup = new TableRowGroup();

                            TableRow headerRow = new TableRow();
                            headerRow.Background = System.Windows.Media.Brushes.Gainsboro;

                            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Produit")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Left }) { BorderBrush = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(6) });
                            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Qté")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }) { BorderBrush = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(6) });
                            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Prix")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }) { BorderBrush = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(6) });
                            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Total")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }) { BorderBrush = System.Windows.Media.Brushes.Gray, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(6) });

                            rowGroup.Rows.Add(headerRow);

                            foreach (var item in cart)
                            {
                                TableRow row = new TableRow();
                                row.Cells.Add(new TableCell(new Paragraph(new Run(item.ProductName)) { TextAlignment = TextAlignment.Left }) { BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(6) });
                                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Quantity.ToString())) { TextAlignment = TextAlignment.Center }) { BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(6) });
                                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Price.ToString("N2"))) { TextAlignment = TextAlignment.Center }) { BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(6) });
                                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Total.ToString("N2"))) { TextAlignment = TextAlignment.Center }) { BorderBrush = System.Windows.Media.Brushes.LightGray, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(6) });

                                rowGroup.Rows.Add(row);
                            }
                            table.RowGroups.Add(rowGroup);
                            doc.Blocks.Add(table);

                            Paragraph totalsPar = new Paragraph();
                            totalsPar.TextAlignment = TextAlignment.Right;
                            totalsPar.FontSize = 12;
                            totalsPar.Margin = new Thickness(0, 15, 0, 0);

                            totalsPar.Inlines.Add(new Run($"Sous-total : {originalTotal:N2} DA\n"));
                            if (totalDiscount > 0)
                            {
                                totalsPar.Inlines.Add(new Run($"Remise : {totalDiscount:N2} DA\n"));
                            }

                            Run finalRun = new Run($"Total Général : {finalAmount:N2} DA");
                            finalRun.FontWeight = FontWeights.Bold;
                            finalRun.FontSize = 14;
                            totalsPar.Inlines.Add(finalRun);

                            doc.Blocks.Add(totalsPar);

                            Paragraph footerPar = new Paragraph(new Run("\nMerci pour votre visite ! À bientôt"))
                            {
                                FontSize = 11,
                                FontStyle = FontStyles.Italic,
                                TextAlignment = TextAlignment.Center,
                                Margin = new Thickness(0, 20, 0, 0)
                            };
                            doc.Blocks.Add(footerPar);

                            IDocumentPaginatorSource idpSource = doc;
                            printDlg.PrintDocument(idpSource.DocumentPaginator, $"Facture N° {invoiceNumber}");

                            // --- Notification finale et réinitialisation de l'interface ---
                            MessageBox.Show($"✅ Vente effectuée avec succès par le caissier : {loggedInSeller}\nN° de Facture : {invoiceNumber}\nMontant après remise : {finalAmount:N2} DA",
                                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                            cart.Clear();
                            if (TxtDiscountInput != null) TxtDiscountInput.Text = "0";
                            UpdateCart();
                            LoadProductsForSuggestions();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la finalisation de la facture : {ex.Message}", "Erreur",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnReturnProduct_Click(object sender, RoutedEventArgs e)
        {
            string invoiceNumber = PromptForInput("Gestion des Retours", "Veuillez entrer le numéro de facture :");
            if (string.IsNullOrEmpty(invoiceNumber)) return;

            try
            {
                using (var con = new SqliteConnection(DatabaseHelper.ConnectionString))
                {
                    con.Open();
                    long saleId = 0;

                    string getSaleQuery = "SELECT Id FROM Sales WHERE InvoiceNumber = @invoice";
                    using (var cmd = new SqliteCommand(getSaleQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@invoice", invoiceNumber);
                        var result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            saleId = Convert.ToInt64(result);
                        }
                        else
                        {
                            MessageBox.Show("Le numéro de facture n'existe pas !", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    string query = @"
                SELECT p.Id, p.Name, SUM(si.Quantity) as NetQuantity, 
                       MAX(p.SellingPrice) as OriginalPrice, 
                       (SUM(si.TotalPrice) / SUM(si.Quantity)) as DiscountedUnitPrice
                FROM SaleItems si
                JOIN Products p ON si.ProductId = p.Id
                WHERE si.SaleId = @saleId
                GROUP BY p.Id, p.Name
                HAVING SUM(si.Quantity) > 0
            ";

                    var itemsList = new List<ReturnItemModel>();
                    using (var cmd = new SqliteCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@saleId", saleId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                itemsList.Add(new ReturnItemModel
                                {
                                    ProductId = reader.GetInt32(0),
                                    ProductName = reader.GetString(1),
                                    Quantity = reader.GetInt32(2),
                                    UnitPrice = reader.GetDecimal(3),
                                    DiscountedPrice = reader.GetDecimal(4)
                                });
                            }
                        }
                    }

                    if (itemsList.Count == 0)
                    {
                        MessageBox.Show("Aucun produit disponible pour le retour dans cette facture, ou a déjà été totalement retourné.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // نافذة خيارات مخصصة باللغة الفرنسية لتجنب أي التباس لدى البائع
                    bool returnFullInvoice = false;
                    bool userCancelled = true;

                    Window choiceWindow = new Window()
                    {
                        Width = 450,
                        Height = 220,
                        Title = "Mode de Retour",
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        FlowDirection = FlowDirection.LeftToRight,
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252))
                    };

                    StackPanel mainStack = new StackPanel() { Margin = new Thickness(20) };

                    TextBlock titleText = new TextBlock()
                    {
                        Text = $"Facture N° : {invoiceNumber}\nSélectionnez le type de retour souhaité :",
                        FontSize = 14,
                        FontWeight = FontWeights.Bold,
                        Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 58, 138)),
                        Margin = new Thickness(0, 0, 0, 15)
                    };

                    Button btnFull = new Button()
                    {
                        Content = "Retourner toute la facture",
                        Height = 35,
                        Margin = new Thickness(0, 0, 0, 8),
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(37, 99, 235)),
                        Foreground = System.Windows.Media.Brushes.White,
                        FontWeight = FontWeights.Bold
                    };

                    Button btnItem = new Button()
                    {
                        Content = "Retourner un seul produit",
                        Height = 35,
                        Margin = new Thickness(0, 0, 0, 8),
                        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105)),
                        Foreground = System.Windows.Media.Brushes.White
                    };

                    btnFull.Click += (s, args) => { returnFullInvoice = true; userCancelled = false; choiceWindow.Close(); };
                    btnItem.Click += (s, args) => { returnFullInvoice = false; userCancelled = false; choiceWindow.Close(); };

                    mainStack.Children.Add(titleText);
                    mainStack.Children.Add(btnFull);
                    mainStack.Children.Add(btnItem);
                    choiceWindow.Content = mainStack;
                    choiceWindow.ShowDialog();

                    if (userCancelled) return;

                    using (var transaction = con.BeginTransaction())
                    {
                        try
                        {
                            if (returnFullInvoice)
                            {
                                // إرجاع الفاتورة بالكامل
                                decimal totalRefund = 0;

                                foreach (var item in itemsList)
                                {
                                    string updateStock = "UPDATE Products SET Quantity = Quantity + @qty WHERE Id = @id";
                                    using (var updateCmd = new SqliteCommand(updateStock, con, transaction))
                                    {
                                        updateCmd.Parameters.AddWithValue("@qty", item.Quantity);
                                        updateCmd.Parameters.AddWithValue("@id", item.ProductId);
                                        updateCmd.ExecuteNonQuery();
                                    }

                                    decimal refundItemTotal = item.Quantity * item.DiscountedPrice;
                                    totalRefund += refundItemTotal;

                                    string insertReturnItem = @"
                                INSERT INTO SaleItems (SaleId, ProductId, Quantity, UnitPrice, TotalPrice) 
                                VALUES (@SaleId, @ProductId, -@Quantity, @UnitPrice, -@TotalPrice);
                            ";
                                    using (var returnCmd = new SqliteCommand(insertReturnItem, con, transaction))
                                    {
                                        returnCmd.Parameters.AddWithValue("@SaleId", saleId);
                                        returnCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                                        returnCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                                        returnCmd.Parameters.AddWithValue("@UnitPrice", item.DiscountedPrice);
                                        returnCmd.Parameters.AddWithValue("@TotalPrice", refundItemTotal);
                                        returnCmd.ExecuteNonQuery();
                                    }
                                }

                                string updateSalesTable = "UPDATE Sales SET FinalAmount = FinalAmount - @refund, TotalAmount = TotalAmount - @refund WHERE Id = @saleId";
                                using (var updateSalesCmd = new SqliteCommand(updateSalesTable, con, transaction))
                                {
                                    updateSalesCmd.Parameters.AddWithValue("@refund", totalRefund);
                                    updateSalesCmd.Parameters.AddWithValue("@saleId", saleId);
                                    updateSalesCmd.ExecuteNonQuery();
                                }

                                transaction.Commit();
                                MessageBox.Show("✅ La facture a été entièrement retournée avec succès et le stock a été mis à jour.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                // إرجاع منتج محدد
                                int targetProductId = 0;
                                string targetProductName = "";
                                int maxQuantity = 0;
                                decimal effectiveUnitPrice = 0;

                                if (itemsList.Count == 1)
                                {
                                    targetProductId = itemsList[0].ProductId;
                                    targetProductName = itemsList[0].ProductName;
                                    maxQuantity = itemsList[0].Quantity;
                                    effectiveUnitPrice = itemsList[0].DiscountedPrice;
                                }
                                else
                                {
                                    var selectionWindow = new ReturnItemsSelectionWindow(itemsList)
                                    {
                                        Owner = this
                                    };

                                    if (selectionWindow.ShowDialog() == true && selectionWindow.SelectedItem != null)
                                    {
                                        targetProductId = selectionWindow.SelectedItem.ProductId;
                                        targetProductName = selectionWindow.SelectedItem.ProductName;
                                        maxQuantity = selectionWindow.SelectedItem.Quantity;
                                        effectiveUnitPrice = selectionWindow.SelectedItem.DiscountedPrice;
                                    }
                                    else
                                    {
                                        transaction.Rollback();
                                        return;
                                    }
                                }

                                string qtyStr = PromptForInput("Définir la quantité", $"Entrez la quantité à retourner pour '{targetProductName}' (Max: {maxQuantity}) :");
                                if (!int.TryParse(qtyStr, out int returnQty) || returnQty <= 0)
                                {
                                    MessageBox.Show("La quantité saisie est incorrecte !", "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    transaction.Rollback();
                                    return;
                                }

                                if (returnQty > maxQuantity)
                                {
                                    MessageBox.Show($"La quantité à retourner dépasse la quantité restante pouvant être retournée ({maxQuantity}) !", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                                    transaction.Rollback();
                                    return;
                                }

                                string updateStock = "UPDATE Products SET Quantity = Quantity + @qty WHERE Id = @id";
                                using (var updateCmd = new SqliteCommand(updateStock, con, transaction))
                                {
                                    updateCmd.Parameters.AddWithValue("@qty", returnQty);
                                    updateCmd.Parameters.AddWithValue("@id", targetProductId);
                                    updateCmd.ExecuteNonQuery();
                                }

                                decimal refundItemTotal = returnQty * effectiveUnitPrice;

                                string insertReturnItem = @"
                            INSERT INTO SaleItems (SaleId, ProductId, Quantity, UnitPrice, TotalPrice) 
                            VALUES (@SaleId, @ProductId, -@Quantity, @UnitPrice, -@TotalPrice);
                        ";
                                using (var returnCmd = new SqliteCommand(insertReturnItem, con, transaction))
                                {
                                    returnCmd.Parameters.AddWithValue("@SaleId", saleId);
                                    returnCmd.Parameters.AddWithValue("@ProductId", targetProductId);
                                    returnCmd.Parameters.AddWithValue("@Quantity", returnQty);
                                    returnCmd.Parameters.AddWithValue("@UnitPrice", effectiveUnitPrice);
                                    returnCmd.Parameters.AddWithValue("@TotalPrice", refundItemTotal);
                                    returnCmd.ExecuteNonQuery();
                                }

                                string updateSalesTable = "UPDATE Sales SET FinalAmount = FinalAmount - @refund, TotalAmount = TotalAmount - @refund WHERE Id = @saleId";
                                using (var updateSalesCmd = new SqliteCommand(updateSalesTable, con, transaction))
                                {
                                    updateSalesCmd.Parameters.AddWithValue("@refund", refundItemTotal);
                                    updateSalesCmd.Parameters.AddWithValue("@saleId", saleId);
                                    updateSalesCmd.ExecuteNonQuery();
                                }

                                transaction.Commit();
                                MessageBox.Show("✅ Le produit a été retourné avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                            }

                            LoadProductsForSuggestions();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBackToMain_Click(object sender, RoutedEventArgs e)
        {
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

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // Confirmer la déconnexion avec l'utilisateur
            MessageBoxResult result = MessageBox.Show(
                "Voulez-vous vraiment vous déconnecter ?",
                "Déconnexion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Ouvrir la fenêtre de Login et fermer SalesWindow
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}