using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AkhmetshinChudoShoes
{
    public partial class OrderPage : Page
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();
        private List<Categorys> _categories = new List<Categorys>();
        private bool _isOpeningProduct = false;

        public OrderPage()
        {
            InitializeComponent();
            LoadCategories();
            UpdateOrder();
        }

        private void LoadCategories()
        {
            _categories = _context.Categorys.OrderBy(c => c.Category).ToList();
            ComboFilter.Items.Clear();
            ComboFilter.Items.Add("Все категории");
            foreach (var category in _categories)
            {
                ComboFilter.Items.Add(category.Category);
            }
            ComboFilter.SelectedIndex = 0;
        }

        private void UpdateOrder()
        {
            if (TBoxSearch == null || ComboSort == null || ComboFilter == null || OrderListView == null)
                return;

            var current = _context.Products.ToList();

            DateTime lastMonth = DateTime.Now.AddMonths(-1);

            var productsWithOrders = _context.OrderProducts
                .Where(op => op.Orders.OrderDate.Month == lastMonth.Month &&
                             op.Orders.OrderDate.Year == lastMonth.Year)
                .Select(op => op.StockItems.ProductID)
                .Distinct()
                .ToList();

            foreach (var product in current)
            {
                product.HasOrdersLastMonth = productsWithOrders.Contains(product.ProductID);
            }

            // Поиск по наименованию и описанию в реальном времени
            if (!string.IsNullOrWhiteSpace(TBoxSearch.Text))
            {
                string text = TBoxSearch.Text.ToLower().Trim();
                current = current.Where(p =>
                    p.ProductName.ToLower().Contains(text) ||
                    (p.ProductDescription != null && p.ProductDescription.ToLower().Contains(text))
                ).ToList();
            }

            // Фильтрация по категории из БД ("Все категории" имеет индекс 0)
            if (ComboFilter.SelectedIndex > 0 && ComboFilter.SelectedIndex <= _categories.Count)
            {
                int catId = _categories[ComboFilter.SelectedIndex - 1].CategoryID;
                current = current.Where(p => p.ProductCategoryID == catId).ToList();
            }

            // Сортировка по цене с сохранением при поиске и фильтрации
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

        private void OrderListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isOpeningProduct) return;
            var selectedProduct = OrderListView.SelectedItem as Products;
            if (selectedProduct == null) return;

            OpenProductOrderWindow(selectedProduct);
        }

        private void OrderBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedProduct = (sender as Button)?.DataContext as Products;
            if (selectedProduct == null) return;

            OpenProductOrderWindow(selectedProduct);
        }

        private void OpenProductOrderWindow(Products selectedProduct)
        {
            if (_isOpeningProduct) return;

            try
            {
                _isOpeningProduct = true;

                if (Manager.CurrentUser == null)
                {
                    MessageBox.Show(
                        "Для просмотра карточки товара и оформления заказа необходимо войти в систему по логину.",
                        "Требуется авторизация",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var hasStock = _context.StockItems.Any(si => si.ProductID == selectedProduct.ProductID && si.QuantityForOrders > 0);
                if (!hasStock)
                {
                    MessageBox.Show(
                        "Выбранная модель сейчас недоступна для заказа (нет пар в наличии). Выберите другую модель или размер.",
                        "Нет в наличии",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                SelectSizeWindow window = new SelectSizeWindow(selectedProduct);
                window.ShowDialog();
                UpdateOrder();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось открыть форму товара: {ex.Message}\nПроверьте подключение к базе данных и повторите попытку.",
                    "Ошибка открытия",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                OrderListView.SelectedItem = null;
                _isOpeningProduct = false;
            }
        }
    }
}