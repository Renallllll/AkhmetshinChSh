using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AkhmetshinChudoShoes
{
    public partial class OrdersListPage : Page
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();
        private Button _btnCancelClientOrder;

        public OrdersListPage()
        {
            InitializeComponent();
            InitCancelClientOrderButton();
            ConfigureClientOrderMode();
            UpdateOrders();
        }

        private void InitCancelClientOrderButton()
        {
            if (_btnCancelClientOrder != null) return;

            _btnCancelClientOrder = new Button
            {
                Content = "Отменить заказ клиента",
                Height = 30,
                Width = 170,
                Margin = new Thickness(0, 0, 10, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = System.Windows.Input.Cursors.Hand,
                Visibility = Visibility.Collapsed
            };
            _btnCancelClientOrder.Click += BtnCancelClientOrder_Click;

            if (BtnCreateOrder.Parent is Panel parentPanel)
            {
                parentPanel.Children.Add(_btnCancelClientOrder);
            }
        }

        private void ConfigureClientOrderMode()
        {
            if (Manager.CurrentUser != null && Manager.CurrentUser.RoleID == 3)
            {
                BtnCreateOrder.Visibility = Visibility.Collapsed;
                if (_btnCancelClientOrder != null)
                    _btnCancelClientOrder.Visibility = Visibility.Collapsed;
                return;
            }

            if (_btnCancelClientOrder != null)
            {
                _btnCancelClientOrder.Visibility = Manager.OrderClient != null
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void BtnCancelClientOrder_Click(object sender, RoutedEventArgs e)
        {
            if (Manager.OrderClient == null) return;

            if (MessageBox.Show("Отменить создание заказа для выбранного клиента? Выбранные товары в корзине будут очищены.",
                "Отмена создания заказа", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Manager.OrderClient = null;
                Manager.Cart.Clear();
                ConfigureClientOrderMode();
                MessageBox.Show("Создание заказа для клиента отменено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UpdateOrders()
        {
            if (Manager.CurrentUser == null)
            {
                OrdersListView.ItemsSource = null;
                return;
            }

            if (Manager.CurrentUser.RoleID == 3)
            {
                var myOrders = _context.Orders
                    .Where(o => o.ClientID == Manager.CurrentUser.UserID)
                    .ToList();
                OrdersListView.ItemsSource = myOrders;
            }
            else
            {
                var orders = _context.Orders.ToList();
                OrdersListView.ItemsSource = orders;
            }
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                ConfigureClientOrderMode();
                UpdateOrders();
            }
        }

        private void DeleteOrderBtn_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && Manager.CurrentUser != null && Manager.CurrentUser.RoleID == 3)
            {
                btn.Visibility = Visibility.Collapsed;
            }
        }

        private void ViewCompositionBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = (sender as Button).DataContext as Orders;
            if (selectedOrder == null) return;

            OrderCompositionWindow window = new OrderCompositionWindow(selectedOrder);
            window.ShowDialog();
            UpdateOrders();
        }

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

        private void BtnCreateOrder_Click(object sender, RoutedEventArgs e)
        {
            Window selectWindow = new Window
            {
                Title = "Создание заказа",
                Width = 350,
                Height = 450,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };

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

            Button btnSelect = new Button { Content = "Перейти к выбору товаров", Height = 35, Width = 300, Margin = new Thickness(0, 15, 0, 0) };
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
                ConfigureClientOrderMode();

                selectWindow.Close();

                MessageBox.Show($"Клиент выбран. Сейчас откроется каталог. Добавьте нужные товары в корзину и перейдите в раздел 'Корзина' для оформления.", "Переход в каталог", MessageBoxButton.OK, MessageBoxImage.Information);

                Manager.MainFrame.Navigate(new OrderPage());
            };

            Button btnCancel = new Button { Content = "Отмена", Width = 300, Margin = new Thickness(0, 8, 0, 0) };
            btnCancel.Click += (s, args) => selectWindow.Close();

            panel.Children.Add(tbHint);
            panel.Children.Add(lbClients);
            panel.Children.Add(btnSelect);
            panel.Children.Add(btnCancel);
            selectWindow.Content = panel;
            selectWindow.ShowDialog();
        }
    }
}