using task_manager.ViewsModels; // Agrega esta lín
namespace task_manager.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
        BindingContext = new RegisterViewModel();
    }
}