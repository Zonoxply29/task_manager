using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using Polly;

namespace task_manager.Services
{
    public class AuthService
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "https://taskmanager-webservice.onrender.com";

        public AuthService()
        {
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            _client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        // Método para registrar un nuevo usuario
        public async Task<(bool Success, string Token, string Error)> RegisterAsync(string name, string email, string password)
        {
            try
            {
                var registerModel = new
                {
                    Nombre = name,
                    CorreoElectronico = email,
                    Contraseña = password
                };

                var json = JsonConvert.SerializeObject(registerModel);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Debug.WriteLine($"Enviando registro: {json}");

                var response = await ExecuteWithRetryPolicy(() =>
                    _client.PostAsync($"{BaseUrl}/api/auth/register", content));

                var responseString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Respuesta de registro: {response.StatusCode} - {responseString}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = response.StatusCode switch
                    {
                        HttpStatusCode.Conflict => "El correo ya está registrado",
                        HttpStatusCode.BadRequest => "Datos de registro inválidos",
                        _ => $"Error en el registro: {response.StatusCode}"
                    };
                    return (false, null, error);
                }

                // Hacer login automático después del registro exitoso
                return await LoginAsync(email, password);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en RegisterAsync: {ex}");
                return (false, null, "Error inesperado durante el registro");
            }
        }

        // Método mejorado para hacer login
        public async Task<(bool Success, string Token, string Error)> LoginAsync(string email, string password)
        {
            try
            {
                var loginModel = new
                {
                    CorreoElectronico = email,
                    Contraseña = password
                };

                var json = JsonConvert.SerializeObject(loginModel);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Debug.WriteLine($"Enviando login: {json}");

                var response = await ExecuteWithRetryPolicy(() =>
                    _client.PostAsync($"{BaseUrl}/api/auth/login", content));

                var responseString = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Respuesta de login: {response.StatusCode} - {responseString}");

                if (!response.IsSuccessStatusCode)
                {
                    var error = response.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized => "Credenciales incorrectas",
                        HttpStatusCode.NotFound => "Servicio no encontrado",
                        HttpStatusCode.BadRequest => "Solicitud incorrecta",
                        _ => $"Error del servidor: {response.StatusCode}"
                    };
                    return (false, null, error);
                }

                // Parsear la respuesta para obtener el token
                var responseObj = JsonConvert.DeserializeObject<dynamic>(responseString);
                if (responseObj.token == null)
                {
                    return (false, null, "El servidor no devolvió un token válido");
                }

                return (true, responseObj.token.ToString(), null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en LoginAsync: {ex}");
                return (false, null, "Error inesperado durante el login");
            }
        }

        // Método para crear una tarea (mejorado)
        public async Task<(bool Success, string Error)> CreateTareaAsync(string titulo, string descripcion, string token)
        {
            try
            {
                var tareaModel = new
                {
                    Titulo = titulo,
                    Descripcion = descripcion,
                    EstaCompletada = false
                };

                var json = JsonConvert.SerializeObject(tareaModel);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await ExecuteWithRetryPolicy(() =>
                    _client.PostAsync($"{BaseUrl}/api/tareas", content));

                if (!response.IsSuccessStatusCode)
                {
                    var error = response.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized => "No autorizado - token inválido",
                        HttpStatusCode.BadRequest => "Datos de tarea inválidos",
                        _ => $"Error al crear tarea: {response.StatusCode}"
                    };
                    return (false, error);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en CreateTareaAsync: {ex}");
                return (false, "Error inesperado al crear la tarea");
            }
            finally
            {
                _client.DefaultRequestHeaders.Authorization = null;
            }
        }

        // Política de reintentos para manejar fallos temporales
        private async Task<HttpResponseMessage> ExecuteWithRetryPolicy(Func<Task<HttpResponseMessage>> action)
        {
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

            return await retryPolicy.ExecuteAsync(action);
        }
    }
}