using GroundStation.Models;

namespace GroundStation.Utils.Filters
{
    /// <summary>
    /// Gelen telemetri verilerini temizleme ve düzeltme işlemleri için arayüz.
    /// Her filtre bir önceki geçerli veri durumunu referans alarak veriyi işleyebilir.
    /// </summary>
    public interface IDataFilter
    {
        /// <summary>
        /// Gelen telemetri verisini filtreler ve temizler.
        /// </summary>
        /// <param name="rawData">Ham/Gelen telemetri verisi</param>
        /// <param name="lastValidData">Filtreden geçmiş en son geçerli veri</param>
        /// <returns>Filtrelenmiş ve temizlenmiş telemetri verisi. Eğer veri tamamen geçersizse null dönebilir.</returns>
        TelemetryData Process(TelemetryData rawData, TelemetryData lastValidData);
    }
}
