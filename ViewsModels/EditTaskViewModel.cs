using task_manager.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using System.ComponentModel;

namespace task_manager.ViewsModels
{
    public class EditTaskViewModel : BindableObject
    {
        private Tarea _tarea;
        private readonly HttpClient _httpClient;

        public Tarea Tarea
        {
            get => _tarea;
            set
            {
                _tarea = value;
                OnPropertyChanged();
            }
        }

        public ICommand GuardarCommand { get; }
        public ICommand CancelarCommand { get; }

        public EditTaskViewModel()
        {
            _httpClient = new HttpClient();
            GuardarCommand = new Command(async () => await GuardarTarea());
            CancelarCommand = new Command(async () => await Cancelar());

            // Inicializa con una tarea vacía (será reemplazada)
            Tarea = new Tarea();
        }

        // Método para cargar la tarea a editar
        public void LoadTarea(Tarea tarea)
        {
            Tarea = new Tarea
            {
                Id = tarea.Id,
                Titulo = tarea.Titulo,
                Descripcion = tarea.Descripcion,
                EstaCompletada = tarea.EstaCompletada,
                UsuarioId = tarea.UsuarioId
            };
        }

        private async Task GuardarTarea()
        {
            if (string.IsNullOrWhiteSpace(Tarea.Titulo))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "El título es requerido", "OK");
                return;
            }

            try
            {
                var token = Preferences.Get("AuthToken", string.Empty);
                if (string.IsNullOrEmpty(token))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Sesión no válida", "OK");
                    return;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var json = JsonSerializer.Serialize(Tarea);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(
                    $"https://taskmanager-webservice.onrender.com/api/tareas/{Tarea.Id}", content);

                if (response.IsSuccessStatusCode)
                {
                    await Application.Current.MainPage.DisplayAlert("Éxito", "Tarea actualizada", "OK");
                    await Shell.Current.GoToAsync(".."); // Regresar a la página anterior
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error",
                        $"Error al actualizar: {response.StatusCode}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Error de conexión: {ex.Message}", "OK");
            }
        }

        private async Task Cancelar()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
