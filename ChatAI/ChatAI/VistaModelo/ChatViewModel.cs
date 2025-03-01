using ChatAI.Commands;
using ChatAI.Modelo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Speech.Recognition;
using System.Speech.Synthesis; // 🔹 Importamos TTS
using System.Threading;

namespace ChatAI.VistaModelo
{
	public class ChatViewModel : ViewModelBase
	{
		private readonly HttpClient _httpClient = new();
		private string _texto;
		private bool _puedeEnviar;
		private readonly SpeechSynthesizer _sintetizador = new(); // 🔹 Agregamos el sintetizador de voz
		public bool HayTexto => !string.IsNullOrWhiteSpace(Texto);
		
		// Propiedades para controlar la visibilidad de los botones
		public bool MostrarMicrofono => string.IsNullOrWhiteSpace(Texto);
		public bool MostrarEnviar => !string.IsNullOrWhiteSpace(Texto);

		public ObservableCollection<Mensaje> Mensajes { get; } = new();

		private SpeechRecognitionEngine _reconocedor;
		public ICommand GrabarVozCommand { get; }
		public ICommand LeerMensajeCommand { get; }

		public ChatViewModel()
		{
			EnviarMensajeCommand = new RelayCommand(async () => await EnviarMensaje(), () => PuedeEnviar);
			GrabarVozCommand = new RelayCommand(IniciarReconocimiento);
			LeerMensajeCommand = new RelayCommandSyncro<Mensaje>(LeerMensaje); // ✅ Cambiar a RelayCommand<Mensaje>
		}

		// Lista de mensajes
		new List<object> messages = new List<object>
		{
			new { content = "Hola Hola saludos cordiales. Soy Llama el asistente de esta épica conversación.", role = "system" }
		};

		public string Texto
		{
			get => _texto;
			set
			{
				if (_texto != value)
				{
					_texto = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(HayTexto));
					OnPropertyChanged(nameof(MostrarMicrofono));
					OnPropertyChanged(nameof(MostrarEnviar));
					PuedeEnviar = HayTexto;
					((RelayCommand)EnviarMensajeCommand).RaiseCanExecuteChanged();
				}
			}
		}

		public bool PuedeEnviar
		{
			get => _puedeEnviar;
			set
			{
				if (_puedeEnviar != value)
				{
					_puedeEnviar = value;
					OnPropertyChanged();
				}
			}
		}

		public ICommand EnviarMensajeCommand { get; }

		private async Task EnviarMensaje()
		{
			var mensajeUsuario = new Mensaje { Contenido = Texto, EsUsuario = true };
			Mensajes.Add(mensajeUsuario);
			Texto = string.Empty;
			PuedeEnviar = false;
			((RelayCommand)EnviarMensajeCommand).RaiseCanExecuteChanged();

			messages.Add(new { content = mensajeUsuario.Contenido, role = "user" });

			var requestBody = new
			{
				messages = messages.ToArray(),
				model = "llama-3.2-1b-instruct",
				max_tokens = 2048
			};

			var json = JsonSerializer.Serialize(requestBody);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			try
			{
				var response = await _httpClient.PostAsync("http://localhost:1234/v1/chat/completions", content);
				response.EnsureSuccessStatusCode();
				var responseBody = await response.Content.ReadAsStringAsync();
				var resultado = JsonSerializer.Deserialize<ChatResponse>(responseBody);

				var mensajeBot = new Mensaje { Contenido = resultado.choices[0].message.content, EsUsuario = false };
				Mensajes.Add(mensajeBot);
				messages.Add(new { content = mensajeBot.Contenido, role = "assistant" });
			}
			catch (Exception ex)
			{
				Mensajes.Add(new Mensaje { Contenido = "Error en la respuesta: " + ex.Message, EsUsuario = false });
			}
		}

		private void IniciarReconocimiento()
		{
			try
			{
				if (_reconocedor == null)
				{
					_reconocedor = new SpeechRecognitionEngine();
					_reconocedor.SetInputToDefaultAudioDevice();
					_reconocedor.LoadGrammar(new DictationGrammar());
					_reconocedor.SpeechRecognized += (s, e) =>
					{
						Texto = e.Result.Text;
					};
				}

				_reconocedor.RecognizeAsync(RecognizeMode.Single);
			}
			catch (Exception ex)
			{
				Mensajes.Add(new Mensaje { Contenido = "Error en reconocimiento: " + ex.Message, EsUsuario = false });
			}
		}

		private Mensaje _mensajeSeleccionado;

		private void LeerMensaje(Mensaje mensaje)
		{
			if (mensaje != null)
			{
				_sintetizador.SpeakAsyncCancelAll(); // ✅ Detiene cualquier otra lectura en curso
				_sintetizador.SelectVoice("Microsoft David Desktop");
				_sintetizador.SpeakAsync(mensaje.Contenido);
			}
		}

		public void LimpiarConversacion()
		{
			Mensajes.Clear(); // 🔹 Borra los mensajes visibles en la UI
			messages.Clear(); // 🔹 Borra la memoria de la IA
			messages.Add(new { content = "Hola Hola saludos cordiales. Soy Llama el asistente de esta épica conversación.", role = "system" });
		}


	}
}
