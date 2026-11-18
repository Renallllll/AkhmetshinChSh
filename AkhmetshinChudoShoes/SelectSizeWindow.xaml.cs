using System;
using System.Linq;
using System.Windows;

namespace AkhmetshinChudoShoes
{
    public partial class SelectSizeWindow : Window
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();

        public SelectSizeWindow(Products selectedProduct)
        {
            InitializeComponent();
            TBlockProductName.Text = selectedProduct.ProductName;

            var availableSizes = _context.StockItems
                .Where(si => si.ProductID == selectedProduct.ProductID && si.QuantityForOrders > 0)
                .ToList();

            SizesGrid.ItemsSource = availableSizes;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var selectedStockItem = SizesGrid.SelectedItem as StockItems;

            if (selectedStockItem == null)
            {
                MessageBox.Show("Выберите размер!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TBoxQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var existingItem = Manager.Cart.FirstOrDefault(c => c.StockItem.PositionID == selectedStockItem.PositionID);
            int currentInCart = existingItem != null ? existingItem.Quantity : 0;

            if (currentInCart + quantity > selectedStockItem.QuantityForOrders)
            {
                MessageBox.Show($"На складе недостаточно! Доступно: {selectedStockItem.QuantityForOrders - currentInCart}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Manager.Cart.Add(new CartItem { StockItem = selectedStockItem, Quantity = quantity });
            }

            MessageBox.Show($"Добавлено: {quantity} шт.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}