using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AkhmetshinChudoShoes
{
    public partial class OrderCompositionWindow : Window
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();

        private Orders _currentOrder;
        private bool _isLoading = true;

        public OrderCompositionWindow(Orders selectedOrder)
        {
            InitializeComponent();
            _currentOrder = selectedOrder;

            DatePickerOrder.SelectedDate = selectedOrder.OrderDate;

            if (Manager.CurrentUser == null || Manager.CurrentUser.RoleID != 1)
            {
                DatePickerOrder.Visibility = Visibility.Hidden;
                BtnDeleteItem.Visibility = Visibility.Hidden;
                BtnSaveDate.Visibility = Visibility.Hidden;
            }

            UpdateGrid();
            _isLoading = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            try
            {
                if (_currentOrder != null)
                {
                    _context.Entry(_currentOrder).Reload();
                }
            }
            catch { }
        }

        private void UpdateGrid()
        {
            TBlockTitle.Text = $"Состав заказа №{_currentOrder.OrderID} от {_currentOrder.OrderDate:dd.MM.yyyy}";

            var composition = _context.OrderProducts
                .Where(op => op.OrderID == _currentOrder.OrderID)
                .ToList();

            decimal totalSum = 0;
            foreach (var item in composition)
            {
                if (item.StockItems != null && item.StockItems.Products != null)
                {
                    totalSum += item.Quantity * item.StockItems.Products.ProductPrice;
                }
            }

            CompositionGrid.ItemsSource = composition;
            TBlockTotalSum.Text = $"{totalSum} руб.";
        }

        private void DatePickerOrder_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading || !DatePickerOrder.SelectedDate.HasValue) return;

            if (DatePickerOrder.SelectedDate.Value.Date > DateTime.Now.Date)
            {
                MessageBox.Show("Дата заказа не может быть в будущем!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                _isLoading = true;
                DatePickerOrder.SelectedDate = _currentOrder.OrderDate;
                _isLoading = false;
                BtnSaveDate.IsEnabled = false;
                return;
            }

            if (DatePickerOrder.SelectedDate.Value.Date != _currentOrder.OrderDate.Date)
            {
                BtnSaveDate.IsEnabled = true;
            }
            else
            {
                BtnSaveDate.IsEnabled = false;
            }
        }

        private void BtnSaveDate_Click(object sender, RoutedEventArgs e)
        {
            if (!DatePickerOrder.SelectedDate.HasValue) return;

            try
            {
                _currentOrder.OrderDate = DatePickerOrder.SelectedDate.Value;
                _context.SaveChanges();

                UpdateGrid();
                BtnSaveDate.IsEnabled = false;

                MessageBox.Show("Дата заказа успешно обновлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении даты: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = CompositionGrid.SelectedItem as OrderProducts;
            if (selectedItem == null)
            {
                MessageBox.Show("Выберите товар для удаления!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int itemsCount = _context.OrderProducts.Count(op => op.OrderID == _currentOrder.OrderID);
            if (itemsCount <= 1)
            {
                MessageBox.Show("Нельзя удалить последнюю позицию! Заказ не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show("Удалить эту позицию из заказа?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                var stockItem = _context.StockItems.FirstOrDefault(si => si.PositionID == selectedItem.PositionID);
                if (stockItem != null)
                {
                    stockItem.QuantityForOrders += selectedItem.Quantity;
                }

                _context.OrderProducts.Remove(selectedItem);
                _context.SaveChanges();

                UpdateGrid();
            }
        }
    }
}