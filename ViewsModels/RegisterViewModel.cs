using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Text;
using System.Diagnostics;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using System.ComponentModel;
using System.Net;

namespace task_manager.ViewsModels
{
    public class RegisterViewModel : BindableObject
    {
        private string _name;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private bool _isBusy;
        private string _errorMessage;
        private readonly HttpClient _httpClient;

        public string Name { get => _name; set { _name = value; OnPropertyChanged(); ClearError(); } }
        public string Email { get => _email; set { _email = value; OnPropertyChanged(); ClearError(); } }
        public string Password { get => _password; set { _password = value; OnPropertyChanged(); ClearError(); } }
        public string ConfirmPassword { get => _confirmPassword; set { _confirmPassword = value; OnPropertyChanged(); ClearError(); } }
        public bool IsBusy { get => _isBusy; set { _isBusy = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotBusy)); } }
        public bool IsNotBusy => !IsBusy;
        public string ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public Command RegisterCommand { get; }
        public Command NavigateToLoginCommand { get; }

        public RegisterViewModel()
        {
            _httpClient = new HttpClient(new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            })
            {
                BaseAddress = new Uri("https://taskmanager-webservice.onrender.com"),
                Timeout = TimeSpan.FromSeconds(30)
            };

            RegisterCommand = new Command(async () => await ExecuteRegister(), () => !IsBusy);
            NavigateToLoginCommand = new Command(async () => await ExecuteNavigationToLogin());
        }

        private void ClearError() => ErrorMessage = string.Empty;

        private async Task ExecuteNavigationToLogin()
        {
            try
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en navegación a Login: {ex}");
                ErrorMessage = "Error al navegar al login";
            }
        }

        private async Task ExecuteRegister()
        {
            if (IsBusy) return;

            // Validaciones
            if (string.IsNullOrWhiteSpace(Name))
            {
                ErrorMessage = "El nombre es requerido";
                return;
            }

            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@") || !Email.Contains("."))
            {
                ErrorMessage = "Ingrese un correo electrónico válido";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 6)
            {
                ErrorMessage = "La contraseña debe tener al menos 6 caracteres";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Las contraseñas no coinciden";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                // Verificar conectividad
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    ErrorMessage = "No hay conexión a Internet";
                    return;
                }

                var payload = new
                {
                    Nombre = Name,
                    CorreoElectronico = Email,
                    Contraseña = Password
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Debug.WriteLine($"Enviando registro: {json}");

                var response = await _httpClient.PostAsync("/api/auth/register", content);
                var responseString = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"Respuesta de registro: {response.StatusCode} - {responseString}");

                if (response.IsSuccessStatusCode)
                {
                    await Shell.Current.DisplayAlert("Éxito", "Registro completado. Por favor inicie sesión.", "OK");
                    await Shell.Current.GoToAsync("//LoginPage");
                }
                else
                {
                    ErrorMessage = response.StatusCode switch
                    {
                        HttpStatusCode.Conflict => "El correo ya está registrado",
                        HttpStatusCode.BadRequest => "Datos de registro inválidos",
                        _ => $"Error en el registro: {response.StatusCode}"
                    };
                }
            }
            catch (HttpRequestException httpEx)
            {
                ErrorMessage = "Error de conexión con el servidor";
                Debug.WriteLine($"HTTP Error: {httpEx}");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error inesperado durante el registro";
                Debug.WriteLine($"Error crítico: {ex}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
