using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace WarehouseApp
{
    public class ReturnItemModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
    }

    public partial class ReturnItemsSelectionWindow : Window
    {
        public ReturnItemModel SelectedItem { get; private set; }

        public ReturnItemsSelectionWindow(List<ReturnItemModel> items)
        {
            InitializeComponent();
            ProductsGrid.ItemsSource = items;
        }

        private void ProductsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ProductsGrid.SelectedItem is ReturnItemModel item)
            {
                SelectedItem = item;
                DialogResult = true;
                Close();
            }
        }
    }
}