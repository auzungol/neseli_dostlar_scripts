using UnityEngine;

// Herhangi bir sahneye/objeye eklemene GEREK YOK - [RuntimeInitializeOnLoadMethod]
// sayesinde oyun açılır açılmaz, ilk sahne yüklenmeden ÖNCE otomatik çalışır.
// Android'de Unity varsayılan olarak kare hızını 30 FPS'e sabitliyor (targetFrameRate
// hiç ayarlanmazsa) - Editor'deki Play modu bu sınırlamaya tabi olmadığı için
// bilgisayarda akıcı görünüp telefonda 30-40 FPS'e düşmesinin sebebi büyük ihtimalle buydu.
public static class PerformansAyarlayici
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AyarlariUygula()
    {
        // NOT: Bu bir "tavan" - cihaz 60Hz ise otomatik 60'ta kalır, 90/120/144Hz
        // destekliyorsa o hıza kadar çıkabilir. Düşük bir sayı vermek gereksiz yere
        // cihazı kısıtlar, yüksek bir sayı vermek ise güvenli - donanım zaten kendi
        // gerçek üst sınırını uygular.
        Application.targetFrameRate = 144;

        // VSync bazı platformlarda targetFrameRate'i ezip kendi tavanını uygulayabiliyor -
        // kapatıyoruz ki yukarıdaki ayar gerçekten etkili olsun.
        QualitySettings.vSyncCount = 0;
    }
}
