using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Input;

namespace WarehouseApp
{
    public partial class DamagedProductsWindow : Window
    {
        private string currentUsername;

        public DamagedProductsWindow(string username = "admin")
        {
            InitializeComponent();
            currentUsername = username;
            InitializeDamagedTableAndCleanup();
            LoadDamagedProducts();
        }

        private void InitializeDamagedTableAndCleanup()
        {
            try
            {
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS DamagedProducts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ProductId INTEGER,
                        ProductCode TEXT,
                        ProductName TEXT,
                        Quantity INTEGER,
                        PurchasePrice REAL,
                        Reason TEXT,
                        DamageDate TEXT,
                        RawDate TEXT
                    );";
                DatabaseHelper.ExecuteNonQuery(createTableQuery);

                string oneYearAgo = DateTime.Now.AddYears(-1).ToString("yyyy-MM-dd HH:mm");
                string deleteOldQuery = "DELETE FROM DamagedProducts WHERE RawDate < @OneYearAgo;";
                var paramsDelete = new Dictionary<string, object> { { "@OneYearAgo", oneYearAgo } };
                DatabaseHelper.ExecuteNonQuery(deleteOldQuery, paramsDelete);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur d'initialisation : " + ex.Message);
            }
        }

        private void LoadDamagedProducts()
        {
            try
            {
                string query = "SELECT * FROM DamagedProducts ORDER BY Id DESC";
                DataTable dt = DatabaseHelper.ExecuteQuery(query);

                if (!dt.Columns.Contains("TotalPurchasePrice"))
                {
                    dt.Columns.Add("TotalPurchasePrice", typeof(decimal));
                }

                decimal grandTotalPurchase = 0;

                foreach (DataRow row in dt.Rows)
                {
                    decimal pPrice = row["PurchasePrice"] != DBNull.Value ? Convert.ToDecimal(row["PurchasePrice"]) : 0;
                    int qty = row["Quantity"] != DBNull.Value ? Convert.ToInt32(row["Quantity"]) : 0;
                    decimal totalItemPrice = pPrice * qty;

                    row["TotalPurchasePrice"] = totalItemPrice;
                    grandTotalPurchase += totalItemPrice;
                }

                if (DamagedGrid != null)
                {
                    DamagedGrid.ItemsSource = dt.DefaultView;
                }

                if (TxtTotalDamagePurchase != null)
                {
                    TxtTotalDamagePurchase.Text = $"{grandTotalPurchase:N2} DA";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement : " + ex.Message);
            }
        }

        private void TxtSearchProduct_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                string keyword = TxtSearchProduct.Text.Trim();
                if (string.IsNullOrEmpty(keyword) || keyword.Length < 1)
                {
                    SuggestionsPopup.IsOpen = false;
                    return;
                }

                string query = "SELECT Name, Code FROM Products WHERE Name LIKE @kw OR Code LIKE @kw LIMIT 10";
                var parameters = new Dictionary<string, object>
                {
                    { "@kw", $"%{keyword}%" }
                };

                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);
                if (dt != null && dt.Rows.Count > 0)
                {
                    List<string> suggestions = new List<string>();
                    foreach (DataRow row in dt.Rows)
                    {
                        suggestions.Add($"{row["Name"]} (Code: {row["Code"]})");
                    }
                    SuggestionsList.ItemsSource = suggestions;
                    SuggestionsPopup.IsOpen = true;
                }
                else
                {
                    SuggestionsPopup.IsOpen = false;
                }
            }
            catch (Exception)
            {
                SuggestionsPopup.IsOpen = false;
            }
        }

        private void SuggestionsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SuggestionsList.SelectedItem != null)
            {
                string selectedText = SuggestionsList.SelectedItem.ToString();
                if (selectedText.Contains("(Code: "))
                {
                    string productName = selectedText.Substring(0, selectedText.IndexOf("(Code: ")).Trim();
                    TxtSearchProduct.Text = productName;
                }
                else
                {
                    TxtSearchProduct.Text = selectedText;
                }

                SuggestionsPopup.IsOpen = false;
                TxtDamageQty.Focus();
                TxtDamageQty.SelectAll();
            }
        }

        private void TxtSearchProduct_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SuggestionsPopup.IsOpen = false;
                BtnAddToDamageCart_Click(sender, e);
            }
        }

        private void BtnAddToDamageCart_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SuggestionsPopup.IsOpen = false;
                string searchInput = TxtSearchProduct.Text.Trim();
                if (string.IsNullOrWhiteSpace(searchInput))
                {
                    MessageBox.Show("Veuillez entrer le code-barres ou le nom du produit.", "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(TxtDamageQty.Text.Trim(), out int damagedQty) || damagedQty <= 0)
                {
                    damagedQty = 1;
                }

                // قراءة السبب من الـ ComboBox (Cassé أو Périmé)
                string reason = (CmbDamageReason.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content.ToString() ?? "Cassé";

                string checkQuery = "SELECT Id, Code, Name, PurchasePrice, Quantity FROM Products WHERE Code = @search OR Name = @search";
                var paramsCheck = new Dictionary<string, object> { { "@search", searchInput } };
                DataTable dt = DatabaseHelper.ExecuteQuery(checkQuery, paramsCheck);

                if (dt == null || dt.Rows.Count == 0)
                {
                    MessageBox.Show("Aucun produit trouvé avec ce nom ou code-barres.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int productId = Convert.ToInt32(dt.Rows[0]["Id"]);
                string code = dt.Rows[0]["Code"].ToString();
                string name = dt.Rows[0]["Name"].ToString();
                decimal purchasePrice = Convert.ToDecimal(dt.Rows[0]["PurchasePrice"]);
                int currentQty = Convert.ToInt32(dt.Rows[0]["Quantity"]);

                if (damagedQty > currentQty)
                {
                    MessageBox.Show($"La quantité demandée ({damagedQty}) dépasse le stock actuel ({currentQty}) !", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string updateStock = "UPDATE Products SET Quantity = Quantity - @Qty WHERE Id = @Id";
                var updateParams = new Dictionary<string, object>
                {
                    { "@Qty", damagedQty },
                    { "@Id", productId }
                };
                DatabaseHelper.ExecuteNonQuery(updateStock, updateParams);

                string insertDamage = @"
                    INSERT INTO DamagedProducts (ProductId, ProductCode, ProductName, Quantity, PurchasePrice, Reason, DamageDate, RawDate) 
                    VALUES (@PID, @PCode, @PName, @Qty, @Price, @Reason, @DateFormatted, @DateRaw)";

                var insertParams = new Dictionary<string, object>
                {
                    { "@PID", productId },
                    { "@PCode", code },
                    { "@PName", name },
                    { "@Qty", damagedQty },
                    { "@Price", purchasePrice },
                    { "@Reason", reason },
                    { "@DateFormatted", DateTime.Now.ToString("yyyy-MM-dd HH:mm") },
                    { "@DateRaw", DateTime.Now.ToString("yyyy-MM-dd HH:mm") }
                };
                DatabaseHelper.ExecuteNonQuery(insertDamage, insertParams);

                LogsWindow.LogSystemAction(currentUsername, $"Ajout au panier des endommagés : {damagedQty} unités de ({name}) - Raison: {reason}");

                TxtSearchProduct.Text = string.Empty;
                TxtDamageQty.Text = "1";
                CmbDamageReason.SelectedIndex = 0;
                TxtSearchProduct.Focus();
                LoadDamagedProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteSelectedDamage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DamagedGrid.SelectedItem is DataRowView rowView)
                {
                    int recordId = Convert.ToInt32(rowView["Id"]);
                    int productId = Convert.ToInt32(rowView["ProductId"]);
                    int qtyToRestore = Convert.ToInt32(rowView["Quantity"]);

                    MessageBoxResult result = MessageBox.Show("Voulez-vous supprimer cet élément et restaurer sa quantité dans le stock ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        string restoreStock = "UPDATE Products SET Quantity = Quantity + @Qty WHERE Id = @PID";
                        var restoreParams = new Dictionary<string, object>
                        {
                            { "@Qty", qtyToRestore },
                            { "@PID", productId }
                        };
                        DatabaseHelper.ExecuteNonQuery(restoreStock, restoreParams);

                        string deleteQuery = "DELETE FROM DamagedProducts WHERE Id = @ID";
                        var deleteParams = new Dictionary<string, object> { { "@ID", recordId } };
                        DatabaseHelper.ExecuteNonQuery(deleteQuery, deleteParams);

                        LoadDamagedProducts();
                        MessageBox.Show("L'élément a été supprimé et la quantité a été restaurée dans le stock.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Veuillez sélectionner un élément dans le tableau.", "Avertissement", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}