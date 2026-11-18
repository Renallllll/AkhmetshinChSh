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

        // ТВОЙ РОДНОЙ КОД (не тронут)
        private void UpdateOrders()
        {
            var orders = _context.Orders.ToList();
            OrdersListView.ItemsSource = orders;
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                UpdateOrders();
            }
        }

        // ТВОЙ РОДНОЙ КОД (не тронут)
        private void ViewCompositionBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = (sender as Button).DataContext as Orders;
            if (selectedOrder == null) return;

            OrderCompositionWindow window = new OrderCompositionWindow(selectedOrder);
            window.ShowDialog();
            UpdateOrders();
        }

        // ТВОЙ РОДНОЙ КОД (не тронут)
        private void DeleteOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = (sender as Button).DataContext as Orders;
            if (selectedOrder == null) return;

            if (MessageBox.Show($"Вы точно хотите удалить заказ №{selectedOrder.OrderID}?", "Внимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    var productsInOrder = _context.OrderProducts.Where(op => op.OrderID == selectedOrder.OrderID).ToList();
                    foreach (var item in productsInOrder)
                    {
                        var stockToRestore = _context.StockItems.FirstOrDefault(si => si.PositionID == item.PositionID);
                        if (stockToRestore != null)
                        {
                            stockToRestore.QuantityForOrders += item.Quantity;
                        }
                        _context.OrderProducts.Remove(item);
                    }
                    _context.Orders.Remove(selectedOrder);
                    _context.SaveChanges();
                    MessageBox.Show("Заказ удален, товары возвращены на склад!", "Успех");
                    UpdateOrders();
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении: " + ex.Message);
                }
            }
        }

        // ДОБАВЛЕНО: Создание заказа Менеджером/Админом
        private void BtnCreateOrder_Click(object sender, RoutedEventArgs e)
        {
            Window selectWindow = new Window
            {
                Title = "Создание заказа",
                Width = 350,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };

            // ПОДСКАЗКА 1: Что делать в этом окне
            TextBlock tbHint = new TextBlock
            {
                Text = "Шаг 1: Выберите клиента, для которого оформляется заказ:",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            ListBox lbClients = new ListBox { Height = 250, DisplayMemberPath = "DisplayName" };

            var clients = _context.Users.Select(u => new {
                UserID = u.UserID,
                DisplayName = u.UserLastName + " " + u.UserFirstName + " " + u.UserPytronimic
            }).ToList();

            lbClients.ItemsSource = clients;

            Button btnSelect = new Button { Content = "Перейти к выбору товаров", Height = 35, Margin = new Thickness(0, 15, 0, 0), Background = System.Windows.Media.Brushes.LightGreen };
            btnSelect.Click += (s, args) =>
            {
                if (lbClients.SelectedItem == null)
                {
                    MessageBox.Show("Выберите клиента!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                dynamic selectedClient = lbClients.SelectedItem;
                int clientId = selectedClient.UserID;

                Manager.OrderClient = _context.Users.FirstOrDefault(u => u.UserID == clientId);
                Manager.Cart.Clear();

                selectWindow.Close();

                // ПОДСКАЗКА 2: Что делать дальше
                MessageBox.Show($"Клиент выбран. Сейчас откроется каталог. Добавьте нужные товары в корзину и перейдите в раздел 'Корзина' для оформления.", "Переход в каталог", MessageBoxButton.OK, MessageBoxImage.Information);

                Manager.MainFrame.Navigate(new OrderPage());
            };

            panel.Children.Add(tbHint); // Добавили подсказку
            panel.Children.Add(lbClients);
            panel.Children.Add(btnSelect);
            selectWindow.Content = panel;
            selectWindow.ShowDialog();
        }
    }
}