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
    /// Логика взаимодействия для TeacherMainPage.xaml
    /// </summary>
    public partial class TeacherMainPage : Page
    {
        public TeacherMainPage()
        {
            InitializeComponent();
            LoadUserData();
            LoadTests();
            LoadFilters();
            LoadResults();
        }

        private void LoadUserData()
        {
            if (App.CurrentUser != null)
            {
                txtUserInfo.Text = $"{App.CurrentUser.FirstName} {App.CurrentUser.LastName} (Преподаватель)";
            }
        }

        private void LoadTests()
        {
            try
            {
                List<DataBase.Tests> tests;

                if (App.CurrentUser.RoleID == 2) // Преподаватель
                {
                    // Получаем группы преподавателя
                    var myGroupIds = MainWindow.db.TeacherGroups
                        .Where(tg => tg.TeacherID == App.CurrentUser.UserID)
                        .Select(tg => tg.GroupID)
                        .ToList();

                    // Получаем тесты, назначенные этим группам
                    var testIdsForMyGroups = MainWindow.db.TestGroups
                        .Where(tg => myGroupIds.Contains(tg.GroupID))
                        .Select(tg => tg.TestID)
                        .Distinct()
                        .ToList();

                    tests = MainWindow.db.Tests
                        .Where(t => testIdsForMyGroups.Contains(t.TestID) || t.CreatedBy == App.CurrentUser.UserID)
                        .OrderByDescending(t => t.CreatedDate)
                        .ToList();
                }
                else // Администратор
                {
                    tests = MainWindow.db.Tests
                        .OrderByDescending(t => t.CreatedDate)
                        .ToList();
                }

                lvTests.ItemsSource = tests;
                txtNoTests.Visibility = tests.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке тестов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadFilters()
        {
            try
            {
                // Получаем группы преподавателя
                var myGroupIds = MainWindow.db.TeacherGroups
                    .Where(tg => tg.TeacherID == App.CurrentUser.UserID)
                    .Select(tg => tg.GroupID)
                    .ToList();

                // Получаем студентов из групп преподавателя (без форматирования)
                var studentsFromDb = MainWindow.db.Users
                    .Include("Groups")
                    .Where(u => u.RoleID == 1 && myGroupIds.Contains(u.GroupID ?? 0))
                    .ToList();

                // Форматируем на стороне клиента
                var students = studentsFromDb.Select(u => new {
                    u.UserID,
                    FullName = u.LastName + " " + u.FirstName + (u.Groups != null ? " (гр. " + u.Groups.GroupName + ")" : "")
                }).ToList();

                students.Insert(0, new { UserID = 0, FullName = "Все студенты" });
                cmbStudentFilter.ItemsSource = students;
                cmbStudentFilter.SelectedIndex = 0;

                var tests = MainWindow.db.Tests.ToList();
                tests.Insert(0, new DataBase.Tests { TestID = 0, TestName = "Все тесты" });
                cmbTestFilter.ItemsSource = tests;
                cmbTestFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadResults()
        {
            try
            {
                var results = MainWindow.db.TestResults
                    .OrderByDescending(r => r.EndTime)
                    .ToList();
                lvResults.ItemsSource = results;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке результатов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var query = MainWindow.db.TestResults.AsQueryable();

                int testId = (int)(cmbTestFilter.SelectedValue ?? 0);
                if (testId > 0)
                {
                    query = query.Where(r => r.TestID == testId);
                }

                int studentId = (int)(cmbStudentFilter.SelectedValue ?? 0);
                if (studentId > 0)
                {
                    query = query.Where(r => r.UserID == studentId);
                }

                lvResults.ItemsSource = query.OrderByDescending(r => r.EndTime).ToList();
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

        private void btnAddTest_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TestEditPage(0));
        }

        private void txtStatus_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlock txt = sender as TextBlock;
            if (txt != null)
            {
                var test = txt.DataContext as dynamic;
                if (test != null)
                {
                    bool isActive = test.IsActive;
                    txt.Text = isActive ? "Активен" : "Неактивен";
                    txt.Foreground = isActive ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Gray);
                }
            }
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
                    "Все связанные вопросы и ответы будут также удалены!",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var test = MainWindow.db.Tests.Find(testId);
                        if (test != null)
                        {
                            // Для преподавателя лучше деактивировать, а не удалять
                            test.IsActive = false;
                            MainWindow.db.SaveChanges();

                            LoadTests();
                            MessageBox.Show("Тест успешно деактивирован", "Успех",
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

        private void cmbTestFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void cmbStudentFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }


        private void txtStatusResult_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlock txt = sender as TextBlock;
            if (txt != null)
            {
                var result = txt.DataContext as dynamic;
                if (result != null)
                {
                    bool isPassed = result.IsPassed;
                    txt.Text = isPassed ? "Сдал" : "Не сдал";
                    txt.Foreground = isPassed ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                }
            }
        }

        private void btnViewStudentResult_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int resultId = (int)button.Tag;
                NavigationService.Navigate(new TestResultPage(resultId));
            }
        }

        private void btnManageGroups_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TeacherGroupsPage());
        }
    }
}
