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

            if (selectedProduct == null)
            {
                MessageBox.Show("Не выбран товар для просмотра.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            // Загружаем актуальные данные товара и связанных сущностей из БД
            var product = _context.Products.FirstOrDefault(p => p.ProductID == selectedProduct.ProductID) ?? selectedProduct;

            Title = $"Товар: {product.ProductName}";
            TBlockProductName.Text = $"{product.ManufacturerName} | {product.ProductName}";

            try
            {
                ImgProduct.Source = new System.Windows.Media.Imaging.BitmapImage(
                    new Uri(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, product.ProductPhoto.Replace('/', '\\'))));
            }
            catch
            {
                ImgProduct.Source = null;
            }

            TBlockManufacturer.Text = $"Производство: {product.ManufacturerName}";
            TBlockCategory.Text = $"Категория: {product.CategoryName}";

            if (product.HasOrdersLastMonth)
                TBlockPrice.Text = $"Цена со скидкой: {product.FinalPrice} руб.";
            else
                TBlockPrice.Text = $"Цена со скидкой: {product.FinalPrice} руб. (базовая: {product.ProductPrice} руб.)";

            TBlockStructure.Text = $"Состав: {product.ProductStructure}";
            TBlockDescription.Text = $"Описание: {product.ProductDescription}";

            // Весь размерный ряд товара из БД
            var allProductSizes = _context.StockItems
                .Where(si => si.ProductID == product.ProductID)
                .Select(si => si.Sizes.Size)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            TBlockSizeRange.Text = allProductSizes.Count > 0
                ? $"Размерный ряд: {string.Join(", ", allProductSizes)}"
                : "Размерный ряд: не указан";

            // Только размеры, доступные для заказа
            var availableSizes = _context.StockItems
                .Where(si => si.ProductID == product.ProductID && si.QuantityForOrders > 0)
                .OrderBy(si => si.Sizes.Size)
                .ToList();

            SizesGrid.ItemsSource = availableSizes;
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var selectedStockItem = SizesGrid.SelectedItem as StockItems;

            if (selectedStockItem == null)
            {
                MessageBox.Show("Выберите размер из списка доступных размеров!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TBoxQuantity.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Введите корректное количество пар (целое число больше 0)!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var existingItem = Manager.Cart.FirstOrDefault(c => c.StockItem.PositionID == selectedStockItem.PositionID);
            int currentInCart = existingItem != null ? existingItem.Quantity : 0;

            if (currentInCart + quantity > selectedStockItem.QuantityForOrders)
            {
                MessageBox.Show($"На складе недостаточно пар! Доступно для добавления: {selectedStockItem.QuantityForOrders - currentInCart}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}