using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Cevap seçeneklerindeki her hayvan kartına bu scripti ekleyeceğiz. Sürükleme YOK, sadece tıklama.
// Kart artık iki görsel katmandan oluşuyor: sabit tahta arka planı (elle atanmış) + değişen hayvan görseli.
public class TahminSecenekKarti : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("Kartın İÇİNDEKİ hayvan görselini gösteren Image - tahta arka planın DEĞİL, ayrı bir çocuk obje olmalı")]
    public Image hayvanGorselAlani;

    private bool dogruMu;
    private TahminYoneticisi yonetici;
    private bool kilitliMi = false;

    // dolulukOrani: kutunun ne kadarını dolduracağı (1 = kutuyu tam kullan, 0.7 = biraz daha küçük göster).
    // 0.1-1 arasına SIKIŞTIRILIR (clamp) - böylece hangi değer gelirse gelsin görsel asla
    // kutunun dışına taşıp komşu kartın üstüne binemez.
    public void KartiKur(Sprite gorsel, bool dogruCevapMi, TahminYoneticisi oyunYoneticisi, float dolulukOrani = 1f)
    {
        dogruMu = dogruCevapMi;
        yonetici = oyunYoneticisi;
        kilitliMi = false;

        if (hayvanGorselAlani != null)
        {
            hayvanGorselAlani.sprite = gorsel;
            hayvanGorselAlani.preserveAspect = true;

            dolulukOrani = Mathf.Clamp(dolulukOrani, 0.1f, 1f);
            float inset = (1f - dolulukOrani) / 2f;

            RectTransform rt = hayvanGorselAlani.rectTransform;
            rt.localScale = Vector3.one; // önceki (bozuk) scale kalıntısı varsa sıfırla
            rt.anchorMin = new Vector2(inset, inset);
            rt.anchorMax = new Vector2(1f - inset, 1f - inset);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
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