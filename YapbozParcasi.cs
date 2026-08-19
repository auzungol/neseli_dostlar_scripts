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

    public void ParcayiKur(Sprite gorsel, Vector2 hedefKonum, YapbozYoneticisi oyunYoneticisi, int havuzSlotIndex)
    {
        dogruKonum = hedefKonum;
        yonetici = oyunYoneticisi;
        havuzSlotu = havuzSlotIndex;
        havuzdanCiktiBildirildi = false;
        yerinePlacedMi = false;

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
    // böylece oyuncu yeni bir parça geldiğini hemen fark ediyor.
    System.Collections.IEnumerator SpawnAnimasyonuOynat()
    {
        Vector3 hedefOlcek = transform.localScale;
        transform.localScale = Vector3.zero;

        float buyumeSuresi = 0.18f;
        float sekmeSuresi = 0.10f;
        float gecenZaman = 0f;

        // 1. Faz: 0 -> %115 (fırlama/baloncuk hissi)
        while (gecenZaman < buyumeSuresi)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / buyumeSuresi;
            transform.localScale = hedefOlcek * Mathf.Lerp(0f, 1.15f, t);
            yield return null;
        }

        // 2. Faz: %115 -> %100 (yerine yumuşakça oturma)
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
        transform.localScale = Vector3.one * 1.08f;
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

        transform.localScale = Vector3.one;

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