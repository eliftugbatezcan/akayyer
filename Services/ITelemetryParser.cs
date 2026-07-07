using GroundStation.Models;

namespace GroundStation.Services
{
    /// <summary>
    /// Ham telemetri string/hex paketlerini okuyup ayrıştıran (parser) servis arayüzü.
    /// </summary>
    public interface ITelemetryParser
    {
        /// <summary>
        /// Gelen ham string satırı ayrıştırarak TelemetryData modeline dönüştürür.
        /// </summary>
        /// <param name="rawPacket">Seri porttan gelen ham CSV satırı</param>
        /// <returns>Ayrıştırılmış veri modeli</returns>
        TelemetryData Parse(string rawPacket);
    }
}
