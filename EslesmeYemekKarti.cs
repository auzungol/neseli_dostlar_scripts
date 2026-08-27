using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EslesmeYemekKarti : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private bool dogruMu;
    private EslesmeYoneticisi yonetici;

    private RectTransform rectTransform;
    private Image gorselBileseni;
    private Canvas rootCanvas;

    private Transform orijinalParent;
    private int orijinalSiblingIndex;
    private bool kilitliMi = false; // Doğru eşleşme sonrası tekrar sürüklenmesin

    public void KartiKur(Sprite gorsel, bool dogruCevapMi, EslesmeYoneticisi oyunYoneticisi)
    {
        dogruMu = dogruCevapMi;
        yonetici = oyunYoneticisi;
        kilitliMi = false;

        rectTransform = GetComponent<RectTransform>();
        gorselBileseni = GetComponent<Image>();
        gorselBileseni.sprite = gorsel;
        gorselBileseni.raycastTarget = true;

        rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (kilitliMi) return;

        orijinalParent = transform.parent;
        orijinalSiblingIndex = transform.GetSiblingIndex();

        // Horizontal Layout Group'un etkisinden çıkması için geçici olarak üst seviyeye taşı
        transform.SetParent(yonetici.oyunAlani, true);
        transform.SetAsLastSibling();
        transform.localScale = Vector3.one * 1.1f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (kilitliMi) return;

        float olcek = (rootCanvas != null) ? rootCanvas.scaleFactor : 1f;
        rectTransform.anchoredPosition += eventData.delta / olcek;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (kilitliMi) return;

        transform.localScale = Vector3.one;

        bool hedefinUzerinde = RectTransformUtility.RectangleContainsScreenPoint(
            yonetici.hedefBolge, eventData.position, eventData.pressEventCamera);

        if (hedefinUzerinde && dogruMu)
        {
            // DOĞRU EŞLEŞME!
            // Kilitliyoruz ama Destroy etmiyoruz - manager DogruEslesme() içinde
            // TemizleYemekKartlari() ile bu kart dahil tüm kalan kartları temizliyor
            // (cümle efekti sırasında başka kart sürüklenemesin diye).
            kilitliMi = true;
            gorselBileseni.raycastTarget = false;
            yonetici.DogruEslesme(gorselBileseni.sprite);
            return;
        }

        if (hedefinUzerinde && !dogruMu)
        {
            yonetici.YanlisEslesme();
        }

        // Yanlışsa ya da hedefe hiç bırakılmadıysa eski yerine (Layout Group içine) geri dön
        transform.SetParent(orijinalParent, true);
        transform.SetSiblingIndex(orijinalSiblingIndex);
        rectTransform.anchoredPosition = Vector2.zero; // Layout Group zaten doğru yere oturtacak
    }
}