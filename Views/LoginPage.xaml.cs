using task_manager.ViewsModels;

namespace task_manager.Views;

public partial class LoginPage : ContentPage
{
	public LoginPage()
	{
		InitializeComponent();

        BindingContext = new LoginViewModel(); // Asignamos el ViewModel directamente aquí
    }
}