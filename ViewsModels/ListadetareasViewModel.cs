using task_manager.Models;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Net.Http.Headers;

namespace task_manager.ViewsModels
{
    public class ListadetareasViewModel : BindableObject
    {
        private ObservableCollection<Tarea> _tareas; // Cambia de TaskModel a Tarea
        private readonly HttpClient _httpClient; // Declara el HttpClient

        public ObservableCollection<Tarea> Tareas
        {
            get => _tareas;
            set
            {
                _tareas = value;
                OnPropertyChanged();
            }
        }

        public Command LoadTareasCommand { get; }
        public Command EditTaskCommand { get; }
        public Command DeleteTaskCommand { get; }
        public Command NavigateToCreateTaskCommand { get; }

        public ListadetareasViewModel()
        {
            LoadTareasCommand = new Command(async () => await LoadTareas());
            EditTaskCommand = new Command<Tarea>(async (task) => await EditTask(task));
            DeleteTaskCommand = new Command<Tarea>(async (task) => await DeleteTask(task));
            NavigateToCreateTaskCommand = new Command(() => NavigateToCreateTask());
        }

        // Método para cargar las tareas desde la API
        private async Task LoadTareas()
        {
            var token = Preferences.Get("AuthToken", string.Empty);
            if (string.IsNullOrEmpty(token))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "No hay sesión activa", "OK");
                return;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.GetAsync("https://taskmanager-webservice.onrender.com/api/tareas");

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var tareas = JsonSerializer.Deserialize<ObservableCollection<Tarea>>(responseBody); // Cambia de TaskModel a Tarea
                Tareas = tareas;
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudieron obtener las tareas", "OK");
            }
        }

        // Editar tarea
        private async Task EditTask(Tarea task)
        {
            await Shell.Current.GoToAsync("//EditTaskPage", true);
        }

        // Eliminar tarea
        private async Task DeleteTask(Tarea task)
        {
            var token = Preferences.Get("AuthToken", string.Empty);
            if (string.IsNullOrEmpty(token))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "No hay sesión activa", "OK");
                return;
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.DeleteAsync($"https://taskmanager-webservice.onrender.com/api/tareas/{task.Id}");

            if (response.IsSuccessStatusCode)
            {
                Tareas.Remove(task);
                await Application.Current.MainPage.DisplayAlert("Éxito", "Tarea eliminada", "OK");
            }
            else
            {
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo eliminar la tarea", "OK");
            }
        }

        // Navegar a la página de crear tarea
        private void NavigateToCreateTask()
        {
            Shell.Current.GoToAsync("//CreateTaskPage");
        }
    }
}
