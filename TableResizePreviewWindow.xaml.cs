using System.Collections.Generic;
using System.Data;
using System.Windows;

namespace WarehouseApp
{
    public partial class TableResizePreviewWindow : Window
    {
        public Dictionary<int, double> ColumnWidths { get; private set; } = new Dictionary<int, double>();
        private List<DataRowView> _items;

        public TableResizePreviewWindow(List<DataRowView> items)
        {
            InitializeComponent();
            _items = items;
            LoadDataIntoGrid();
        }

        private void LoadDataIntoGrid()
        {
            var displayList = new List<object>();
            foreach (var item in _items)
            {
                displayList.Add(new
                {
                    Selection = "✓",
                    Code = item["Code"]?.ToString() ?? "",
                    Name = item["Name"]?.ToString() ?? "",
                    Quantity = item["Quantity"]?.ToString() ?? ""
                });
            }
            PreviewDataGrid.ItemsSource = displayList;
        }

        private void BtnPrintNow_Click(object sender, RoutedEventArgs e)
        {
            // Enregistrer les largeurs actuelles des colonnes après les avoir modifiées avec la souris[cite: 3]
            for (int i = 0; i < PreviewDataGrid.Columns.Count; i++)
            {
                ColumnWidths[i] = PreviewDataGrid.Columns[i].ActualWidth;
            }

            this.DialogResult = true;
            this.Close();
        }
    }
}