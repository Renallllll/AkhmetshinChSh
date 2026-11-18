using System.Collections.Generic;
using System.Windows.Controls;

namespace AkhmetshinChudoShoes
{
    public static class Manager
    {
        public static Users CurrentUser { get; set; }
        public static Frame MainFrame { get; set; }

        // ДОБАВЛЕНО: Наша корзина
        public static List<CartItem> Cart = new List<CartItem>();
    }

    // ДОБАВЛЕНО: Класс, описывающий одну строчку в корзине
    public class CartItem
    {
        public StockItems StockItem { get; set; } // Конкретная позиция (товар + размер)
        public int Quantity { get; set; }         // Сколько штук хотим купить
    }
}