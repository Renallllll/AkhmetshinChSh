using System;
using System.Windows;
using System.Windows.Controls;

namespace AkhmetshinChudoShoes
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Manager.MainFrame = MainFrame;
            MainFrame.Navigate(new OrderPage());

            SetupUI();
        }

        private void SetupUI()
        {
            var user = Manager.CurrentUser;

            if (user == null)
            {
                TBlockUserInfo.Text = "Гость";
                BtnOrders.Visibility = Visibility.Collapsed;
                BtnAnalytics.Visibility = Visibility.Collapsed;
            }
            else
            {
                string roleName = user.Roles != null ? user.Roles.RoleName : "Пользователь";
                string fullName = $"{user.UserLastName} {user.UserFirstName} {user.UserPytronimic}".Trim();
                TBlockUserInfo.Text = $"{fullName} ({roleName})";

                if (user.RoleID == 1 || user.RoleID == 2 || user.RoleID == 3)
                    BtnOrders.Visibility = Visibility.Visible;
                else
                    BtnOrders.Visibility = Visibility.Collapsed;

                if (user.RoleID == 1)
                    BtnAnalytics.Visibility = Visibility.Visible;
                else
                    BtnAnalytics.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnAnalytics_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AnalyticsPage());
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Manager.CurrentUser = null;
            Manager.OrderClient = null;
            Manager.Cart.Clear();
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }

        private void BtnCatalog_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new OrderPage());
        }

        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new OrdersListPage());
        }

        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CartPage());
        }
    }
}