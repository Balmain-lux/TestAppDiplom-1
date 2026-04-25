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
    /// Логика взаимодействия для TeacherGroupsPage.xaml
    /// </summary>
    public partial class TeacherGroupsPage : Page
    {
        public TeacherGroupsPage()
        {
            InitializeComponent();
            LoadGroups();
        }

        private void LoadGroups()
        {
            try
            {
                var allGroups = MainWindow.db.Groups
                    .OrderBy(g => g.Specialty)
                    .ThenBy(g => g.GroupName)
                    .ToList();

                var myGroups = MainWindow.db.TeacherGroups
                    .Where(tg => tg.TeacherID == App.CurrentUser.UserID)
                    .Select(tg => tg.Groups)
                    .ToList();

                var availableGroups = allGroups.Where(g => !myGroups.Any(mg => mg.GroupID == g.GroupID)).ToList();
                lvAvailableGroups.ItemsSource = availableGroups;
                lvMyGroups.ItemsSource = myGroups;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки групп: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TeacherMainPage());
        }

        private void lvAvailableGroups_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            btnAddGroup.IsEnabled = lvAvailableGroups.SelectedItem != null;
        }

        private void btnAddGroup_Click(object sender, RoutedEventArgs e)
        {
            var selectedGroup = lvAvailableGroups.SelectedItem as DataBase.Groups;
            if (selectedGroup == null) return;

            try
            {
                var teacherGroup = new DataBase.TeacherGroups
                {
                    TeacherID = App.CurrentUser.UserID,
                    GroupID = selectedGroup.GroupID
                };

                MainWindow.db.TeacherGroups.Add(teacherGroup);
                MainWindow.db.SaveChanges();

                LoadGroups();
                MessageBox.Show("Группа успешно добавлена!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка добавления группы: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnRemoveGruop_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int groupId = (int)button.Tag;

                var result = MessageBox.Show("Удалить эту группу?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var teacherGroup = MainWindow.db.TeacherGroups
                            .FirstOrDefault(tg => tg.TeacherID == App.CurrentUser.UserID && tg.GroupID == groupId);

                        if (teacherGroup != null)
                        {
                            MainWindow.db.TeacherGroups.Remove(teacherGroup);
                            MainWindow.db.SaveChanges();

                            LoadGroups();
                            MessageBox.Show("Группа удалена!", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
