using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Cevap seçeneklerindeki her hayvana bu script eklenir. Artık "kart/kutu" konsepti yok -
// hayvan doğrudan sahnede, kendi ölçeğiyle, ortak bir "yer çizgisi" üzerinde duruyor.
// Tıklama var, sürükleme yok.
public class TahminSecenekKarti : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Hayvan görselini gösteren Image - bu objenin KENDİSİ olabilir ya da bir child.")]
    public Image hayvanGorselAlani;

    private bool dogruMu;
    private TahminYoneticisi yonetici;
    private bool kilitliMi = false;

    // olcek: TahminGorselKalibratoru ile hesaplanan, hayvanın DOĞAL oranını koruyan ölçek çarpanı.
    // tabanBoslugu: PNG'nin kendi alt kenarı ile karakterin GERÇEK ayak noktası arasındaki boşluk
    // (orijinal kaynak piksel cinsinden) - hayvanlar arası PNG padding farkını telafi eder.
    public void KartiKur(Sprite gorsel, bool dogruCevapMi, TahminYoneticisi oyunYoneticisi, float olcek, float tabanBoslugu = 0f)
    {
        dogruMu = dogruCevapMi;
        yonetici = oyunYoneticisi;
        kilitliMi = false;

        if (hayvanGorselAlani != null)
        {
            hayvanGorselAlani.sprite = gorsel;
            hayvanGorselAlani.preserveAspect = true;

            // YENİ: Boş (şeffaf) alana tıklanınca artık "hayvana tıklandı" sayılmasın -
            // Yapboz'daki YapbozParcasi.cs'te kullanılan aynı teknik. NOT: sprite'ın Import
            // Settings'inde "Read/Write Enabled" AÇIK olmalı, yoksa çalışmaz.
            hayvanGorselAlani.alphaHitTestMinimumThreshold = 0.1f;

            RectTransform rt = hayvanGorselAlani.rectTransform;

            // KRİTİK FIX: Bu obje önceden "stretch" anchor modundaydı, o modda ham sizeDelta
            // değeri (0,0) olarak saklanır. Anchor'ı NOKTA moduna çevirince Unity o (0,0)
            // sizeDelta'yı kullanır - yani RectTransform'un genişlik/yüksekliği SIFIR kalırdı.
            // Çözüm: sizeDelta'yı sprite'ın gerçek piksel boyutuna EXPLICIT olarak set ediyoruz.
            rt.sizeDelta = new Vector2(gorsel.rect.width, gorsel.rect.height);

            // Pivot alt-orta (0.5, 0) - böylece scale büyürken hayvan YUKARI doğru büyür.
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.localScale = Vector3.one * olcek;

            // YENİ: tabanBoslugu kadar AŞAĞI kaydır - böylece PNG'nin alt kenarı değil,
            // karakterin GERÇEK ayak noktası, slot'un anchoredPosition'ıyla (yer çizgisi) çakışır.
            // tabanBoslugu kaynak pikselde ölçüldüğü için, ekrandaki gerçek etkisini bulmak için
            // aynı 'olcek' çarpanıyla ölçeklenir.
            rt.anchoredPosition = new Vector2(0f, -(tabanBoslugu * olcek));
        }
    }

    // YENİ: TahminYoneticisi.OnValidate() bunu çağırır - Play modundayken Inspector'da
    // secenekGenelCarpan gibi bir değeri değiştirdiğinizde, sprite'ı yeniden atamadan
    // (gereksiz) sadece ölçek/pozisyonu anında güncelleyip canlı önizleme sağlar.
    public void OlcegiGuncelle(float olcek, float tabanBoslugu)
    {
        if (hayvanGorselAlani == null) return;
        RectTransform rt = hayvanGorselAlani.rectTransform;
        rt.localScale = Vector3.one * olcek;
        rt.anchoredPosition = new Vector2(0f, -(tabanBoslugu * olcek));
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (kilitliMi) return;

        if (dogruMu)
        {
            kilitliMi = true;
            yonetici.DogruTahmin();
        }
        else
        {
            yonetici.YanlisTahmin();
        }
    }
}