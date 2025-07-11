using task_manager.ViewsModels;
namespace task_manager.Views;

public partial class ListadetareasPage : ContentPage
{
    public ListadetareasPage()
    {
        InitializeComponent();
        BindingContext = new ListadetareasViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is ListadetareasViewModel vm)
            vm.LoadTareasCommand.Execute(null);
    }
}