using GroundStation.Models;

namespace GroundStation.Utils.Filters
{
    /// <summary>
    /// Gelen telemetri verilerindeki fiziksel parametrelerin (İrtifa, GPS koordinatları, Açı vb.)
    /// mantıksal sınırlar içerisinde olup olmadığını denetler. Sınır dışı (Out of Range) değerleri,
    /// son bilinen geçerli değerlerle temizler (Data Cleaning).
    /// </summary>
    public class RangeCheckFilter : IDataFilter
    {
        // Maksimum/Minimum mantıksal sınırlar (Orta İrtifa testleri için)
        private const double MinAltitude = -100.0;     // Deniz seviyesi altı tolerans sınırı
        private const double MaxAltitude = 12000.0;    // 12 km (Orta İrtifa için üst sınır)
        private const double MinLatitude = -90.0;
        private const double MaxLatitude = 90.0;
        private const double MinLongitude = -180.0;
        private const double MaxLongitude = 180.0;
        private const double MinAngle = -360.0;
        private const double MaxAngle = 360.0;

        public TelemetryData Process(TelemetryData rawData, TelemetryData lastValidData)
        {
            if (rawData == null) return null;
            if (lastValidData == null) return rawData; // Referans veri yoksa ham veriyi kabul et

            // Aykırı değerlerin temizlenmesi (Out of Range -> Last Valid Value)
            double cleanedRocketAlt = IsInAltitudeRange(rawData.RocketAltitude) 
                ? rawData.RocketAltitude 
                : lastValidData.RocketAltitude;

            double cleanedRocketGpsAlt = IsInAltitudeRange(rawData.RocketGpsAltitude) 
                ? rawData.RocketGpsAltitude 
                : lastValidData.RocketGpsAltitude;

            double cleanedRocketLat = IsInLatitudeRange(rawData.RocketLatitude) 
                ? rawData.RocketLatitude 
                : lastValidData.RocketLatitude;

            double cleanedRocketLon = IsInLongitudeRange(rawData.RocketLongitude) 
                ? rawData.RocketLongitude 
                : lastValidData.RocketLongitude;

            double cleanedPayloadGpsAlt = IsInAltitudeRange(rawData.PayloadGpsAltitude) 
                ? rawData.PayloadGpsAltitude 
                : lastValidData.PayloadGpsAltitude;

            double cleanedPayloadLat = IsInLatitudeRange(rawData.PayloadLatitude) 
                ? rawData.PayloadLatitude 
                : lastValidData.PayloadLatitude;

            double cleanedPayloadLon = IsInLongitudeRange(rawData.PayloadLongitude) 
                ? rawData.PayloadLongitude 
                : lastValidData.PayloadLongitude;

            double cleanedStageGpsAlt = IsInAltitudeRange(rawData.StageGpsAltitude) 
                ? rawData.StageGpsAltitude 
                : lastValidData.StageGpsAltitude;

            double cleanedStageLat = IsInLatitudeRange(rawData.StageLatitude) 
                ? rawData.StageLatitude 
                : lastValidData.StageLatitude;

            double cleanedStageLon = IsInLongitudeRange(rawData.StageLongitude) 
                ? rawData.StageLongitude 
                : lastValidData.StageLongitude;

            double cleanedAngle = (rawData.Angle >= MinAngle && rawData.Angle <= MaxAngle) 
                ? rawData.Angle 
                : lastValidData.Angle;

            // Eğer veriler değiştirildiyse temizlenmiş yeni bir TelemetryData nesnesi oluşturup döneriz.
            return new TelemetryData(
                rawData.TeamId,
                rawData.PacketNumber,
                cleanedRocketAlt,
                cleanedRocketGpsAlt,
                cleanedRocketLat,
                cleanedRocketLon,
                cleanedPayloadGpsAlt,
                cleanedPayloadLat,
                cleanedPayloadLon,
                cleanedStageGpsAlt,
                cleanedStageLat,
                cleanedStageLon,
                rawData.GyroX,
                rawData.GyroY,
                rawData.GyroZ,
                rawData.AccelX,
                rawData.AccelY,
                rawData.AccelZ,
                cleanedAngle,
                rawData.StatusCode,
                rawData.IntegrityCode
            );
        }

        private bool IsInAltitudeRange(double alt) => alt >= MinAltitude && alt <= MaxAltitude;
        private bool IsInLatitudeRange(double lat) => lat >= MinLatitude && lat <= MaxLatitude;
        private bool IsInLongitudeRange(double lon) => lon >= MinLongitude && lon <= MaxLongitude;
    }
}
