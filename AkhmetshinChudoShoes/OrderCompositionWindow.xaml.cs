using System.Linq;
using System.Windows;

namespace AkhmetshinChudoShoes
{
    public partial class OrderCompositionWindow : Window
    {
        private AkhmetshinDBShoesEntities _context = AkhmetshinDBShoesEntities.GetContext();

        // В конструктор мы передаем заказ, который хотим посмотреть
        public OrderCompositionWindow(Orders selectedOrder)
        {
            InitializeComponent();

            TBlockTitle.Text = $"Состав заказа №{selectedOrder.OrderID} от {selectedOrder.OrderDate:dd.MM.yyyy}";

            // Берем все строки из OrderProducts, которые относятся к этому заказу
            var composition = _context.OrderProducts
                .Where(op => op.OrderID == selectedOrder.OrderID)
                .ToList();

            // Считаем итоговую сумму
            decimal totalSum = 0;
            foreach (var item in composition)
            {
                // Чтобы не было ошибки, если вдруг товар удалили (хотя мы это запретили), 
                // берем цену с проверкой на null
                if (item.StockItems != null && item.StockItems.Products != null)
                {
                    totalSum += item.Quantity * item.StockItems.Products.ProductPrice;
                }
            }

            // Выводим данные
            CompositionGrid.ItemsSource = composition;
            TBlockTotalSum.Text = $"{totalSum} руб.";
        }
    }
}