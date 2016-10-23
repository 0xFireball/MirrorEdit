using Avalonia.Markup.Xaml;
using nkyUI.Controls;

namespace MirrorEdit.Demo
{
    public class MainWindow : KYUIWindow
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