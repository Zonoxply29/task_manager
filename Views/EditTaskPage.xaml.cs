using task_manager.Models;
using task_manager.ViewsModels;
using Microsoft.Maui.Controls;

namespace task_manager.Views
{
    public partial class EditTaskPage : ContentPage
    {
        public EditTaskPage(Tarea tareaParaEditar)
        {
            InitializeComponent();

            // Asigna el ViewModel y carga la tarea
            var viewModel = new EditTaskViewModel();
            viewModel.LoadTarea(tareaParaEditar);
            this.BindingContext = viewModel;
        }
    }
}