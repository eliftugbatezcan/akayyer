using System;
using System.IO.Ports;
using System.Text;

namespace GroundStation.Services
{
    /// <summary>
    /// .NET System.IO.Ports yardımıyla seri port donanım seviyesi haberleşmesini
    /// gerçekleştiren somut servis sınıfıdır.
    /// </summary>
    public class SerialPortService : ISerialPortService, IDisposable
    {
        private SerialPort _serialPort;
        private bool _disposed = false;

        public event EventHandler<string> DataReceived;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<bool> ConnectionStateChanged;

        public bool IsConnected => _serialPort != null && _serialPort.IsOpen;

        public string[] GetAvailablePorts()
        {
            try
            {
                return SerialPort.GetPortNames();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Kullanılabilir portlar listelenirken hata oluştu: {ex.Message}");
                return Array.Empty<string>();
            }
        }

        public void Connect(string portName, int baudRate)
        {
            if (IsConnected)
            {
                Disconnect();
            }

            try
            {
                _serialPort = new SerialPort
                {
                    PortName = portName,
                    BaudRate = baudRate,
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    Encoding = Encoding.ASCII,
                    ReadTimeout = 2000,
                    WriteTimeout = 2000
                };

                // Olay tabanlı veri okuma
                _serialPort.DataReceived += OnSerialPortDataReceived;
                
                _serialPort.Open();

                OnConnectionStateChanged(true);
                System.Diagnostics.Debug.WriteLine($"[SERIAL] Connected to {portName} at {baudRate} baud.");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Seri porta bağlanırken hata oluştu ({portName}): {ex.Message}");
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (_serialPort == null) return;

            try
            {
                _serialPort.DataReceived -= OnSerialPortDataReceived;
                
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Seri port kapatılırken hata oluştu: {ex.Message}");
            }
            finally
            {
                _serialPort.Dispose();
                _serialPort = null;
                OnConnectionStateChanged(false);
                System.Diagnostics.Debug.WriteLine("[SERIAL] Disconnected.");
            }
        }

        public void SendData(string data)
        {
            if (!IsConnected)
            {
                OnErrorOccurred("Bağlantı aktif değil, veri gönderilemedi.");
                return;
            }

            try
            {
                _serialPort.WriteLine(data);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Veri gönderilirken hata oluştu: {ex.Message}");
            }
        }

        private void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            try
            {
                // Seri port buffer'ından satır sonuna kadar olan veriyi oku (\n veya \r\n)
                string rawLine = _serialPort.ReadLine();
                if (!string.IsNullOrWhiteSpace(rawLine))
                {
                    DataReceived?.Invoke(this, rawLine.Trim());
                }
            }
            catch (TimeoutException)
            {
                // Okuma zaman aşımı normal bir durum olabilir, sessizce geçilebilir veya loglanabilir
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Veri okuma hatası: {ex.Message}");
            }
        }

        protected virtual void OnConnectionStateChanged(bool isConnected)
        {
            ConnectionStateChanged?.Invoke(this, isConnected);
        }

        protected virtual void OnErrorOccurred(string errorMessage)
        {
            ErrorOccurred?.Invoke(this, errorMessage);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Disconnect();
                }
                _disposed = true;
            }
        }
    }
}
