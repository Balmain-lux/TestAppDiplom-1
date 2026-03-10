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
    /// Логика взаимодействия для TestResultPage.xaml
    /// </summary>
    public partial class TestResultPage : Page
    {
        private int resultId;
        public TestResultPage(int resultId)
        {
            InitializeComponent();
            this.resultId = resultId;
            LoadResult();
        }

        private void LoadResult()
        {
            try
            {
                var result = MainWindow.db.TestResults.FirstOrDefault(r => r.ResultID == resultId);
                if (result == null)
                {
                    MessageBox.Show("Результат не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService.GoBack();
                    return;
                }

                // Основная информация
                txtTestName.Text = $"Результат теста: {result.Tests.TestName}";

                double percentage = result.PercentageScore ?? 0;
                txtScore.Text = $"Баллы: {result.Score} из {result.MaxScore}";
                txtPercentage.Text = $"Процент выполнения: {percentage:F1}%";

                if (result.IsPassed == true)
                {
                    txtStatus.Text = "Статус: ТЕСТ СДАН";
                    txtStatus.Foreground = new SolidColorBrush(Colors.Green);
                    StatusBorder.Background = new SolidColorBrush(Colors.Green);
                    txtStatusIcon.Text = "✓";
                }
                else
                {
                    txtStatus.Text = "Статус: ТЕСТ НЕ СДАН";
                    txtStatus.Foreground = new SolidColorBrush(Colors.Red);
                    StatusBorder.Background = new SolidColorBrush(Colors.Red);
                    txtStatusIcon.Text = "✗";
                }

                if (result.StartTime != null && result.EndTime != null)
                {
                    var timeSpent = result.EndTime - result.StartTime;
                    txtTimeSpent.Text = $"Время выполнения: {timeSpent.Value.ToString(@"hh\:mm\:ss")}";
                }

                // Загружаем детали по вопросам
                LoadQuestionDetails(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке результатов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadQuestionDetails(DataBase.TestResults result)
        {
            var questions = MainWindow.db.Questions
                .Where(q => q.TestID == result.TestID)
                .OrderBy(q => q.SortOrder)
                .ToList();

            var userAnswers = MainWindow.db.UserAnswers
                .Where(ua => ua.ResultID == resultId)
                .ToList();

            var questionDetails = new List<QuestionDetail>();

            int questionNumber = 1;
            foreach (var question in questions)
            {
                var detail = new QuestionDetail
                {
                    QuestionNumber = $"Вопрос {questionNumber++}:",
                    QuestionText = question.QuestionText
                };

                // Получаем ответы пользователя на этот вопрос
                var userAnswersForQuestion = userAnswers
                    .Where(ua => ua.QuestionID == question.QuestionID)
                    .Select(ua => ua.AnswerID)
                    .ToList();

                // Получаем правильные ответы
                var correctAnswers = MainWindow.db.Answers
                    .Where(a => a.QuestionID == question.QuestionID && a.IsCorrect == true)
                    .ToList();

                // Формируем строку с ответами пользователя
                if (userAnswersForQuestion.Any())
                {
                    var userAnswerTexts = MainWindow.db.Answers
                        .Where(a => userAnswersForQuestion.Contains(a.AnswerID))
                        .Select(a => a.AnswerText)
                        .ToList();
                    detail.UserAnswer = string.Join(", ", userAnswerTexts);
                }
                else
                {
                    detail.UserAnswer = "Нет ответа";
                }

                // Формируем строку с правильными ответами
                var correctAnswerTexts = correctAnswers.Select(a => a.AnswerText).ToList();
                detail.CorrectAnswer = string.Join(", ", correctAnswerTexts);

                // Проверяем правильность ответа
                var correctAnswerIds = correctAnswers.Select(a => a.AnswerID).ToList();

                if (question.QuestionType == 1) // Один вариант
                {
                    detail.IsCorrect = correctAnswerIds.Count == 1 &&
                                      userAnswersForQuestion.Count == 1 &&
                                      correctAnswerIds.First() == userAnswersForQuestion.First();
                }
                else // Несколько вариантов
                {
                    detail.IsCorrect = correctAnswerIds.Count == userAnswersForQuestion.Count &&
                                      !correctAnswerIds.Except(userAnswersForQuestion).Any();
                }

                questionDetails.Add(detail);
            }

            lvQuestions.ItemsSource = questionDetails;
        }



        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleID == 1) // Студент
                NavigationService.Navigate(new StudentMainPage());
            else if (App.CurrentUser.RoleID == 2) // Преподаватель
                NavigationService.Navigate(new TeacherMainPage());
            else // Админ
                NavigationService.Navigate(new AdminMainPage());
        }

        private void ResultIndicator_Loaded(object sender, RoutedEventArgs e)
        {
            Border border = sender as Border;
            if (border != null)
            {
                var detail = border.DataContext as QuestionDetail;
                if (detail != null)
                {
                    border.Background = detail.IsCorrect ?
                        new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);

                    ToolTipService.SetToolTip(border, detail.IsCorrect ? "Верно" : "Неверно");
                }
            }
        }

        private void txtUserAnswer_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlock txt = sender as TextBlock;
            if (txt != null)
            {
                var detail = txt.DataContext as QuestionDetail;
                if (detail != null)
                {
                    txt.Foreground = detail.IsCorrect ?
                        new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                }
            }
        }

        private void txtCorrectAnswer_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlock txt = sender as TextBlock;
            if (txt != null)
            {
                txt.Foreground = new SolidColorBrush(Colors.Green);
            }
        }

        public class QuestionDetail
        {
            public string QuestionNumber { get; set; }
            public string QuestionText { get; set; }
            public string UserAnswer { get; set; }
            public string CorrectAnswer { get; set; }
            public bool IsCorrect { get; set; }
        }
    }
}
