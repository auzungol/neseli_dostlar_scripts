using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// OyunSecimGrubu altındaki ModCarouselAlani objesine eklenecek.
// HayvanCarouselKontrolcusu (Yapboz'daki hayvan seçim carousel'i) ile BİREBİR AYNI mimari -
// tek fark: "SEÇ" butonuna basılınca tek bir yöneticiye değil, aktif index'e göre FARKLI bir
// UnityEvent'e gidiyor (her mod kendi oyun yöneticisinin "...ModunaGirildi" metoduna bağlanır).
public class ModCarouselKontrolcusu : MonoBehaviour
{
    [Header("Mod Kartları")]
    [Tooltip("4 mod ikonu (Hafıza, Yapboz, Eşleştirme, Bilmece) - sırası aşağıdaki " +
             "'Secildi Aksiyonlari' dizisiyle AYNI olmalı.")]
    public RectTransform[] modKartlari;

    [Tooltip("Her index için, 'BAŞLA' butonuna basılınca çalışacak aksiyon. Örn: index 0 -> " +
             "HafizaOyunuYoneticisi.HafizaModunaGirildi, index 1 -> YapbozYoneticisi.YapbozModunaGirildi...")]
    public UnityEvent[] secildiAksiyonlari;

    public Button solOkButonu;
    public Button sagOkButonu;
    public Button baslaButonu;

    [Header("Görünüm Ayarları")]
    public float kartAraligi = 260f;
    public float kartYKonumu = 0f;
    public float merkezOlcek = 1f;
    public float kenarOlcek = 0.7f;
    public float uzakOlcek = 0.5f;
    public float merkezAlfa = 1f;
    public float kenarAlfa = 0.55f;
    public float uzakAlfa = 0.15f;
    public float animasyonSuresi = 0.5f;
    public float ekKartBasinaSure = 0.12f;

    [Header("Sürükleme (Swipe) Ayarları")]
    public float hizProjeksiyonSuresi = 0.05f;
    [Range(0f, 1f)] public float hizYumusatma = 0.5f;

    private int aktifIndex = 0;
    private Coroutine aktifAnimasyon;

    // YENİ: ModKarti.cs bunu okuyup, sadece merkezdeki (aktif) karta tıklamaya izin veriyor -
    // kenardaki/uzaktaki saydam kartlar artık tıklanamıyor.
    public int AktifIndex => aktifIndex;

    private bool suruklemeDevamEdiyor = false;
    private float surukleBaslangicX;
    private float surukleAninOffseti = 0f;
    private float suruklemeHizi = 0f;

    void OnEnable()
    {
        aktifIndex = 0;
        GorseliGuncelle(anlik: true);
    }

    public void SagaKaydir()
    {
        if (suruklemeDevamEdiyor) return;
        if (aktifIndex >= modKartlari.Length - 1) return;
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

    // Kenardaki bir mod ikonuna tıklanınca ModKarti.cs bunu çağırır
    public void OdaklanModa(int index)
    {
        if (index < 0 || index >= modKartlari.Length) return;
        int mesafe = Mathf.Abs(index - aktifIndex);
        aktifIndex = index;
        GorseliGuncelle(mesafe: mesafe);
    }

    // "BAŞLA" butonuna bağlanacak - aktif index'in kendi UnityEvent'ini tetikler
    public void BaslaButonunaBasildi()
    {
        if (secildiAksiyonlari == null || aktifIndex < 0 || aktifIndex >= secildiAksiyonlari.Length) return;
        secildiAksiyonlari[aktifIndex]?.Invoke();
    }

    void GorseliGuncelle(bool anlik = false, int mesafe = 1)
    {
        if (aktifAnimasyon != null) StopCoroutine(aktifAnimasyon);

        if (solOkButonu != null) solOkButonu.interactable = aktifIndex > 0;
        if (sagOkButonu != null) sagOkButonu.interactable = aktifIndex < modKartlari.Length - 1;

        if (anlik)
        {
            for (int i = 0; i < modKartlari.Length; i++)
                KartiAnindaYerlestir(i);
            ButonlariOneAl();
            aktifAnimasyon = null;
        }
        else
        {
            float sure = animasyonSuresi + Mathf.Max(0, mesafe - 1) * ekKartBasinaSure;
            aktifAnimasyon = StartCoroutine(GorseliAnimasyonluGuncelle(sure));
        }
    }

    void KartiAnindaYerlestir(int i)
    {
        RectTransform kart = modKartlari[i];
        int mesafe = Mathf.Abs(i - aktifIndex);

        kart.anchoredPosition = new Vector2((i - aktifIndex) * kartAraligi, kartYKonumu);
        kart.localScale = Vector3.one * OlcekGetirSurekli(mesafe);
        AlfaAyarla(kart, AlfaGetirSurekli(mesafe));

        kart.SetAsLastSibling();
        if (mesafe > 0) kart.SetSiblingIndex(modKartlari.Length - mesafe);
    }

    float OlcekGetirSurekli(float mesafe)
    {
        mesafe = Mathf.Abs(mesafe);
        if (mesafe <= 1f) return Mathf.Lerp(merkezOlcek, kenarOlcek, mesafe);
        float ikinciSegment = Mathf.Clamp01(mesafe - 1f);
        return Mathf.Lerp(kenarOlcek, uzakOlcek, ikinciSegment);
    }

    float AlfaGetirSurekli(float mesafe)
    {
        mesafe = Mathf.Abs(mesafe);
        if (mesafe <= 1f) return Mathf.Lerp(merkezAlfa, kenarAlfa, mesafe);
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

    // YENİ - KRİTİK FIX: SolOk/SagOk/Basla butonları ModCarouselAlani'nin İÇİNDE, kartlarla
    // AYNI seviyede duruyor. Merkezdeki kart her güncellemede SetAsLastSibling() ile en öne
    // itiliyor - bu, o kartın SolOk/SagOk/Basla'nın da ÖNÜNE geçmesine, onları görsel ve
    // tıklama olarak KAPATMASINA yol açıyordu ("BAŞLA hiçbir zaman çalışmıyor" bugu buydu).
    // Çözüm: her kart güncellemesinden SONRA butonları TEKRAR en öne zorluyoruz.
    void ButonlariOneAl()
    {
        if (solOkButonu != null) solOkButonu.transform.SetAsLastSibling();
        if (sagOkButonu != null) sagOkButonu.transform.SetAsLastSibling();
        if (baslaButonu != null) baslaButonu.transform.SetAsLastSibling();
    }

    IEnumerator GorseliAnimasyonluGuncelle(float sure)
    {
        Vector2[] baslangicPos = new Vector2[modKartlari.Length];
        float[] baslangicOlcek = new float[modKartlari.Length];
        float[] baslangicAlfa = new float[modKartlari.Length];

        Vector2[] hedefPos = new Vector2[modKartlari.Length];
        float[] hedefOlcek = new float[modKartlari.Length];
        float[] hedefAlfa = new float[modKartlari.Length];

        for (int i = 0; i < modKartlari.Length; i++)
        {
            RectTransform kart = modKartlari[i];
            baslangicPos[i] = kart.anchoredPosition;
            baslangicOlcek[i] = kart.localScale.x;
            baslangicAlfa[i] = AlfaOku(kart);

            int mesafe = Mathf.Abs(i - aktifIndex);
            hedefPos[i] = new Vector2((i - aktifIndex) * kartAraligi, kartYKonumu);
            hedefOlcek[i] = OlcekGetirSurekli(mesafe);
            hedefAlfa[i] = AlfaGetirSurekli(mesafe);

            kart.SetAsLastSibling();
            if (mesafe > 0) kart.SetSiblingIndex(modKartlari.Length - mesafe);
        }

        float gecenZaman = 0f;
        while (gecenZaman < sure)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / sure;
            t = t * t * (3f - 2f * t);

            for (int i = 0; i < modKartlari.Length; i++)
            {
                RectTransform kart = modKartlari[i];
                kart.anchoredPosition = Vector2.Lerp(baslangicPos[i], hedefPos[i], t);
                kart.localScale = Vector3.one * Mathf.Lerp(baslangicOlcek[i], hedefOlcek[i], t);
                AlfaAyarla(kart, Mathf.Lerp(baslangicAlfa[i], hedefAlfa[i], t));
            }
            yield return null;
        }

        for (int i = 0; i < modKartlari.Length; i++)
            KartiAnindaYerlestir(i);
        ButonlariOneAl();

        aktifAnimasyon = null;
    }

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

        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        float aninHiz = eventData.delta.x / dt;
        suruklemeHizi = Mathf.Lerp(aninHiz, suruklemeHizi, hizYumusatma);

        surukleAninOffseti = eventData.position.x - surukleBaslangicX;

        int enYakinIndex = 0;
        float enKucukMesafe = float.MaxValue;

        for (int i = 0; i < modKartlari.Length; i++)
        {
            RectTransform kart = modKartlari[i];
            float temelX = (i - aktifIndex) * kartAraligi;
            float canliX = temelX + surukleAninOffseti;
            kart.anchoredPosition = new Vector2(canliX, kartYKonumu);

            float surekliMesafe = kartAraligi > 0f ? Mathf.Abs(canliX) / kartAraligi : 0f;
            kart.localScale = Vector3.one * OlcekGetirSurekli(surekliMesafe);
            AlfaAyarla(kart, AlfaGetirSurekli(surekliMesafe));

            if (surekliMesafe < enKucukMesafe)
            {
                enKucukMesafe = surekliMesafe;
                enYakinIndex = i;
            }
        }

        modKartlari[enYakinIndex].SetAsLastSibling();
        ButonlariOneAl();
    }

    public void SuruklemeyiBitir(PointerEventData eventData)
    {
        suruklemeDevamEdiyor = false;

        float projekteEdilenOffset = surukleAninOffseti + suruklemeHizi * hizProjeksiyonSuresi;
        int kaymaMiktari = Mathf.RoundToInt(-projekteEdilenOffset / kartAraligi);
        int yeniIndex = Mathf.Clamp(aktifIndex + kaymaMiktari, 0, modKartlari.Length - 1);
        int gercekMesafe = Mathf.Abs(yeniIndex - aktifIndex);

        if (yeniIndex == aktifIndex)
        {
            GorseliGuncelle(mesafe: 1);
            return;
        }

        aktifIndex = yeniIndex;
        GorseliGuncelle(mesafe: gercekMesafe);
    }
}