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
using TestAppDiplom.DataBase;

namespace TestAppDiplom.Pages
{
    /// <summary>
    /// Логика взаимодействия для RegisterPage.xaml
    /// </summary>
    public partial class RegisterPage : Page
    {
        public RegisterPage()
        {
            InitializeComponent();
            LoadRoles();
            LoadGroups();
            cmbRole.SelectionChanged += CmbRole_SelectionChanged;
        }

        private void LoadGroups()
        {
            try
            {
                var groups = MainWindow.db.Groups
                    .OrderBy(g => g.Specialty)
                    .ThenBy(g => g.GroupName)
                    .ToList();
                cmbGroup.ItemsSource = groups;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки групп: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRoles()
        {
            try
            {
                cmbRole.ItemsSource = MainWindow.db.Roles
                    .Where(r => r.RoleID != 3)
                    .ToList();
                cmbRole.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки ролей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRole.SelectedItem != null)
            {
                var selectedRole = (Roles)cmbRole.SelectedItem;
                cmbGroup.IsEnabled = selectedRole.RoleID == 1;
                if (!cmbGroup.IsEnabled)
                {
                    cmbGroup.SelectedItem = null;
                }
            }
        }


        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }


        private void btnGoToLogin_Click(object sender, RoutedEventArgs e)
        {

            NavigationService.Navigate(new LoginPage());

        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUsername.Text) ||
                            string.IsNullOrEmpty(txtPassword.Password) ||
                            string.IsNullOrEmpty(txtConfirmPassword.Password) ||
                            string.IsNullOrEmpty(txtFirstName.Text) ||
                            string.IsNullOrEmpty(txtLastName.Text) ||
                            cmbRole.SelectedItem == null)
            {
                MessageBox.Show("Заполните все обязательные поля!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedRole = (Roles)cmbRole.SelectedItem;
            if (selectedRole.RoleID == 1 && cmbGroup.SelectedItem == null)
            {
                MessageBox.Show("Для студентов обязательно выбрать группу!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtPassword.Password != txtConfirmPassword.Password)
            {
                MessageBox.Show("Пароли не совпадают!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtPassword.Password.Length < 4)
            {
                MessageBox.Show("Пароль должен содержать минимум 4 символа!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(txtEmail.Text) && !IsValidEmail(txtEmail.Text))
            {
                MessageBox.Show("Введите корректный Email адрес!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var existingUser = MainWindow.db.Users
                    .FirstOrDefault(u => u.Username == txtUsername.Text);

                if (existingUser != null)
                {
                    MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newUser = new Users
                {
                    Username = txtUsername.Text,
                    Password = txtPassword.Password,
                    FirstName = txtFirstName.Text,
                    LastName = txtLastName.Text,
                    Email = string.IsNullOrEmpty(txtEmail.Text) ? null : txtEmail.Text,
                    RoleID = (int)cmbRole.SelectedValue,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    GroupID = selectedRole.RoleID == 1 ? (int?)cmbGroup.SelectedValue : null
                };

                MainWindow.db.Users.Add(newUser);
                MainWindow.db.SaveChanges();

                MessageBox.Show("Регистрация прошла успешно! Теперь вы можете войти.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.Navigate(new LoginPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при регистрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
    }
}
