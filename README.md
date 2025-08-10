# Aymed Medical - gRPC İstemci Case Study
Bu proje, Aymed Medical tarafından sağlanan case study gereksinimlerini karşılamak üzere geliştirilmiş bir .NET MAUI istemci uygulamasıdır. Uygulama, sağlanan sunucu (`AymedCase.exe`) ile gRPC üzerinden gerçek zamanlı olarak iletişim kurar, iki farklı veri akışını yönetir ve bu verileri kullanıcı arayüzünde görüntüler.
## Özellikler
- gRPC ile gerçek zamanlı sunucu-istemci iletişimi.
- İki farklı sunucu veri akışını (parametreli ve parametresiz) eşzamanlı olarak dinleme.
- Gelen verilerin son 10 tanesini arayüzdeki listede gösterme.
- Çoklu iş parçacığı (multi-threading) ortamında veri bütünlüğünü sağlamak için **SemaphoreSlim** kullanımı.
- İki akışın da aktif olduğu durumda, listeden belirli bir index'teki veriyi güvenli bir şekilde silebilme.
- Kullanıcı girişini sadece 0-9 arası rakamlarla kısıtlayan arayüz doğrulaması.
## Kullanılan Teknolojiler
- **Framework:** .NET 8, .NET MAUI
- **Dil:** C#
- **Arayüz:** XAML
- **İletişim Protokolü:** gRPC (Google Remote Procedure Call)
- **Asenkron Programlama:** `async/await`, `Task`
- **Thread Güvenliği:** `SemaphoreSlim`, `Interlocked`
## Kurulum ve Çalıştırma
1.  **Sunucuyu Çalıştırma:** İlk olarak, case study ile birlikte sağlanan `AymedCase.exe` sunucu uygulamasını çalıştırın.
2.  **İstemciyi Çalıştırma:** Projeyi Visual Studio 2022 ile açın. Araç çubuğunda başlangıç projesinin `MauiApp1`  ve hedefin `Windows Machine` olarak seçili olduğundan emin olun.
3.  Klavyeden **F5** tuşuna basarak veya yeşil 'Oynat' butonuna tıklayarak projeyi hata ayıklama modunda başlatın.
4.  Uygulamanın donma olmadan, tam performansla çalışmasını test etmek için **Ctrl + F5** (Start Without Debugging) ile de çalıştırabilirsiniz.
## Kod Mimarisi ve Önemli Kavramlar
### Asenkron Akış Yönetimi
gRPC'den gelen iki farklı veri akışı, kod tekrarını önlemek ve yönetimi kolaylaştırmak için merkezi bir `HandleStreamAsync` metodu tarafından yönetilir. Bu metot, `async/await` ve `await foreach` yapılarını kullanarak non-blocking (bloke etmeyen) bir şekilde verileri işler ve arayüze güvenli bir şekilde gönderir.
### Thread Güvenliği ve `SemaphoreSlim`
Uygulamanın en kritik noktası, iki farklı arka plan thread'inin (gRPC akışları) ve UI thread'inin (kullanıcı etkileşimleri) aynı veri kaynağını (`Messages` listesi) manipüle etmesidir. Bu durumun yol açabileceği "race condition"ları ve çökmeleri önlemek için `SemaphoreSlim` kullanılmıştır. `Messages` listesine yapılan tüm ekleme ve silme işlemleri, semafor tarafından korunan kritik bölgeler (critical sections) içinde, UI thread üzerinde güvenli bir şekilde gerçekleştirilir. Bu, uygulamanın aynı anda birden fazla işlem yaparken bile stabil kalmasını sağlar.
