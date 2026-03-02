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
using TestAppDiplom.Pages;

namespace TestAppDiplom
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public static StudentTestingDBEntities db = new StudentTestingDBEntities();

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                
                if (!db.Database.Exists())
                {
                    MessageBox.Show("Нет подключения к базе данных!", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }


                MainFrame.Navigate(new LoginPage());
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка при подключении к базе данных: {ex.Message}",
                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }

        }
    }
}
