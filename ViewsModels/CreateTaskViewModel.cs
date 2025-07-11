using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Text.Json;
using task_manager.Models;

namespace task_manager.ViewsModels
{
    public class CreateTaskViewModel : BindableObject
    {
        private string _titulo;
        private string _descripcion;
        private bool _estaCompletada;

        private readonly HttpClient _httpClient = new HttpClient();

        public string Titulo
        {
            get => _titulo;
            set
            {
                _titulo = value;
                OnPropertyChanged();
            }
        }

        public string Descripcion
        {
            get => _descripcion;
            set
            {
                _descripcion = value;
                OnPropertyChanged();
            }
        }

        public bool EstaCompletada
        {
            get => _estaCompletada;
            set
            {
                _estaCompletada = value;
                OnPropertyChanged();
            }
        }

        // Comando para crear una tarea
        public Command CreateTaskCommand { get; }

        public CreateTaskViewModel()
        {
            // Inicializamos el comando
            CreateTaskCommand = new Command(async () => await CreateTaskAsync());
        }

        // Método para crear una tarea
        public async Task CreateTaskAsync()
        {
            // Verificar si los campos están completos
            if (string.IsNullOrEmpty(Titulo) || string.IsNullOrEmpty(Descripcion))
            {
                await Shell.Current.DisplayAlert("Error", "Todos los campos son obligatorios", "OK");
                return;
            }

            var token = Preferences.Get("AuthToken", string.Empty);
            if (string.IsNullOrEmpty(token))
            {
                await Shell.Current.DisplayAlert("Error", "No hay sesión activa", "OK");
                return;
            }

            var tarea = new Tarea
            {
                Titulo = Titulo,
                Descripcion = Descripcion,
                EstaCompletada = EstaCompletada
            };

            var json = JsonSerializer.Serialize(tarea);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Agregar el token a las cabeceras
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Llamada a la API para crear la tarea
            var response = await _httpClient.PostAsync("https://taskmanager-webservice.onrender.com/api/tareas", content);

            if (response.IsSuccessStatusCode)
            {
                await Shell.Current.DisplayAlert("Éxito", "Tarea creada con éxito", "OK");
                // Navegar de vuelta a la lista de tareas
                await Shell.Current.GoToAsync("//ListadetareasPage");
            }
            else
            {
                await Shell.Current.DisplayAlert("Error", "No se pudo crear la tarea", "OK");
            }
        }
    }
}

