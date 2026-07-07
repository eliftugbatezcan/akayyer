using System;
using System.Globalization;
using GroundStation.Models;

namespace GroundStation.Services
{
    /// <summary>
    /// Gelen virgülle ayrılmış (CSV) 21 parametreli telemetri verisini ayrıştıran ve
    /// TelemetryData modeline dönüştüren somut parser sınıfıdır.
    /// </summary>
    public class TelemetryParser : ITelemetryParser
    {
        private const int ExpectedFieldCount = 21;

        public TelemetryData Parse(string rawPacket)
        {
            if (string.IsNullOrWhiteSpace(rawPacket))
            {
                throw new ArgumentException("Gelen telemetri verisi boş olamaz.");
            }

            // Gelen paketi virgüllerine göre ayır
            string[] fields = rawPacket.Split(',');

            if (fields.Length != ExpectedFieldCount)
            {
                throw new FormatException($"Hatalı telemetri paketi! Beklenen parametre sayısı: {ExpectedFieldCount}, Gelen: {fields.Length}");
            }

            try
            {
                // Kültürden bağımsız (Invariant Culture - Nokta (.) ondalık ayracı için) dönüştürme işlemleri
                string teamId = fields[0].Trim();
                int packetNumber = int.Parse(fields[1].Trim(), CultureInfo.InvariantCulture);
                
                double rocketAltitude = double.Parse(fields[2].Trim(), CultureInfo.InvariantCulture);
                double rocketGpsAltitude = double.Parse(fields[3].Trim(), CultureInfo.InvariantCulture);
                double rocketLatitude = double.Parse(fields[4].Trim(), CultureInfo.InvariantCulture);
                double rocketLongitude = double.Parse(fields[5].Trim(), CultureInfo.InvariantCulture);
                
                double payloadGpsAltitude = double.Parse(fields[6].Trim(), CultureInfo.InvariantCulture);
                double payloadLatitude = double.Parse(fields[7].Trim(), CultureInfo.InvariantCulture);
                double payloadLongitude = double.Parse(fields[8].Trim(), CultureInfo.InvariantCulture);
                
                double stageGpsAltitude = double.Parse(fields[9].Trim(), CultureInfo.InvariantCulture);
                double stageLatitude = double.Parse(fields[10].Trim(), CultureInfo.InvariantCulture);
                double stageLongitude = double.Parse(fields[11].Trim(), CultureInfo.InvariantCulture);

                double gyroX = double.Parse(fields[12].Trim(), CultureInfo.InvariantCulture);
                double gyroY = double.Parse(fields[13].Trim(), CultureInfo.InvariantCulture);
                double gyroZ = double.Parse(fields[14].Trim(), CultureInfo.InvariantCulture);

                double accelX = double.Parse(fields[15].Trim(), CultureInfo.InvariantCulture);
                double accelY = double.Parse(fields[16].Trim(), CultureInfo.InvariantCulture);
                double accelZ = double.Parse(fields[17].Trim(), CultureInfo.InvariantCulture);

                double angle = double.Parse(fields[18].Trim(), CultureInfo.InvariantCulture);
                int statusCode = int.Parse(fields[19].Trim(), CultureInfo.InvariantCulture);
                string integrityCode = fields[20].Trim();

                return new TelemetryData(
                    teamId,
                    packetNumber,
                    rocketAltitude,
                    rocketGpsAltitude,
                    rocketLatitude,
                    rocketLongitude,
                    payloadGpsAltitude,
                    payloadLatitude,
                    payloadLongitude,
                    stageGpsAltitude,
                    stageLatitude,
                    stageLongitude,
                    gyroX,
                    gyroY,
                    gyroZ,
                    accelX,
                    accelY,
                    accelZ,
                    angle,
                    statusCode,
                    integrityCode
                );
            }
            catch (Exception ex)
            {
                throw new FormatException($"Telemetri verisi sayısallaştırılırken tip uyuşmazlığı hatası oluştu: {ex.Message}", ex);
            }
        }
    }
}
