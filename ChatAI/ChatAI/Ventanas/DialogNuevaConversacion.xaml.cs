using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ChatAI.Ventanas
{
	/// <summary>
	/// Lógica de interacción para DialogNuevaConversacion.xaml
	/// </summary>
	public partial class DialogNuevaConversacion : Window
	{
		public bool Confirmado { get; private set; } = false;

		public DialogNuevaConversacion()
		{
			InitializeComponent();
		}

		private void Aceptar_Click(object sender, RoutedEventArgs e)
		{
			Confirmado = true;
			this.Close();
		}

		private void Cancelar_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
