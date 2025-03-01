using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using ChatAI.Modelo;
using ChatAI.Services;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Speech.Synthesis;
using System.Collections.Generic;

namespace ChatAI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly VoskSpeechRecognitionService _speechRecognitionService;
        private readonly HttpClient _httpClient;
        private readonly SpeechSynthesizer _sintetizador;
        private string _texto;
        private bool _mostrarMicrofono = true;
        private bool _mostrarEnviar;
        private bool _estaGrabando;

        // Lista de mensajes para el contexto de la IA
        private readonly List<object> _messages;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<Mensaje> Mensajes { get; set; }

        public string Texto
        {
            get => _texto;
            set
            {
                if (_texto != value)
                {
                    _texto = value;
                    OnPropertyChanged(nameof(Texto));
                    ActualizarVisibilidadBotones();
                }
            }
        }

        public bool MostrarMicrofono
        {
            get => _mostrarMicrofono;
            set
            {
                if (_mostrarMicrofono != value)
                {
                    _mostrarMicrofono = value;
                    OnPropertyChanged(nameof(MostrarMicrofono));
                }
            }
        }

        public bool MostrarEnviar
        {
            get => _mostrarEnviar;
            set
            {
                if (_mostrarEnviar != value)
                {
                    _mostrarEnviar = value;
                    OnPropertyChanged(nameof(MostrarEnviar));
                }
            }
        }

        public bool EstaGrabando
        {
            get => _estaGrabando;
            set
            {
                if (_estaGrabando != value)
                {
                    _estaGrabando = value;
                    OnPropertyChanged(nameof(EstaGrabando));
                }
            }
        }

        private void ActualizarVisibilidadBotones()
        {
            bool hayTexto = !string.IsNullOrEmpty(Texto);
            MostrarEnviar = hayTexto && !EstaGrabando;
            MostrarMicrofono = !MostrarEnviar;
        }

        public ICommand EnviarMensajeCommand { get; }
        public ICommand GrabarVozCommand { get; }

        public MainViewModel()
        {
            _httpClient = new HttpClient();
            _sintetizador = new SpeechSynthesizer();
            _messages = new List<object>
            {
                new { content = "Hola Hola saludos cordiales. Soy Llama el asistente de esta épica conversación.", role = "system" }
            };

            Mensajes = new ObservableCollection<Mensaje>();
            EnviarMensajeCommand = new RelayCommand(async () => await EnviarMensaje());
            GrabarVozCommand = new RelayCommand(GrabarVoz);
            _speechRecognitionService = new VoskSpeechRecognitionService();
            ActualizarVisibilidadBotones();
        }

        private async Task EnviarMensaje()
        {
            if (string.IsNullOrWhiteSpace(Texto)) return;

            var mensajeUsuario = new Mensaje { Contenido = Texto, EsUsuario = true };
            Mensajes.Add(mensajeUsuario);
            _messages.Add(new { content = mensajeUsuario.Contenido, role = "user" });

            var textoEnviado = Texto;
            Texto = string.Empty;
            ActualizarVisibilidadBotones();

            try
            {
                var requestBody = new
                {
                    messages = _messages.ToArray(),
                    model = "llama-3.2-1b-instruct",
                    max_tokens = 2048
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("http://localhost:1234/v1/chat/completions", content);
                response.EnsureSuccessStatusCode();
                var responseBody = await response.Content.ReadAsStringAsync();
                var resultado = JsonSerializer.Deserialize<ChatResponse>(responseBody);

                var mensajeBot = new Mensaje { Contenido = resultado.choices[0].message.content, EsUsuario = false };
                Mensajes.Add(mensajeBot);
                _messages.Add(new { content = mensajeBot.Contenido, role = "assistant" });
            }
            catch (Exception ex)
            {
                Mensajes.Add(new Mensaje { Contenido = "Error en la respuesta: " + ex.Message, EsUsuario = false });
            }
        }

        public void LeerMensaje(Mensaje mensaje)
        {
            if (mensaje != null)
            {
                _sintetizador.SpeakAsyncCancelAll();
                _sintetizador.SelectVoice("Microsoft David Desktop");
                _sintetizador.SpeakAsync(mensaje.Contenido);
            }
        }

        private void GrabarVoz()
        {
            Debug.WriteLine($"GrabarVoz llamado. Estado actual: {(EstaGrabando ? "Grabando" : "No grabando")}");
            if (EstaGrabando)
            {
                DetenerGrabacion();
            }
            else
            {
                IniciarGrabacion();
            }
            ActualizarVisibilidadBotones();
        }

        private void IniciarGrabacion()
        {
            try
            {
                Debug.WriteLine("Intentando iniciar grabación...");
                EstaGrabando = true;
                ActualizarVisibilidadBotones();
                _speechRecognitionService.StartRecording(texto =>
                {
                    Debug.WriteLine($"Texto reconocido: {texto}");
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        Texto = texto;
                        ActualizarVisibilidadBotones();
                    });
                });
                Debug.WriteLine("Grabación iniciada con éxito");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al iniciar grabación: {ex.Message}");
                EstaGrabando = false;
                ActualizarVisibilidadBotones();
            }
        }

        private async void DetenerGrabacion()
        {
            if (EstaGrabando)
            {
                Debug.WriteLine("Deteniendo grabación...");
                await _speechRecognitionService.StopRecording();
                EstaGrabando = false;
                ActualizarVisibilidadBotones();
                Debug.WriteLine("Grabación detenida");
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }
} 