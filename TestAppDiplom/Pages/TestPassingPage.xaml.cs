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
using System.Windows.Threading;

namespace TestAppDiplom.Pages
{
    /// <summary>
    /// Логика взаимодействия для TestPassingPage.xaml
    /// </summary>
    public partial class TestPassingPage : Page
    {
        private int testId;
        private DataBase.Tests currentTest;
        private List<DataBase.Questions> questions;
        private int currentQuestionIndex = 0;
        private Dictionary<int, List<int>> userAnswers = new Dictionary<int, List<int>>();
        private DispatcherTimer timer;
        private DateTime startTime;
        private int? resultId = null;

        public TestPassingPage(int testId)
        {
            InitializeComponent();
            this.testId = testId;
            LoadTest();
            StartTimer();
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

                // Загружаем вопросы
                questions = MainWindow.db.Questions
                    .Where(q => q.TestID == testId)
                    .OrderBy(q => q.SortOrder)
                    .ToList();

                if (!questions.Any())
                {
                    MessageBox.Show("В тесте нет вопросов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService.GoBack();
                    return;
                }

                // Отображаем информацию о тесте
                txtTestName.Text = currentTest.TestName;
                txtTestInfo.Text = $"Всего вопросов: {questions.Count} | Максимальный балл: {questions.Sum(q => q.Points)}";

                // Создаем запись о начале теста
                var testResult = new DataBase.TestResults
                {
                    UserID = App.CurrentUser.UserID,
                    TestID = testId,
                    StartTime = DateTime.Now,
                    MaxScore = questions.Sum(q => q.Points)
                };

                MainWindow.db.TestResults.Add(testResult);
                MainWindow.db.SaveChanges();
                resultId = testResult.ResultID;

                startTime = DateTime.Now;

                // Показываем первый вопрос
                ShowQuestion(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке теста: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (currentTest.TimeLimit.HasValue && currentTest.TimeLimit.Value > 0)
            {
                var elapsed = DateTime.Now - startTime;
                var remaining = TimeSpan.FromMinutes(currentTest.TimeLimit.Value) - elapsed;

                if (remaining.TotalSeconds <= 0)
                {
                    timer.Stop();
                    MessageBox.Show("Время вышло! Тест будет завершен.", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    FinishTest();
                }
                else
                {
                    txtTimer.Text = remaining.ToString(@"hh\:mm\:ss");

                    if (remaining.TotalMinutes < 5)
                        txtTimer.Foreground = new SolidColorBrush(Colors.Orange);
                    if (remaining.TotalMinutes < 1)
                        txtTimer.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
        }

        private void ShowQuestion(int index)
        {
            if (index < 0 || index >= questions.Count) return;

            currentQuestionIndex = index;
            var question = questions[index];

            // Обновляем информацию о вопросе
            txtQuestionNumber.Text = $"Вопрос {index + 1} из {questions.Count}";
            txtQuestionText.Text = question.QuestionText;
            txtQuestionType.Text = question.QuestionType == 1 ? "(Один вариант)" : "(Несколько вариантов)";

            // Обновляем прогресс
            int progress = (int)((double)(index + 1) / questions.Count * 100);
            progressBar.Value = progress;
            txtProgress.Text = $"Прогресс: {index + 1}/{questions.Count} ({progress}%)";

            // Загружаем ответы для вопроса
            var answers = MainWindow.db.Answers
                .Where(a => a.QuestionID == question.QuestionID)
                .OrderBy(a => a.SortOrder)
                .ToList();

            // Очищаем и создаем новые элементы для ответов
            spAnswers.Children.Clear();

            if (question.QuestionType == 1) // Один вариант
            {
                foreach (var answer in answers)
                {
                    var radioButton = new RadioButton
                    {
                        Content = answer.AnswerText,
                        Tag = answer.AnswerID,
                        Margin = new Thickness(0, 5, 0, 5),
                        FontSize = 14,
                        GroupName = "Answers"
                    };

                    // Проверяем, был ли уже выбран этот ответ
                    if (userAnswers.ContainsKey(question.QuestionID) &&
                        userAnswers[question.QuestionID].Contains(answer.AnswerID))
                    {
                        radioButton.IsChecked = true;
                    }

                    radioButton.Checked += Answer_Changed;
                    spAnswers.Children.Add(radioButton);
                }
            }
            else // Несколько вариантов
            {
                foreach (var answer in answers)
                {
                    var checkBox = new CheckBox
                    {
                        Content = answer.AnswerText,
                        Tag = answer.AnswerID,
                        Margin = new Thickness(0, 5, 0, 5),
                        FontSize = 14
                    };

                    // Проверяем, был ли уже выбран этот ответ
                    if (userAnswers.ContainsKey(question.QuestionID) &&
                        userAnswers[question.QuestionID].Contains(answer.AnswerID))
                    {
                        checkBox.IsChecked = true;
                    }

                    checkBox.Checked += Answer_Changed;
                    checkBox.Unchecked += Answer_Changed;
                    spAnswers.Children.Add(checkBox);
                }
            }

            // Обновляем навигацию
            btnPrevious.IsEnabled = index > 0;

            if (index == questions.Count - 1)
                btnNext.Content = "Завершить";
            else
                btnNext.Content = "Следующий →";

            txtQuestionCounter.Text = $"{index + 1} / {questions.Count}";
        }

        private void Answer_Changed(object sender, RoutedEventArgs e)
        {
            var question = questions[currentQuestionIndex];

            // Собираем выбранные ответы
            var selectedAnswers = new List<int>();

            foreach (var child in spAnswers.Children)
            {
                if (child is RadioButton rb && rb.IsChecked == true)
                {
                    selectedAnswers.Add((int)rb.Tag);
                }
                else if (child is CheckBox cb && cb.IsChecked == true)
                {
                    selectedAnswers.Add((int)cb.Tag);
                }
            }

            // Сохраняем ответы
            if (selectedAnswers.Any())
                userAnswers[question.QuestionID] = selectedAnswers;
            else if (userAnswers.ContainsKey(question.QuestionID))
                userAnswers.Remove(question.QuestionID);
        }

        private void FinishTest()
        {
            try
            {
                timer?.Stop();

                if (!resultId.HasValue)
                {
                    NavigationService.Navigate(new StudentMainPage());
                    return;
                }

                var testResult = MainWindow.db.TestResults.Find(resultId.Value);
                if (testResult == null)
                {
                    NavigationService.Navigate(new StudentMainPage());
                    return;
                }

                // Подсчитываем баллы
                int totalScore = 0;

                foreach (var question in questions)
                {
                    if (userAnswers.ContainsKey(question.QuestionID))
                    {
                        var correctAnswers = MainWindow.db.Answers
                            .Where(a => a.QuestionID == question.QuestionID && a.IsCorrect == true)
                            .Select(a => a.AnswerID)
                            .ToList();

                        var userSelectedAnswers = userAnswers[question.QuestionID];

                        // Проверяем правильность ответа
                        bool isCorrect;
                        if (question.QuestionType == 1) // Один вариант
                        {
                            isCorrect = correctAnswers.Count == 1 &&
                                       userSelectedAnswers.Count == 1 &&
                                       correctAnswers.First() == userSelectedAnswers.First();
                        }
                        else // Несколько вариантов
                        {
                            isCorrect = correctAnswers.Count == userSelectedAnswers.Count &&
                                       !correctAnswers.Except(userSelectedAnswers).Any();
                        }

                        if (isCorrect)
                            totalScore += question.Points ?? 1;

                        // Сохраняем ответы пользователя
                        foreach (var answerId in userSelectedAnswers)
                        {
                            var userAnswer = new DataBase.UserAnswers
                            {
                                ResultID = resultId.Value,
                                QuestionID = question.QuestionID,
                                AnswerID = answerId,
                                IsSelected = true
                            };
                            MainWindow.db.UserAnswers.Add(userAnswer);
                        }
                    }
                }

                // Обновляем результат
                testResult.EndTime = DateTime.Now;
                testResult.Score = totalScore;
                testResult.IsPassed = (double)totalScore / testResult.MaxScore * 100 >= currentTest.PassingScore;

                MainWindow.db.SaveChanges();

                MessageBox.Show($"Тест завершен!\nВаш результат: {totalScore} из {testResult.MaxScore} баллов\n" +
                    $"Процент выполнения: {(double)totalScore / testResult.MaxScore * 100:F1}%",
                    "Результат", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.Navigate(new TestResultPage(resultId.Value));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при завершении теста: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                NavigationService.Navigate(new StudentMainPage());
            }
        }

        private void btnFinishTest_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите завершить тест досрочно?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                FinishTest();
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (currentQuestionIndex > 0)
                ShowQuestion(currentQuestionIndex - 1);
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (currentQuestionIndex < questions.Count - 1)
            {
                ShowQuestion(currentQuestionIndex + 1);
            }
            else
            {
                // Завершаем тест
                FinishTest();
            }
        }
    }
}
