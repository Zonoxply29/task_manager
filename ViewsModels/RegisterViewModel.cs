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
using Polly;
using System.Threading;

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

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
                ClearError();
            }
        }

        public string Email
        {
            get => _email;
            set
            {
                _email = value;
                OnPropertyChanged();
                ClearError();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                ClearError();
            }
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set
            {
                _confirmPassword = value;
                OnPropertyChanged();
                ClearError();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }

        public bool IsNotBusy => !IsBusy;

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasError));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        public Command RegisterCommand { get; }
        public Command NavigateToLoginCommand { get; }

        public RegisterViewModel()
        {
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _httpClient = new HttpClient(handler)
            {
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

            // Validaciones mejoradas
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
                var currentAccess = Connectivity.NetworkAccess;
                if (currentAccess != NetworkAccess.Internet)
                {
                    ErrorMessage = currentAccess == NetworkAccess.ConstrainedInternet ?
                        "Conexión limitada detectada" :
                        "No hay conexión a Internet disponible";
                    return;
                }

                // Configuración de política de reintentos
                var retryPolicy = Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .Or<TaskCanceledException>()
                    .Or<WebException>()
                    .WaitAndRetryAsync(
                        retryCount: 3,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        onRetry: (exception, delay, retryCount, context) =>
                        {
                            Debug.WriteLine($"Reintento #{retryCount} debido a: {exception?.Exception?.Message}");
                        });

                // Datos para el registro
                var registerPayload = new
                {
                    Nombre = Name,
                    CorreoElectronico = Email,
                    Contraseña = Password
                };

                var registerJson = JsonSerializer.Serialize(registerPayload);
                var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");

                Debug.WriteLine($"Enviando registro: {registerJson}");

                // Ejecutar la petición de registro con política de reintentos
                var registerResponse = await retryPolicy.ExecuteAsync(async () =>
                {
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(_httpClient.Timeout);

                    var request = new HttpRequestMessage(HttpMethod.Post, "https://taskmanager-webservice.onrender.com/api/auth/register")
                    {
                        Content = registerContent
                    };

                    return await _httpClient.SendAsync(request, cts.Token);
                });

                var registerResponseString = await registerResponse.Content.ReadAsStringAsync();
                Debug.WriteLine($"Respuesta de registro: {registerResponse.StatusCode} - {registerResponseString}");

                if (registerResponse.IsSuccessStatusCode)
                {
                    // Datos para el login automático
                    var loginPayload = new
                    {
                        CorreoElectronico = Email,
                        Contraseña = Password
                    };

                    var loginJson = JsonSerializer.Serialize(loginPayload);
                    var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

                    Debug.WriteLine($"Intentando login automático...");

                    // Ejecutar la petición de login con política de reintentos
                    var loginResponse = await retryPolicy.ExecuteAsync(async () =>
                    {
                        using var cts = new CancellationTokenSource();
                        cts.CancelAfter(_httpClient.Timeout);

                        var request = new HttpRequestMessage(HttpMethod.Post, "https://taskmanager-webservice.onrender.com/api/auth/login")
                        {
                            Content = loginContent
                        };

                        return await _httpClient.SendAsync(request, cts.Token);
                    });

                    var loginResponseString = await loginResponse.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Respuesta de login: {loginResponse.StatusCode} - {loginResponseString}");

                    if (loginResponse.IsSuccessStatusCode)
                    {
                        try
                        {
                            using var jsonDoc = JsonDocument.Parse(loginResponseString);
                            var root = jsonDoc.RootElement;

                            if (root.TryGetProperty("token", out var tokenProp) &&
                                !string.IsNullOrWhiteSpace(tokenProp.GetString()))
                            {
                                var token = tokenProp.GetString();
                                Preferences.Set("AuthToken", token);

                                // Navegación en el hilo principal
                                await MainThread.InvokeOnMainThreadAsync(async () =>
                                {
                                    try
                                    {
                                        await Shell.Current.GoToAsync("//ListadetareasPage", true);
                                        Debug.WriteLine("Navegación exitosa a ListadetareasPage");
                                    }
                                    catch (Exception navEx)
                                    {
                                        Debug.WriteLine($"Error en navegación: {navEx}");
                                        await TryAlternativeNavigation();
                                    }
                                });
                            }
                            else
                            {
                                ErrorMessage = "El servidor no devolvió un token válido";
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            ErrorMessage = "Error procesando la respuesta del servidor";
                            Debug.WriteLine($"Error JSON: {jsonEx}");
                        }
                    }
                    else
                    {
                        ErrorMessage = loginResponse.StatusCode switch
                        {
                            HttpStatusCode.Unauthorized => "Credenciales incorrectas",
                            HttpStatusCode.NotFound => "Servicio no encontrado",
                            HttpStatusCode.BadRequest => "Solicitud incorrecta",
                            HttpStatusCode.InternalServerError => "Error interno del servidor",
                            HttpStatusCode.ServiceUnavailable => "Servicio no disponible",
                            _ => $"Error del servidor: {(int)loginResponse.StatusCode}"
                        };
                    }
                }
                else
                {
                    ErrorMessage = registerResponse.StatusCode switch
                    {
                        HttpStatusCode.Conflict => "El correo ya está registrado",
                        HttpStatusCode.BadRequest => "Datos de registro inválidos",
                        _ => $"Error en el registro: {registerResponse.StatusCode}"
                    };
                }
            }
            catch (TaskCanceledException)
            {
                ErrorMessage = "La solicitud tardó demasiado. Intente nuevamente";
            }
            catch (HttpRequestException httpEx) when (httpEx.Message.Contains("Socket closed"))
            {
                ErrorMessage = "Error de conexión. Verifique su red e intente nuevamente";
                Debug.WriteLine($"Error de socket: {httpEx}");
            }
            catch (HttpRequestException httpEx)
            {
                ErrorMessage = "No se pudo conectar al servidor";
                Debug.WriteLine($"Error HTTP: {httpEx}");
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error inesperado durante el registro";
                Debug.WriteLine($"Error crítico: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task TryAlternativeNavigation()
        {
            try
            {
                Debug.WriteLine("Intentando navegación alternativa...");
                var shell = new AppShell();
                Application.Current.MainPage = shell;
                await shell.GoToAsync("//ListadetareasPage");
            }
            catch (Exception altEx)
            {
                Debug.WriteLine($"Error en navegación alternativa: {altEx}");
                await Shell.Current.DisplayAlert("Error", "No se pudo cargar la aplicación", "OK");
            }
        }
    }
}