using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AkhmetshinChudoShoes
{
    public partial class CartPage : Page
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();
        private Button _btnCancelOrderTarget;

        public CartPage()
        {
            InitializeComponent();
            InitCancelOrderTargetButton();
            UpdateCart();
        }

        private void InitCancelOrderTargetButton()
        {
            if (_btnCancelOrderTarget != null) return;

            _btnCancelOrderTarget = new Button
            {
                Content = "Отменить заказ для клиента",
                Width = 260,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 5, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Visibility = Visibility.Collapsed
            };
            _btnCancelOrderTarget.Click += BtnCancelOrderTarget_Click;

            if (TBlockOrderTarget.Parent is Panel parentPanel)
            {
                parentPanel.Children.Add(_btnCancelOrderTarget);
            }
        }

        private void BtnCancelOrderTarget_Click(object sender, RoutedEventArgs e)
        {
            if (Manager.OrderClient == null) return;

            if (MessageBox.Show("Отменить оформление заказа для выбранного клиента и очистить корзину?",
                "Отмена заказа", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Manager.OrderClient = null;
                Manager.Cart.Clear();
                UpdateCart();
                MessageBox.Show("Оформление заказа для клиента отменено.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void UpdateCart()
        {
            CartGrid.ItemsSource = null;
            CartGrid.ItemsSource = Manager.Cart;

            decimal total = 0;
            foreach (var item in Manager.Cart)
            {
                total += item.Quantity * item.StockItem.Products.ProductPrice;
            }
            TBlockTotal.Text = $"{total} руб.";

            if (Manager.OrderClient != null)
            {
                TBlockOrderTarget.Text = $"Внимание! Заказ оформляется для клиента: {Manager.OrderClient.UserLastName} {Manager.OrderClient.UserFirstName} {Manager.OrderClient.UserPytronimic}";
                TBlockOrderTarget.Visibility = Visibility.Visible;
                if (_btnCancelOrderTarget != null)
                    _btnCancelOrderTarget.Visibility = Visibility.Visible;
            }
            else
            {
                TBlockOrderTarget.Visibility = Visibility.Collapsed;
                if (_btnCancelOrderTarget != null)
                    _btnCancelOrderTarget.Visibility = Visibility.Collapsed;
            }
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible) UpdateCart();
        }

        private void DeleteFromCartBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = CartGrid.SelectedItem as CartItem;
            if (selectedItem == null)
            {
                MessageBox.Show("Выберите товар для удаления!", "Внимание");
                return;
            }

            Manager.Cart.Remove(selectedItem);
            UpdateCart();
        }

        private void CheckoutBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Manager.Cart.Count == 0)
            {
                MessageBox.Show("Корзина пуста!", "Ошибка");
                return;
            }

            if (Manager.CurrentUser == null)
            {
                MessageBox.Show("Необходимо авторизоваться!", "Ошибка");
                return;
            }

            try
            {
                int targetClientId = Manager.CurrentUser.UserID;
                if (Manager.OrderClient != null) targetClientId = Manager.OrderClient.UserID;

                var newOrder = new Orders
                {
                    OrderDate = DateTime.Now,
                    ClientID = targetClientId
                };
                _context.Orders.Add(newOrder);
                _context.SaveChanges();

                foreach (var item in Manager.Cart)
                {
                    var orderProduct = new OrderProducts
                    {
                        OrderID = newOrder.OrderID,
                        PositionID = item.StockItem.PositionID,
                        Quantity = item.Quantity
                    };
                    _context.OrderProducts.Add(orderProduct);

                    var stockToUpdate = _context.StockItems.FirstOrDefault(si => si.PositionID == item.StockItem.PositionID);
                    if (stockToUpdate != null) stockToUpdate.QuantityForOrders -= item.Quantity;
                }

                _context.SaveChanges();

                MessageBox.Show($"Заказ №{newOrder.OrderID} успешно оформлен!", "Успех");

                Manager.Cart.Clear();
                Manager.OrderClient = null;
                UpdateCart();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при оформлении: " + ex.Message);
            }
        }

        private void PlusBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var cartItem = button.DataContext as CartItem;
            if (cartItem == null) return;

            if (cartItem.Quantity < cartItem.StockItem.QuantityForOrders)
            {
                cartItem.Quantity++;
                UpdateCart();
            }
            else
            {
                MessageBox.Show($"На складе больше нет товара (максимум {cartItem.StockItem.QuantityForOrders} пар)!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MinusBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var cartItem = button.DataContext as CartItem;
            if (cartItem == null) return;

            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
                UpdateCart();
            }
            else
            {
                if (MessageBox.Show("Удалить этот товар из корзины?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Manager.Cart.Remove(cartItem);
                    UpdateCart();
                }
            }
        }
    }
}