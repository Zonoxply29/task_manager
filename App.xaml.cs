using task_manager.Views;
using Microsoft.Maui.Controls;

namespace task_manager
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}
