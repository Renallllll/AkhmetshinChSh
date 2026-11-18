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

            // ТВОЙ РОДНОЙ КОД
            var current = _context.Products.ToList();

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

            // ТВОЙ РОДНОЙ ВЫВОД
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

    // МАГИЯ: Расширяем модель БД, чтобы XAML видел новые поля, а база не ломалась
    public partial class Products
    {

        public int TotalStock { get { return StockItems != null ? StockItems.Sum(s => s.QuantityForOrders) : 0; } }
        public string StockText { get { return TotalStock > 5 ? "Много" : "Мало"; } }
        public bool IsLowStock { get { return TotalStock <= 3; } }

        public decimal FinalPrice
        {
            get
            {
                var context = AkhmetshinDBShoesEntities.GetContext();
                DateTime lastMonth = DateTime.Now.AddMonths(-1);
                bool hasOrders = context.OrderProducts.Any(op => op.StockItems.ProductID == ProductID &&
                                                                  op.Orders.OrderDate.Month == lastMonth.Month &&
                                                                  op.Orders.OrderDate.Year == lastMonth.Year);
                return hasOrders ? ProductPrice : ProductPrice * 0.75m;
            }
        }
    }
}