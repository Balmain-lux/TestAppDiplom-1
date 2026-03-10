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

namespace TestAppDiplom.Pages
{
    /// <summary>
    /// Логика взаимодействия для AdminMainPage.xaml
    /// </summary>
    public partial class AdminMainPage : Page
    {
        private int? editingUserId = null;
        public AdminMainPage()
        {
            InitializeComponent();
            LoadUserData();
            LoadRoles();
            LoadUsers();
            LoadTests();

            // Очищаем текстовые подсказки
            ClearTextHint();
        }

        private void ClearTextHint()
        {
            txtUsername.Text = "";
            txtFirstName.Text = "";
            txtLastName.Text = "";
            txtEmail.Text = "";
        }

        private void LoadUserData()
        {
            if (App.CurrentUser != null)
            {
                txtUserInfo.Text = $"{App.CurrentUser.FirstName} {App.CurrentUser.LastName} (Администратор)";
            }
        }

        private void LoadRoles()
        {
            try
            {
                var roles = MainWindow.db.Roles.ToList();
                cmbRole.ItemsSource = roles;

                var rolesForFilter = roles.ToList();
                rolesForFilter.Insert(0, new DataBase.Roles { RoleID = 0, RoleName = "Все роли" });
                cmbRoleFilter.ItemsSource = rolesForFilter;
                cmbRoleFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке ролей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUsers()
        {
            try
            {
                var users = MainWindow.db.Users
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToList();
                lvUsers.ItemsSource = users;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTests()
        {
            try
            {
                var tests = MainWindow.db.Tests
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();
                lvTests.ItemsSource = tests;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке тестов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearForm()
        {
            txtUsername.Text = "";
            txtPassword.Password = "";
            txtFirstName.Text = "";
            txtLastName.Text = "";
            txtEmail.Text = "";
            cmbRole.SelectedIndex = -1;
            editingUserId = null;
            btnAddUser.Content = "Добавить";
        }

        private void ApplyUserFilters()
        {
            try
            {
                var query = MainWindow.db.Users.AsQueryable();

                int roleId = (int)(cmbRoleFilter.SelectedValue ?? 0);
                if (roleId > 0)
                {
                    query = query.Where(u => u.RoleID == roleId);
                }

                string searchText = txtSearch.Text?.ToLower() ?? "";
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query = query.Where(u =>
                        u.Username.ToLower().Contains(searchText) ||
                        u.FirstName.ToLower().Contains(searchText) ||
                        u.LastName.ToLower().Contains(searchText) ||
                        (u.Email != null && u.Email.ToLower().Contains(searchText)));
                }

                lvUsers.ItemsSource = query.OrderBy(u => u.LastName).ThenBy(u => u.FirstName).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при фильтрации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentUser = null;
            NavigationService.Navigate(new LoginPage());
        }

        private void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) ||
                string.IsNullOrWhiteSpace(txtPassword.Password) ||
                string.IsNullOrWhiteSpace(txtFirstName.Text) ||
                string.IsNullOrWhiteSpace(txtLastName.Text) ||
                cmbRole.SelectedItem == null)
            {
                MessageBox.Show("Заполните все обязательные поля!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (editingUserId == null)
                {
                    var existingUser = MainWindow.db.Users
                        .FirstOrDefault(u => u.Username == txtUsername.Text);
                    if (existingUser != null)
                    {
                        MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var newUser = new DataBase.Users
                    {
                        Username = txtUsername.Text,
                        Password = txtPassword.Password,
                        FirstName = txtFirstName.Text,
                        LastName = txtLastName.Text,
                        Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text,
                        RoleID = (int)cmbRole.SelectedValue,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    MainWindow.db.Users.Add(newUser);
                    MainWindow.db.SaveChanges();

                    MessageBox.Show("Пользователь успешно добавлен!", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    var user = MainWindow.db.Users.Find(editingUserId);
                    if (user != null)
                    {
                        if (user.Username != txtUsername.Text)
                        {
                            var existingUser = MainWindow.db.Users
                                .FirstOrDefault(u => u.Username == txtUsername.Text);
                            if (existingUser != null)
                            {
                                MessageBox.Show("Пользователь с таким логином уже существует!", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }

                        user.Username = txtUsername.Text;
                        if (!string.IsNullOrWhiteSpace(txtPassword.Password))
                        {
                            user.Password = txtPassword.Password;
                        }
                        user.FirstName = txtFirstName.Text;
                        user.LastName = txtLastName.Text;
                        user.Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text;
                        user.RoleID = (int)cmbRole.SelectedValue;

                        MainWindow.db.SaveChanges();

                        MessageBox.Show("Пользователь успешно обновлен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                ClearForm();
                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении пользователя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnClearForm_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void cmbRoleFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyUserFilters();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyUserFilters();
        }

        private void btnEditUser_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int userId = (int)button.Tag;
                var user = MainWindow.db.Users.Find(userId);

                if (user != null)
                {
                    txtUsername.Text = user.Username;
                    txtPassword.Password = "";
                    txtFirstName.Text = user.FirstName;
                    txtLastName.Text = user.LastName;
                    txtEmail.Text = user.Email;
                    cmbRole.SelectedValue = user.RoleID;

                    editingUserId = userId;
                    btnAddUser.Content = "Обновить";
                }
            }
        }

        private void btnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int userId = (int)button.Tag;

                if (userId == App.CurrentUser.UserID)
                {
                    MessageBox.Show("Вы не можете удалить свою учетную запись!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var result = MessageBox.Show("Вы уверены, что хотите удалить этого пользователя?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var user = MainWindow.db.Users.Find(userId);
                        if (user != null)
                        {
                            user.IsActive = false;
                            MainWindow.db.SaveChanges();

                            LoadUsers();
                            MessageBox.Show("Пользователь успешно деактивирован", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void btnAddTest_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TestEditPage(0)); // 0 - новый тест
        }

        private void btnEditTest_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int testId = (int)button.Tag;
                NavigationService.Navigate(new TestEditPage(testId));
            }
        }

        private void btnDeleteTest_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int testId = (int)button.Tag;

                var result = MessageBox.Show("Вы уверены, что хотите удалить этот тест?\n" +
                    "Все связанные данные (вопросы, ответы, результаты) будут также удалены!",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var test = MainWindow.db.Tests.Find(testId);
                        if (test != null)
                        {
                            MainWindow.db.Tests.Remove(test);
                            MainWindow.db.SaveChanges();

                            LoadTests();
                            MessageBox.Show("Тест успешно удален", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
