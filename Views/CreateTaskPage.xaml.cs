using task_manager.ViewsModels;

namespace task_manager.Views;

public partial class CreateTaskPage : ContentPage
{
    public CreateTaskPage()
    {
        InitializeComponent();
        BindingContext = new CreateTaskViewModel();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}

