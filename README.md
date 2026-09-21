# Akay Ground Station (Yer İstasyonu)

Akay Yer İstasyonu, yarışma kapsamında geliştirilmiş, telemetri verilerinin gerçek zamanlı olarak izlenebildiği, kaydedilebildiği ve analiz edilebildiği bir masaüstü arayüz yazılımıdır. 

## 🚀 Özellikler ve Donanım Testleri
Bu arayüz temel olarak aşağıdaki işlevleri yerine getirir:
- **Gerçek Zamanlı Veri Takibi:** Sensörlerden alınan telemetri verilerini (sıcaklık, basınç, ivme vb.) anlık olarak alır.
- **Grafiksel Gösterim:** Gelen veriler, analiz edilebilirliği artırmak için anlık olarak grafiklere dökülür.
- **Konum Takibi:** GPS verileri üzerinden anlık konum, harita üzerinde (GMap) gösterilir.

**Test Durumu:** Sistem, gerçek sensör donanımlarıyla entegre edilerek test edilmiştir. Sensörlerden gelen veri akışının (seri port üzerinden) başarılı bir şekilde arayüze aktarıldığı, grafiklerin ve haritanın doğru bir şekilde güncellendiği görülmüş ve doğrulanmıştır.

## 🤖 Geliştirme Süreci
Yarışmadaki kısıtlı süreyi en verimli şekilde kullanmak ve geliştirme hızını artırmak amacıyla yapay zeka (AI) araçlarından destek alınmıştır. Proje geçmişindeki AI tabanlı commit'ler, bu hızlı prototipleme sürecinin bir sonucudur.

## 💻 Kullanılan Teknolojiler
- **C# & .NET 10:** Windows Presentation Foundation (WPF) mimarisi ile geliştirilmiştir.
- **GMap.NET:** Harita entegrasyonu ve GPS konum takibi için kullanılmıştır.
- **LiveCharts:** Gelen telemetri verilerinin gerçek zamanlı grafiksel gösterimi için tercih edilmiştir.
- **System.IO.Ports:** Gerçek sensör donanımlarıyla seri port (COM) üzerinden haberleşmeyi sağlamak amacıyla kullanılmıştır.

## 🛠️ Nasıl Çalıştırılır?
Projeyi kendi yerel ortamınızda çalıştırmak için aşağıdaki adımları izleyebilirsiniz:

1. Bu depoyu bilgisayarınıza klonlayın:
   ```bash
   git clone <repo-url>
   ```
2. **Visual Studio 2022** (veya .NET 10 destekleyen güncel bir sürüm) kullanarak `GroundStation.csproj` dosyasını açın.
3. Proje açıldığında NuGet paketlerinin (GMap.NET, LiveCharts) otomatik olarak geri yüklenmesini bekleyin.
4. Gerekli donanım (sensör) bağlantılarını bilgisayarınıza yapın.
5. Projeyi **Start (F5)** ile çalıştırın.
6. Arayüz açıldığında, donanımınızın bağlı olduğu uygun **COM Port** ve **Baud Rate** ayarlarını seçerek veri akışını başlatın.
