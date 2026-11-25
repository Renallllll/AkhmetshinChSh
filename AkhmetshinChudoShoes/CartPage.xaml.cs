using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AkhmetshinChudoShoes
{
    public partial class CartPage : Page
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();

        public CartPage()
        {
            InitializeComponent();
            UpdateCart();
        }

        private void UpdateCart()
        {
            LoadCustomers();

            CartGrid.ItemsSource = null;
            CartGrid.ItemsSource = Manager.Cart;

            decimal total = 0;
            foreach (var item in Manager.Cart)
            {
                total += item.Quantity * item.StockItem.Products.FinalPrice;
            }
            TBlockTotal.Text = $"{total} руб.";
        }

        private void LoadCustomers()
        {
            int roleId = Manager.CurrentUser != null ? Manager.CurrentUser.RoleID : 0;
            bool isStaff = roleId == 1 || roleId == 2;

            CustomerSelectPanel.Visibility = isStaff ? Visibility.Visible : Visibility.Collapsed;
            if (!isStaff) return;

            var prevSelectedId = CbCustomer.SelectedValue is int id ? id : (int?)null;

            var customers = _context.Users
                .OrderBy(u => u.UserID)
                .ToList()
                .Select(u => new
                {
                    u.UserID,
                    FullNameDisplay = $"{u.UserLastName} {u.UserFirstName} {u.UserPytronimic}"
                })
                .ToList();

            CbCustomer.ItemsSource = customers;

            if (prevSelectedId.HasValue && customers.Any(c => c.UserID == prevSelectedId.Value))
            {
                CbCustomer.SelectedValue = prevSelectedId.Value;
            }
            else if (customers.Count > 0)
            {
                CbCustomer.SelectedIndex = 0;
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
                MessageBox.Show(
                    "Выберите позицию в таблице корзины, которую нужно удалить.",
                    "Не выбрана позиция",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Manager.Cart.Remove(selectedItem);
            UpdateCart();
        }

        private void CheckoutBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Manager.Cart.Count == 0)
            {
                MessageBox.Show(
                    "Корзина пуста. Перейдите в каталог и добавьте одну или несколько пар обуви нужного размера перед оформлением заказа.",
                    "Корзина пуста",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (Manager.CurrentUser == null)
            {
                MessageBox.Show(
                    "Оформлять заказ может только авторизованный пользователь. Войдите в систему по логину и повторите попытку.",
                    "Требуется авторизация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            int roleId = Manager.CurrentUser.RoleID;
            int targetClientId;

            if (roleId == 1 || roleId == 2)
            {
                if (CbCustomer.SelectedValue == null)
                {
                    MessageBox.Show(
                        "Выберите пользователя, для которого оформляется заказ.",
                        "Не выбран заказчик",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                targetClientId = (int)CbCustomer.SelectedValue;
            }
            else
            {
                targetClientId = Manager.CurrentUser.UserID;
            }

            try
            {
                foreach (var item in Manager.Cart)
                {
                    var stockToUpdate = _context.StockItems.FirstOrDefault(si => si.PositionID == item.StockItem.PositionID);
                    if (stockToUpdate == null || stockToUpdate.QuantityForOrders < item.Quantity)
                    {
                        int available = stockToUpdate != null ? stockToUpdate.QuantityForOrders : 0;
                        MessageBox.Show(
                            $"Недостаточно остатка для товара «{item.StockItem.Products.ProductName}» (размер {item.StockItem.Sizes.Size}): запрошено {item.Quantity} пар, доступно {available}. Уменьшите количество в корзине и повторите оформление.",
                            "Недостаточно товара",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }

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

                MessageBox.Show(
                    $"Заказ №{newOrder.OrderID} успешно оформлен.",
                    "Заказ оформлен",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                Manager.Cart.Clear();
                UpdateCart();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при оформлении заказа: {ex.Message}\nПроверьте корректность данных и состояние подключения к базе данных.",
                    "Ошибка оформления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void PlusBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var cartItem = button?.DataContext as CartItem;
            if (cartItem == null) return;

            if (cartItem.Quantity < cartItem.StockItem.QuantityForOrders)
            {
                cartItem.Quantity++;
                UpdateCart();
            }
            else
            {
                MessageBox.Show(
                    $"На складе больше нет товара данного размера (максимум {cartItem.StockItem.QuantityForOrders} пар).",
                    "Ограничение остатка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void MinusBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var cartItem = button?.DataContext as CartItem;
            if (cartItem == null) return;

            if (cartItem.Quantity > 1)
            {
                cartItem.Quantity--;
                UpdateCart();
            }
            else
            {
                if (MessageBox.Show(
                    "Удалить этот товар из корзины?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Manager.Cart.Remove(cartItem);
                    UpdateCart();
                }
            }
        }
    }
}