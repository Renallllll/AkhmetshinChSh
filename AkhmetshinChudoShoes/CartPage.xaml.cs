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

        // ТВОЙ РОДНОЙ КОД (не тронут, только плашка)
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
            }
            else
            {
                TBlockOrderTarget.Visibility = Visibility.Collapsed;
            }
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible) UpdateCart();
        }

        // ТВОЙ РОДНОЙ КОД
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

        // ТВОЙ РОДНОЙ КОД (с проверкой targetClientId)
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

        // ДОБАВЛЕНО: Увеличение количества
        private void PlusBtn_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var cartItem = button.DataContext as CartItem;
            if (cartItem == null) return;

            // Проверяем остаток на складе
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

        // ДОБАВЛЕНО: Уменьшение количества
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
                // Если количество становится 0, предлагаем удалить
                if (MessageBox.Show("Удалить этот товар из корзины?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Manager.Cart.Remove(cartItem);
                    UpdateCart();
                }
            }
        }
    }
}