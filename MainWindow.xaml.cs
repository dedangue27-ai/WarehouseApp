using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using AutoUpdaterDotNET;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.Data.Sqlite;
using WarehouseApp;

namespace WarehouseApp
{
    public partial class MainWindow : Window
    {
        private string currentUsername;
        private string currentUserRole;

        private List<DataRowView> selectedInventoryItems = new List<DataRowView>();


        public MainWindow(string username, string role)
        {
            InitializeComponent();
            currentUsername = username;
            currentUserRole = role;

            if (TxtWorkerName != null)
            {
                TxtWorkerName.Text = currentUsername;
            }

            // --- أضف هذا السطر هنا لجلب رقم الإصدار الحقيقي وتحديثه في الواجهة ---
            TxtAppVersion.Text = "v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            ApplyUserPermissions();

            this.Loaded += MainWindow_Loaded;

            ApplyUserPermissions();

            this.Loaded += MainWindow_Loaded;

            
            / كود التحقق من التحديثات
            AutoUpdater.ReportErrors = true;
            AutoUpdater.ShowRemindLaterButton = true;
            AutoUpdater.ShowSkipButton = true;
            AutoUpdater.RunUpdateAsAdmin = true;

            AutoUpdater.CheckForUpdateEvent += args =>
            {
                if (args.Error != null)
                {
                    MessageBox.Show($"حدث خطأ أثناء التحديث: {args.Error.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            AutoUpdater.ExecutablePath = "WarehouseApp.exe";

            AutoUpdater.Start("https://raw.githubusercontent.com/dedangue27-ai/WarehouseApp/main/update.xml");
        }


        // دالة لفتح نافذة التعديل عند النقر المزدوج على صف المنتج
        private void ProductsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                // التحقق من وجود صف محدد
                if (ProductsGrid.SelectedItem is DataRowView rowView)
                {
                    // جلب بيانات المنتج من الصف المحدد
                    int productId = Convert.ToInt32(rowView["Id"]);
                    string productCode = rowView["Code"]?.ToString();
                    string productName = rowView["Name"]?.ToString();
                    decimal purchasePrice = Convert.ToDecimal(rowView["PurchasePrice"]);
                    decimal sellingPrice = Convert.ToDecimal(rowView["SellingPrice"]);
                    int quantity = Convert.ToInt32(rowView["Quantity"]);
                    // ملاحظة: تأكد من وجود عمود ExpiryDate في DataTable المصدرة للـ Grid
                    string expiryDate = rowView["ExpiryDate"]?.ToString();

                    // فتح نافذة الإضافة/التعديل في وضع التعديل (تمرير البيانات)
                    // سنحتاج لتعديل منشئ (Constructor) نافذة AddProductWindow لاستقبال هذه البيانات
                    AddProductWindow editProdWin = new AddProductWindow(productId, productCode, productName, purchasePrice, sellingPrice, quantity, expiryDate);

                    // إظهار النافذة وانتظار النتيجة
                    if (editProdWin.ShowDialog() == true)
                    {
                        // إذا تم الحفظ بنجاح، تحديث بيانات المخزون المعروضة
                        LoadInventoryData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("حدث خطأ أثناء محاولة فتح المنتج للتعديل: " + ex.Message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnShowNetProfit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Calcul_des_intérêts__bénéfice_net_ profitWin = new Calcul_des_intérêts__bénéfice_net_();

                // 1. Masque la fenêtre principale
                this.Hide();

                // 2. Ouvre la fenêtre du bénéfice net en mode dialogue
                profitWin.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Impossible d'ouvrir la fenêtre du bénéfice net : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 3. Réaffiche la fenêtre principale dès la fermeture de profitWin
                this.Show();
            }
        }
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        string updateOldSales = "UPDATE Sales SET SoldBy = 'admin' WHERE SoldBy IS NULL OR SoldBy = '';";
                        DatabaseHelper.ExecuteNonQuery(updateOldSales);

                        // --- ضع أمر تحديث الحد الأدنى للمخزون هنا ---
                        string updateMinQty = "UPDATE Products SET MinQuantity = 2;";
                        DatabaseHelper.ExecuteNonQuery(updateMinQty);
                    }
                    catch (Exception exLoop)
                    {
                        MessageBox.Show("Erreur de mise à jour en arrière-plan : " + exLoop.Message);
                    }
                });

                LoadInventoryData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement de la fenêtre principale : " + ex.Message, "Erreur fatale", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenSalesHistory_Click(object sender, RoutedEventArgs e)
        {
            SalesHistoryWindow historyWin = new SalesHistoryWindow();

            // 1. Masquer MainWindow
            this.Hide();

            // 2. Ouvrir SalesHistoryWindow
            historyWin.ShowDialog();

            // 3. Réafficher MainWindow quand SalesHistoryWindow se ferme
            this.Show();
        }

        public void BtnShowDamagedProducts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DamagedProductsWindow damagedWin = new DamagedProductsWindow(currentUsername);
                damagedWin.ShowDialog();
                LoadInventoryData(); // لتحديث المخزون والكميات في الواجهة الرئيسية فور إغلاق النافذة
            }
            catch (Exception ex)
            {
                MessageBox.Show("Impossible d'ouvrir la fenêtre : " + ex.Message);
            }
        }

        public void ApplyUserPermissions()
        {
            if (currentUserRole == "Cashier" || currentUserRole == "كاشير")
            {
                if (BtnAddUser != null) BtnAddUser.Visibility = Visibility.Collapsed;
                if (BtnLogs != null) BtnLogs.Visibility = Visibility.Collapsed;
                if (BtnSellersRevenue != null) BtnSellersRevenue.Visibility = Visibility.Collapsed;
            }
        }

        public void LoadInventoryData()
        {
            LoadInventoryProducts();
            LoadFinancialStatistics();
        }

        public void BtnShowInventory_Click(object sender, RoutedEventArgs e)
        {
            LoadInventoryData();
        }

        public void BtnShowSales_Click(object sender, RoutedEventArgs e)
        {
            SalesWindow salesWin = new SalesWindow();

            // 1. Masque la fenêtre principale
            this.Hide();

            // 2. Ouvre la fenêtre SalesWindow en mode dialogue
            salesWin.ShowDialog();

            // 3. Réaffiche la fenêtre principale dès que SalesWindow est fermée
            this.Show();
        }

        public void BtnShowLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogsWindow logsWin = new LogsWindow();

                // 1. Masque la fenêtre principale
                this.Hide();

                // 2. Ouvre la fenêtre LogsWindow en mode dialogue
                logsWin.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Impossible d'ouvrir la fenêtre des journaux : " + ex.Message);
            }
            finally
            {
                // 3. Réaffiche la fenêtre principale dès que LogsWindow est fermée (même en cas d'erreur)
                this.Show();
            }
        }

        public void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }

        public void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PerformSearch();
        }

        public void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        public void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            LoadInventoryData();
        }

        public void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        public void BtnShowAddUserWindow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddUserWindow addUserWin = new AddUserWindow();
                addUserWin.ShowDialog();
            }
            catch (Exception)
            {
                MessageBox.Show("La fenêtre d'ajout d'utilisateurs n'est pas disponible actuellement.", "Avertissement", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void BtnShowAddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string searchInput = CustomInputBox.Show("Entrez le code-barres ou le nom du produit à vérifier (laissez vide pour générer un code-barres automatique) :", "Ajouter / Modifier un produit", "");

                if (string.IsNullOrWhiteSpace(searchInput))
                {
                    string defaultBarcode = DateTime.Now.ToString("yyyyMMddHHmmss");
                    AddProductWindow addProdWinWithDefault = new AddProductWindow(defaultBarcode, "");
                    if (addProdWinWithDefault.ShowDialog() == true)
                    {
                        LoadInventoryData();
                    }
                    return;
                }

                searchInput = searchInput.Trim();

                string checkQuery = "SELECT Id, Code, Name, PurchasePrice, Quantity FROM Products WHERE Code = @search OR Name = @search";
                var checkParams = new Dictionary<string, object> { { "@search", searchInput } };
                DataTable dt = DatabaseHelper.ExecuteQuery(checkQuery, checkParams);

                if (dt != null && dt.Rows.Count > 0)
                {
                    string purchasePriceInput = CustomInputBox.Show("Entrez le prix d'achat du produit :", "Prix d'achat", "100");
                    if (!decimal.TryParse(purchasePriceInput, out decimal inputPurchasePrice)) inputPurchasePrice = 100.00m;

                    int existingId = Convert.ToInt32(dt.Rows[0]["Id"]);
                    string productName = dt.Rows[0]["Name"].ToString();
                    decimal existingPurchasePrice = Convert.ToDecimal(dt.Rows[0]["PurchasePrice"]);
                    int currentQty = Convert.ToInt32(dt.Rows[0]["Quantity"]);

                    if (existingPurchasePrice == inputPurchasePrice)
                    {
                        string qtyInput = CustomInputBox.Show($"Le produit ({productName}) existe avec le même prix d'achat ({existingPurchasePrice}).\nEntrez la quantité à ajouter :", "Mise à jour de la quantité", "1");
                        if (!int.TryParse(qtyInput, out int addedQty) || addedQty <= 0) addedQty = 1;

                        int newTotalQty = currentQty + addedQty;

                        string updateQuery = "UPDATE Products SET Quantity = @Quantity WHERE Id = @Id";
                        var updateParams = new Dictionary<string, object>
                        {
                            { "@Quantity", newTotalQty },
                            { "@Id", existingId }
                        };

                        DatabaseHelper.ExecuteNonQuery(updateQuery, updateParams);
                        LogsWindow.LogSystemAction(currentUsername, $"Mise à jour de la quantité du produit ({productName}) en ajoutant {addedQty}");
                        LoadInventoryData();

                        MessageBox.Show($"{addedQty} a été ajouté à la quantité, total pour le produit ({productName}) : {newTotalQty}", "Mise à jour du stock", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        string defaultGeneratedBarcode = "BAR-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                        AddProductWindow addProdWin = new AddProductWindow(defaultGeneratedBarcode, productName);
                        if (addProdWin.ShowDialog() == true)
                        {
                            LoadInventoryData();
                        }
                    }
                }
                else
                {
                    string generatedBarcode = DateTime.Now.ToString("yyyyMMddHHmmss");
                    AddProductWindow addProdWin = new AddProductWindow(generatedBarcode, searchInput);
                    if (addProdWin.ShowDialog() == true)
                    {
                        LoadInventoryData();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Une erreur s'est produite lors du traitement du produit : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public void LoadFinancialStatistics()
        {
            try
            {
                bool isCashier = (currentUserRole == "Cashier" || currentUserRole == "كاشير");
                string todayPrefix = DateTime.Now.ToString("yyyy-MM-dd");
                string currentMonthPrefix = DateTime.Now.ToString("yyyy-MM");

                string dailyQuery;
                var dailyParams = new Dictionary<string, object> { { "@Today", $"{todayPrefix}%" } };

                string monthlyRevenueQuery;
                var monthlyRevenueParams = new Dictionary<string, object> { { "@Month", $"{currentMonthPrefix}%" } };

                string monthlyProfitQuery;
                var monthlyProfitParams = new Dictionary<string, object> { { "@Month", $"{currentMonthPrefix}%" } };

                if (isCashier)
                {
                    dailyQuery = "SELECT SUM(FinalAmount) FROM Sales WHERE SaleDate LIKE @Today AND Status = 'Active' AND SoldBy = @Username";
                    dailyParams.Add("@Username", currentUsername);

                    monthlyRevenueQuery = "SELECT SUM(FinalAmount) FROM Sales WHERE SaleDate LIKE @Month AND Status = 'Active' AND SoldBy = @Username";
                    monthlyRevenueParams.Add("@Username", currentUsername);

                    monthlyProfitQuery = @"
                SELECT (
                    SELECT COALESCE(SUM(sl.FinalAmount), 0) 
                    FROM Sales sl 
                    WHERE sl.SaleDate LIKE @Month AND sl.Status = 'Active' AND sl.SoldBy = @Username
                ) - (
                    SELECT COALESCE(SUM(p.PurchasePrice * s.Quantity), 0) 
                    FROM SaleItems s 
                    JOIN Products p ON s.ProductId = p.Id 
                    JOIN Sales sl ON s.SaleId = sl.Id 
                    WHERE sl.SaleDate LIKE @Month AND sl.Status = 'Active' AND sl.SoldBy = @Username
                )";
                    monthlyProfitParams.Add("@Username", currentUsername);
                }
                else
                {
                    dailyQuery = "SELECT SUM(FinalAmount) FROM Sales WHERE SaleDate LIKE @Today AND Status = 'Active'";
                    monthlyRevenueQuery = "SELECT SUM(FinalAmount) FROM Sales WHERE SaleDate LIKE @Month AND Status = 'Active'";

                    monthlyProfitQuery = @"
                SELECT (
                    SELECT COALESCE(SUM(sl.FinalAmount), 0) 
                    FROM Sales sl 
                    WHERE sl.SaleDate LIKE @Month AND sl.Status = 'Active'
                ) - (
                    SELECT COALESCE(SUM(p.PurchasePrice * s.Quantity), 0) 
                    FROM SaleItems s 
                    JOIN Products p ON s.ProductId = p.Id 
                    JOIN Sales sl ON s.SaleId = sl.Id 
                    WHERE sl.SaleDate LIKE @Month AND sl.Status = 'Active'
                )";
                }

                var dailyResult = DatabaseHelper.ExecuteScalar(dailyQuery, dailyParams);
                if (dailyResult != null && dailyResult != DBNull.Value && TxtDailyRevenue != null)
                {
                    decimal dailyRevenue = Convert.ToDecimal(dailyResult);
                    TxtDailyRevenue.Text = dailyRevenue.ToString("N2");
                }
                else if (TxtDailyRevenue != null)
                {
                    TxtDailyRevenue.Text = "0.00";
                }

                var monthlyRevenueResult = DatabaseHelper.ExecuteScalar(monthlyRevenueQuery, monthlyRevenueParams);
                if (monthlyRevenueResult != null && monthlyRevenueResult != DBNull.Value)
                {
                    decimal totalMonthlyRevenue = Convert.ToDecimal(monthlyRevenueResult);
                    if (TxtMonthlyProfit != null)
                    {
                        TxtMonthlyProfit.Text = totalMonthlyRevenue.ToString("N2");
                    }
                }
                else
                {
                    if (TxtMonthlyProfit != null) TxtMonthlyProfit.Text = "0.00";
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la récupération des statistiques financières : " + ex.Message, "Erreur logique");
            }
        }

        public void BtnShowExpenses_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ExpensesWindow expensesWin = new ExpensesWindow();

                // 1. Masque la fenêtre principale
                this.Hide();

                // 2. Ouvre la fenêtre ExpensesWindow en mode dialogue
                expensesWin.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Impossible d'ouvrir la fenêtre des charges : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 3. Réaffiche la fenêtre principale dès que ExpensesWindow est fermée (même en cas d'erreur)
                this.Show();
            }
        }

        public void BtnReturnProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string inputInvoiceNumber = CustomInputBox.Show("Entrez le numéro de facture à retourner :", "Retour produit", "");
                if (string.IsNullOrWhiteSpace(inputInvoiceNumber)) return;

                inputInvoiceNumber = inputInvoiceNumber.Trim();

                string getSaleInfoQuery = "SELECT Id, FinalAmount, Status FROM Sales WHERE InvoiceNumber = @InvoiceNo";
                var saleParams = new Dictionary<string, object> { { "@InvoiceNo", inputInvoiceNumber } };

                DataTable saleDt = DatabaseHelper.ExecuteQuery(getSaleInfoQuery, saleParams);

                if (saleDt == null || saleDt.Rows.Count == 0)
                {
                    MessageBox.Show("Aucune facture trouvée avec ce numéro.", "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string currentStatus = saleDt.Rows[0]["Status"].ToString();
                if (currentStatus == "Returned")
                {
                    MessageBox.Show("Cette facture a déjà été retournée.", "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int saleId = Convert.ToInt32(saleDt.Rows[0]["Id"]);
                decimal saleAmount = Convert.ToDecimal(saleDt.Rows[0]["FinalAmount"]);

                string itemsQuery = "SELECT ProductId, Quantity FROM SaleItems WHERE SaleId = @SaleId";
                var saleIdParam = new Dictionary<string, object> { { "@SaleId", saleId } };
                DataTable dtItems = DatabaseHelper.ExecuteQuery(itemsQuery, saleIdParam);

                if (dtItems != null && dtItems.Rows.Count > 0)
                {
                    foreach (DataRow row in dtItems.Rows)
                    {
                        int productId = Convert.ToInt32(row["ProductId"]);
                        int returnedQty = Convert.ToInt32(row["Quantity"]);

                        string updateStockQuery = "UPDATE Products SET Quantity = Quantity + @Qty WHERE Id = @ProductId";
                        var stockParams = new Dictionary<string, object>
                        {
                            { "@Qty", returnedQty },
                            { "@ProductId", productId }
                        };
                        DatabaseHelper.ExecuteNonQuery(updateStockQuery, stockParams);
                    }

                    string updateSaleStatusQuery = "UPDATE Sales SET Status = 'Returned', FinalAmount = 0 WHERE Id = @SaleId";
                    DatabaseHelper.ExecuteNonQuery(updateSaleStatusQuery, saleIdParam);

                    LogsWindow.LogSystemAction(currentUsername, $"Retour de la facture n° : {inputInvoiceNumber} et conservation avec une valeur nulle");

                    LoadInventoryData();
                    UpdateUIAfterReturn(saleAmount);

                    MessageBox.Show($"La facture n° ({inputInvoiceNumber}) a été retournée avec succès, enregistrée comme annulée et sa valeur a été déduite.", "Retour effectué", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("La facture existe mais aucun produit n'y est enregistré.", "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Une erreur s'est produite lors du retour : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSelectAllProductsForInventory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                bool shouldSelectAll = true;

                if (sender is CheckBox chkBox)
                {
                    shouldSelectAll = chkBox.IsChecked ?? false;
                }

                if (ProductsGrid.ItemsSource is DataView dv)
                {
                    if (shouldSelectAll)
                    {
                        foreach (DataRowView row in dv)
                        {
                            if (!selectedInventoryItems.Contains(row))
                            {
                                selectedInventoryItems.Add(row);
                            }
                        }
                    }
                    else
                    {
                        foreach (DataRowView row in dv)
                        {
                            selectedInventoryItems.Remove(row);
                        }
                    }

                    RestoreSelectionVisuals();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la sélection de tout : " + ex.Message);
            }
        }

        private void BtnSelectProductForInventory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;
                if (btn != null)
                {
                    DataRowView row = btn.DataContext as DataRowView;
                    if (row != null)
                    {
                        if (!selectedInventoryItems.Contains(row))
                        {
                            selectedInventoryItems.Add(row);
                            btn.Content = "✓";
                            btn.Foreground = System.Windows.Media.Brushes.White;
                            btn.Background = new System.Windows.Media.BrushConverter().ConvertFrom("#10B981") as System.Windows.Media.Brush;
                        }
                        else
                        {
                            selectedInventoryItems.Remove(row);
                            btn.Content = string.Empty;
                            btn.Background = new System.Windows.Media.BrushConverter().ConvertFrom("#E2E8F0") as System.Windows.Media.Brush;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la sélection du produit : " + ex.Message);
            }
        }

        private void BtnPrintInventory_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedInventoryItems.Count == 0)
                {
                    MessageBox.Show("Veuillez sélectionner au moins un produit pour l'inventaire.", "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Ouvrir la fenêtre d'aperçu et de redimensionnement des colonnes avec la souris
                TableResizePreviewWindow previewWin = new TableResizePreviewWindow(selectedInventoryItems);
                if (previewWin.ShowDialog() == true)
                {
                    var customWidths = previewWin.ColumnWidths;

                    PrintDialog printDlg = new PrintDialog();
                    if (printDlg.ShowDialog() == true)
                    {
                        FlowDocument doc = new FlowDocument();
                        doc.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
                        doc.FlowDirection = FlowDirection.RightToLeft;
                        doc.PagePadding = new Thickness(40);
                        doc.ColumnWidth = double.PositiveInfinity;

                        Paragraph title = new Paragraph(new Run("Rapport d'inventaire du stock"))
                        {
                            FontSize = 18,
                            FontWeight = FontWeights.Bold,
                            TextAlignment = TextAlignment.Center
                        };
                        doc.Blocks.Add(title);

                        Paragraph datePar = new Paragraph(new Run("Date d'inventaire : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm")))
                        {
                            FontSize = 12,
                            TextAlignment = TextAlignment.Center
                        };
                        doc.Blocks.Add(datePar);

                        Table table = new Table();
                        table.CellSpacing = 0;
                        table.BorderBrush = System.Windows.Media.Brushes.Black;
                        table.BorderThickness = new Thickness(1);

                        // Appliquer les largeurs modifiées par l'utilisateur avec la souris
                        for (int i = 0; i < customWidths.Count; i++)
                        {
                            double w = customWidths.ContainsKey(i) ? customWidths[i] * 1.5 : 100; // Facteur de conversion simple pour s'adapter à la taille d'impression
                            table.Columns.Add(new TableColumn() { Width = new GridLength(w) });
                        }

                        TableRowGroup rowGroup = new TableRowGroup();

                        TableRow headerRow = new TableRow();
                        headerRow.Background = System.Windows.Media.Brushes.LightGray;

                        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Sélection")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(5) });
                        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Code-barres")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(5) });
                        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Nom du produit")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(5) });
                        headerRow.Cells.Add(new TableCell(new Paragraph(new Run("Quantité")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(5) });

                        rowGroup.Rows.Add(headerRow);

                        foreach (var item in selectedInventoryItems)
                        {
                            TableRow dataRow = new TableRow();

                            dataRow.Cells.Add(new TableCell(new Paragraph(new Run("✓")) { TextAlignment = TextAlignment.Center }) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(5) });
                            dataRow.Cells.Add(new TableCell(new Paragraph(new Run(item["Code"]?.ToString() ?? "")) { TextAlignment = TextAlignment.Center }) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(5) });
                            dataRow.Cells.Add(new TableCell(new Paragraph(new Run(item["Name"]?.ToString() ?? ""))) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(0, 0, 1, 1), Padding = new Thickness(5) });
                            dataRow.Cells.Add(new TableCell(new Paragraph(new Run(item["Quantity"]?.ToString() ?? "")) { TextAlignment = TextAlignment.Center }) { BorderBrush = System.Windows.Media.Brushes.Black, BorderThickness = new Thickness(0, 0, 0, 1), Padding = new Thickness(5) });

                            rowGroup.Rows.Add(dataRow);
                        }

                        table.RowGroups.Add(rowGroup);
                        doc.Blocks.Add(table);

                        IDocumentPaginatorSource idpSource = doc;
                        printDlg.PrintDocument(idpSource.DocumentPaginator, "Rapport d'inventaire des produits sélectionnés");

                        MessageBox.Show("La liste d'inventaire a été envoyée à l'imprimante avec succès.", "Impression", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Une erreur s'est produite lors de l'impression : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStockTotals()
        {
            decimal totalPurchase = 0;
            decimal totalSelling = 0;

            if (ProductsGrid.ItemsSource is DataView dv)
            {
                foreach (DataRowView row in dv)
                {
                    if (decimal.TryParse(row["PurchasePrice"]?.ToString(), out decimal pPrice) &&
                        decimal.TryParse(row["SellingPrice"]?.ToString(), out decimal sPrice) &&
                        int.TryParse(row["Quantity"]?.ToString(), out int qty))
                    {
                        totalPurchase += pPrice * qty;
                        totalSelling += sPrice * qty;
                    }
                }
            }

            TxtTotalPurchaseStock.Text = $"{totalPurchase:N2} DA";
            TxtTotalSellingStock.Text = $"{totalSelling:N2} DA";
        }

        private void UpdateUIAfterReturn(decimal deductedAmount)
        {
            try
            {
                LoadFinancialStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la mise à jour de l'interface : " + ex.Message);
            }
        }

        public void LoadInventoryProducts()
        {
            try
            {
                // أضفنا هنا (CASE WHEN Quantity <= MinQuantity THEN 2 ELSE 0 END) AS IsLowStock ليتعرف عليها البرنامج
                string query = "SELECT Id, Code, Name, Category, ExpiryDate, PurchasePrice, SellingPrice, Quantity, MinQuantity, (CASE WHEN Quantity <= MinQuantity THEN 1 ELSE 0 END) AS IsLowStock FROM Products";
                DataTable data = DatabaseHelper.ExecuteQuery(query);

                // إضافة عمود لاكتشاف قرب انتهاء الصلاحية
                if (!data.Columns.Contains("IsExpiringSoon"))
                {
                    data.Columns.Add("IsExpiringSoon", typeof(bool));
                }

                DateTime today = DateTime.Now;
                foreach (DataRow row in data.Rows)
                {
                    if (row["ExpiryDate"] != DBNull.Value && DateTime.TryParse(row["ExpiryDate"].ToString(), out DateTime expiryDate))
                    {
                        // إذا كان التاريخ قد انقضى أو يتبقى عليه أقل من شهر (30 يوماً)
                        if (expiryDate <= today.AddMonths(1))
                        {
                            row["IsExpiringSoon"] = true;
                        }
                        else
                        {
                            row["IsExpiringSoon"] = false;
                        }
                    }
                    else
                    {
                        row["IsExpiringSoon"] = false;
                    }
                }

                if (ProductsGrid != null)
                {
                    ProductsGrid.ItemsSource = data?.DefaultView;
                    UpdateStockTotals();
                    RestoreSelectionVisuals();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des produits : " + ex.Message);
            }
        }

        public void PerformSearch()
        {
            try
            {
                string searchText = SearchBox.Text.Trim();
                // أضفنا أيضاً شرط الـ IsLowStock هنا ليبقى التنبيه يعمل أثناء البحث
                string query = "SELECT Id, Code, Name, Category, PurchasePrice, SellingPrice, Quantity, MinQuantity, ExpiryDate, (CASE WHEN Quantity <= MinQuantity THEN 1 ELSE 0 END) AS IsLowStock FROM Products WHERE Name LIKE @search OR Code LIKE @search OR Category LIKE @search";
                var parameters = new Dictionary<string, object>
                {
                    { "@search", $"%{searchText}%" }
                };
                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);
                if (ProductsGrid != null)
                {
                    ProductsGrid.ItemsSource = dt.DefaultView;
                    UpdateStockTotals();
                    RestoreSelectionVisuals();
                }
            }
            catch (Exception) { }
        }

        private void RestoreSelectionVisuals()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var item in ProductsGrid.Items)
                {
                    if (item is DataRowView rowView)
                    {
                        var presenter = ProductsGrid.ItemContainerGenerator.ContainerFromItem(rowView) as DataGridRow;
                        if (presenter != null)
                        {
                            var button = FindVisualChild<Button>(presenter);
                            if (button != null)
                            {
                                if (selectedInventoryItems.Contains(rowView))
                                {
                                    button.Content = "✓";
                                    button.Foreground = System.Windows.Media.Brushes.White;
                                    button.Background = new System.Windows.Media.BrushConverter().ConvertFrom("#10B981") as System.Windows.Media.Brush;
                                }
                                else
                                {
                                    button.Content = string.Empty;
                                    button.Background = new System.Windows.Media.BrushConverter().ConvertFrom("#E2E8F0") as System.Windows.Media.Brush;
                                }
                            }
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    T descendant = FindVisualChild<T>(child);
                    if (descendant != null)
                        return descendant;
                }
            }
            return null;
        }

        public void BtnShowSellersRevenue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SellersRevenueWindow sellersWin = new SellersRevenueWindow();

                // 1. Masque la fenêtre principale
                this.Hide();

                // 2. Ouvre la fenêtre SellersRevenueWindow en mode dialogue
                sellersWin.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Impossible d'ouvrir la fenêtre des revenus des vendeurs : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // 3. Réaffiche la fenêtre principale dès que SellersRevenueWindow est fermée (même en cas d'erreur)
                this.Show();
            }
        }

        public void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
