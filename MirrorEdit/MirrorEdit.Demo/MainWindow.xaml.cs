using Avalonia.Markup.Xaml;
using MirrorEdit.Demo.Controls;

namespace MirrorEdit.Demo
{
    public class MainWindow : CoolWindow
    {
        public MainWindow()
        {
            this.InitializeComponent();
            App.AttachDevTools(this);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}