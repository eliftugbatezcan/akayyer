using GroundStation.Models;
using System;

namespace GroundStation.Utils.Filters
{
    /// <summary>
    /// Sensörlerdeki anlık gürültüleri (radyasyon/parazit) ve sıçramaları (Outliers) temizlemek amacıyla
    /// 1D Kalman Filtresi ve Değişim Oranı Kontrolü (Rate of Change) uygular.
    /// </summary>
    public class OutlierRemovalFilter : IDataFilter
    {
        // 1D Kalman Filtre Parametreleri (Roket İrtifası için)
        private double _kalmanQ = 1.0;   // Süreç Gürültüsü (Process Noise)
        private double _kalmanR = 15.0;  // Ölçüm Gürültüsü (Measurement Noise)
        private double _kalmanP = 1.0;   // Hata Kovaryansı (Estimation Error)
        private double _kalmanK = 0.0;   // Kalman Kazancı (Kalman Gain)
        private double _kalmanX = 0.0;   // Tahmin Edilen İrtifa State (State Estimate)
        private bool _isKalmanInitialized = false;

        // Sensör Sıçrama Eşikleri (Maksimum makul değişim oranları / 100ms veya saniye başına)
        private const double MaxAltitudeStep = 180.0; // Maksimum irtifa adımı (m / paket) - Ses hızına yakın limit
        private const double MaxAccelStep = 10.0;     // Maksimum ivme değişimi (g / paket)
        private const double MaxGyroStep = 500.0;     // Maksimum jiroskop değişimi (deg/s / paket)

        public TelemetryData Process(TelemetryData rawData, TelemetryData lastValidData)
        {
            if (rawData == null) return null;
            if (lastValidData == null)
            {
                // İlk gelen veriyle Kalman filtresini başlat
                _kalmanX = rawData.RocketAltitude;
                _isKalmanInitialized = true;
                return rawData;
            }

            // --- 1. Değişim Oranı ile Sıçrama (Outlier) Ayıklama ---
            
            // İrtifa Sıçrama Kontrolü
            double altitudeDiff = Math.Abs(rawData.RocketAltitude - lastValidData.RocketAltitude);
            double checkedAltitude = rawData.RocketAltitude;
            if (altitudeDiff > MaxAltitudeStep)
            {
                // Sıçrama tespit edildi, son geçerli veriyle değiştirilir
                checkedAltitude = lastValidData.RocketAltitude;
                System.Diagnostics.Debug.WriteLine($"[FILTER] Altitude outlier detected: Diff={altitudeDiff:F2}m. Replaced with last valid value.");
            }

            // İvme Sıçrama Kontrolleri
            double cleanedAccX = CheckSensorStep(rawData.AccelX, lastValidData.AccelX, MaxAccelStep, "AccelX");
            double cleanedAccY = CheckSensorStep(rawData.AccelY, lastValidData.AccelY, MaxAccelStep, "AccelY");
            double cleanedAccZ = CheckSensorStep(rawData.AccelZ, lastValidData.AccelZ, MaxAccelStep, "AccelZ");

            // Jiroskop Sıçrama Kontrolleri
            double cleanedGyroX = CheckSensorStep(rawData.GyroX, lastValidData.GyroX, MaxGyroStep, "GyroX");
            double cleanedGyroY = CheckSensorStep(rawData.GyroY, lastValidData.GyroY, MaxGyroStep, "GyroY");
            double cleanedGyroZ = CheckSensorStep(rawData.GyroZ, lastValidData.GyroZ, MaxGyroStep, "GyroZ");


            // --- 2. Kalman Filtresi ile İrtifa Düzleştirme (Smoothing) ---
            if (!_isKalmanInitialized)
            {
                _kalmanX = checkedAltitude;
                _isKalmanInitialized = true;
            }

            // Predict (Tahmin)
            _kalmanP = _kalmanP + _kalmanQ;

            // Update (Güncelleme)
            _kalmanK = _kalmanP / (_kalmanP + _kalmanR);
            _kalmanX = _kalmanX + _kalmanK * (checkedAltitude - _kalmanX);
            _kalmanP = (1 - _kalmanK) * _kalmanP;

            double smoothedAltitude = _kalmanX;

            // Filtrelenmiş yeni veri paketi döndürülür
            return new TelemetryData(
                rawData.TeamId,
                rawData.PacketNumber,
                smoothedAltitude,            // Filtrelenmiş ve yumuşatılmış irtifa
                rawData.RocketGpsAltitude,
                rawData.RocketLatitude,
                rawData.RocketLongitude,
                rawData.PayloadGpsAltitude,
                rawData.PayloadLatitude,
                rawData.PayloadLongitude,
                rawData.StageGpsAltitude,
                rawData.StageLatitude,
                rawData.StageLongitude,
                cleanedGyroX,
                cleanedGyroY,
                cleanedGyroZ,
                cleanedAccX,
                cleanedAccY,
                cleanedAccZ,
                rawData.Angle,
                rawData.StatusCode,
                rawData.IntegrityCode
            );
        }

        private double CheckSensorStep(double rawValue, double lastValue, double maxStep, string sensorName)
        {
            double diff = Math.Abs(rawValue - lastValue);
            if (diff > maxStep)
            {
                System.Diagnostics.Debug.WriteLine($"[FILTER] {sensorName} outlier detected: Diff={diff:F2}. Replaced with last valid value.");
                return lastValue;
            }
            return rawValue;
        }
    }
}
