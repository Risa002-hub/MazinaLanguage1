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
using System.Data.Entity;

namespace MazinaLanguage1
{
    public partial class ServicePage : Page
    {
       
        private int currentPage = 0;
        private int pageSize = 10;           // размер страницы (10, 50, 200, 0=все)
        private int totalRecords = 0;
        private List<Client> filteredClients; // отфильтрованный и отсортированный список

        public ServicePage()
        {
            InitializeComponent();

            PageSizeComboBox.SelectedIndex = 0;   // допустим, первый элемент "10"
            GenderFilterComboBox.SelectedIndex = 0;
            SortComboBox.SelectedIndex = 0; 

            LoadClients();
        }

        //фильтр поиск сортировка
        private void LoadClients()
        {
            using (var db = new MazinaLanguageEntities())
            {
                IQueryable<Client> query = db.Client.Include(c => c.ClientService);

                //по полу
                if (GenderFilterComboBox.SelectedItem is ComboBoxItem selectedGender && selectedGender.Content.ToString() != "Все")
                {
                    string genderCode = selectedGender.Content.ToString() == "Мужской" ? "м" : "ж";
                    query = query.Where(c => c.GenderCode == genderCode);
                }

                // поиск (ФИО, email, телефон)
                string searchText = SearchTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(searchText))
                {
                    string cleanSearch = searchText.ToLower().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
                    query = query.Where(c =>
                        (c.LastName + " " + c.FirstName + " " + c.Patronymic).ToLower().Contains(searchText.ToLower()) ||
                        c.Email.ToLower().Contains(searchText.ToLower()) ||
                        c.Phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Contains(cleanSearch)
                    );
                }

                //сортировка
                string sortBy = (SortComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                switch (sortBy)
                {
                    case "Фамилия (А-Я)":
                        query = query.OrderBy(c => c.LastName);
                        break;
                    case "Дата последнего посещения (новые→старые)":
                        query = query.OrderByDescending(c => c.ClientService.Max(cs => (DateTime?)cs.StartTime));
                        break;
                    case "Количество посещений (по убыванию)":
                        query = query.OrderByDescending(c => c.ClientService.Count());
                        break;
                    default:
                        query = query.OrderBy(c => c.LastName);
                        break;
                }

                // Выполняем запрос и сохраняем в filteredClients
                filteredClients = query.ToList();
            }
            currentPage = 0;
            ChangePage(0, null);
        }

        // смена страницы
        private void ChangePage(int direction, int? selectedPage)
        {
            if (filteredClients == null) return;

            totalRecords = filteredClients.Count;
            int totalPages = (pageSize == 0) ? 1 : (int)Math.Ceiling((double)totalRecords / pageSize);

            // Определяем новую страницу
            if (selectedPage.HasValue)
                currentPage = selectedPage.Value;
            else if (direction == -1) currentPage--;
            else if (direction == 1) currentPage++;

            // Корректировка границ
            if (currentPage < 0) currentPage = 0;
            if (totalPages > 0 && currentPage >= totalPages) currentPage = totalPages - 1;

            // Формируем список для текущей страницы
            List<Client> pageClients;
            if (pageSize == 0)  // "все"
                pageClients = filteredClients.ToList();
            else
                pageClients = filteredClients.Skip(currentPage * pageSize).Take(pageSize).ToList();

            foreach (var client in pageClients)
            {
                // Проверяем, загружена ли коллекция ClientService
                if (client.ClientService != null && client.ClientService.Any())
                {
                    client.LastVisitDate = client.ClientService.Max(cs => cs.StartTime);
                    client.VisitsCount = client.ClientService.Count();
                }
                else
                {
                    client.LastVisitDate = null;
                    client.VisitsCount = 0;
                }

            }
                //отображение 
                ServiceListView.ItemsSource = pageClients;
            InfoLabel.Content = $"{pageClients.Count} из {totalRecords}";

            //навигация
            LeftButton.IsEnabled = (currentPage > 0);
            RightButton.IsEnabled = (pageSize != 0) && (currentPage + 1 < totalPages);

            UpdatePageListBox(totalPages);
        }

        //список страниц
        private void UpdatePageListBox(int totalPages)
        {
            PageListBox.Items.Clear();
            for (int i = 1; i <= totalPages; i++)
                PageListBox.Items.Add(i);
            if (totalPages > 0)
                PageListBox.SelectedIndex = currentPage;
        }

        private void LeftButton_Click(object sender, RoutedEventArgs e) => ChangePage(-1, null);
        private void RightButton_Click(object sender, RoutedEventArgs e) => ChangePage(1, null);

        private void PageListBox_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (PageListBox.SelectedItem != null && int.TryParse(PageListBox.SelectedItem.ToString(), out int pageNumber))
                ChangePage(0, pageNumber - 1);
        }

        private void PageSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PageSizeComboBox.SelectedItem is ComboBoxItem item)
            {
                string content = item.Content.ToString();
                if (content == "Все")
                    pageSize = 0;
                else
                    pageSize = int.Parse(content);

                currentPage = 0;
                ChangePage(0, null);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = ServiceListView.SelectedItem as Client;
            
            using (var db = new MazinaLanguageEntities())
            {
                bool hasVisits = db.ClientService.Any(cs => cs.ClientID == selected.ID);
                if (hasVisits)
                {
                    MessageBox.Show("Нельзя удалить клиента, у которого есть посещения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var clientToDelete = db.Client.Find(selected.ID);
                db.Client.Remove(clientToDelete);
                db.SaveChanges();
            }

            LoadClients();
        }
        private void AddEditItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedClient = ServiceListView.SelectedItem as Client;

            if (selectedClient != null)
            {
                Manager.MainFrame.Navigate(new AddEditPage(selectedClient));
            }
            else
            {
                MessageBox.Show("Выберите клиента для редактирования");
            }
        }

        // Поиск, фильтр по полу, сортировка – при изменении вызываем LoadClients()
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => LoadClients();
        private void GenderFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadClients();
        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadClients();
    }
}
