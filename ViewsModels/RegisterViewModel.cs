using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace task_manager.ViewsModels
{
    public class RegisterViewModel : BindableObject
    {
        private string _name;
        private string _email;
        private string _password;
        private readonly HttpClient _httpClient = new HttpClient();

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
            }
        }

        // Define the Command
        public Command OnRegisterClicked { get; }

        public RegisterViewModel()
        {
            // Initialize the Command
            OnRegisterClicked = new Command(async () => await Register());
        }

        private async Task Register()
        {
            var payload = new
            {
                Nombre = Name,
                CorreoElectronico = Email,
                Contraseña = Password
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("https://taskmanager-webservice.onrender.com/api/auth/register", content);

                if (response.IsSuccessStatusCode)
                {
                    await Shell.Current.DisplayAlert("Success", "User registered successfully", "OK");
                    // After successful registration, navigate to the tasks page
                    await Shell.Current.GoToAsync("//TasksPage");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "Registration failed", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Connection error: {ex.Message}", "OK");
            }
        }
    }
}

