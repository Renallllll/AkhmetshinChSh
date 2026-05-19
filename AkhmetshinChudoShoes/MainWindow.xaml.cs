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
                TBlockUserInfo.Text = $"{user.UserLastName} {user.UserFirstName} {user.UserPytronimic} ({roleName})";

                if (user.RoleID == 1 || user.RoleID == 2)
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

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            btnBack.Visibility = Visibility.Hidden;
            if (Manager.MainFrame.CanGoBack) {
                btnBack.Visibility = Visibility.Visible;
                Manager.MainFrame.GoBack();
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Manager.CurrentUser = null;
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

        private void MainFrame_ContentRendered(object sender, EventArgs e)
        {
        }

        private void BtnCart_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CartPage());
        }
    }
}