using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// --- BU BİR OYUN SCRIPTI DEĞİL, SADECE SENİN İÇİN BİR EDİTÖR ARACI ---
// Play moduna hiç girmeden, Scene view'da elle boyutlandırıp tek tıkla kaydetmek için.
// YENİ: Artık tek bir "gorselOlcek" var - hem ödül görseli hem cevap seçenekleri bunu kullanıyor
// (cevap seçeneklerinin genel boyutu ayrıca TahminYoneticisi.secenekGenelCarpan ile ölçekleniyor).
public class TahminGorselKalibratoru : MonoBehaviour
{
    [Header("Nereye Kaydedilecek?")]
    public TahminYoneticisi hedefYonetici;
    [Tooltip("hayvanlar dizisindeki index (0-7)")]
    public int hedefHayvanIndex;

    [Header("Ölçülecek Obje")]
    [Tooltip("Genelde bu objenin KENDİ Rect Transform'u. Üstüne bir Image koy, o hayvanın " +
             "sprite'ını yerleştir, Scene view'da elle boyutlandır (Scale ya da Rect Tool ile), " +
             "diğer hayvanlarla görsel olarak dengeli duracak şekilde ayarla.")]
    public RectTransform gorselTransform;

#if UNITY_EDITOR
    [ContextMenu("★ Bu Ölçeği Hayvana Kaydet")]
    void Kaydet()
    {
        if (hedefYonetici == null)
        {
            Debug.LogError("[TahminGorselKalibratoru] Hedef Yonetici atanmamış!");
            return;
        }
        if (gorselTransform == null)
        {
            Debug.LogError("[TahminGorselKalibratoru] Gorsel Transform atanmamış!");
            return;
        }
        if (hedefYonetici.hayvanlar == null || hedefHayvanIndex < 0 || hedefHayvanIndex >= hedefYonetici.hayvanlar.Length)
        {
            Debug.LogError("[TahminGorselKalibratoru] Hedef Hayvan Index geçersiz!");
            return;
        }
        if (hedefYonetici.hayvanGorseli == null)
        {
            Debug.LogError("[TahminGorselKalibratoru] Hedef Yonetici'nin Hayvan Gorseli alanı boş!");
            return;
        }

        RectTransform referansAlan = hedefYonetici.hayvanGorseli.rectTransform;

        float referansGenislik = referansAlan.rect.width * referansAlan.lossyScale.x;
        float efektifGenislik = gorselTransform.rect.width * gorselTransform.lossyScale.x;

        if (referansGenislik <= 0f)
        {
            Debug.LogError("[TahminGorselKalibratoru] Referans Alan genişliği sıfır görünüyor!");
            return;
        }

        float olcek = efektifGenislik / referansGenislik;
        hedefYonetici.hayvanlar[hedefHayvanIndex].gorselOlcek = olcek;

        EditorUtility.SetDirty(hedefYonetici);
        Debug.Log("[TahminGorselKalibratoru] hayvan[" + hedefHayvanIndex + "] (" +
            hedefYonetici.hayvanlar[hedefHayvanIndex].hayvanAdi + ") için gorselOlcek = " + olcek +
            " olarak kaydedildi. ŞİMDİ SAHNEYİ KAYDET (Ctrl+S) yoksa kaybolur!");
    }
#endif
}