using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// --- BU BİR OYUN SCRIPTI DEĞİL, SADECE SENİN İÇİN BİR EDİTÖR ARACI ---
// Sahnede geçici bir GameObject'e ekle, parçaların konumunu/boyutunu gözle ayarla,
// sonra sağ tık (component başlığındaki ⋮) > "Bu Konumları Hayvana Kaydet" de.
public class YapbozKalibratoru : MonoBehaviour
{
    [Header("Nereye Kaydedilecek?")]
    public YapbozYoneticisi hedefYonetici;
    [Tooltip("hayvanSetleri dizisindeki index (0-7)")]
    public int hedefHayvanIndex;
    [Tooltip("İşaretliyse 4x3 (12 parça) için kaydeder, değilse 3x2 (6 parça) için")]
    public bool zorMod;

    [Header("Referans Alan (ÖNEMLİ)")]
    [Tooltip("YapbozYoneticisi'ndeki 'Oyun Alani' ile AYNI RectTransform'u buraya sürükle. " +
             "Bu sayede parçalarını istediğin gibi bir grup altında büyütüp/küçültebilirsin " +
             "(örn. hepsini büyük boyutta hizalayıp sonra parent'ı ölçeklendirerek küçültebilirsin), " +
             "kalibratör her zaman doğru gerçek boyutu/konumu hesaplar.")]
    public RectTransform referansAlan;

    [Header("Kalibrasyon İşaretçileri")]
    [Tooltip("Elle sürükleyip hizaladığın RectTransform'lar. Sırası parça sırasıyla AYNI olmalı " +
             "(r1c1, r1c2, r1c3, r2c1...). İSTEDİĞİN KADAR BÜYÜK/KÜÇÜK OLABİLİRLER, istediğin bir " +
             "parent'ın altında olabilirler, istediğin ölçekte (Scale) olabilirler - kaydederken " +
             "hepsi otomatik doğru orana çevrilir.")]
    public RectTransform[] parcaIsaretcileri;

#if UNITY_EDITOR
    [ContextMenu("★ Bu Konumları Hayvana Kaydet")]
    void Kaydet()
    {
        if (hedefYonetici == null)
        {
            Debug.LogError("[YapbozKalibratoru] Hedef Yonetici atanmamış!");
            return;
        }
        if (referansAlan == null)
        {
            Debug.LogError("[YapbozKalibratoru] Referans Alan atanmamış! YapbozYoneticisi'ndeki Oyun Alani'nı sürükle.");
            return;
        }
        if (hedefYonetici.hayvanSetleri == null || hedefHayvanIndex < 0 || hedefHayvanIndex >= hedefYonetici.hayvanSetleri.Length)
        {
            Debug.LogError("[YapbozKalibratoru] Hedef Hayvan Index geçersiz!");
            return;
        }
        if (parcaIsaretcileri == null || parcaIsaretcileri.Length == 0)
        {
            Debug.LogError("[YapbozKalibratoru] Parça İşaretçileri boş!");
            return;
        }

        int n = parcaIsaretcileri.Length;
        Vector2[] konumlar = new Vector2[n];
        Vector2[] boyutlar = new Vector2[n];
        Vector3[] koseler = new Vector3[4];

        for (int i = 0; i < n; i++)
        {
            RectTransform p = parcaIsaretcileri[i];
            if (p == null)
            {
                Debug.LogError("[YapbozKalibratoru] " + i + ". işaretçi boş, kaydetmiyorum!");
                return;
            }

            // Parçanın 4 köşesini DÜNYA koordinatında al (scale/rotation/parent ne olursa olsun doğru sonuç verir)
            p.GetWorldCorners(koseler); // [0]=sol-alt, [1]=sol-üst, [2]=sağ-üst, [3]=sağ-alt

            // Dünya köşelerini, OYUN İÇİNDE parçaların gerçekten oluşacağı alanın (referansAlan) YEREL uzayına çevir
            Vector2 solAlt = referansAlan.InverseTransformPoint(koseler[0]);
            Vector2 sagUst = referansAlan.InverseTransformPoint(koseler[2]);

            boyutlar[i] = new Vector2(Mathf.Abs(sagUst.x - solAlt.x), Mathf.Abs(sagUst.y - solAlt.y));
            konumlar[i] = (solAlt + sagUst) / 2f;
        }

        var hayvan = hedefYonetici.hayvanSetleri[hedefHayvanIndex];

        if (zorMod)
        {
            hayvan.konumlar4x3 = konumlar;
            hayvan.boyutlar4x3 = boyutlar;
        }
        else
        {
            hayvan.konumlar3x2 = konumlar;
            hayvan.boyutlar3x2 = boyutlar;
        }

        EditorUtility.SetDirty(hedefYonetici);
        Debug.Log("[YapbozKalibratoru] " + (zorMod ? "4x3" : "3x2") + " için " + n +
            " konum, hayvan[" + hedefHayvanIndex + "] (" + hayvan.hayvanAdi + ") içine kaydedildi. " +
            "ŞİMDİ SAHNEYİ KAYDET (Ctrl+S) yoksa kaybolur!");
    }
#endif
}