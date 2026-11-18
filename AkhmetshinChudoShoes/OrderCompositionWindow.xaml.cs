using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AkhmetshinChudoShoes
{
    public partial class OrderCompositionWindow : Window
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();

        private Orders _currentOrder;

        public OrderCompositionWindow(Orders selectedOrder)
        {
            InitializeComponent();
            _currentOrder = selectedOrder;

            DatePickerOrder.SelectedDate = selectedOrder.OrderDate;

            if (Manager.CurrentUser == null || Manager.CurrentUser.RoleID != 1)
            {
                DatePickerOrder.Visibility = Visibility.Hidden;
                BtnDeleteItem.Visibility = Visibility.Hidden;
            }

            UpdateGrid();
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
            if (DatePickerOrder.SelectedDate.HasValue && _currentOrder != null)
            {
                _currentOrder.OrderDate = DatePickerOrder.SelectedDate.Value;
                _context.SaveChanges(); // Сохраняем в БД
                UpdateGrid(); // Обновляем заголовок
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

            if (MessageBox.Show("Удалить эту позицию из заказа?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // Возвращаем на склад
                var stockItem = _context.StockItems.FirstOrDefault(si => si.PositionID == selectedItem.PositionID);
                if (stockItem != null)
                {
                    stockItem.QuantityForOrders += selectedItem.Quantity;
                }

                // Удаляем из заказа
                _context.OrderProducts.Remove(selectedItem);
                _context.SaveChanges();

                UpdateGrid(); // Обновляем таблицу и сумму
            }
        }
    }
}