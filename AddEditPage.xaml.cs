using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace MazinaLanguage1
{
    /// <summary>
    /// Логика взаимодействия для AddEditPage.xaml
    /// </summary>
    public partial class AddEditPage : Page
    {
        private Client _currentClient;
        private bool _isEditMode;
        private string _selectedPhotoPath;
        public AddEditPage(Client client = null)
        {
            InitializeComponent();
            if (client != null && client.ID != 0)
            {
                _isEditMode = true;
                _currentClient = client;
                LoadClientData();
                IDTextBox.Text = client.ID.ToString();
                IDTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                _isEditMode = false;
                _currentClient = new Client();
                IDTextBox.Visibility = Visibility.Collapsed;
            }
        }
        private void LoadClientData()
        {
            LastNameTextBox.Text = _currentClient.LastName;
            FirstNameTextBox.Text = _currentClient.FirstName;
            PatronymicTextBox.Text = _currentClient.Patronymic;
            EmailTextBox.Text = _currentClient.Email;
            PhoneTextBox.Text = _currentClient.Phone;
            BirthdayDatePicker.SelectedDate = _currentClient.Birthday;

            if (_currentClient.GenderCode == "м")
                MaleRadioButton.IsChecked = true;
            else if (_currentClient.GenderCode == "ж")
                FemaleRadioButton.IsChecked = true;

            if (!string.IsNullOrEmpty(_currentClient.PhotoPath) && File.Exists(_currentClient.PhotoPath))
            {
                PhotoImage.Source = new BitmapImage(new Uri(_currentClient.PhotoPath));
                _selectedPhotoPath = _currentClient.PhotoPath;
            }
        }

        private bool ValidateFields()
        {
            Regex nameRegex = new Regex(@"^[а-яА-Яa-zA-Z\s\-]+$");

            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                MessageBox.Show("Введите фамилию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!nameRegex.IsMatch(LastNameTextBox.Text))
            {
                MessageBox.Show("Фамилия может содержать только буквы, пробел и дефис", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (LastNameTextBox.Text.Length > 50)
            {
                MessageBox.Show("Фамилия не может быть длиннее 50 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                MessageBox.Show("Введите имя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!nameRegex.IsMatch(FirstNameTextBox.Text))
            {
                MessageBox.Show("Имя может содержать только буквы, пробел и дефис", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (FirstNameTextBox.Text.Length > 50)
            {
                MessageBox.Show("Имя не может быть длиннее 50 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(PatronymicTextBox.Text))
            {
                if (!nameRegex.IsMatch(PatronymicTextBox.Text))
                {
                    MessageBox.Show("Отчество может содержать только буквы, пробел и дефис", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                if (PatronymicTextBox.Text.Length > 50)
                {
                    MessageBox.Show("Отчество не может быть длиннее 50 символов", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                MessageBox.Show("Введите email", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            try
            {
                var addr = new System.Net.Mail.MailAddress(EmailTextBox.Text);
                if (addr.Address != EmailTextBox.Text)
                    throw new Exception();
            }
            catch
            {
                MessageBox.Show("Введите корректный email", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                MessageBox.Show("Введите телефон", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            Regex phoneRegex = new Regex(@"^[\d\+\-\(\)\s]+$");
            if (!phoneRegex.IsMatch(PhoneTextBox.Text))
            {
                MessageBox.Show("Телефон может содержать только цифры, +, -, (), пробел", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (BirthdayDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите дату рождения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (MaleRadioButton.IsChecked != true && FemaleRadioButton.IsChecked != true)
            {
                MessageBox.Show("Выберите пол", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateFields())
                return;

            _currentClient.LastName = LastNameTextBox.Text.Trim();
            _currentClient.FirstName = FirstNameTextBox.Text.Trim();
            _currentClient.Patronymic = PatronymicTextBox.Text.Trim();
            _currentClient.Email = EmailTextBox.Text.Trim();
            _currentClient.Phone = PhoneTextBox.Text.Trim();
            _currentClient.Birthday = BirthdayDatePicker.SelectedDate.Value;
            _currentClient.GenderCode = MaleRadioButton.IsChecked == true ? "м" : "ж";

            if (!_isEditMode)
            {
                _currentClient.RegistrationDate = DateTime.Today;
            }

            if (!string.IsNullOrEmpty(_selectedPhotoPath))
            {
                _currentClient.PhotoPath = _selectedPhotoPath;
            }

            try
            {
                using (var db = MazinaLanguageEntities.GetContext())
                {
                    if (!_isEditMode)
                    {
                        db.Client.Add(_currentClient);
                    }
                    else
                    {
                        var clientToUpdate = db.Client.Find(_currentClient.ID);
                        if (clientToUpdate != null)
                        {
                            clientToUpdate.LastName = _currentClient.LastName;
                            clientToUpdate.FirstName = _currentClient.FirstName;
                            clientToUpdate.Patronymic = _currentClient.Patronymic;
                            clientToUpdate.Email = _currentClient.Email;
                            clientToUpdate.Phone = _currentClient.Phone;
                            clientToUpdate.Birthday = _currentClient.Birthday;
                            clientToUpdate.GenderCode = _currentClient.GenderCode;
                            clientToUpdate.PhotoPath = _currentClient.PhotoPath;
                        }
                    }
                    db.SaveChanges();
                }

                MessageBox.Show("Клиент успешно сохранён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                if (NavigationService != null)
                    NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string fileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                    string destPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Клиенты", fileName);

                    Directory.CreateDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Клиенты"));

                    File.Copy(openFileDialog.FileName, destPath, true);

                    PhotoImage.Source = new BitmapImage(new Uri(destPath));

                    _selectedPhotoPath = $@"\Клиенты\{fileName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}");
                }
            }
    }
    }
}
