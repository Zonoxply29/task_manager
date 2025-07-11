using task_manager.Models;
using task_manager.Views;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System.Net.Http.Headers;
using System.Diagnostics;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace task_manager.ViewsModels
{
    public class ListadetareasViewModel : BindableObject
    {
        private ObservableCollection<Tarea> _tareas = new ObservableCollection<Tarea>();
        private bool _isRefreshing;
        private readonly HttpClient _httpClient = new HttpClient();

        public ObservableCollection<Tarea> Tareas
        {
            get => _tareas;
            set
            {
                _tareas = value;
                OnPropertyChanged();
            }
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }

        public Command LoadTareasCommand { get; }
        public Command EditTaskCommand { get; }
        public Command DeleteTaskCommand { get; }
        public Command NavigateToCreateTaskCommand { get; }
        public Command LogoutCommand { get; }

        public ListadetareasViewModel()
        {
            LoadTareasCommand = new Command(async () => await LoadTareas());
            EditTaskCommand = new Command<Tarea>(async (task) => await EditTask(task));
            DeleteTaskCommand = new Command<Tarea>(async (task) => await DeleteTask(task));
            NavigateToCreateTaskCommand = new Command(() => NavigateToCreateTask());
            LogoutCommand = new Command(async () => await ExecuteLogout());

            // Cargar tareas al iniciar
            Task.Run(async () => await LoadTareas());
        }

        private async Task LoadTareas()
        {
            IsRefreshing = true;
            try
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
                    Debug.WriteLine($"JSON recibido: {responseBody}"); // Para depuración

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    };

                    var tareas = JsonSerializer.Deserialize<ObservableCollection<Tarea>>(responseBody, options);

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Tareas.Clear();
                        foreach (var tarea in tareas)
                        {
                            Tareas.Add(tarea);
                        }
                    });
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error",
                        $"Error al obtener tareas: {response.StatusCode}", "OK");
                }
            }
            catch (JsonException jsonEx)
            {
                await Application.Current.MainPage.DisplayAlert("Error JSON",
                    $"Error al procesar los datos: {jsonEx.Message}", "OK");
            }
            catch (HttpRequestException httpEx)
            {
                await Application.Current.MainPage.DisplayAlert("Error de conexión",
                    $"No se pudo conectar al servidor: {httpEx.Message}", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Error inesperado: {ex.Message}", "OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        //Editar
        private async Task EditTask(Tarea task)
        {
            if (task == null) return;

            try
            {
                await Shell.Current.Navigation.PushAsync(new EditTaskPage(task));
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"No se pudo abrir la edición: {ex.Message}", "OK");
            }
        }


        private async Task DeleteTask(Tarea task)
        {
            if (task == null) return;

            bool confirmar = await Application.Current.MainPage.DisplayAlert(
                "Confirmar eliminación",
                $"¿Estás seguro de eliminar la tarea '{task.Titulo}'?",
                "Sí", "No");

            if (!confirmar) return;

            try
            {
                var token = Preferences.Get("AuthToken", string.Empty);
                if (string.IsNullOrEmpty(token))
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Sesión no válida", "OK");
                    return;
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _httpClient.DeleteAsync($"https://taskmanager-webservice.onrender.com/api/tareas/{task.Id}");

                if (response.IsSuccessStatusCode)
                {
                    Device.BeginInvokeOnMainThread(() => Tareas.Remove(task));
                    await Application.Current.MainPage.DisplayAlert("Éxito", "Tarea eliminada", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error",
                        $"Error al eliminar: {response.StatusCode}", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Error de conexión: {ex.Message}", "OK");
            }
        }

        private async Task ExecuteLogout()
        {
            try
            {
                Preferences.Remove("AuthToken");
                await Shell.Current.GoToAsync("//LoginPage");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error",
                    $"Error al cerrar sesión: {ex.Message}", "OK");
            }
        }

        private void NavigateToCreateTask()
        {
            try
            {
                Shell.Current.GoToAsync("//CreateTaskPage");
            }
            catch (Exception ex)
            {
                Application.Current.MainPage.DisplayAlert("Error",
                    $"Error al navegar: {ex.Message}", "OK");
            }
        }
    }
}
