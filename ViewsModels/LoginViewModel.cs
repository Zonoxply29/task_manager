using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Text;
using System.Diagnostics;
using Microsoft.Maui.Networking; // Para Connectivity
using Microsoft.Maui.Storage;    // Para Preferences
using Polly;
using System.Net;
using System.Threading;
using System.ComponentModel;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace task_manager.ViewsModels
{
    public class LoginViewModel : BindableObject
    {
        private string _email;
        private string _password;
        private readonly HttpClient _httpClient;
        private bool _isBusy;
        private string _errorMessage;

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

        public Command OnLoginClicked { get; }
        public Command NavigateToRegisterCommand { get; }
        public Command OnFingerprintLoginClicked { get; }

        public LoginViewModel()
        {
            // Configuración robusta de HttpClient
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://taskmanager-webservice.onrender.com"),
                Timeout = TimeSpan.FromSeconds(30) // Timeout aumentado
            };

            // Configuración específica para Android
#if ANDROID
            System.Net.ServicePointManager.SecurityProtocol = 
                System.Net.SecurityProtocolType.Tls12 | 
                System.Net.SecurityProtocolType.Tls13;
#endif

            OnLoginClicked = new Command(async () => await ExecuteLogin(), () => !IsBusy);
            NavigateToRegisterCommand = new Command(async () => await ExecuteNavigationToRegister());
            OnFingerprintLoginClicked = new Command(async () => await ExecuteFingerprintLogin(), () => !IsBusy);

            // _ = LoadLastCredentialsAsync();
        }

        private void ClearError() => ErrorMessage = string.Empty;

        private async Task LoadLastCredentialsAsync()
        {
            try
            {
                Email = await SecureStorage.GetAsync("LastEmail") ?? string.Empty;
                Password = await SecureStorage.GetAsync("LastPassword") ?? string.Empty;
            }
            catch
            {
                // Si falla, ignora (puede ser primera vez)
            }
        }

        private async Task ExecuteNavigationToRegister()
        {
            try
            {
                await Shell.Current.GoToAsync("//RegisterPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en navegación a Register: {ex}");
                ErrorMessage = "Error al navegar a registro";
            }
        }

        private async Task ExecuteLogin()
        {
            if (IsBusy) return;

            // Validación mejorada
            if (string.IsNullOrWhiteSpace(Email) || !Email.Contains("@") || !Email.Contains("."))
            {
                ErrorMessage = "Ingrese un correo electrónico válido";
                return;
            }

            if (string.IsNullOrWhiteSpace(Password) || Password.Length < 4)
            {
                ErrorMessage = "La contraseña debe tener al menos 4 caracteres";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                // Verificar conectividad usando MAUI
                var currentAccess = Connectivity.NetworkAccess;
                if (currentAccess != NetworkAccess.Internet)
                {
                    ErrorMessage = currentAccess == NetworkAccess.ConstrainedInternet ?
                        "Conexión limitada detectada" :
                        "No hay conexión a Internet disponible";
                    return;
                }   

                var payload = new { CorreoElectronico = Email, Contraseña = Password };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Debug.WriteLine($"Enviando credenciales...");

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

                var response = await retryPolicy.ExecuteAsync(async () =>
                {
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(_httpClient.Timeout);

                    var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
                    {
                        Content = content
                    };

                    return await _httpClient.SendAsync(request, cts.Token);
                });

                var responseString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Respuesta recibida: {response.StatusCode} - {responseString}");

                await ProcessLoginResponse(response, responseString);
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
                ErrorMessage = "Error inesperado";
                Debug.WriteLine($"Error crítico: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteFingerprintLogin()
        {
            if (IsBusy) return;

            var result = await CrossFingerprint.Current.AuthenticateAsync(new AuthenticationRequestConfiguration(
                "Autenticación biométrica", "Accede usando tu huella digital"));

            if (result.Authenticated)
            {
                // Recupera las credenciales pero NO las asigna a las propiedades
                var savedEmail = await SecureStorage.GetAsync("LastEmail") ?? string.Empty;
                var savedPassword = await SecureStorage.GetAsync("LastPassword") ?? string.Empty;

                // Llama a un método de login directo con las credenciales recuperadas
                await ExecuteLoginWithCredentials(savedEmail, savedPassword);
            }
            else
            {
                ErrorMessage = "Autenticación biométrica cancelada o fallida";
            }
        }

        // Nuevo método privado para login directo sin tocar los inputs
        private async Task ExecuteLoginWithCredentials(string email, string password)
        {
            if (IsBusy) return;

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || !email.Contains("."))
            {
                ErrorMessage = "Ingrese un correo electrónico válido";
                return;
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            {
                ErrorMessage = "La contraseña debe tener al menos 4 caracteres";
                return;
            }

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var currentAccess = Connectivity.NetworkAccess;
                if (currentAccess != NetworkAccess.Internet)
                {
                    ErrorMessage = currentAccess == NetworkAccess.ConstrainedInternet ?
                        "Conexión limitada detectada" :
                        "No hay conexión a Internet disponible";
                    return;
                }

                var payload = new { CorreoElectronico = email, Contraseña = password };
                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Debug.WriteLine($"Enviando credenciales (huella)...");

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

                var response = await retryPolicy.ExecuteAsync(async () =>
                {
                    using var cts = new CancellationTokenSource();
                    cts.CancelAfter(_httpClient.Timeout);

                    var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
                    {
                        Content = content
                    };

                    return await _httpClient.SendAsync(request, cts.Token);
                });

                var responseString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Respuesta recibida: {response.StatusCode} - {responseString}");

                // Aquí puedes pasar el email y password si necesitas guardarlos de nuevo
                await ProcessLoginResponse(response, responseString, email, password);
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
                ErrorMessage = "Error inesperado";
                Debug.WriteLine($"Error crítico: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Modifica ProcessLoginResponse para aceptar email y password opcionales
        private async Task ProcessLoginResponse(HttpResponseMessage response, string responseString, string? loginEmail = null, string? loginPassword = null)
        {
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(responseString);
                    var root = jsonDoc.RootElement;

                    if (root.TryGetProperty("token", out var tokenProp) &&
                        !string.IsNullOrWhiteSpace(tokenProp.GetString()))
                    {
                        var token = tokenProp.GetString();
                        Preferences.Set("AuthToken", token);

                        // Guarda credenciales de forma segura
                        var emailToSave = loginEmail ?? Email;
                        var passwordToSave = loginPassword ?? Password;
                        await SecureStorage.SetAsync("LastEmail", emailToSave);
                        await SecureStorage.SetAsync("LastPassword", passwordToSave);

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
                        return;
                    }
                    ErrorMessage = "El servidor no devolvió un token válido";
                }
                catch (JsonException jsonEx)
                {
                    ErrorMessage = "Error procesando la respuesta del servidor";
                    Debug.WriteLine($"Error JSON: {jsonEx}");
                }
            }
            else
            {
                ErrorMessage = response.StatusCode switch
                {
                    HttpStatusCode.Unauthorized => "Credenciales incorrectas",
                    HttpStatusCode.NotFound => "Servicio no encontrado",
                    HttpStatusCode.BadRequest => "Solicitud incorrecta",
                    HttpStatusCode.InternalServerError => "Error interno del servidor",
                    HttpStatusCode.ServiceUnavailable => "Servicio no disponible",
                    _ => $"Error del servidor: {(int)response.StatusCode}"
                };
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