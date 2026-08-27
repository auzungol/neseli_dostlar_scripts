using UnityEngine;
using UnityEngine.EventSystems;

// Her mod ikonuna (Hafıza, Yapboz, Eşleştirme, Bilmece) eklenecek.
// HayvanKarti.cs (Yapboz'daki hayvan seçim carousel'i) ile birebir aynı mantık.
public class ModKarti : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Tooltip("Bu kartın ModCarouselKontrolcusu.modKartlari VE secildiAksiyonlari dizilerindeki index'i")]
    public int index;

    private ModCarouselKontrolcusu carousel;

    void Awake()
    {
        carousel = GetComponentInParent<ModCarouselKontrolcusu>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (carousel == null) return;

        // YENİ: Sadece merkezdeki (aktif) kart tıklanabilir - kenardaki/uzaktaki saydam
        // kartlara tıklamak artık hiçbir şey yapmıyor.
        if (index != carousel.AktifIndex) return;

        carousel.OdaklanModa(index);
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