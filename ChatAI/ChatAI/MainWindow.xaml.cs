using ChatAI.Modelo;
using ChatAI.ViewModels;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChatAI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void NuevaConversacion_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel viewModel)
            {
                viewModel.Mensajes.Clear();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnListen_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && 
                button.DataContext is Mensaje mensaje && 
                DataContext is MainViewModel viewModel)
            {
                viewModel.LeerMensaje(mensaje);
            }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is MainViewModel viewModel)
            {
                if (viewModel.MostrarEnviar) // Solo enviar si el botón de enviar está visible
                {
                    viewModel.EnviarMensajeCommand.Execute(null);
                    e.Handled = true; // Evitar que se agregue un salto de línea
                }
            }
        }
    }
}