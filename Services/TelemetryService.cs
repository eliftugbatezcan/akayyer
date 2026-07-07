using System;
using System.Collections.Generic;
using GroundStation.Models;
using GroundStation.Utils.Filters;

namespace GroundStation.Services
{
    /// <summary>
    /// Seri port veri akışını dinleyen, ayrıştıran ve veri temizleme (data cleaning) boru hattından 
    /// geçirerek arayüze güncel verileri sunan merkezi telemetri servisidir.
    /// </summary>
    public class TelemetryService : IDisposable
    {
        private readonly ISerialPortService _serialPortService;
        private readonly ITelemetryParser _telemetryParser;
        private readonly List<IDataFilter> _filtersPipeline;

        /// <summary>
        /// Filtreleme boru hattından geçmiş, temizlenmiş son geçerli telemetri verisi.
        /// </summary>
        public TelemetryData LastValidTelemetry { get; private set; }

        /// <summary>
        /// Temizlenmiş yeni bir telemetri paketi başarıyla işlendiğinde tetiklenir.
        /// </summary>
        public event EventHandler<TelemetryData> TelemetryProcessed;

        /// <summary>
        /// Telemetri ayrıştırma veya filtre zincirinde hata oluştuğunda tetiklenir.
        /// </summary>
        public event EventHandler<string> LogMessageOccurred;

        public TelemetryService(ISerialPortService serialPortService, ITelemetryParser telemetryParser)
        {
            _serialPortService = serialPortService ?? throw new ArgumentNullException(nameof(serialPortService));
            _telemetryParser = telemetryParser ?? throw new ArgumentNullException(nameof(telemetryParser));
            
            // Filtre Boru Hattı (Pipeline) Tanımlaması (Bütünlük -> Mantıksal Sınır -> Gürültü/Sıçrama)
            _filtersPipeline = new List<IDataFilter>
            {
                new IntegrityCheckFilter(),
                new RangeCheckFilter(),
                new OutlierRemovalFilter()
            };

            // Seri port servisinden gelen ham veriyi dinle
            _serialPortService.DataReceived += OnRawDataReceived;
            _serialPortService.ErrorOccurred += (sender, error) => Log($"[SERIAL ERROR] {error}");
        }

        private void OnRawDataReceived(object sender, string rawPacket)
        {
            try
            {
                // 1. Ayrıştırma (Parsing)
                TelemetryData telemetry = _telemetryParser.Parse(rawPacket);
                
                // 2. Filtreleme Boru Hattı (Pipeline)
                TelemetryData processedTelemetry = telemetry;
                foreach (var filter in _filtersPipeline)
                {
                    processedTelemetry = filter.Process(processedTelemetry, LastValidTelemetry);
                    if (processedTelemetry == null)
                    {
                        // Bütünlük doğrulanamadıysa veya tamamen geçersiz bir paketse zincir sonlandırılır
                        Log($"[PIPELINE] Paket bütünlük kontrolü veya kritik filtreleme sebebiyle reddedildi: #{telemetry.PacketNumber}");
                        return;
                    }
                }

                // 3. Geçerli veriyi kaydet ve dışarıya fırlat
                LastValidTelemetry = processedTelemetry;
                TelemetryProcessed?.Invoke(this, processedTelemetry);
            }
            catch (FormatException fex)
            {
                Log($"[PARSER ERROR] Paket ayrıştırılamadı: {fex.Message} -> Veri: {rawPacket}");
            }
            catch (Exception ex)
            {
                Log($"[PIPELINE ERROR] Beklenmedik işlem hatası: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            LogMessageOccurred?.Invoke(this, message);
            System.Diagnostics.Debug.WriteLine(message);
        }

        public void Dispose()
        {
            if (_serialPortService != null)
            {
                _serialPortService.DataReceived -= OnRawDataReceived;
            }
        }
    }
}
