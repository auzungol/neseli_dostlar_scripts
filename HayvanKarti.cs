using UnityEngine;
using UnityEngine.EventSystems;

// Her bir hayvan kartına (İnek, Kuş, Maymun...) eklenecek.
// Tıklamayı (kendine odaklan) ve sürüklemeyi (carousel'e ilet) yönetir.
public class HayvanKarti : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("Bu kartın YapbozYoneticisi.hayvanSetleri dizisindeki index'i (0=İlk hayvan, 1=İkinci...)")]
    public int index;

    private HayvanCarouselKontrolcusu carousel;

    void Awake()
    {
        carousel = GetComponentInParent<HayvanCarouselKontrolcusu>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (carousel != null)
            carousel.OdaklanHayvana(index);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (carousel != null)
            carousel.SuruklemeyeBasla(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (carousel != null)
            carousel.SuruklemeyiIlerlet(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (carousel != null)
            carousel.SuruklemeyiBitir(eventData);
    }
}
