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

        // Обновление списка и подсчет суммы
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
        }

        private void Page_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                UpdateCart();
            }
        }

        // Удаление позиции из корзины
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

        // ОФОРМЛЕНИЕ ЗАКАЗА (Создание записи в БД)
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
                //Создаем сам заказ (шапку)
                var newOrder = new Orders
                {
                    OrderDate = DateTime.Now,
                    ClientID = Manager.CurrentUser.UserID
                };
                _context.Orders.Add(newOrder);
                _context.SaveChanges(); // Сохраняем, чтобы получить OrderID

                //Переносим товары из корзины в OrderProducts
                foreach (var item in Manager.Cart)
                {
                    var orderProduct = new OrderProducts
                    {
                        OrderID = newOrder.OrderID,
                        PositionID = item.StockItem.PositionID,
                        Quantity = item.Quantity
                    };
                    _context.OrderProducts.Add(orderProduct);

                    //Списываем остатки со склада
                    var stockToUpdate = _context.StockItems.FirstOrDefault(si => si.PositionID == item.StockItem.PositionID);
                    if (stockToUpdate != null)
                    {
                        stockToUpdate.QuantityForOrders -= item.Quantity;
                    }
                }

                _context.SaveChanges(); //Сохраняем все товары и остатки

                MessageBox.Show($"Заказ №{newOrder.OrderID} успешно оформлен!", "Успех");

                //Очищаем корзину
                Manager.Cart.Clear();
                UpdateCart();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при оформлении: " + ex.Message);
            }
        }
    }
}