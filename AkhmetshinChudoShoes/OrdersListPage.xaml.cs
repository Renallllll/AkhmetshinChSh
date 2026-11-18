using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AkhmetshinChudoShoes
{
    public partial class OrdersListPage : Page
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();

        public OrdersListPage()
        {
            InitializeComponent();
            UpdateOrders();
        }

        // Функция обновления списка заказов
        private void UpdateOrders()
        {
            // Подгружаем заказы вместе с клиентами
            var orders = _context.Orders.ToList();
            OrdersListView.ItemsSource = orders;
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                UpdateOrders(); // Обновляем при возвращении на страницу
            }
        }

        // Просмотр состава заказа
        private void ViewCompositionBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = (sender as Button).DataContext as Orders;
            if (selectedOrder == null) return;

            // Открываем окно состава и передаем туда выбранный заказ
            OrderCompositionWindow window = new OrderCompositionWindow(selectedOrder);
            window.ShowDialog(); // ShowDialog блокирует главное окно, пока не закроют это
        }

        // Удаление заказа
        private void DeleteOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = (sender as Button).DataContext as Orders;
            if (selectedOrder == null) return;

            if (MessageBox.Show($"Вы точно хотите удалить заказ №{selectedOrder.OrderID}?", "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    // 1. Находим все товары в этом заказе
                    var productsInOrder = _context.OrderProducts.Where(op => op.OrderID == selectedOrder.OrderID).ToList();

                    foreach (var item in productsInOrder)
                    {
                        // ВОЗВРАЩАЕМ ТОВАР НА СКЛАД (QuantityForOrders)
                        var stockToRestore = _context.StockItems.FirstOrDefault(si => si.PositionID == item.PositionID);
                        if (stockToRestore != null)
                        {
                            stockToRestore.QuantityForOrders += item.Quantity; // Прибавляем обратно
                        }

                        // Удаляем позицию из заказа
                        _context.OrderProducts.Remove(item);
                    }

                    // 2. Удаляем сам заказ
                    _context.Orders.Remove(selectedOrder);
                    _context.SaveChanges();

                    MessageBox.Show("Заказ удален, товары возвращены на склад!", "Успех");
                    UpdateOrders(); // Обновляем список заказов на экране
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении: " + ex.Message);
                }
            }
        }
    }
}