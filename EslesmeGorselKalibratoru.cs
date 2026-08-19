using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// --- BU BİR OYUN SCRIPTI DEĞİL, SADECE SENİN İÇİN BİR EDİTÖR ARACI ---
// Play moduna hiç girmeden, Scene view'da elle boyutlandırıp tek tıkla kaydetmek için.
public class EslesmeGorselKalibratoru : MonoBehaviour
{
    [Header("Nereye Kaydedilecek?")]
    public EslesmeYoneticisi hedefYonetici;
    [Tooltip("eslesmeler dizisindeki index (0-7)")]
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
            Debug.LogError("[EslesmeGorselKalibratoru] Hedef Yonetici atanmamış!");
            return;
        }
        if (gorselTransform == null)
        {
            Debug.LogError("[EslesmeGorselKalibratoru] Gorsel Transform atanmamış!");
            return;
        }
        if (hedefYonetici.eslesmeler == null || hedefHayvanIndex < 0 || hedefHayvanIndex >= hedefYonetici.eslesmeler.Length)
        {
            Debug.LogError("[EslesmeGorselKalibratoru] Hedef Hayvan Index geçersiz!");
            return;
        }

        if (hedefYonetici.hayvanGorseli == null)
        {
            Debug.LogError("[EslesmeGorselKalibratoru] Hedef Yonetici'nin Hayvan Gorseli alanı boş!");
            return;
        }

        // Gerçek (efektif) görünen genişliği hesapla - Scale Tool VEYA Rect Tool ile
        // boyutlandırmış olman fark etmez, ikisini de doğru okur.
        RectTransform referans = hedefYonetici.hayvanGorseli.rectTransform;
        float referansGenislik = referans.rect.width * referans.lossyScale.x;
        float efektifGenislik = gorselTransform.rect.width * gorselTransform.lossyScale.x;

        if (referansGenislik <= 0f)
        {
            Debug.LogError("[EslesmeGorselKalibratoru] Referans (HayvanGorseli) genişliği sıfır görünüyor!");
            return;
        }

        float olcek = efektifGenislik / referansGenislik;
        hedefYonetici.eslesmeler[hedefHayvanIndex].gorselOlcek = olcek;

        EditorUtility.SetDirty(hedefYonetici);
        Debug.Log("[EslesmeGorselKalibratoru] hayvan[" + hedefHayvanIndex + "] (" +
            hedefYonetici.eslesmeler[hedefHayvanIndex].hayvanAdi + ") için ölçek " + olcek +
            " olarak kaydedildi. ŞİMDİ SAHNEYİ KAYDET (Ctrl+S) yoksa kaybolur!");
    }
#endif
}