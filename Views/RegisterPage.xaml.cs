using task_manager.ViewsModels; // Agrega esta l�n
namespace task_manager.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
        BindingContext = new RegisterViewModel();
    }
}