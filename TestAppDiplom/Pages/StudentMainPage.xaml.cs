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
    /// Логика взаимодействия для StudentMainPage.xaml
    /// </summary>
    public partial class StudentMainPage : Page
    {
        public StudentMainPage()
        {
            InitializeComponent();
            LoadUserData();
            LoadAvailableTests();
            LoadCompletedTests();
        }

        private void LoadUserData()
        {
            if (App.CurrentUser != null)
            {
                // Загружаем пользователя с группой
                var userWithGroup = MainWindow.db.Users
                    .Include("Groups")
                    .FirstOrDefault(u => u.UserID == App.CurrentUser.UserID);

                string groupInfo = "";
                if (userWithGroup != null && userWithGroup.GroupID.HasValue && userWithGroup.Groups != null)
                {
                    groupInfo = $" (Группа: {userWithGroup.Groups.GroupName}, {userWithGroup.Groups.Specialty})";
                    App.CurrentUser.Groups = userWithGroup.Groups; // Обновляем ссылку
                }
                txtUserInfo.Text = $"{userWithGroup.FirstName} {userWithGroup.LastName}{groupInfo}";
            }
        }

        private void LoadAvailableTests()
        {
            try
            {
                var completedTestIds = MainWindow.db.TestResults
                    .Where(tr => tr.UserID == App.CurrentUser.UserID)
                    .Select(tr => tr.TestID)
                    .Distinct()
                    .ToList();

                var availableTests = MainWindow.db.Tests
                    .Where(t => t.IsActive == true && !completedTestIds.Contains(t.TestID))
                    .OrderBy(t => t.TestName)
                    .ToList();

                lvAvailableTests.ItemsSource = availableTests;
                txtNoAvailableTests.Visibility = availableTests.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке тестов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadCompletedTests()
        {
            try
            {
                var completedTests = MainWindow.db.TestResults
                    .Where(tr => tr.UserID == App.CurrentUser.UserID)
                    .OrderByDescending(tr => tr.EndTime)
                    .ToList();

                lvCompletedTests.ItemsSource = completedTests;
                txtNoCompletedTests.Visibility = completedTests.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке результатов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentUser = null;
            NavigationService.Navigate(new LoginPage());
        }

        private void btnStartTest_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int testId = (int)button.Tag;
                NavigationService.Navigate(new TestPassingPage(testId));
            }
        }

        private void btnViewResult_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int resultId = (int)button.Tag;
                NavigationService.Navigate(new TestResultPage(resultId));
            }
        }

        private void txtPercentage_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlock txt = sender as TextBlock;
            if (txt != null)
            {
                var result = txt.DataContext as dynamic;
                if (result != null)
                {
                    bool isPassed = result.IsPassed;
                    txt.Foreground = isPassed ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                }
            }
        }

        private void txtStatus_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlock txt = sender as TextBlock;
            if (txt != null)
            {
                var result = txt.DataContext as dynamic;
                if (result != null)
                {
                    bool isPassed = result.IsPassed;
                    txt.Text = isPassed ? "Сдан" : "Не сдан";
                    txt.Foreground = isPassed ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                }
            }
        }
    }
}
