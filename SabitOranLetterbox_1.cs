using UnityEngine;

// Bu scripti Main Camera objesine ekle. Ayrıca Canvas'ı "Screen Space - Camera"
// moduna al ve Render Camera alanına Main Camera'yı sürükle (aşağıdaki notlara bak).
//
// Ekran oranı ne olursa olsun (telefon, tablet, hangi model olursa olsun) oyun
// HER ZAMAN sabit 16:9 alanında gösterilir - geri kalan kısım kameranın
// Background Color'ıyla (siyah) doldurulur. GERÇEK, garantili letterbox/pillarbox.
//
// ÖNEMLİ: Hiçbir child objeye, hiçbir Anchor'a dokunmuyor - hiyerarşideki
// TEK bir obje bile kaymıyor. Sadece kameranın "görüş alanını" (viewport) kısıyoruz.
[RequireComponent(typeof(Camera))]
public class SabitOranLetterbox : MonoBehaviour
{
    [Tooltip("Kalibrasyonun tasarlandığı oran değil, hedef cihazın (Redmi Note 10 Pro) " +
             "gerçek ekran oranı - 20:9. Bu sayede o cihazda SIFIR bar ile tam kusursuz oturur.")]
    public float hedefGenislik = 20f;
    public float hedefYukseklik = 9f;

    Camera kamera;
    int sonEkranGenislik = -1;
    int sonEkranYukseklik = -1;

    void Awake()
    {
        kamera = GetComponent<Camera>();
        OranAyarla();
    }

    void Update()
    {
        // Ekran boyutu değiştiyse (döndürme, farklı pencere vs.) yeniden hesapla -
        // her karede sadece iki int karşılaştırması, maliyetsiz.
        if (Screen.width != sonEkranGenislik || Screen.height != sonEkranYukseklik)
        {
            OranAyarla();
        }
    }

    void OranAyarla()
    {
        sonEkranGenislik = Screen.width;
        sonEkranYukseklik = Screen.height;

        float hedefOran = hedefGenislik / hedefYukseklik;
        float ekranOran = (float)Screen.width / Screen.height;

        Rect rect = kamera.rect;

        if (ekranOran > hedefOran)
        {
            // Ekran hedeften daha GENİŞ (çoğu modern telefon) - YANLARDA dikey siyah bar
            float genislikOrani = hedefOran / ekranOran;
            rect.width = genislikOrani;
            rect.height = 1f;
            rect.x = (1f - genislikOrani) / 2f;
            rect.y = 0f;
        }
        else
        {
            // Ekran hedeften daha DAR/UZUN - ÜST/ALTTA yatay siyah bar
            float yukseklikOrani = ekranOran / hedefOran;
            rect.width = 1f;
            rect.height = yukseklikOrani;
            rect.x = 0f;
            rect.y = (1f - yukseklikOrani) / 2f;
        }

        kamera.rect = rect;
    }
}
