using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// --- BU BİR OYUN SCRIPTI DEĞİL, SADECE SENİN İÇİN BİR EDİTÖR ARACI ---
// Play moduna hiç girmeden, Scene view'da elle boyutlandırıp tek tıkla kaydetmek için.
public class TahminGorselKalibratoru : MonoBehaviour
{
    [Header("Nereye Kaydedilecek?")]
    public TahminYoneticisi hedefYonetici;
    [Tooltip("hayvanlar dizisindeki index (0-7)")]
    public int hedefHayvanIndex;

    [Tooltip("İŞARETLİYSE: 'secenekGorselOlcek' alanına kaydeder (cevap kartındaki küçük görsel için - " +
             "artık 0-1 arası bir 'doluluk oranı' olarak kullanılıyor, kutudan taşma imkansız). " +
             "İŞARETLİ DEĞİLSE: 'gorselOlcek' alanına kaydeder (ödül/reveal görseli için, eski davranış, taşıma korumasız).")]
    public bool kartIcinMi = false;

    [Header("Ölçülecek Obje")]
    [Tooltip("Genelde bu objenin KENDİ Rect Transform'u. Üstüne bir Image koy, o hayvanın " +
             "sprite'ını yerleştir, Scene view'da elle boyutlandır (Scale ya da Rect Tool ile, ikisi de olur), " +
             "diğer hayvanlarla görsel olarak dengeli duracak şekilde ayarla.")]
    public RectTransform gorselTransform;

    [Tooltip("Karşılaştırma için referans. 'Kart İçin mi?' İŞARETLİ DEĞİLSE: TahminYoneticisi'ndeki " +
             "'Hayvan Gorseli' (ödül/reveal görseli) objesini sürükle. İŞARETLİYSE: sahnedeki bir " +
             "SecenekKarti örneğinin İÇİNDEKİ 'HayvanGorseli' objesini sürükle - Oyun Alani'ndaki " +
             "'Hayvan Gorseli' DEĞİL, kartın kendi içindeki küçük görsel alanı olmalı. " +
             "Ölçek, bu objenin taban genişliğine göre hesaplanır.")]
    public RectTransform referansAlan;

#if UNITY_EDITOR
    [ContextMenu("★ Bu Ölçeği Hayvana Kaydet")]
    void Kaydet()
    {
        if (hedefYonetici == null)
        {
            Debug.LogError("[TahminGorselKalibratoru] Hedef Yonetici atanmamış!");
            return;
        }
        if (gorselTransform == null || referansAlan == null)
        {
            Debug.LogError("[TahminGorselKalibratoru] Gorsel Transform ya da Referans Alan atanmamış!");
            return;
        }
        if (hedefYonetici.hayvanlar == null || hedefHayvanIndex < 0 || hedefHayvanIndex >= hedefYonetici.hayvanlar.Length)
        {
            Debug.LogError("[TahminGorselKalibratoru] Hedef Hayvan Index geçersiz!");
            return;
        }

        float referansGenislik = referansAlan.rect.width * referansAlan.lossyScale.x;
        float efektifGenislik = gorselTransform.rect.width * gorselTransform.lossyScale.x;

        if (referansGenislik <= 0f)
        {
            Debug.LogError("[TahminGorselKalibratoru] Referans Alan genişliği sıfır görünüyor!");
            return;
        }

        float olcek = efektifGenislik / referansGenislik;

        if (kartIcinMi)
            hedefYonetici.hayvanlar[hedefHayvanIndex].secenekGorselOlcek = olcek;
        else
            hedefYonetici.hayvanlar[hedefHayvanIndex].gorselOlcek = olcek;

        EditorUtility.SetDirty(hedefYonetici);
        Debug.Log("[TahminGorselKalibratoru] hayvan[" + hedefHayvanIndex + "] (" +
            hedefYonetici.hayvanlar[hedefHayvanIndex].hayvanAdi + ") için " +
            (kartIcinMi ? "secenekGorselOlcek" : "gorselOlcek") + " = " + olcek +
            " olarak kaydedildi. ŞİMDİ SAHNEYİ KAYDET (Ctrl+S) yoksa kaybolur!");
    }
#endif
}