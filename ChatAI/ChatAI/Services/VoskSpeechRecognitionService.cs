using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using Vosk;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace ChatAI.Services
{
    public class VoskSpeechRecognitionService : IDisposable
    {
        private readonly Model model;
        private WaveInEvent waveSource;
        private VoskRecognizer recognizer;
        private Action<string> onTextRecognized;
        private MemoryStream memoryStream;
        private bool isRecording;

        public VoskSpeechRecognitionService()
        {
            try
            {
                // Actualización de la ruta al modelo
                string modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Vosk", "vosk-model-small-es-0.42");
                Debug.WriteLine($"Buscando modelo en: {modelPath}");
                
                if (!Directory.Exists(modelPath))
                {
                    throw new DirectoryNotFoundException($"No se encontró el directorio del modelo en: {modelPath}");
                }

                Debug.WriteLine("Cargando modelo Vosk...");
                model = new Model(modelPath);
                Debug.WriteLine("Modelo cargado correctamente");

                Debug.WriteLine("Inicializando reconocedor...");
                recognizer = new VoskRecognizer(model, 16000.0f);
                recognizer.SetMaxAlternatives(0);
                recognizer.SetWords(true);
                Debug.WriteLine("Reconocedor inicializado correctamente");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al inicializar Vosk: {ex.Message}");
                throw new Exception($"Error al inicializar el servicio de reconocimiento de voz: {ex.Message}");
            }
        }

        public void StartRecording(Action<string> onTextRecognizedCallback)
        {
            if (isRecording) return;

            try
            {
                Debug.WriteLine("Iniciando servicio de grabación...");
                onTextRecognized = onTextRecognizedCallback;
                memoryStream = new MemoryStream();
                isRecording = true;

                // Verificar dispositivos de audio disponibles
                Debug.WriteLine("Dispositivos de audio disponibles:");
                int selectedDevice = -1;
                for (int i = 0; i < WaveIn.DeviceCount; i++)
                {
                    var capabilities = WaveIn.GetCapabilities(i);
                    Debug.WriteLine($"Dispositivo {i}: {capabilities.ProductName}");
                    Debug.WriteLine($"  - Canales: {capabilities.Channels}");
                    Debug.WriteLine($"  - Formatos soportados:");
                    foreach (SupportedWaveFormat format in Enum.GetValues(typeof(SupportedWaveFormat)))
                    {
                        Debug.WriteLine($"    - {format}: {capabilities.SupportsWaveFormat(format)}");
                    }
                    
                    // Seleccionar el primer dispositivo que contenga "mic" en el nombre
                    if (selectedDevice == -1 && 
                        capabilities.ProductName.ToLower().Contains("mic"))
                    {
                        selectedDevice = i;
                        Debug.WriteLine($"Seleccionando dispositivo: {capabilities.ProductName}");
                    }
                }

                if (selectedDevice == -1)
                {
                    selectedDevice = 0; // Usar el primer dispositivo si no se encuentra uno específico
                    Debug.WriteLine("No se encontró un micrófono específico, usando el dispositivo predeterminado");
                }

                waveSource = new WaveInEvent
                {
                    DeviceNumber = selectedDevice,
                    WaveFormat = new WaveFormat(16000, 16, 1), // Formato específico: 16kHz, 16 bits, mono
                    BufferMilliseconds = 50
                };

                Debug.WriteLine($"Formato de audio configurado: {waveSource.WaveFormat}");
                Debug.WriteLine($"Dispositivo seleccionado: {WaveIn.GetCapabilities(selectedDevice).ProductName}");

                // Verificar si el dispositivo está recibiendo audio
                var testBuffer = new byte[1600];
                bool audioDetected = false;
                
                waveSource.DataAvailable += (s, e) =>
                {
                    if (!audioDetected)
                    {
                        float maxVolume = CalculateVolume(e.Buffer, e.BytesRecorded);
                        if (maxVolume > 0)
                        {
                            audioDetected = true;
                            Debug.WriteLine($"Audio detectado! Nivel: {maxVolume:F2}");
                        }
                    }
                };

                waveSource.DataAvailable += WaveSource_DataAvailable;
                waveSource.RecordingStopped += WaveSource_RecordingStopped;

                Debug.WriteLine("Iniciando grabación...");
                waveSource.StartRecording();
                Debug.WriteLine("Grabación iniciada correctamente");

                // Esperar un momento para verificar si se detecta audio
                Task.Delay(1000).ContinueWith(_ =>
                {
                    if (!audioDetected)
                    {
                        Debug.WriteLine("ADVERTENCIA: No se detecta audio después de 1 segundo.");
                        Debug.WriteLine("Por favor, verifica:");
                        Debug.WriteLine("1. Que el micrófono no esté silenciado en Windows");
                        Debug.WriteLine("2. Que la aplicación tenga permisos para usar el micrófono");
                        Debug.WriteLine("3. Que el micrófono esté seleccionado como dispositivo de entrada predeterminado");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al iniciar la grabación: {ex.Message}");
                isRecording = false;
                memoryStream?.Dispose();
                waveSource?.Dispose();
                throw;
            }
        }

        private float CalculateVolume(byte[] buffer, int bytesRecorded)
        {
            float maxVolume = 0;
            for (int i = 0; i < bytesRecorded; i += 2)
            {
                short sample = BitConverter.ToInt16(buffer, i);
                float normalizedSample = sample / 32768f;
                maxVolume = Math.Max(maxVolume, Math.Abs(normalizedSample));
            }
            return maxVolume;
        }

        public async Task StopRecording()
        {
            if (!isRecording) return;

            try
            {
                isRecording = false;
                waveSource?.StopRecording();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al detener la grabación: {ex.Message}");
            }
        }

        private void WaveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            try
            {
                float maxVolume = CalculateVolume(e.Buffer, e.BytesRecorded);
                Debug.WriteLine($"Nivel de audio: {maxVolume:F2}");

                if (maxVolume < 0.005f) // Reducido el umbral para mayor sensibilidad
                {
                    Debug.WriteLine("Nivel de audio muy bajo");
                    return;
                }

                if (recognizer.AcceptWaveform(e.Buffer, e.BytesRecorded))
                {
                    var result = recognizer.Result();
                    Debug.WriteLine($"Datos de audio procesados: {result}");
                    ProcessResult(result);
                }
                else
                {
                    var partial = recognizer.PartialResult();
                    Debug.WriteLine($"Resultado parcial: {partial}");
                    
                    // Procesar también resultados parciales si contienen texto
                    var jsonPartial = JObject.Parse(partial);
                    var partialText = jsonPartial["partial"].ToString();
                    if (!string.IsNullOrWhiteSpace(partialText))
                    {
                        Debug.WriteLine($"Texto parcial reconocido: {partialText}");
                        onTextRecognized?.Invoke(partialText);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al procesar audio: {ex.Message}");
            }
        }

        private void WaveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            try
            {
                Debug.WriteLine("Procesando resultado final...");
                var result = recognizer.FinalResult();
                Debug.WriteLine($"Resultado final: {result}");
                ProcessResult(result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al procesar el resultado final: {ex.Message}");
            }
            finally
            {
                waveSource?.Dispose();
                waveSource = null;
                memoryStream?.Dispose();
                memoryStream = null;
            }
        }

        private void ProcessResult(string result)
        {
            try
            {
                var jsonResult = JObject.Parse(result);
                var text = jsonResult["text"].ToString();
                Debug.WriteLine($"Procesando resultado: {text}");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    Debug.WriteLine($"Texto reconocido: {text}");
                    onTextRecognized?.Invoke(text);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al procesar resultado: {ex.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                waveSource?.Dispose();
                memoryStream?.Dispose();
                recognizer?.Dispose();
                model?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al liberar recursos: {ex.Message}");
            }
        }
    }
} 