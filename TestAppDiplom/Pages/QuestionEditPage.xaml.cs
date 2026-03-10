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
    /// Логика взаимодействия для QuestionEditPage.xaml
    /// </summary>
    public partial class QuestionEditPage : Page
    {
        private int testId;
        private int questionId;
        private DataBase.Questions currentQuestion;
        private List<AnswerItem> answers = new List<AnswerItem>();
        private int nextTempId = -1;

        public QuestionEditPage(int testId, int questionId)
        {
            InitializeComponent();
            this.testId = testId;
            this.questionId = questionId;

            if (questionId == 0)
            {
                txtTitle.Text = "Добавление нового вопроса";
                currentQuestion = new DataBase.Questions
                {
                    TestID = testId,
                    QuestionType = 1,
                    Points = 1
                };
            }
            else
            {
                txtTitle.Text = "Редактирование вопроса";
                LoadQuestion();
            }
        }

        private void LoadQuestion()
        {
            try
            {
                currentQuestion = MainWindow.db.Questions.FirstOrDefault(q => q.QuestionID == questionId);
                if (currentQuestion == null)
                {
                    MessageBox.Show("Вопрос не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    NavigationService.GoBack();
                    return;
                }

                // Заполняем информацию о вопросе
                txtQuestionText.Text = currentQuestion.QuestionText;
                txtPoints.Text = currentQuestion.Points?.ToString() ?? "1";

                // Устанавливаем тип вопроса
                foreach (ComboBoxItem item in cmbQuestionType.Items)
                {
                    if (item.Tag.ToString() == (currentQuestion.QuestionType ?? 1).ToString())
                    {
                        cmbQuestionType.SelectedItem = item;
                        break;
                    }
                }

                // Загружаем ответы
                LoadAnswers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке вопроса: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAnswers()
        {
            try
            {
                var dbAnswers = MainWindow.db.Answers
                    .Where(a => a.QuestionID == questionId)
                    .OrderBy(a => a.SortOrder)
                    .ToList();

                answers.Clear();
                foreach (var a in dbAnswers)
                {
                    answers.Add(new AnswerItem
                    {
                        AnswerID = a.AnswerID,
                        AnswerText = a.AnswerText,
                        IsCorrect = a.IsCorrect ?? false,
                        SortOrder = a.SortOrder ?? 0
                    });
                }

                RefreshAnswersList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке ответов: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshAnswersList()
        {
            icAnswers.ItemsSource = null;
            icAnswers.ItemsSource = answers;
            txtNoAnswers.Visibility = answers.Any() ? Visibility.Collapsed : Visibility.Visible;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(txtQuestionText.Text))
            {
                MessageBox.Show("Введите текст вопроса!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtPoints.Text, out int points) || points < 1)
            {
                MessageBox.Show("Введите корректное количество баллов (минимум 1)!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!answers.Any())
            {
                MessageBox.Show("Добавьте хотя бы один вариант ответа!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!answers.Any(a => a.IsCorrect))
            {
                MessageBox.Show("Выберите правильный ответ (или ответы)!", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Сохраняем вопрос
                currentQuestion.QuestionText = txtQuestionText.Text;
                currentQuestion.QuestionType = int.Parse(((ComboBoxItem)cmbQuestionType.SelectedItem).Tag.ToString());
                currentQuestion.Points = points;
                currentQuestion.SortOrder = 0; // Можно добавить логику сортировки

                if (questionId == 0)
                {
                    MainWindow.db.Questions.Add(currentQuestion);
                }

                MainWindow.db.SaveChanges();
                questionId = currentQuestion.QuestionID;

                // Сохраняем ответы
                // Удаляем старые ответы, если они были
                var oldAnswers = MainWindow.db.Answers.Where(a => a.QuestionID == questionId).ToList();
                foreach (var oldAnswer in oldAnswers)
                {
                    MainWindow.db.Answers.Remove(oldAnswer);
                }

                // Добавляем новые ответы
                foreach (var answer in answers)
                {
                    var newAnswer = new DataBase.Answers
                    {
                        QuestionID = questionId,
                        AnswerText = answer.AnswerText,
                        IsCorrect = answer.IsCorrect,
                        SortOrder = answer.SortOrder
                    };
                    MainWindow.db.Answers.Add(newAnswer);
                }

                MainWindow.db.SaveChanges();

                MessageBox.Show("Вопрос успешно сохранен!", "Успех",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // Возвращаемся к редактированию теста
                NavigationService.Navigate(new TestEditPage(testId));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new TestEditPage(testId));
        }

        private void cmbQuestionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbQuestionType.SelectedItem != null)
            {
                int type = int.Parse(((ComboBoxItem)cmbQuestionType.SelectedItem).Tag.ToString());

                // Если тип изменился на "один вариант", снимаем лишние правильные ответы
                if (type == 1 && answers.Count(a => a.IsCorrect) > 1)
                {
                    bool firstFound = false;
                    foreach (var answer in answers)
                    {
                        if (answer.IsCorrect)
                        {
                            if (!firstFound)
                                firstFound = true;
                            else
                                answer.IsCorrect = false;
                        }
                    }
                    RefreshAnswersList();
                }
            }
        }

        private void btnAddAnswer_Click(object sender, RoutedEventArgs e)
        {
            int newSortOrder = answers.Any() ? answers.Max(a => a.SortOrder) + 1 : 0;

            answers.Add(new AnswerItem
            {
                AnswerID = nextTempId--,
                AnswerText = "Новый ответ",
                IsCorrect = false,
                SortOrder = newSortOrder
            });

            RefreshAnswersList();
        }

        private void chkIsCorrect_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            if (checkBox != null)
            {
                var answer = checkBox.DataContext as AnswerItem;
                if (answer != null)
                {
                    answer.IsCorrect = checkBox.IsChecked ?? false;

                    // Если тип вопроса - один вариант, снимаем правильность с других ответов
                    if (cmbQuestionType.SelectedItem != null)
                    {
                        int type = int.Parse(((ComboBoxItem)cmbQuestionType.SelectedItem).Tag.ToString());
                        if (type == 1 && answer.IsCorrect)
                        {
                            foreach (var otherAnswer in answers)
                            {
                                if (otherAnswer.AnswerID != answer.AnswerID)
                                    otherAnswer.IsCorrect = false;
                            }
                            RefreshAnswersList();
                        }
                    }
                }
            }
        }

        private void txtAnswerText_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                var answer = textBox.DataContext as AnswerItem;
                if (answer != null)
                {
                    answer.AnswerText = textBox.Text;
                }
            }
        }

        private void btnMoveAnswerUp_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int answerId = (int)button.Tag;
                int index = answers.FindIndex(a => a.AnswerID == answerId);

                if (index > 0)
                {
                    // Меняем местами
                    var temp = answers[index];
                    answers[index] = answers[index - 1];
                    answers[index - 1] = temp;

                    // Обновляем SortOrder
                    for (int i = 0; i < answers.Count; i++)
                    {
                        answers[i].SortOrder = i;
                    }

                    RefreshAnswersList();
                }
            }
        }


        private void btnDeleteAnswer_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int answerId = (int)button.Tag;

                var answer = answers.FirstOrDefault(a => a.AnswerID == answerId);
                if (answer != null)
                {
                    answers.Remove(answer);
                    RefreshAnswersList();
                }
            }
        }

        private void btnMoveAnswerDown_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                int answerId = (int)button.Tag;
                int index = answers.FindIndex(a => a.AnswerID == answerId);

                if (index < answers.Count - 1)
                {
                    // Меняем местами
                    var temp = answers[index];
                    answers[index] = answers[index + 1];
                    answers[index + 1] = temp;

                    // Обновляем SortOrder
                    for (int i = 0; i < answers.Count; i++)
                    {
                        answers[i].SortOrder = i;
                    }

                    RefreshAnswersList();
                }
            }
        }
    }

    public class AnswerItem
    {
        public int AnswerID { get; set; }
        public string AnswerText { get; set; }
        public bool IsCorrect { get; set; }
        public int SortOrder { get; set; }
    }

}
