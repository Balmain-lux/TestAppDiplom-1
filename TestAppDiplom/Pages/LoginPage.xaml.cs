using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;

namespace TestAppDiplom.Pages
{
    /// <summary>
    /// Логика взаимодействия для LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void btnGoToRegister_Click(object sender, RoutedEventArgs e)
        {

            NavigationService.Navigate(new RegisterPage());

        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {

                var user = MainWindow.db.Users
                    .FirstOrDefault(u => u.Username == username &&
                                        u.Password == password &&
                                        u.IsActive == true);

                if (user != null)
                {

                    App.CurrentUser = user;

                    MessageBox.Show($"Добро пожаловать, {user.FirstName} {user.LastName}!",
                        "Успешный вход", MessageBoxButton.OK, MessageBoxImage.Information);


                    switch (user.RoleID)
                    {
                        case 1: // Студент
                            NavigationService.Navigate(new StudentMainPage());
                            break;
                        case 2: // Преподаватель
                            NavigationService.Navigate(new TeacherMainPage());
                            break;
                        case 3: // Администратор
                            NavigationService.Navigate(new AdminMainPage());
                            break;
                    }
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при входе: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}
