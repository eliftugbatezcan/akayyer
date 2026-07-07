using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GroundStation.Models;
using GroundStation.Services;
using GroundStation.Utils.Helpers;
using LiveCharts;
using LiveCharts.Wpf;

namespace GroundStation.ViewModels
{
    /// <summary>
    /// Ana ekran arayüz veri bağlama (data binding) ve kullanıcı komut yönetimini üstlenen ViewModel sınıfı.
    /// </summary>
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly ISerialPortService _serialPortService;
        private readonly ITelemetryParser _telemetryParser;
        private readonly TelemetryService _telemetryService;
        private readonly FlightLoggerService _loggerService;

        public SeriesCollection AltitudeSeries { get; set; }
        public ChartValues<double> AltitudeValues { get; set; }

        // Seri Port Ayarları
        private ObservableCollection<string> _availablePorts = new ObservableCollection<string>();
        private string _selectedPort = string.Empty;
        private ObservableCollection<int> _availableBaudRates = new ObservableCollection<int>();
        private int _selectedBaudRate = 115200;
        private bool _isConnected;
        private string _connectionButtonText = "BAĞLAN";

        // LED / Gösterge Durumları
        private bool _rxActive;
        private bool _txActive;
        private DispatcherTimer _rxTimer;
        private DispatcherTimer _txTimer;

        // Konsol Günlükleri (Console Logs)
        private string _consoleLogs = "";

        // Simülasyon Kontrolleri
        private bool _isSimulating;
        private string _simulationButtonText = "UÇUŞ SİMÜLASYONU BAŞLAT";
        private DispatcherTimer _simulationTimer;
        private int _simulatedPacketNumber = 0;
        private double _simulatedAltitude = 0.0;
        private double _simulatedVelocity = 15.0; // m/s
        private Random _random = new Random();

        // 21 Telemetri Parametresi Bindings
        private string _teamId = "TR-1024";
        private int _packetNumber;
        private double _rocketAltitude;
        private double _rocketGpsAltitude;
        private double _rocketLatitude = 38.3686;   // Aksaray Atış Alanı Varsayılan Koordinatları
        private double _rocketLongitude = 33.7225;
        private double _payloadGpsAltitude;
        private double _payloadLatitude = 38.3686;
        private double _payloadLongitude = 33.7225;
        private double _stageGpsAltitude;
        private double _stageLatitude = 38.3686;
        private double _stageLongitude = 33.7225;
        private double _gyroX;
        private double _gyroY;
        private double _gyroZ;
        private double _accelX;
        private double _accelY;
        private double _accelZ;
        private double _angle;
        private int _statusCode = 1; // 1: Uçuşa Hazır, 2: Yükselme, 3: Apogee, 4: 1.Paraşüt, 5: 2.Paraşüt, 6: Yerde
        private string _integrityCode = "N/A";
        private string _timestamp = "00:00:00.000";

        // Komutlar
        public ICommand ToggleConnectionCommand { get; }
        public ICommand RefreshPortsCommand { get; }
        public ICommand ToggleSimulationCommand { get; }
        public ICommand SendCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand OpenLogsCommand { get; }

        public Func<double, string> YFormatter { get; set; } = value => value.ToString("N0");

        public MainWindowViewModel()
        {
            // Servislerin ilklendirilmesi
            _serialPortService = new SerialPortService();
            _telemetryParser = new TelemetryParser();
            _telemetryService = new TelemetryService(_serialPortService, _telemetryParser);
            _loggerService = new FlightLoggerService();

            AltitudeValues = new ChartValues<double>();
            AltitudeSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "ALTITUDE",
                    Values = AltitudeValues,
                    PointGeometry = null,
                    LineSmoothness = 0.7,
                    StrokeThickness = 2,
                    Stroke = (System.Windows.Media.SolidColorBrush)new System.Windows.Media.BrushConverter().ConvertFromString("#00FF41")!,
                    Fill = new System.Windows.Media.LinearGradientBrush
                    {
                        StartPoint = new System.Windows.Point(0, 0),
                        EndPoint = new System.Windows.Point(0, 1),
                        GradientStops = new System.Windows.Media.GradientStopCollection
                        {
                            new System.Windows.Media.GradientStop((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4400FF41"), 0),
                            new System.Windows.Media.GradientStop(System.Windows.Media.Colors.Transparent, 1)
                        }
                    }
                }
            };

            // Koleksiyonların doldurulması
            AvailablePorts = new ObservableCollection<string>(_serialPortService.GetAvailablePorts());
            AvailableBaudRates = new ObservableCollection<int> { 9600, 19200, 38400, 57600, 115200 };

            // Olay abonelikleri
            _telemetryService.TelemetryProcessed += OnTelemetryProcessed;
            _telemetryService.LogMessageOccurred += (s, msg) => AddLog(msg);
            _serialPortService.ConnectionStateChanged += OnConnectionStateChanged;

            // Komut atamaları
            ToggleConnectionCommand = new RelayCommand(ExecuteToggleConnection);
            RefreshPortsCommand = new RelayCommand(ExecuteRefreshPorts);
            ToggleSimulationCommand = new RelayCommand(ExecuteToggleSimulation);
            SendCommand = new RelayCommand(ExecuteSend);
            ClearLogsCommand = new RelayCommand(() => ConsoleLogs = string.Empty);
            OpenLogsCommand = new RelayCommand(ExecuteOpenLogs);

            // LED Sönme Zamanlayıcıları (Aktivite bitince sönecekler)
            _rxTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _rxTimer.Tick += (s, e) => { RxActive = false; _rxTimer.Stop(); };

            _txTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _txTimer.Tick += (s, e) => { TxActive = false; _txTimer.Stop(); };

            // Uçuş Simülasyonu Zamanlayıcısı
            _simulationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) }; // 5Hz veri hızı
            _simulationTimer.Tick += GenerateSimulatedTelemetry;

            AddLog("[SYSTEM] Yer Kontrol İstasyonu Başlatıldı.");
        }

        #region Özellik Tanımları (Properties)

        public ObservableCollection<string> AvailablePorts
        {
            get => _availablePorts;
            set => SetProperty(ref _availablePorts, value);
        }

        public string SelectedPort
        {
            get => _selectedPort;
            set => SetProperty(ref _selectedPort, value);
        }

        public ObservableCollection<int> AvailableBaudRates
        {
            get => _availableBaudRates;
            set => SetProperty(ref _availableBaudRates, value);
        }

        public int SelectedBaudRate
        {
            get => _selectedBaudRate;
            set => SetProperty(ref _selectedBaudRate, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (SetProperty(ref _isConnected, value))
                {
                    ConnectionButtonText = _isConnected ? "KES" : "BAĞLAN";
                }
            }
        }

        public string ConnectionButtonText
        {
            get => _connectionButtonText;
            set => SetProperty(ref _connectionButtonText, value);
        }

        public bool RxActive
        {
            get => _rxActive;
            set => SetProperty(ref _rxActive, value);
        }

        public bool TxActive
        {
            get => _txActive;
            set => SetProperty(ref _txActive, value);
        }

        public string ConsoleLogs
        {
            get => _consoleLogs;
            set => SetProperty(ref _consoleLogs, value);
        }

        public bool IsSimulating
        {
            get => _isSimulating;
            set
            {
                if (SetProperty(ref _isSimulating, value))
                {
                    SimulationButtonText = _isSimulating ? "SİMÜLASYONU DURDUR" : "UÇUŞ SİMÜLASYONU BAŞLAT";
                }
            }
        }

        public string SimulationButtonText
        {
            get => _simulationButtonText;
            set => SetProperty(ref _simulationButtonText, value);
        }

        // Telemetri Özellikleri
        public string TeamId
        {
            get => _teamId;
            set => SetProperty(ref _teamId, value);
        }

        public int PacketNumber
        {
            get => _packetNumber;
            set => SetProperty(ref _packetNumber, value);
        }

        public double RocketAltitude
        {
            get => _rocketAltitude;
            set => SetProperty(ref _rocketAltitude, value);
        }

        public double RocketGpsAltitude
        {
            get => _rocketGpsAltitude;
            set => SetProperty(ref _rocketGpsAltitude, value);
        }

        public double RocketLatitude
        {
            get => _rocketLatitude;
            set => SetProperty(ref _rocketLatitude, value);
        }

        public double RocketLongitude
        {
            get => _rocketLongitude;
            set => SetProperty(ref _rocketLongitude, value);
        }

        public double PayloadGpsAltitude
        {
            get => _payloadGpsAltitude;
            set => SetProperty(ref _payloadGpsAltitude, value);
        }

        public double PayloadLatitude
        {
            get => _payloadLatitude;
            set => SetProperty(ref _payloadLatitude, value);
        }

        public double PayloadLongitude
        {
            get => _payloadLongitude;
            set => SetProperty(ref _payloadLongitude, value);
        }

        private double _payloadDistance;
        public double PayloadDistance
        {
            get => _payloadDistance;
            set => SetProperty(ref _payloadDistance, value);
        }
        
        private bool _isAutoFollowEnabled = true;
        public bool IsAutoFollowEnabled
        {
            get => _isAutoFollowEnabled;
            set => SetProperty(ref _isAutoFollowEnabled, value);
        }

        public double StageGpsAltitude
        {
            get => _stageGpsAltitude;
            set => SetProperty(ref _stageGpsAltitude, value);
        }

        public double StageLatitude
        {
            get => _stageLatitude;
            set => SetProperty(ref _stageLatitude, value);
        }

        public double StageLongitude
        {
            get => _stageLongitude;
            set => SetProperty(ref _stageLongitude, value);
        }

        public double GyroX
        {
            get => _gyroX;
            set => SetProperty(ref _gyroX, value);
        }

        public double GyroY
        {
            get => _gyroY;
            set => SetProperty(ref _gyroY, value);
        }

        public double GyroZ
        {
            get => _gyroZ;
            set => SetProperty(ref _gyroZ, value);
        }

        public double AccelX
        {
            get => _accelX;
            set => SetProperty(ref _accelX, value);
        }

        public double AccelY
        {
            get => _accelY;
            set => SetProperty(ref _accelY, value);
        }

        public double AccelZ
        {
            get => _accelZ;
            set => SetProperty(ref _accelZ, value);
        }

        public double Angle
        {
            get => _angle;
            set => SetProperty(ref _angle, value);
        }

        public int StatusCode
        {
            get => _statusCode;
            set 
            { 
                if(SetProperty(ref _statusCode, value))
                {
                    OnPropertyChanged(nameof(IsStateReady));
                    OnPropertyChanged(nameof(IsStateAscent));
                    OnPropertyChanged(nameof(IsStateApogee));
                    OnPropertyChanged(nameof(IsStatePara1));
                    OnPropertyChanged(nameof(IsStatePara2));
                    OnPropertyChanged(nameof(IsStateLanded));
                }
            }
        }
        
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            if (lat1 == 0 && lon1 == 0) return 0;
            var R = 6371e3; // metres
            var p1 = lat1 * Math.PI/180; 
            var p2 = lat2 * Math.PI/180;
            var dp = (lat2-lat1) * Math.PI/180;
            var dl = (lon2-lon1) * Math.PI/180;

            var a = Math.Sin(dp/2) * Math.Sin(dp/2) +
                    Math.Cos(p1) * Math.Cos(p2) *
                    Math.Sin(dl/2) * Math.Sin(dl/2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));

            return R * c; 
        }

        public bool IsStateReady => StatusCode == 1;
        public bool IsStateAscent => StatusCode == 2;
        public bool IsStateApogee => StatusCode == 3;
        public bool IsStatePara1 => StatusCode == 4;
        public bool IsStatePara2 => StatusCode == 5;
        public bool IsStateLanded => StatusCode == 6;

        public string IntegrityCode
        {
            get => _integrityCode;
            set => SetProperty(ref _integrityCode, value);
        }

        public string Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        #endregion

        #region İş Mantığı ve Tetikleyiciler

        private void OnTelemetryProcessed(object sender, TelemetryData data)
        {
            // Seri port arka planda çalıştığı için UI güncellemelerini Dispatcher ile ana thread'e yönlendiriyoruz
            Application.Current.Dispatcher.Invoke(() =>
            {
                // UI Değişkenlerini Güncelle
                TeamId = data.TeamId;
                PacketNumber = data.PacketNumber;
                RocketAltitude = data.RocketAltitude;
                RocketGpsAltitude = data.RocketGpsAltitude;
                RocketLatitude = data.RocketLatitude;
                RocketLongitude = data.RocketLongitude;

                PayloadGpsAltitude = data.PayloadGpsAltitude;
                PayloadLatitude = data.PayloadLatitude;
                PayloadLongitude = data.PayloadLongitude;

                StageGpsAltitude = data.StageGpsAltitude;
                StageLatitude = data.StageLatitude;
                StageLongitude = data.StageLongitude;

                GyroX = data.GyroX;
                GyroY = data.GyroY;
                GyroZ = data.GyroZ;

                AccelX = data.AccelX;
                AccelY = data.AccelY;
                AccelZ = data.AccelZ;

                Angle = data.Angle;
                StatusCode = data.StatusCode;
                IntegrityCode = data.IntegrityCode;
                Timestamp = data.Timestamp.ToString("HH:mm:ss.fff");
                
                PayloadDistance = CalculateDistance(data.RocketLatitude, data.RocketLongitude, data.PayloadLatitude, data.PayloadLongitude);

                // Rx Aktivite LED'ini yak (Blink)
                RxActive = true;
                _rxTimer.Stop();
                _rxTimer.Start();
                
                _loggerService.LogTelemetry(data);
                
                AltitudeValues.Add(data.RocketAltitude);
                if (AltitudeValues.Count > 150)
                {
                    AltitudeValues.RemoveAt(0);
                }
            });
        }

        private void OnConnectionStateChanged(object sender, bool state)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = state;
                if (state)
                {
                    AddLog($"[SYSTEM] {SelectedPort} portuna başarıyla bağlanıldı.");
                    _loggerService.StartLogging();
                }
                else
                {
                    AddLog("[SYSTEM] Seri port bağlantısı sonlandırıldı.");
                    _loggerService.StopLogging();
                }
            });
        }

        private void ExecuteToggleConnection()
        {
            if (IsConnected)
            {
                _serialPortService.Disconnect();
            }
            else
            {
                if (string.IsNullOrEmpty(SelectedPort))
                {
                    MessageBox.Show("Lütfen bağlanmak için bir COM Portu seçin.", "Hata", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                _serialPortService.Connect(SelectedPort, SelectedBaudRate);
            }
        }

        private void ExecuteRefreshPorts()
        {
            AvailablePorts.Clear();
            foreach (var port in _serialPortService.GetAvailablePorts())
            {
                AvailablePorts.Add(port);
            }
            AddLog("[SYSTEM] COM port listesi güncellendi.");
        }

        private void ExecuteToggleSimulation()
        {
            if (IsSimulating)
            {
                _simulationTimer.Stop();
                IsSimulating = false;
                AddLog("[SIMULATION] Uçuş simülasyonu sonlandırıldı.");
                _loggerService.StopLogging();
            }
            else
            {
                if (IsConnected)
                {
                    _serialPortService.Disconnect();
                }
                _simulatedPacketNumber = 0;
                _simulatedAltitude = 0.0;
                _simulatedVelocity = 18.0; // Başlangıç dikey hızı
                IsSimulating = true;
                _simulationTimer.Start();
                AddLog("[SIMULATION] Uçuş simülasyonu başlatıldı.");
                _loggerService.StartLogging();
            }
        }

        private void ExecuteOpenLogs()
        {
            try
            {
                string logsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (System.IO.Directory.Exists(logsDir))
                {
                    System.Diagnostics.Process.Start("explorer.exe", logsDir);
                }
                else
                {
                    AddLog("[SYSTEM] Logs klasörü henüz oluşturulmamış.");
                }
            }
            catch (Exception ex)
            {
                AddLog($"[ERROR] Logs açılamadı: {ex.Message}");
            }
        }

        private void ExecuteSend(object cmdText)
        {
            string cmd = cmdText as string;
            if (string.IsNullOrWhiteSpace(cmd)) return;

            if (IsConnected)
            {
                _serialPortService.SendData(cmd);
                TxActive = true;
                _txTimer.Stop();
                _txTimer.Start();
                AddLog($"[COMMAND SENT] {cmd}");
            }
            else
            {
                AddLog($"[COMMAND NOT SENT] Bağlantı yok, komut iletilemedi: {cmd}");
            }
        }

        private void AddLog(string msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ConsoleLogs += $"[{DateTime.Now:HH:mm:ss}] {msg}\n";
            });
        }

        /// <summary>
        /// Test ve gösterim amaçlı gerçekçi 21 parametreli telemetri üreten simülasyon motoru.
        /// Üretilen CSV paketi, sistemin kendi alıcı boru hattından (TelemetryService) geçirilerek test edilir.
        /// </summary>
        private void GenerateSimulatedTelemetry(object sender, EventArgs e)
        {
            _simulatedPacketNumber++;

            // Roket İrtifa Profil Simülasyonu (Basit Balistik Eğri)
            if (_simulatedAltitude < 3000.0 && _simulatedVelocity > 0)
            {
                StatusCode = 2; // Yükselme
            }
            else if (_simulatedAltitude >= 3000.0 && StatusCode == 2)
            {
                StatusCode = 3; // Apogee (Tepe Noktası)
                _simulatedVelocity = -5.0; // 1. Paraşüt hızı
                StatusCode = 4; // Hemen 1. paraşüt açıldığını varsayalım
            }
            else if (_simulatedAltitude < 1000.0 && StatusCode == 4)
            {
                StatusCode = 5; // 2. Paraşüt (Ana paraşüt)
                _simulatedVelocity = -2.0; // Daha yavaş iniş
            }

            _simulatedAltitude += _simulatedVelocity * 0.2; // 200ms tick
            if (_simulatedAltitude <= 0)
            {
                _simulatedAltitude = 0;
                _simulatedVelocity = 0;
                StatusCode = 6; // Yerde
                _simulationTimer.Stop();
                IsSimulating = false;
                AddLog("[SIMULATION] Roket yere iniş yaptı. Simülasyon durduruldu.");
            }

            // Gürültü ve Uç Noktalar (Parazit / Outlier Simülasyonu)
            double altitudeNoise = (_random.NextDouble() - 0.5) * 4.0; // normal gürültü +/-2m
            
            // %2 olasılıkla radyo paraziti sebebiyle uç değer (outlier) oluştur
            if (_random.NextDouble() < 0.02)
            {
                altitudeNoise = 850.0; // Anlık 850 metrelik sıçrama ( OutlierRemovalFilter bunu temizlemeli!)
            }

            double currentAlt = _simulatedAltitude + altitudeNoise;
            double rocketGpsAlt = _simulatedAltitude + (_random.NextDouble() - 0.5) * 8.0;

            // GPS Sürüklenme
            double latShift = (_simulatedPacketNumber * 0.000005);
            double currentLat = 38.3686 + latShift;
            double currentLon = 33.7225 + (_random.NextDouble() - 0.5) * 0.0001;

            // Görev yükü ve Kademe GPS
            double payloadGpsAlt = StatusCode >= 3 ? currentAlt * 0.95 : 0;
            double stageGpsAlt = StatusCode >= 4 ? currentAlt * 0.3 : 0;

            // Jiroskop ve İvme
            double gyroX = (_random.NextDouble() - 0.5) * 10;
            double gyroY = (_random.NextDouble() - 0.5) * 10;
            double gyroZ = (_random.NextDouble() - 0.5) * 360; // Düşme anında dönüş

            // İvme (Yükselirken pozitif, süzülürken sarsıntı)
            double accX = (_random.NextDouble() - 0.5) * 0.2;
            double accY = (_random.NextDouble() - 0.5) * 0.2;
            double accZ = StatusCode == 2 ? 3.5 + (_random.NextDouble() - 0.5) * 0.5 : 1.0;

            double angle = StatusCode == 2 ? 85.0 + (_random.NextDouble() - 0.5) * 2 : 10.0;

            // CRC16 Hesaplaması için Paket Stringi oluştur (Bütünlük parametresi hariç 20 parametre)
            string payload = string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2:F2},{3:F2},{4:F6},{5:F6},{6:F2},{7:F6},{8:F6},{9:F2},{10:F6},{11:F6},{12:F2},{13:F2},{14:F2},{15:F2},{16:F2},{17:F2},{18:F2},{19}",
                TeamId,
                _simulatedPacketNumber,
                currentAlt,
                rocketGpsAlt,
                currentLat,
                currentLon,
                payloadGpsAlt,
                currentLat - 0.0001,
                currentLon + 0.0001,
                stageGpsAlt,
                currentLat - 0.0005,
                currentLon - 0.0005,
                gyroX,
                gyroY,
                gyroZ,
                accX,
                accY,
                accZ,
                angle,
                StatusCode
            );

            // Bütünlük Kodunu CRC16 CCITT ile hesaplayıp ekle
            ushort crc = ChecksumCalculator.ComputeCrc16(payload);
            string rawPacketLine = $"{payload},{crc:X4}";

            // Ham veriyi sanki seri porttan gelmiş gibi alıcı servisimize iletiyoruz.
            // Bu sayede alıcı parser ve filtreleme boru hattı tamamen çalışmış olur.
            // Çift yönlü haberleşmenin Rx bacağını simüle eder.
            typeof(TelemetryService)
                .GetMethod("OnRawDataReceived", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_telemetryService, new object[] { this, rawPacketLine });
        }

        #endregion
    }
}
