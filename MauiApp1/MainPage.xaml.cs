using Grpc.Core; // Grpc kütüphaneleri
using Grpc.Net.Client;
using GrpcServerSample;
using Microsoft.Maui.Controls;
using System;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MauiApp1; 

public partial class MainPage : ContentPage
{
    // _channel: Sunucunun adresine olan fiziksel bağlantıyı temsil eder.
    // _client: Bu bağlantı üzerinden hangi komutları gönderebileceğimizi tanımlar.
    private GrpcChannel? _channel;
    private NotificationProto.NotificationProtoClient? _client;

    // ObservableCollection kullanıyoruz, çünkü içine bir eleman eklenince/silinince arayüzü otomatik olarak günceller.
    public ObservableCollection<string> Messages { get; set; } = new();

    // "Tek kişilik turnike" gibi davranan, listeye aynı anda sadece bir thread'in erişmesini sağlayan koruma mekanizması.
    private readonly SemaphoreSlim _messagesSemaphore;
    private int _activeStreamCount = 0;

    public MainPage()
    {
        // XAML  arayüz baglantı
        InitializeComponent();

        _messagesSemaphore = new SemaphoreSlim(1, 1);

        try
        {
            // Aymed case.exe default 5001 ayarlı 
            _channel = GrpcChannel.ForAddress("http://localhost:5000");
            _client = new NotificationProto.NotificationProtoClient(_channel);
        }
        catch (Exception ex)
        {
            // Hata logla
            Console.WriteLine($"CRITICAL CRASH: gRPC setup failed. ERROR: {ex.ToString()}");
        }
        // Arayüzdeki ListView'in veri kaynağını, bizim Messages listemiz olarak ayarlıyoruz.
        NotificationsListView.ItemsSource = Messages;
    }

    //  Her iki "İzle" butonu tarafından da çağrılan, gRPC akışını dinleyen merkezi metot.
    private async Task HandleStreamAsync(AsyncServerStreamingCall<NotificationResponse> call, Button streamButton, string originalButtonText)
    {
        // Aktif akış sayısını thread-safe (güvenli) bir şekilde bir artır.
        Interlocked.Increment(ref _activeStreamCount);
        try
        {
            //Sunucu her yeni mesaj gönderdiğinde döngünün içine girer.
            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                
                // Kesme fonksiyonunun çalışıp çalışmadığını anlamamız için her mesaj arasında bir saniye  bekle.
                await Task.Delay(1000);
          

                await _messagesSemaphore.WaitAsync(); // Listeye erişmeden önce "turnikeden" geçmek için izin iste.
                try
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Messages.Insert(0, response.Message); // Gelen mesajı listenin başına ekle.
                        if (Messages.Count > 10) Messages.RemoveAt(10); // Listede 10'dan fazla eleman varsa en eskisini (sondakini) sil.
                    });
                }
                finally
                {
                    _messagesSemaphore.Release(); // İşin bitince "turnikeden" çıktığını bildir, başkası girebilsin.
                }
            }
        }
        finally
        {
            // Akış bittiğinde veya hata olduğunda, sayacı güvenli bir şekilde bir azalt.
            Interlocked.Decrement(ref _activeStreamCount);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                streamButton.IsEnabled = true;
                streamButton.Text = originalButtonText;
                if (streamButton == StreamContentBtn)
                {
                    ContentEntry.IsEnabled = true;
                }
            });
        }
    }

    //Bildirimleri İzle (Parametresiz)
    private async void StreamNotificationsBtn_Clicked(object sender, EventArgs e)
    {
        if (_client == null) return;

        StreamNotificationsBtn.IsEnabled = false;
        StreamNotificationsBtn.Text = "Dinleniyor...";

        try
        {
            // Sunucudan parametresiz akışı başlatır ve ana işi HandleStreamAsync metoduna devreder.
            var call = _client.StreamNotifications(new EmptyRequest());
            await HandleStreamAsync(call, StreamNotificationsBtn, "Bildirimleri İzle (Parametresiz)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"StreamNotificationsBtn_Clicked Error: {ex}");
            // Butonu tekrar aktif hale getir.
            StreamNotificationsBtn.IsEnabled = true;
            StreamNotificationsBtn.Text = "Bildirimleri İzle (Parametresiz)";
        }
    }
    //"İzle (Parametreli)
    private async void StreamContentBtn_Clicked(object sender, EventArgs e)
    {
        if (_client == null) return;
        var contentText = ContentEntry.Text;
        if (string.IsNullOrWhiteSpace(contentText)) return;

        StreamContentBtn.IsEnabled = false;
        ContentEntry.IsEnabled = false;

        try
        {
            // Metin kutusundaki değeri parametre olarak gönder
            var call = _client.StreamContentNotifications(new ContentRequest { Title = contentText });
            await HandleStreamAsync(call, StreamContentBtn, "İzle (Parametreli)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"StreamContentBtn_Clicked Error: {ex}");
            // Butonu tekrar aktif hale getirmeyi getir.
            StreamContentBtn.IsEnabled = true;
            ContentEntry.IsEnabled = true;
            StreamContentBtn.Text = "İzle (Parametreli)";
        }
    }

    // "Kes" butonu
    private async void CutBtn_Clicked(object sender, EventArgs e)
    {
        // Akış kontrolü , (Eğer iki akış da başlamadıysa kesme işlemi yapılmaz)
        if (_activeStreamCount < 2)
        {
            await DisplayAlert("Uyarı", "Lütfen önce 2 akışı da başlatın.", "Tamam");
            return;
        }

       // İndex kontrolü (Kullanıcı yalnızca 0 ile 9 arasimda sayı girebilir.)
        if (!int.TryParse(IndexEntry.Text, out int index) || index < 0 || index > 9)
        {
            await DisplayAlert("Hata", "Lütfen 0 ile 9 arasında geçerli bir index girin.", "Tamam");
            return;
        }



        // Eğer girilen index değerinden daha az sayıda liste elemanı var ise uyarı ver.
        if (index >= Messages.Count)
        {
            await DisplayAlert("Hata", "Girdiğiniz index listede bulunmuyor.", "Tamam");
            return;
        }

        // Sadece silme işlemi için listeyi kilitleyelim.
        await _messagesSemaphore.WaitAsync();
        try
        {
            if (index < Messages.Count)
            {
                Messages.RemoveAt(index);
            }
        }
        finally
        {
            _messagesSemaphore.Release();
        }
    }

    // Index metin kutusuna sadece rakam girilmesini sağlayan fonksiyon
    private void IndexEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
       
        if (!string.IsNullOrEmpty(e.NewTextValue) && !char.IsDigit(e.NewTextValue.LastOrDefault()))
        {
            var entry = (Entry)sender;
            entry.Text = e.OldTextValue;
        }
    }
}