using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WarehouseApp
{
    public partial class ExpensesWindow : Window
    {
        public ExpensesWindow()
        {
            InitializeComponent();
            EnsureExpensesTableExists();
            LoadExpensesData();
        }

        private void EnsureExpensesTableExists()
        {
            try
            {
                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Expenses (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Amount DECIMAL NOT NULL,
                        Type TEXT NOT NULL,
                        ExpenseDate TEXT NOT NULL
                    );";
                DatabaseHelper.ExecuteNonQuery(createTableQuery);

                try { DatabaseHelper.ExecuteNonQuery("ALTER TABLE Expenses ADD COLUMN Payé DECIMAL DEFAULT 0;"); } catch { }
                try { DatabaseHelper.ExecuteNonQuery("ALTER TABLE Expenses ADD COLUMN Reste DECIMAL DEFAULT 0;"); } catch { }
                try { DatabaseHelper.ExecuteNonQuery("ALTER TABLE Expenses ADD COLUMN PaymentDate TEXT;"); } catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la création de la table des charges : " + ex.Message);
            }
        }

        private void ExpensesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (ExpensesGrid.SelectedItem is DataRowView rowView)
                {
                    if (rowView["Id"] != null && int.TryParse(rowView["Id"].ToString(), out int id))
                    {
                        string nowDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                        string updateQuery = @"
                            UPDATE Expenses 
                            SET PaymentDate = @PaymentDate, 
                                Payé = Amount, 
                                Reste = 0 
                            WHERE Id = @Id";

                        var parameters = new Dictionary<string, object>
                        {
                            { "@PaymentDate", nowDateTime },
                            { "@Id", id }
                        };

                        DatabaseHelper.ExecuteNonQuery(updateQuery, parameters);

                        LoadExpensesData();

                        MessageBox.Show($"Le paiement de la charge a été validé pour la date et l'heure : {nowDateTime}", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la mise à jour du paiement : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadExpensesData()
        {
            try
            {
                string query = "SELECT Id, Name AS 'Nom de la charge', Amount AS 'Montant', COALESCE(Payé, 0) AS 'Payé', COALESCE(Reste, 0) AS 'Reste', Type AS 'Type', ExpenseDate AS 'Date', PaymentDate AS 'Date de paiement total' FROM Expenses ORDER BY Id DESC";
                DataTable dt = DatabaseHelper.ExecuteQuery(query);

                if (ExpensesGrid != null)
                {
                    ExpensesGrid.ItemsSource = dt?.DefaultView;
                }

                decimal total = 0;
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        total += Convert.ToDecimal(row["Montant"]);
                    }
                }

                if (TxtTotalExpenses != null)
                {
                    TxtTotalExpenses.Text = total.ToString("N2") + " DA";
                }

                string currentDayPrefix = DateTime.Now.ToString("yyyy-MM-dd");
                string currentMonthPrefix = DateTime.Now.ToString("yyyy-MM");
                string currentYearPrefix = DateTime.Now.ToString("yyyy");

                string dailyQuery = "SELECT SUM(Amount) FROM Expenses WHERE ExpenseDate LIKE @Day";
                var dailyParams = new Dictionary<string, object> { { "@Day", $"{currentDayPrefix}%" } };
                var dailyResult = DatabaseHelper.ExecuteScalar(dailyQuery, dailyParams);
                decimal dailyTotal = (dailyResult != null && dailyResult != DBNull.Value) ? Convert.ToDecimal(dailyResult) : 0;

                string monthlyQuery = "SELECT SUM(Amount) FROM Expenses WHERE ExpenseDate LIKE @Month";
                var monthlyParams = new Dictionary<string, object> { { "@Month", $"{currentMonthPrefix}%" } };
                var monthlyResult = DatabaseHelper.ExecuteScalar(monthlyQuery, monthlyParams);
                decimal monthlyTotal = (monthlyResult != null && monthlyResult != DBNull.Value) ? Convert.ToDecimal(monthlyResult) : 0;

                string yearlyQuery = "SELECT SUM(Amount) FROM Expenses WHERE ExpenseDate LIKE @Year";
                var yearlyParams = new Dictionary<string, object> { { "@Year", $"{currentYearPrefix}%" } };
                var yearlyResult = DatabaseHelper.ExecuteScalar(yearlyQuery, yearlyParams);
                decimal yearlyTotal = (yearlyResult != null && yearlyResult != DBNull.Value) ? Convert.ToDecimal(yearlyResult) : 0;

                if (TxtDailyExpenses != null) TxtDailyExpenses.Text = dailyTotal.ToString("N2") + " DA";
                if (TxtMonthlyExpenses != null) TxtMonthlyExpenses.Text = monthlyTotal.ToString("N2") + " DA";
                if (TxtYearlyExpenses != null) TxtYearlyExpenses.Text = yearlyTotal.ToString("N2") + " DA";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des charges : " + ex.Message);
            }
        }

        private void BtnAddExpense_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (TxtExpenseName == null || TxtExpenseAmount == null || TxtExpensePaye == null || CmbExpenseType == null)
                {
                    MessageBox.Show("Éléments d'interface non chargés.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string name = TxtExpenseName.Text.Trim();
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("Veuillez entrer le nom de la charge.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!decimal.TryParse(TxtExpenseAmount.Text.Trim(), out decimal amount) || amount < 0)
                {
                    MessageBox.Show("Veuillez entrer un montant valide.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal paye = 0;
                if (!string.IsNullOrWhiteSpace(TxtExpensePaye.Text))
                {
                    if (!decimal.TryParse(TxtExpensePaye.Text.Trim(), out paye) || paye < 0)
                    {
                        MessageBox.Show("Veuillez entrer un montant payé valide.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                if (paye > amount)
                {
                    MessageBox.Show("Le montant payé ne peut pas dépasser le montant total.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                decimal reste = amount - paye;

                string type = (CmbExpenseType.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Mensuel";
                string expenseDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                string paymentDate = null;
                var dpControl = this.FindName("DpPaymentDate") as DatePicker;
                if (dpControl != null && dpControl.SelectedDate.HasValue)
                {
                    paymentDate = dpControl.SelectedDate.Value.ToString("yyyy-MM-dd");
                }

                string insertQuery = "INSERT INTO Expenses (Name, Amount, Payé, Reste, Type, ExpenseDate, PaymentDate) VALUES (@Name, @Amount, @Payé, @Reste, @Type, @ExpenseDate, @PaymentDate)";
                var parameters = new Dictionary<string, object>
                {
                    { "@Name", name },
                    { "@Amount", amount },
                    { "@Payé", paye },
                    { "@Reste", reste },
                    { "@Type", type },
                    { "@ExpenseDate", expenseDate },
                    { "@PaymentDate", (object)paymentDate ?? DBNull.Value }
                };

                DatabaseHelper.ExecuteNonQuery(insertQuery, parameters);

                TxtExpenseName.Clear();
                TxtExpenseAmount.Clear();
                TxtExpensePaye.Clear();
                if (dpControl != null)
                {
                    dpControl.SelectedDate = null;
                }

                LoadExpensesData();

                MessageBox.Show("Charge ajoutée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'ajout : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}