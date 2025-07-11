using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace task_manager.Services
{
    public class AuthService
    {
        private static readonly HttpClient _client = new HttpClient();

        // Método para hacer login
        public async Task<string> LoginAsync(string email, string password)
        {
            var loginModel = new
            {
                CorreoElectronico = email,
                Contraseña = password
            };

            var json = JsonConvert.SerializeObject(loginModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("https://taskmanager-webservice.onrender.com/api/auth/login", content);

            if (response.IsSuccessStatusCode)
            {
                var token = await response.Content.ReadAsStringAsync();
                return token; // Retorna el token JWT
            }

            return null; // Si el login falla
        }

        // Método para crear una tarea
        public async Task CreateTareaAsync(string titulo, string descripcion, string token)
        {
            var tareaModel = new
            {
                Titulo = titulo,
                Descripcion = descripcion,
                EstaCompletada = false
            };

            var json = JsonConvert.SerializeObject(tareaModel);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Agregar el token al encabezado
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.PostAsync("https://taskmanager-webservice.onrender.com/api/tareas", content);

            if (response.IsSuccessStatusCode)
            {
                // Realiza alguna acción si la tarea es creada correctamente
            }
            else
            {
                // Maneja el error si la tarea no pudo ser creada
            }
        }
    }
}