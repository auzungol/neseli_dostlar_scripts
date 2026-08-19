using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Kart : MonoBehaviour
{
    [Header("Kart Bileşenleri")]
    public Image onYuzGorseli;     
    public Button kartButonu;      
    
    [HideInInspector] public int kartID; 
    [HideInInspector] public bool acikMi = false;

    private HafizaOyunuYoneticisi yonetici;
    private bool donuyorMu = false; // Animasyon sırasında çift tıklamayı engellemek için

    public void KartKur(int id, Sprite hayvanSprite, HafizaOyunuYoneticisi oyunYoneticisi)
    {
        kartID = id;
        onYuzGorseli.sprite = hayvanSprite;
        yonetici = oyunYoneticisi;
        
        // Oyun başlarken anında kapalı ve tam boyutta başla
        acikMi = false;
        onYuzGorseli.gameObject.SetActive(false);
        kartButonu.interactable = true;
        transform.localScale = Vector3.one;
        GetComponent<Image>().enabled = true;
    }

    public void KartaTiklandi()
    {
        // Eğer kart zaten açıksa, dönme animasyonu sürüyorsa veya yönetici engelliyorsa basılamaz!
        if (acikMi || donuyorMu || !yonetici.TiklamaMuzunmu()) return;

        KartiAc();
        yonetici.KartSecildi(this);
    }

    public void KartiAc()
    {
        if (!acikMi)
        {
            acikMi = true;
            kartButonu.interactable = false;
            StartCoroutine(KartDonmeAnimasyonu(true)); // Açılma animasyonunu başlat!
        }
    }

    public void KartiKapat()
    {
        if (acikMi)
        {
            acikMi = false;
            StartCoroutine(KartDonmeAnimasyonu(false)); // Kapanma animasyonunu başlat!
        }
    }

    // --- YENİ EKLENEN: KART DÖNDÜRME İLLÜZYONU (FLIP) ---
    IEnumerator KartDonmeAnimasyonu(bool aciliyorMu)
    {
        donuyorMu = true;
        float sure = 0.12f; // Dönüşün yarısı (toplam 0.24 saniyede şimşek gibi dönecek!)
        float gecenZaman = 0f;
        Vector3 baslangicOlcegi = transform.localScale;

        // 1. ADIM: Kartı X ekseninde 1'den 0'a küçült (Kart yan döner)
        while (gecenZaman < sure)
        {
            gecenZaman += Time.deltaTime;
            float yeniX = Mathf.Lerp(1f, 0f, gecenZaman / sure);
            transform.localScale = new Vector3(yeniX, baslangicOlcegi.y, baslangicOlcegi.z);
            yield return null;
        }

        // Tam ortada (Kart incecik olup görünmez olduğunda) resmi değiştir!
        onYuzGorseli.gameObject.SetActive(aciliyorMu);

        // 2. ADIM: Kartı X ekseninde 0'dan tekrar 1'e büyüt (Kart yüzünü açar)
        gecenZaman = 0f;
        while (gecenZaman < sure)
        {
            gecenZaman += Time.deltaTime;
            float yeniX = Mathf.Lerp(0f, 1f, gecenZaman / sure);
            transform.localScale = new Vector3(yeniX, baslangicOlcegi.y, baslangicOlcegi.z);
            yield return null;
        }

        transform.localScale = new Vector3(1f, baslangicOlcegi.y, baslangicOlcegi.z);
        
        if (!aciliyorMu)
        {
            kartButonu.interactable = true;
        }
        
        donuyorMu = false;
    }

    // --- YENİ EKLENEN: EŞLEŞEN KARTIN KÜÇÜLEREK KAYBOLMASI ---
    public void KartiYokEt()
    {
        StartCoroutine(KartKaybolmaAnimasyonu());
    }

    IEnumerator KartKaybolmaAnimasyonu()
    {
        kartButonu.interactable = false;
        float sure = 0.2f;
        float gecenZaman = 0f;
        Vector3 baslangicOlcegi = transform.localScale;

        // Kartı yavaşça küçülterek yok et
        while (gecenZaman < sure)
        {
            gecenZaman += Time.deltaTime;
            transform.localScale = Vector3.Lerp(baslangicOlcegi, Vector3.zero, gecenZaman / sure);
            yield return null;
        }

        GetComponent<Image>().enabled = false;
        onYuzGorseli.enabled = false;
    }
}