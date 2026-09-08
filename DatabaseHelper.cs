using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Xml;
using Microsoft.Data.Sqlite;

namespace WarehouseApp
{
    public static class DatabaseHelper
    {
        private static string _connectionString;
        private static readonly string DatabasePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Data",
            "shop.db"
        );

        static DatabaseHelper()
        {
            string dataDir = Path.GetDirectoryName(DatabasePath);

            if (!string.IsNullOrEmpty(dataDir) && !Directory.Exists(dataDir))
                Directory.CreateDirectory(dataDir);

            _connectionString = $"Data Source={DatabasePath}";
            InitializeDatabase();
        }

        public static string ConnectionString => _connectionString;

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        public static void InitializeDatabase()
        {
            string schema = @"
                CREATE TABLE IF NOT EXISTS Products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Code TEXT UNIQUE NOT NULL,
                    Name TEXT NOT NULL,
                    Category TEXT,
                    PurchasePrice REAL,
                    SellingPrice REAL NOT NULL,
                    Quantity INTEGER DEFAULT 0,
                    MinQuantity INTEGER DEFAULT 5,
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS Customers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Phone TEXT,
                    Email TEXT,
                    Address TEXT
                );

                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT UNIQUE NOT NULL,
                    Password TEXT NOT NULL,
                    Role TEXT DEFAULT 'موظف',
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS Sales (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceNumber TEXT UNIQUE NOT NULL,
                    CustomerId INTEGER,
                    SaleDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    TotalAmount REAL,
                    Discount REAL DEFAULT 0,
                    FinalAmount REAL,
                    PaymentMethod TEXT,
                    Status TEXT DEFAULT 'Active',
                    FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
                );

                CREATE TABLE IF NOT EXISTS SaleItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SaleId INTEGER,
                    ProductId INTEGER,
                    Quantity INTEGER,
                    UnitPrice REAL,
                    TotalPrice REAL,
                    PurchasePrice DECIMAL(18,2) DEFAULT 0,
                    FOREIGN KEY (SaleId) REFERENCES Sales(Id) ON DELETE CASCADE,
                    FOREIGN KEY (ProductId) REFERENCES Products(Id)
                );

                CREATE TABLE IF NOT EXISTS Returns (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    InvoiceNumber TEXT,
                    ProductId INTEGER,
                    Quantity INTEGER,
                    TotalAmount REAL,
                    ReturnDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (ProductId) REFERENCES Products(Id)
                );

                CREATE TABLE IF NOT EXISTS SystemLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT,
                    Action TEXT,
                    LogDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
            ";

            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqliteCommand(schema, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            // تحديثات الجداول الحالية وإضافة الأعمدة إذا لم تكن موجودة مسبقاً
            try
            {
                DatabaseHelper.ExecuteNonQuery("ALTER TABLE Sales ADD COLUMN SoldBy TEXT;");
            }
            catch { }

            try
            {
                DatabaseHelper.ExecuteNonQuery("ALTER TABLE Sales ADD COLUMN Status TEXT DEFAULT 'Active';");
            }
            catch { }

            try
            {
                DatabaseHelper.ExecuteNonQuery("ALTER TABLE Sales ADD COLUMN Discount REAL DEFAULT 0;");
            }
            catch { }

            try
            {
                DatabaseHelper.ExecuteNonQuery("ALTER TABLE Products ADD COLUMN PurchasePrice DECIMAL(18,2) DEFAULT 0;");
            }
            catch { }

            try
            {
                DatabaseHelper.ExecuteNonQuery("ALTER TABLE SaleItems ADD COLUMN PurchasePrice DECIMAL(18,2) DEFAULT 0;");
            }
            catch { }

            try
            {
                DatabaseHelper.ExecuteNonQuery("ALTER TABLE Products ADD COLUMN ExpiryDate TEXT;");
            }
            catch { }

            using (var conn = GetConnection())
            {
                conn.Open();
                string query = "INSERT OR IGNORE INTO Users (Username, Password, Role) VALUES ('DEV', 'Hesoyam.1996', 'DEV')";
                using (var cmd = new SqliteCommand(query, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static bool ProcessSqliteReturn(string invoiceNumber, int productId, int quantityToReturn, decimal itemPrice)
        {
            using (var connection = GetConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string updateStock = "UPDATE Products SET Quantity = Quantity + @Quantity WHERE Id = @ProductId";
                        using (var cmd = new SqliteCommand(updateStock, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@Quantity", quantityToReturn);
                            cmd.Parameters.AddWithValue("@ProductId", productId);
                            cmd.ExecuteNonQuery();
                        }

                        string insertReturn = @"INSERT INTO Returns (InvoiceNumber, ProductId, Quantity, TotalAmount, ReturnDate) 
                                                VALUES (@InvoiceNumber, @ProductId, @Quantity, @TotalAmount, datetime('now', 'localtime'))";
                        using (var cmd = new SqliteCommand(insertReturn, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@InvoiceNumber", invoiceNumber);
                            cmd.Parameters.AddWithValue("@ProductId", productId);
                            cmd.Parameters.AddWithValue("@Quantity", quantityToReturn);
                            cmd.Parameters.AddWithValue("@TotalAmount", quantityToReturn * itemPrice);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception("خطأ أثناء معالجة المردود: " + ex.Message);
                    }
                }
            }
        }

        public static DataTable ExecuteQuery(string query, Dictionary<string, object> parameters = null)
        {
            DataTable dt = new DataTable();
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqliteCommand(query, conn))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    using (var reader = cmd.ExecuteReader())
                    {
                        dt.Load(reader);
                    }
                }
            }
            return dt;
        }

        public static int ExecuteNonQuery(string query, Dictionary<string, object> parameters = null)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqliteCommand(query, conn))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public static object ExecuteScalar(string query, Dictionary<string, object> parameters = null)
        {
            using (var conn = GetConnection())
            {
                conn.Open();
                using (var cmd = new SqliteCommand(query, conn))
                {
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    return cmd.ExecuteScalar();
                }
            }
        }

        public static long GetLastInsertRowId()
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                using (var command = new SqliteCommand("SELECT last_insert_rowid();", connection))
                {
                    return Convert.ToInt64(command.ExecuteScalar());
                }
            }
        }
    }
}