using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AkhmetshinChudoShoes
{
    public partial class OrderPage : Page
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();

        public OrderPage()
        {
            InitializeComponent();
            ComboFilter.Items.Add("Все категории");
            ComboFilter.Items.Add("Детская обувь");
            ComboFilter.Items.Add("Женская обувь");
            ComboFilter.Items.Add("Мужская обувь");
            ComboFilter.SelectedIndex = 0;
            UpdateOrder();
        }

        private void UpdateOrder()
        {
            if (TBoxSearch == null || ComboSort == null || ComboFilter == null || OrderListView == null)
                return;

            var current = _context.Products.ToList();

            DateTime lastMonth = DateTime.Now.AddMonths(-1);

            // Находим ID товаров, на которые БЫЛИ заказы в прошлом месяце
            var productsWithOrders = _context.OrderProducts
                .Where(op => op.Orders.OrderDate.Month == lastMonth.Month &&
                             op.Orders.OrderDate.Year == lastMonth.Year)
                .Select(op => op.StockItems.ProductID)
                .Distinct()
                .ToList();

            // Проставляем флаг каждому товару
            foreach (var product in current)
            {
                product.HasOrdersLastMonth = productsWithOrders.Contains(product.ProductID);
            }

            if (!string.IsNullOrWhiteSpace(TBoxSearch.Text))
            {
                string text = TBoxSearch.Text.ToLower().Trim();
                current = current.Where(p =>
                    p.ProductName.ToLower().Contains(text) ||
                    (p.ProductDescription != null && p.ProductDescription.ToLower().Contains(text))
                ).ToList();
            }

            if (ComboFilter.SelectedIndex > 0)
            {
                int catId = ComboFilter.SelectedIndex;
                current = current.Where(p => p.ProductCategoryID == catId).ToList();
            }

            if (ComboSort.SelectedIndex == 1)
                current = current.OrderBy(p => p.FinalPrice).ToList();
            else if (ComboSort.SelectedIndex == 2)
                current = current.OrderByDescending(p => p.FinalPrice).ToList();

            OrderListView.ItemsSource = current;
        }

        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e) => UpdateOrder();
        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateOrder();
        private void ComboFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateOrder();

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                if (Manager.CurrentUser == null) FilterPanel.Visibility = Visibility.Collapsed;
                else FilterPanel.Visibility = Visibility.Visible;
                UpdateOrder();
            }
        }

        private void OrderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Manager.CurrentUser == null)
            {
                MessageBox.Show("Для оформления заказа необходимо авторизоваться!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedProduct = (sender as Button).DataContext as Products;
            if (selectedProduct == null) return;

            var hasStock = _context.StockItems.Any(si => si.ProductID == selectedProduct.ProductID && si.QuantityForOrders > 0);

            if (!hasStock)
            {
                MessageBox.Show("Этого товара сейчас нет в наличии!", "Нет в наличии", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SelectSizeWindow window = new SelectSizeWindow(selectedProduct);
            window.ShowDialog();
        }
    }
}