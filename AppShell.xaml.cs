using Microsoft.Maui.Controls;
using task_manager.Views;
namespace task_manager
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            RegisterRoutes();
        }

        private void RegisterRoutes()
        {
            Routing.RegisterRoute("//ListadetareasPage", typeof(ListadetareasPage));
            Routing.RegisterRoute("//LoginPage", typeof(LoginPage));
            Routing.RegisterRoute("//RegisterPage", typeof(RegisterPage));
        }
    }
}
