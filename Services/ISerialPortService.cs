using System;

namespace GroundStation.Services
{
    /// <summary>
    /// Roket ile Yer Kontrol İstasyonu arasındaki çift yönlü Seri Port (COM)
    /// haberleşmesini yöneten servis arayüzüdür.
    /// </summary>
    public interface ISerialPortService
    {
        /// <summary>
        /// Seri porttan yeni bir veri satırı okunduğunda tetiklenir.
        /// </summary>
        event EventHandler<string> DataReceived;

        /// <summary>
        /// Haberleşme sırasında bir hata oluştuğunda tetiklenir.
        /// </summary>
        event EventHandler<string> ErrorOccurred;

        /// <summary>
        /// Seri port bağlantı durumu değiştiğinde tetiklenir (Bağlandı / Koptu).
        /// </summary>
        event EventHandler<bool> ConnectionStateChanged;

        /// <summary>
        /// Cihazın şu an bağlı olup olmadığını belirtir.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Sistemdeki kullanılabilir COM portlarını listeler.
        /// </summary>
        string[] GetAvailablePorts();

        /// <summary>
        /// Belirtilen COM portuna belirtilen Baud Rate ile bağlanır.
        /// </summary>
        void Connect(string portName, int baudRate);

        /// <summary>
        /// Mevcut bağlantıyı sonlandırır.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Seri port üzerinden roket aviyonik sistemine veri (komut) gönderir.
        /// </summary>
        void SendData(string data);
    }
}
