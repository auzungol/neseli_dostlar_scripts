using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// HayvanSecimPaneli altındaki CarouselAlani objesine eklenecek.
// 8 hayvan kartının pozisyon/ölçek/alfasını yönetir; ok butonları, sürükleme (swipe)
// ve kenardaki bir karta tıklamayla gezinmeyi destekler.
public class HayvanCarouselKontrolcusu : MonoBehaviour
{
    [Header("Bağlantılar")]
    public YapbozYoneticisi yapbozYoneticisi;

    [Tooltip("8 hayvan kartı, YapbozYoneticisi.hayvanSetleri dizisiyle AYNI SIRADA olmalı! " +
             "(index 0 = hayvanSetleri[0], index 1 = hayvanSetleri[1] ...)")]
    public RectTransform[] hayvanKartlari;

    public Button solOkButonu;
    public Button sagOkButonu;
    public Button secButonu;

    [Header("Görünüm Ayarları")]
    [Tooltip("Kartlar arası yatay mesafe (piksel)")]
    public float kartAraligi = 260f;
    [Tooltip("Tüm kartların ortak dikey konumu (piksel) - eski grid düzeninden kalma farklı Y " +
             "değerlerini ezip hepsini AYNI hizaya getirir. 0 = CarouselAlani'nin dikey merkezi.")]
    public float kartYKonumu = 0f;
    public float merkezOlcek = 1f;
    public float kenarOlcek = 0.7f;
    public float uzakOlcek = 0.5f;
    public float merkezAlfa = 1f;
    public float kenarAlfa = 0.55f;
    public float uzakAlfa = 0.15f;
    public float animasyonSuresi = 0.25f;
    [Tooltip("Flick/sürükleme sonucu BİRDEN FAZLA kart atlanınca, HER EK kart için animasyon " +
             "süresine eklenecek ek süre (saniye). Böylece 3 kart atlamak 1 kart atlamaktan " +
             "orantılı daha uzun sürer - aradaki kartlar da 'geçilirken' görünür, ışınlanmaz. " +
             "0 = tüm atlamalar sabit sürede biter (eski davranış).")]
    public float ekKartBasinaSure = 0.12f;

    [Header("Sürükleme (Swipe) Ayarları")]
    [Tooltip("Bırakma anındaki hızın, kaç kart daha atlanacağını tahmin etmek için ne kadar " +
             "'ileri projekte edileceği' (saniye). Yüksek değer = hızlı 'flick' hareketlerinde " +
             "daha fazla kart atlar.")]
    public float hizProjeksiyonSuresi = 0.12f;
    [Tooltip("Anlık hız hesaplamasının ne kadar yumuşatılacağı (0 = hiç yumuşatma, 1 = çok yumuşak/geç tepki)")]
    [Range(0f, 1f)] public float hizYumusatma = 0.5f;

    private int aktifIndex = 0;
    private Coroutine aktifAnimasyon;

    private bool suruklemeDevamEdiyor = false;
    private float surukleBaslangicX;
    private float surukleAninOffseti = 0f;
    private float suruklemeHizi = 0f; // piksel/saniye, yumuşatılmış (EMA)

    void OnEnable()
    {
        // Hayvan Seçim ekranına her girişte baştan (İnek'ten) başlasın
        aktifIndex = 0;
        GorseliGuncelle(anlik: true);
    }

    // --- OK BUTONLARI ---
    public void SagaKaydir()
    {
        if (suruklemeDevamEdiyor) return;
        if (aktifIndex >= hayvanKartlari.Length - 1) return;
        aktifIndex++;
        GorseliGuncelle(mesafe: 1);
    }

    public void SolaKaydir()
    {
        if (suruklemeDevamEdiyor) return;
        if (aktifIndex <= 0) return;
        aktifIndex--;
        GorseliGuncelle(mesafe: 1);
    }

    // Kenardaki bir hayvana tıklanınca HayvanKarti.cs bunu çağırır - o hayvan ortaya kayar
    public void OdaklanHayvana(int index)
    {
        if (index < 0 || index >= hayvanKartlari.Length) return;
        int mesafe = Mathf.Abs(index - aktifIndex);
        aktifIndex = index;
        GorseliGuncelle(mesafe: mesafe);
    }

    // "SEÇ" butonuna bağlanacak - şu an ortada olan hayvanla oyunu başlatır
    public void SecButonunaBasildi()
    {
        if (yapbozYoneticisi != null)
            yapbozYoneticisi.HayvanSecildi(aktifIndex);
    }

    void GorseliGuncelle(bool anlik = false, int mesafe = 1)
    {
        if (aktifAnimasyon != null) StopCoroutine(aktifAnimasyon);

        if (solOkButonu != null) solOkButonu.interactable = aktifIndex > 0;
        if (sagOkButonu != null) sagOkButonu.interactable = aktifIndex < hayvanKartlari.Length - 1;

        if (anlik)
        {
            for (int i = 0; i < hayvanKartlari.Length; i++)
                KartiAnindaYerlestir(i);
            aktifAnimasyon = null;
        }
        else
        {
            // Atlanan kart sayısına göre süreyi orantılı uzat - 1 kart animasyonSuresi'nde,
            // her ek kart ekKartBasinaSure kadar daha uzun sürsün.
            float sure = animasyonSuresi + Mathf.Max(0, mesafe - 1) * ekKartBasinaSure;
            aktifAnimasyon = StartCoroutine(GorseliAnimasyonluGuncelle(sure));
        }
    }

    void KartiAnindaYerlestir(int i)
    {
        RectTransform kart = hayvanKartlari[i];
        int mesafe = Mathf.Abs(i - aktifIndex);

        kart.anchoredPosition = new Vector2((i - aktifIndex) * kartAraligi, kartYKonumu);
        kart.localScale = Vector3.one * OlcekGetirSurekli(mesafe);
        AlfaAyarla(kart, AlfaGetirSurekli(mesafe));

        // Merkezdeki kart her zaman en önde (diğerlerinin üstünde) görünsün
        kart.SetAsLastSibling();
        if (mesafe > 0) kart.SetSiblingIndex(hayvanKartlari.Length - mesafe);
    }

    // --- SÜREKLİ (kesikli olmayan) ölçek/alfa hesabı ---
    // mesafe = 0 -> merkez, 1 -> ilk komşu, 2+ -> uzak. Ara değerler (0.3, 1.7 gibi)
    // de düzgün enterpole edilir - sürükleme sırasında akıcı büyüme/küçülme için şart.
    float OlcekGetirSurekli(float mesafe)
    {
        mesafe = Mathf.Abs(mesafe);
        if (mesafe <= 1f)
            return Mathf.Lerp(merkezOlcek, kenarOlcek, mesafe);
        float ikinciSegment = Mathf.Clamp01(mesafe - 1f);
        return Mathf.Lerp(kenarOlcek, uzakOlcek, ikinciSegment);
    }

    float AlfaGetirSurekli(float mesafe)
    {
        mesafe = Mathf.Abs(mesafe);
        if (mesafe <= 1f)
            return Mathf.Lerp(merkezAlfa, kenarAlfa, mesafe);
        float ikinciSegment = Mathf.Clamp01(mesafe - 1f);
        return Mathf.Lerp(kenarAlfa, uzakAlfa, ikinciSegment);
    }

    void AlfaAyarla(RectTransform kart, float alfa)
    {
        Image img = kart.GetComponent<Image>();
        if (img != null)
        {
            Color renk = img.color;
            renk.a = alfa;
            img.color = renk;
        }
    }

    float AlfaOku(RectTransform kart)
    {
        Image img = kart.GetComponent<Image>();
        return img != null ? img.color.a : 1f;
    }

    IEnumerator GorseliAnimasyonluGuncelle(float sure)
    {
        Vector2[] baslangicPos = new Vector2[hayvanKartlari.Length];
        float[] baslangicOlcek = new float[hayvanKartlari.Length];
        float[] baslangicAlfa = new float[hayvanKartlari.Length];

        Vector2[] hedefPos = new Vector2[hayvanKartlari.Length];
        float[] hedefOlcek = new float[hayvanKartlari.Length];
        float[] hedefAlfa = new float[hayvanKartlari.Length];

        for (int i = 0; i < hayvanKartlari.Length; i++)
        {
            RectTransform kart = hayvanKartlari[i];
            baslangicPos[i] = kart.anchoredPosition;
            baslangicOlcek[i] = kart.localScale.x;
            baslangicAlfa[i] = AlfaOku(kart);

            int mesafe = Mathf.Abs(i - aktifIndex);
            hedefPos[i] = new Vector2((i - aktifIndex) * kartAraligi, kartYKonumu);
            hedefOlcek[i] = OlcekGetirSurekli(mesafe);
            hedefAlfa[i] = AlfaGetirSurekli(mesafe);

            kart.SetAsLastSibling();
            if (mesafe > 0) kart.SetSiblingIndex(hayvanKartlari.Length - mesafe);
        }

        float gecenZaman = 0f;
        while (gecenZaman < sure)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / sure;
            t = t * t * (3f - 2f * t); // smoothstep - yumuşak geçiş

            for (int i = 0; i < hayvanKartlari.Length; i++)
            {
                RectTransform kart = hayvanKartlari[i];
                kart.anchoredPosition = Vector2.Lerp(baslangicPos[i], hedefPos[i], t);
                kart.localScale = Vector3.one * Mathf.Lerp(baslangicOlcek[i], hedefOlcek[i], t);
                AlfaAyarla(kart, Mathf.Lerp(baslangicAlfa[i], hedefAlfa[i], t));
            }
            yield return null;
        }

        for (int i = 0; i < hayvanKartlari.Length; i++)
            KartiAnindaYerlestir(i);

        aktifAnimasyon = null;
    }

    // --- SÜRÜKLEME (SWIPE) - HayvanKarti.cs bu metodları çağırarak yönlendirir ---
    public void SuruklemeyeBasla(PointerEventData eventData)
    {
        suruklemeDevamEdiyor = true;
        surukleBaslangicX = eventData.position.x;
        surukleAninOffseti = 0f;
        suruklemeHizi = 0f;
        if (aktifAnimasyon != null) { StopCoroutine(aktifAnimasyon); aktifAnimasyon = null; }
    }

    public void SuruklemeyiIlerlet(PointerEventData eventData)
    {
        if (!suruklemeDevamEdiyor) return;

        // Anlık hızı ölç (piksel/saniye) ve yumuşat - bırakma anında "flick" tespiti için
        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        float aninHiz = eventData.delta.x / dt;
        suruklemeHizi = Mathf.Lerp(aninHiz, suruklemeHizi, hizYumusatma);

        surukleAninOffseti = eventData.position.x - surukleBaslangicX;

        int enYakinIndex = 0;
        float enKucukMesafe = float.MaxValue;

        for (int i = 0; i < hayvanKartlari.Length; i++)
        {
            RectTransform kart = hayvanKartlari[i];
            float temelX = (i - aktifIndex) * kartAraligi;
            float canliX = temelX + surukleAninOffseti;
            kart.anchoredPosition = new Vector2(canliX, kartYKonumu);

            // YENİ: sürekli mesafeye göre canlı ölçek/alfa - sürüklerken kart akıcı şekilde
            // büyüyüp küçülsün diye (önceden sadece pozisyon güncelleniyordu).
            float surekliMesafe = kartAraligi > 0f ? Mathf.Abs(canliX) / kartAraligi : 0f;
            kart.localScale = Vector3.one * OlcekGetirSurekli(surekliMesafe);
            AlfaAyarla(kart, AlfaGetirSurekli(surekliMesafe));

            if (surekliMesafe < enKucukMesafe)
            {
                enKucukMesafe = surekliMesafe;
                enYakinIndex = i;
            }
        }

        // Merkeze en yakın kart sürükleme sırasında da en önde görünsün
        hayvanKartlari[enYakinIndex].SetAsLastSibling();
    }

    public void SuruklemeyiBitir(PointerEventData eventData)
    {
        suruklemeDevamEdiyor = false;

        // YENİ: Sadece bırakma anındaki offset'e değil, hıza da bakarak kaç kart
        // atlanacağını hesapla - hızlı "flick" hareketlerinde offset küçük olsa bile
        // birden fazla kart atlanabilir.
        float projekteEdilenOffset = surukleAninOffseti + suruklemeHizi * hizProjeksiyonSuresi;

        int kaymaMiktari = Mathf.RoundToInt(-projekteEdilenOffset / kartAraligi);

        int yeniIndex = Mathf.Clamp(aktifIndex + kaymaMiktari, 0, hayvanKartlari.Length - 1);
        int gercekMesafe = Mathf.Abs(yeniIndex - aktifIndex);

        if (yeniIndex == aktifIndex)
        {
            // Hiç kart değişmedi (küçük/yavaş bir hareketti) - mevcut karta geri "spring" et
            GorseliGuncelle(mesafe: 1);
            return;
        }

        aktifIndex = yeniIndex;
        GorseliGuncelle(mesafe: gercekMesafe);
    }
}