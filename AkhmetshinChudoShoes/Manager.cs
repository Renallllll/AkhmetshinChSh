using System.Collections.Generic;
using System.Windows.Controls;

namespace AkhmetshinChudoShoes
{
    public static class Manager
    {
        public static Users CurrentUser { get; set; }
        public static Frame MainFrame { get; set; }

        public static Users OrderClient { get; set; }

        public static List<CartItem> Cart = new List<CartItem>();
    }

    public class CartItem
    {
        public StockItems StockItem { get; set; }
        public int Quantity { get; set; }
    }
}