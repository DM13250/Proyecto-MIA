using ChatAI.Modelo;
using ChatAI.VistaModelo;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            DataContext = new ChatViewModel();
        }

		private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (sender is TextBlock textBlock && textBlock.DataContext is Mensaje mensaje)
			{
				if (DataContext is ChatViewModel viewModel && viewModel.LeerMensajeCommand.CanExecute(mensaje))
				{
					viewModel.LeerMensajeCommand.Execute(mensaje);
				}
			}
		}
	}
}