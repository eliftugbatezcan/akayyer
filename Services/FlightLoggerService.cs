using System;
using System.IO;

namespace GroundStation.Services
{
    /// <summary>
    /// Uçuş boyunca telemetri verilerini CSV olarak kaydetmekten sorumlu servis.
    /// </summary>
    public class FlightLoggerService
    {
        private string _currentLogFilePath = string.Empty;
        private StreamWriter? _writer;
        private bool _isLogging = false;

        public FlightLoggerService()
        {
            string logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
        }

        public void StartLogging()
        {
            if (_isLogging) return;

            string logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            string fileName = $"FlightData_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            _currentLogFilePath = Path.Combine(logsDir, fileName);

            try
            {
                _writer = new StreamWriter(_currentLogFilePath, true);
                
                // CSV Header yazalım
                _writer.WriteLine("LocalTime,TeamId,PacketNumber,RocketAltitude,RocketGpsAltitude,RocketLatitude,RocketLongitude," +
                                  "PayloadGpsAltitude,PayloadLatitude,PayloadLongitude,StageGpsAltitude,StageLatitude,StageLongitude," +
                                  "GyroX,GyroY,GyroZ,AccelX,AccelY,AccelZ,Angle,StatusCode,IntegrityCode");
                _isLogging = true;
            }
            catch (Exception)
            {
                _isLogging = false;
            }
        }

        public void StopLogging()
        {
            if (!_isLogging || _writer == null) return;

            try
            {
                _writer.Flush();
                _writer.Close();
                _writer.Dispose();
                _writer = null;
            }
            finally
            {
                _isLogging = false;
            }
        }

        public void LogTelemetry(Models.TelemetryData data)
        {
            if (!_isLogging || _writer == null) return;

            string line = $"{DateTime.Now:HH:mm:ss.fff},{data.TeamId},{data.PacketNumber},{data.RocketAltitude:F2},{data.RocketGpsAltitude:F2}," +
                          $"{data.RocketLatitude:F6},{data.RocketLongitude:F6},{data.PayloadGpsAltitude:F2},{data.PayloadLatitude:F6}," +
                          $"{data.PayloadLongitude:F6},{data.StageGpsAltitude:F2},{data.StageLatitude:F6},{data.StageLongitude:F6}," +
                          $"{data.GyroX:F2},{data.GyroY:F2},{data.GyroZ:F2},{data.AccelX:F2},{data.AccelY:F2},{data.AccelZ:F2}," +
                          $"{data.Angle:F2},{data.StatusCode},{data.IntegrityCode}";

            try
            {
                _writer.WriteLine(line);
                _writer.Flush();
            }
            catch
            {
                // Sessizce hatayı yoksay
            }
        }
    }
}
