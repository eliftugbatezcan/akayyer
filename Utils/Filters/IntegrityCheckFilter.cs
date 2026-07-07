using System;
using System.Globalization;
using GroundStation.Models;
using GroundStation.Utils.Helpers;

namespace GroundStation.Utils.Filters
{
    /// <summary>
    /// Gelen paketin bütünlük (Integrity) doğrulamasını yapar.
    /// CRC16 veya MD5 kodunun doğruluğunu kontrol eder. Uyuşmazlık durumunda paketi iptal eder (null döner).
    /// </summary>
    public class IntegrityCheckFilter : IDataFilter
    {
        public TelemetryData Process(TelemetryData rawData, TelemetryData lastValidData)
        {
            if (rawData == null) return null;

            // Bütünlük doğrulama stringi oluşturulur (ilk 20 parametre birleştirilir)
            string payload = string.Format(
                CultureInfo.InvariantCulture,
                "{0},{1},{2:F2},{3:F2},{4:F6},{5:F6},{6:F2},{7:F6},{8:F6},{9:F2},{10:F6},{11:F6},{12:F2},{13:F2},{14:F2},{15:F2},{16:F2},{17:F2},{18:F2},{19}",
                rawData.TeamId,
                rawData.PacketNumber,
                rawData.RocketAltitude,
                rawData.RocketGpsAltitude,
                rawData.RocketLatitude,
                rawData.RocketLongitude,
                rawData.PayloadGpsAltitude,
                rawData.PayloadLatitude,
                rawData.PayloadLongitude,
                rawData.StageGpsAltitude,
                rawData.StageLatitude,
                rawData.StageLongitude,
                rawData.GyroX,
                rawData.GyroY,
                rawData.GyroZ,
                rawData.AccelX,
                rawData.AccelY,
                rawData.AccelZ,
                rawData.Angle,
                rawData.StatusCode
            );

            // Paket üzerindeki doğrulama kodu
            string rawCode = rawData.IntegrityCode?.Trim() ?? string.Empty;

            // MD5 doğrulaması (32 karakter uzunluğundaysa MD5 varsayalım, değilse CRC16)
            if (rawCode.Length == 32)
            {
                string calculatedMd5 = ChecksumCalculator.ComputeMd5(payload);
                if (string.Equals(calculatedMd5, rawCode, StringComparison.OrdinalIgnoreCase))
                {
                    return rawData;
                }
            }
            else
            {
                // CRC16 CCITT
                ushort calculatedCrc = ChecksumCalculator.ComputeCrc16(payload);
                string calculatedCrcHex = calculatedCrc.ToString("X4");
                
                if (string.Equals(calculatedCrcHex, rawCode, StringComparison.OrdinalIgnoreCase) || 
                    rawCode == calculatedCrc.ToString())
                {
                    return rawData;
                }
            }

            // Bütünlük doğrulaması başarısız ise loglanır ve veri reddedilir.
            System.Diagnostics.Debug.WriteLine($"[WARNING] Telemetry integrity verification failed! Expected payload CRC/MD5 does not match raw code: {rawCode}");
            return null; 
        }
    }
}
