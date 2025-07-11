using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Net.Http.Headers;

namespace task_manager.ViewsModels
{
    public class LoginViewModel : BindableObject
    {
        private string _email;
        private string _password;
        private readonly HttpClient _httpClient = new HttpClient(); // Inicialización del HttpClient

        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                }
            }
        }

        // Define the Command for login button
        public Command OnLoginClicked { get; }

        public LoginViewModel()
        {
            // Initialize the Command
            OnLoginClicked = new Command(async () => await Login());
        }

        private async Task Login()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password)) // Validación de campos vacíos
            {
                await Shell.Current.DisplayAlert("Error", "Por favor ingrese correo y contraseña", "OK");
                return;
            }

            var payload = new
            {
                CorreoElectronico = Email,
                Contraseña = Password
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

            try
            {
                // Enviar solicitud POST a la API de login
                var response = await _httpClient.PostAsync("https://taskmanager-webservice.onrender.com/api/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var token = JsonSerializer.Deserialize<JsonElement>(responseBody).GetProperty("token").GetString();

                    if (!string.IsNullOrEmpty(token))  // Verificar que el token no sea null o vacío
                    {
                        Preferences.Set("AuthToken", token); // Guardar el token en Preferences

                        // Redirigir a la página de Lista de Tareas
                        await Shell.Current.GoToAsync("//ListadetareasPage");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Error", "Token no recibido", "OK");
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "Credenciales inválidas", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Error de conexión: {ex.Message}", "OK");
            }
        }
    }
}
