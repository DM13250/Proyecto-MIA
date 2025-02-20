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

namespace ChatAI.VistaModelo
{
    public class ChatViewModel : ViewModelBase
    {
        private readonly HttpClient _httpClient = new();
        private string _texto;
        private bool _puedeEnviar;

        public ObservableCollection<Mensaje> Mensajes { get; } = new();

        // Lista dinámica de mensajes
        new List<object> messages = new List<object>
        {
		    new { content = "Hola Hola saludos coridales. Soy Llama el asistente de esta épica conversación.", role = "system" }
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
                    PuedeEnviar = !string.IsNullOrWhiteSpace(_texto);
                    ((RelayCommand)EnviarMensajeCommand).RaiseCanExecuteChanged();  // 🔹 Forzar actualización del botón
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

        public ChatViewModel()
        {
            EnviarMensajeCommand = new RelayCommand(async () => await EnviarMensaje(), () => PuedeEnviar);
        }

        private async Task EnviarMensaje()
        {
            var mensajeUsuario = new Mensaje { Contenido = Texto, EsUsuario = true };
            Mensajes.Add(mensajeUsuario);
            Texto = string.Empty;
            PuedeEnviar = false;
            ((RelayCommand)EnviarMensajeCommand).RaiseCanExecuteChanged();  // 🔹 Forzar actualización del botón

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
    }
}
