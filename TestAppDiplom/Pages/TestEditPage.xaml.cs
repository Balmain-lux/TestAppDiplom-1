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
    /// Логика взаимодействия для TestEditPage.xaml
    /// </summary>
    public partial class TestEditPage : Page
    {
        private int testId;
        private DataBase.Tests currentTest;
        private List<QuestionItem> questions = new List<QuestionItem>();

        public TestEditPage(int testId)
        {
            InitializeComponent();
            this.testId = testId;
            LoadSubjects();

            if (testId == 0)
            {
                txtTitle.Text = "Создание нового теста";
                currentTest = new DataBase.Tests
                {
                    IsActive = true,
                    PassingScore = 60
                };
            }
            else
            {
                txtTitle.Text = "Редактирование теста";
                LoadTest();
            }
        }

        private void LoadSubjects()
        {
            try
            {
                var subjects = MainWindow.db.Subjects.ToList();
                cmbSubject.ItemsSource = subjects;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке предметов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTest()
        {
            try
            {
                currentTest = MainWindow.db.Tests.FirstOrDefault(t => t.TestID == testId);
                if (currentTest == null)
                {
                    MessageBox.Show("Тест не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService.GoBack();
                    return;
                }

                // Заполняем основную информацию
                txtTestName.Text = currentTest.TestName;
                txtDescription.Text = currentTest.Description;
                txtTimeLimit.Text = currentTest.TimeLimit?.ToString() ?? "";
                txtPassingScore.Text = currentTest.PassingScore.ToString();
                cmbSubject.SelectedValue = currentTest.SubjectID;
                chkIsActive.IsChecked = currentTest.IsActive;

                // Загружаем вопросы
                LoadQuestions();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке теста: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadQuestions()
        {
            try
            {
                var dbQuestions = MainWindow.db.Questions
                    .Where(q => q.TestID == testId)
                    .OrderBy(q => q.SortOrder)
                    .ToList();

                questions.Clear();
                int number = 1;
                foreach (var q in dbQuestions)
                {
                    questions.Add(new QuestionItem
                    {
                        QuestionID = q.QuestionID,
                        QuestionNumber = $"Вопрос {number++}",
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType ?? 1,
                        QuestionTypeName = q.QuestionType == 1 ? "Один вариант" : "Несколько вариантов",
                        Points = q.Points ?? 1
                    });
                }

                lvQuestions.ItemsSource = questions;
                txtNoQuestions.Visibility = questions.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке вопросов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(txtTestName.Text))
            {
                MessageBox.Show("Введите название теста!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbSubject.SelectedItem == null)
            {
                MessageBox.Show("Выберите предмет!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtPassingScore.Text, out int passingScore) ||
                passingScore < 0 || passingScore > 100)
            {
                MessageBox.Show("Введите корректный проходной балл (0-100)!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Сохраняем основную информацию
                currentTest.TestName = txtTestName.Text;
                currentTest.Description = txtDescription.Text;
                currentTest.SubjectID = (int)cmbSubject.SelectedValue;
                currentTest.PassingScore = passingScore;
                currentTest.IsActive = chkIsActive.IsChecked ?? true;

                if (!string.IsNullOrWhiteSpace(txtTimeLimit.Text))
                {
                    if (int.TryParse(txtTimeLimit.Text, out int timeLimit))
                        currentTest.TimeLimit = timeLimit;
                }

                if (testId == 0)
                {
                    currentTest.CreatedBy = App.CurrentUser.UserID;
                    currentTest.CreatedDate = DateTime.Now;
                    MainWindow.db.Tests.Add(currentTest);
                }

                MainWindow.db.SaveChanges();
                testId = currentTest.TestID;

                MessageBox.Show("Тест успешно сохранен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Возвращаемся на предыдущую страницу
                if (App.CurrentUser.RoleID == 3) // Админ
                    NavigationService.Navigate(new AdminMainPage());
                else // Преподаватель
                    NavigationService.Navigate(new TeacherMainPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleID == 3) // Админ
                NavigationService.Navigate(new AdminMainPage());
            else // Преподаватель
                NavigationService.Navigate(new TeacherMainPage());
        }

        private void btnAddQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (testId == 0 && currentTest.TestID == 0)
            {
                MessageBox.Show("Сначала сохраните основную информацию о тесте!", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            NavigationService.Navigate(new QuestionEditPage(testId, 0));
        }

        private void btnEditQuestion_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int questionId = (int)button.Tag;
                NavigationService.Navigate(new QuestionEditPage(testId, questionId));
            }
        }

        private void btnDeleteQuestion_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int questionId = (int)button.Tag;

                var result = MessageBox.Show("Удалить этот вопрос?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var question = MainWindow.db.Questions.Find(questionId);
                        if (question != null)
                        {
                            MainWindow.db.Questions.Remove(question);
                            MainWindow.db.SaveChanges();
                            LoadQuestions();
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
        public class QuestionItem
        {
            public int QuestionID { get; set; }
            public string QuestionNumber { get; set; }
            public string QuestionText { get; set; }
            public int QuestionType { get; set; }
            public string QuestionTypeName { get; set; }
            public int Points { get; set; }
        }
    }
}
