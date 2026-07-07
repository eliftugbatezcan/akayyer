using System;

namespace GroundStation.Models
{
    /// <summary>
    /// Roket ve alt sistemlerinden (Görev Yükü, Kademe) gelen 21 parametreli telemetri paket modelidir.
    /// Bu sınıf veri bütünlüğünün korunması adına "immutable" (değiştirilemez) olarak tasarlanmıştır.
    /// </summary>
    public class TelemetryData
    {
        // 1. Takım ID
        public string TeamId { get; }

        // 2. Sayaç (Paket Numarası)
        public int PacketNumber { get; }

        // 3. Roket İrtifa (Metre)
        public double RocketAltitude { get; }

        // 4. Roket GPS İrtifası (Metre)
        public double RocketGpsAltitude { get; }

        // 5. Roket Enlem (Derece)
        public double RocketLatitude { get; }

        // 6. Roket Boylam (Derece)
        public double RocketLongitude { get; }

        // 7. Görev Yükü GPS İrtifası (Metre)
        public double PayloadGpsAltitude { get; }

        // 8. Görev Yükü Enlem (Derece)
        public double PayloadLatitude { get; }

        // 9. Görev Yükü Boylam (Derece)
        public double PayloadLongitude { get; }

        // 10. Kademe GPS İrtifası (Metre)
        public double StageGpsAltitude { get; }

        // 11. Kademe Enlem (Derece)
        public double StageLatitude { get; }

        // 12. Kademe Boylam (Derece)
        public double StageLongitude { get; }

        // 13. Jiroskop X (deg/s)
        public double GyroX { get; }

        // 14. Jiroskop Y (deg/s)
        public double GyroY { get; }

        // 15. Jiroskop Z (deg/s)
        public double GyroZ { get; }

        // 16. İvme X (g)
        public double AccelX { get; }

        // 17. İvme Y (g)
        public double AccelY { get; }

        // 18. İvme Z (g)
        public double AccelZ { get; }

        // 19. Açı (Derece - Pitch/Roll/Yaw veya genel oryantasyon açısı)
        public double Angle { get; }

        // 20. Durum Kodu (Aviyonik Sistem Durumu)
        public int StatusCode { get; }

        // 21. Bütünlük Doğrulama Kodu (CRC / MD5 vb.)
        public string IntegrityCode { get; }

        // Telemetrinin yer istasyonu tarafından alınma zaman damgası
        public DateTime Timestamp { get; }

        public TelemetryData(
            string teamId,
            int packetNumber,
            double rocketAltitude,
            double rocketGpsAltitude,
            double rocketLatitude,
            double rocketLongitude,
            double payloadGpsAltitude,
            double payloadLatitude,
            double payloadLongitude,
            double stageGpsAltitude,
            double stageLatitude,
            double stageLongitude,
            double gyroX,
            double gyroY,
            double gyroZ,
            double accelX,
            double accelY,
            double accelZ,
            double angle,
            int statusCode,
            string integrityCode)
        {
            TeamId = teamId;
            PacketNumber = packetNumber;
            RocketAltitude = rocketAltitude;
            RocketGpsAltitude = rocketGpsAltitude;
            RocketLatitude = rocketLatitude;
            RocketLongitude = rocketLongitude;
            PayloadGpsAltitude = payloadGpsAltitude;
            PayloadLatitude = payloadLatitude;
            PayloadLongitude = payloadLongitude;
            StageGpsAltitude = stageGpsAltitude;
            StageLatitude = stageLatitude;
            StageLongitude = stageLongitude;
            GyroX = gyroX;
            GyroY = gyroY;
            GyroZ = gyroZ;
            AccelX = accelX;
            AccelY = accelY;
            AccelZ = accelZ;
            Angle = angle;
            StatusCode = statusCode;
            IntegrityCode = integrityCode;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $"Packet #{PacketNumber} | Team: {TeamId} | Alt: {RocketAltitude}m | GPS Alt: {RocketGpsAltitude}m | Lat: {RocketLatitude}, Lon: {RocketLongitude} | Status: {StatusCode}";
        }
    }
}
