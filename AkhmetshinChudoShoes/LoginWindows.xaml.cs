using System.Linq;
using System.Windows;

namespace AkhmetshinChudoShoes
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = TBoxLogin.Text.Trim();

            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Введите логин!", "Ошибка");
                return;
            }

            // Ищем пользователя в БД
            var user = AkhmetshinDBShoesEntities.GetContext().Users.FirstOrDefault(u => u.UserLogin == login);

            if (user != null)
            {
                Manager.CurrentUser = user;
                OpenMainWindow();
            }
            else
            {
                MessageBox.Show("Пользователь с таким логином не найден!", "Ошибка");
            }
        }

        private void BtnGuest_Click(object sender, RoutedEventArgs e)
        {
            Manager.CurrentUser = null; // Гость
            OpenMainWindow();
        }

        private void OpenMainWindow()
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}