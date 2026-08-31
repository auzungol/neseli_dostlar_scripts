using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Bu scripti parça prefabına ekleyeceğiz. Prefab üzerinde Image bileşeni olmalı.
public class YapbozParcasi : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public bool yerinePlacedMi = false; // Doğru yerine oturdu mu?

    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private Image gorselBileseni;
    private Vector2 dogruKonum; // OyunAlani içindeki hedef anchoredPosition (elle yerleştirilmiş yuvadan geliyor)

    private YapbozYoneticisi yonetici;
    private int havuzSlotu;
    private bool havuzdanCiktiBildirildi = false; // Havuzdan ayrıldığını yöneticiye sadece 1 kere bildirsin

    // YENİ: Havuzda beklerken parça, gerçek (kalibre edilmiş) boyutundan bu oranda küçük görünsün -
    // yapbozun üstünü kapatmasın diye. Sürüklemeye başlayınca gerçek boyutuna büyür.
    private float havuzOlcegi = 0.75f;
    private Coroutine aktifOlcekAnimasyonu;

    public void ParcayiKur(Sprite gorsel, Vector2 hedefKonum, YapbozYoneticisi oyunYoneticisi, int havuzSlotIndex, float havuzOlcek = 0.75f)
    {
        dogruKonum = hedefKonum;
        yonetici = oyunYoneticisi;
        havuzSlotu = havuzSlotIndex;
        havuzdanCiktiBildirildi = false;
        yerinePlacedMi = false;
        havuzOlcegi = havuzOlcek;

        rectTransform = GetComponent<RectTransform>();
        gorselBileseni = GetComponent<Image>();
        gorselBileseni.sprite = gorsel;
        gorselBileseni.raycastTarget = true;

        // Tıklama/sürükleme alanı kare değil, parçanın kendi (şeffaf olmayan) şekline uysun diye.
        // NOT: Sprite'ın Import Settings'inde "Read/Write Enabled" AÇIK olmalı.
        gorselBileseni.alphaHitTestMinimumThreshold = 0.1f;

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;

        StartCoroutine(SpawnAnimasyonuOynat());
    }

    // Yeni parça havuza gelince küçükten büyüyerek, hafif bir "zıplama" ile beliriyor -
    // böylece oyuncu yeni bir parça geldiğini hemen fark ediyor. Artık %100'e değil,
    // havuzOlcegi'ne (örn. %75) yerleşiyor - havuzdayken hep küçük görünsün diye.
    System.Collections.IEnumerator SpawnAnimasyonuOynat()
    {
        Vector3 hedefOlcek = Vector3.one * havuzOlcegi;
        transform.localScale = Vector3.zero;

        float buyumeSuresi = 0.18f;
        float sekmeSuresi = 0.10f;
        float gecenZaman = 0f;

        // 1. Faz: 0 -> %115 (fırlama/baloncuk hissi) - havuzOlcegi'nin %115'i
        while (gecenZaman < buyumeSuresi)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / buyumeSuresi;
            transform.localScale = hedefOlcek * Mathf.Lerp(0f, 1.15f, t);
            yield return null;
        }

        // 2. Faz: %115 -> %100 (yerine yumuşakça oturma) - havuzOlcegi'nde sabitlenir
        gecenZaman = 0f;
        while (gecenZaman < sekmeSuresi)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / sekmeSuresi;
            transform.localScale = hedefOlcek * Mathf.Lerp(1.15f, 1f, t);
            yield return null;
        }

        transform.localScale = hedefOlcek;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (yerinePlacedMi) return;

        transform.SetAsLastSibling();

        // YENİ: "Parça tutma" sesi artık TAM BURADA - parçaya gerçekten dokunup
        // sürüklemeye başlanınca. Önceden spawn anında çalıyordu, oyuncu dokunmadan.
        yonetici.ParcaTutuldu();

        // YENİ: Havuzdaki küçük görünümden (havuzOlcegi), sürüklenirken GERÇEK boyutuna
        // (1.08x hafif "elde tutuluyor" vurgusuyla) yumuşakça büyüsün diye.
        if (aktifOlcekAnimasyonu != null) StopCoroutine(aktifOlcekAnimasyonu);
        aktifOlcekAnimasyonu = StartCoroutine(OlcekAnimasyonuOynat(Vector3.one * 1.08f, 0.12f));
    }

    System.Collections.IEnumerator OlcekAnimasyonuOynat(Vector3 hedefOlcek, float sure)
    {
        Vector3 baslangicOlcek = transform.localScale;
        float gecenZaman = 0f;

        while (gecenZaman < sure)
        {
            gecenZaman += Time.deltaTime;
            float t = Mathf.Clamp01(gecenZaman / sure);
            t = t * t * (3f - 2f * t); // smoothstep - yumuşak geçiş
            transform.localScale = Vector3.Lerp(baslangicOlcek, hedefOlcek, t);
            yield return null;
        }

        transform.localScale = hedefOlcek;
        aktifOlcekAnimasyonu = null;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (yerinePlacedMi) return;

        float olcek = (rootCanvas != null) ? rootCanvas.scaleFactor : 1f;
        rectTransform.anchoredPosition += eventData.delta / olcek;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (yerinePlacedMi) return;

        // YENİ: Artık anlık Vector3.one değil - GERÇEK boyuta (1x) yumuşakça iniyor.
        // Bırakıldığında ister doğru yere otursun ister havada kalsın, parça artık
        // "havuzdaki küçük hali" değil, gerçek boyutunda görünmeye devam ediyor.
        if (aktifOlcekAnimasyonu != null) StopCoroutine(aktifOlcekAnimasyonu);
        aktifOlcekAnimasyonu = StartCoroutine(OlcekAnimasyonuOynat(Vector3.one, 0.12f));

        float mesafe = Vector2.Distance(rectTransform.anchoredPosition, dogruKonum);

        if (mesafe <= yonetici.hizalamaToleransi)
        {
            rectTransform.anchoredPosition = dogruKonum;
            yerinePlacedMi = true;
            gorselBileseni.raycastTarget = false;

            yonetici.ParcaYerlestirildi(this);
        }
    }
}