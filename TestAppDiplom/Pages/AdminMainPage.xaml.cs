using System;
using System.Collections.Generic;
using System.Data.Entity;
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
            LoadGroupsForAdmin();

        }

        private void LoadUserData()
        {
            if (App.CurrentUser != null)
            {
                txtUserInfo.Text = $"{App.CurrentUser.FirstName} {App.CurrentUser.LastName} (Администратор)";
            }
        }
        private void LoadGroupsForAdmin()
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

        private void cmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbRole.SelectedItem != null)
            {
                var selectedRole = (DataBase.Roles)cmbRole.SelectedItem;
                // Включаем выбор группы только если выбрана роль "Студент" (ID=1)
                cmbGroup.IsEnabled = selectedRole.RoleID == 1;
                if (!cmbGroup.IsEnabled)
                {
                    cmbGroup.SelectedItem = null;
                }
            }
        }



        private void LoadRoles()
        {
            try
            {
                var roles = MainWindow.db.Roles.ToList();
                cmbRole.ItemsSource = roles;
                cmbRole.DisplayMemberPath = "RoleName";
                cmbRole.SelectedValuePath = "RoleID";

                // Загрузка фильтра ролей
                var rolesForFilter = roles.ToList();
                rolesForFilter.Insert(0, new DataBase.Roles { RoleID = 0, RoleName = "Все роли" });
                cmbRoleFilter.ItemsSource = rolesForFilter;
                cmbRoleFilter.DisplayMemberPath = "RoleName";
                cmbRoleFilter.SelectedValuePath = "RoleID";
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
                    .Include("Roles")
                    .Include("Groups")
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToList();

                // Группа уже загружена через Include, привязка {Binding Groups.GroupName} работает
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
                    .Include("Subjects") // или .Include(t => t.Subjects)
                    .Include("Users")    // или .Include(t => t.Users) для создателя
                    .OrderByDescending(t => t.CreatedDate)
                    .ToList();

                // Создаем анонимный тип для отображения
                var testView = tests.Select(t => new
                {
                    t.TestID,
                    t.TestName,
                    SubjectName = t.Subjects?.SubjectName ?? "Не указан",
                    CreatorName = t.Users != null ? t.Users.LastName + " " + t.Users.FirstName : "Неизвестно",
                    t.TimeLimit,
                    t.PassingScore,
                    t.IsActive
                }).ToList();

                lvTests.ItemsSource = testView;
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
            cmbGroup.SelectedItem = null;  
            cmbGroup.IsEnabled = false;
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

                string searchText = txtSearch.Text?.ToLower().Trim() ?? "";
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    // Получаем ID групп, которые соответствуют поиску
                    var matchingGroupIds = MainWindow.db.Groups
                        .Where(g => g.GroupName.ToLower().Contains(searchText))
                        .Select(g => g.GroupID)
                        .ToList();

                    query = query.Where(u =>
                        (u.Username != null && u.Username.ToLower().Contains(searchText)) ||
                        (u.FirstName != null && u.FirstName.ToLower().Contains(searchText)) ||
                        (u.LastName != null && u.LastName.ToLower().Contains(searchText)) ||
                        (u.Email != null && u.Email.ToLower().Contains(searchText)) ||
                        (u.GroupID != null && matchingGroupIds.Contains(u.GroupID.Value)));
                }

                var filteredUsers = query
                    .Include("Roles")
                    .Include("Groups")
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToList();

                lvUsers.ItemsSource = filteredUsers;
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

            var selectedRole = (DataBase.Roles)cmbRole.SelectedItem;

            // Проверка выбора группы для студентов
            if (selectedRole.RoleID == 1 && cmbGroup.SelectedItem == null)
            {
                MessageBox.Show("Для студента обязательно выбрать группу!", "Ошибка",
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
                        RoleID = selectedRole.RoleID,
                        IsActive = true,
                        CreatedDate = DateTime.Now,
                        GroupID = selectedRole.RoleID == 1 ? (int?)cmbGroup.SelectedValue : null 
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
                        user.RoleID = selectedRole.RoleID;
                        user.GroupID = selectedRole.RoleID == 1 ? (int?)cmbGroup.SelectedValue : null;  

                        MainWindow.db.SaveChanges();

                        MessageBox.Show("Пользователь успешно обновлен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                ClearForm();
                LoadUsers();
                ApplyUserFilters();
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
                var dbUser = MainWindow.db.Users
                    .Include("Groups")  // Добавьте Include для загрузки группы
                    .FirstOrDefault(u => u.UserID == userId);

                if (dbUser != null)
                {
                    txtUsername.Text = dbUser.Username;
                    txtPassword.Password = "";
                    txtFirstName.Text = dbUser.FirstName;
                    txtLastName.Text = dbUser.LastName;
                    txtEmail.Text = dbUser.Email;
                    cmbRole.SelectedValue = dbUser.RoleID;

                    // Загружаем группу, если она есть
                    cmbGroup.IsEnabled = dbUser.RoleID == 1;
                    if (dbUser.GroupID.HasValue)
                    {
                        cmbGroup.SelectedValue = dbUser.GroupID.Value;
                    }
                    else
                    {
                        cmbGroup.SelectedItem = null;
                    }

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
                // Получаем данные из анонимного типа
                dynamic user = button.DataContext;
                int userId = user.UserID;

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
                        var dbUser = MainWindow.db.Users.Find(userId);
                        if (dbUser != null)
                        {
                            // Проверяем, есть ли связанные результаты тестов
                            var hasResults = MainWindow.db.TestResults.Any(tr => tr.UserID == userId);
                            if (hasResults)
                            {
                                // Если есть результаты, просто деактивируем
                                dbUser.IsActive = false;
                                MessageBox.Show("Пользователь имеет результаты тестов и был деактивирован", "Информация",
                                    MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                // Если нет результатов, можно удалить полностью
                                MainWindow.db.Users.Remove(dbUser);
                            }

                            MainWindow.db.SaveChanges();

                            LoadUsers();
                            ApplyUserFilters();
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
            NavigationService.Navigate(new TestEditPage(0));
        }

        private void btnEditTest_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                dynamic test = button.DataContext;
                int testId = test.TestID;
                NavigationService.Navigate(new TestEditPage(testId));
            }
        }

        private void btnDeleteTest_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                dynamic test = button.DataContext;
                int testId = test.TestID;

                var result = MessageBox.Show("Вы уверены, что хотите удалить этот тест?\n" +
                    "Все связанные данные (вопросы, ответы, результаты) будут также удалены!",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var dbTest = MainWindow.db.Tests.Find(testId);
                        if (dbTest != null)
                        {
                            MainWindow.db.Tests.Remove(dbTest);
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
