using System;
using System.Collections;
using UnityEngine;
using TMPro;

// Sahnede HER ZAMAN AKTİF, tek bir obje üzerinde durmalı (PauseController/GecisYoneticisi
// ile aynı mantık). "3-2-1-BAŞLA" geri sayımını gösterir, her sayı küçük bir "zıplama"
// animasyonuyla belirir. Geri sayım sırasında TAM EKRAN görünmez bir katman tüm tıklamaları
// engeller - modların kendi içinde ayrı ayrı "tıklanamasın" mantığı yazmasına gerek kalmaz.
public class GeriSayimYoneticisi : MonoBehaviour
{
    public static GeriSayimYoneticisi Instance;

    [Header("Bağlantılar")]
    [Tooltip("Sayıyı gösteren TMP Text - '3','2','1','BAŞLA!' sırayla buraya yazılacak.")]
    public TextMeshProUGUI sayiYazisi;
    [Tooltip("YENİ: Geri sayım sırasında sayının ALTINDA gösterilen, moda özel kısa talimat " +
             "yazısı (örn. 'Kartlara tıklayarak eşleşen çiftleri bulun'). Boş bırakılırsa ya " +
             "da GeriSayimBaslat()'a talimat verilmezse gizli kalır, hiçbir şeyi bozmaz.")]
    public TextMeshProUGUI talimatYazisi;
    [Tooltip("Tüm ekranı kaplayan, tıklamaları engelleyen katman (Image, Raycast Target AÇIK). " +
             "Rengi tamamen şeffaf olabilir ya da hafif karartma yapabilirsiniz, tercihiniz.")]
    public GameObject engelleyiciKatman;

    [Header("Zamanlama")]
    public float sayiBasinaSure = 0.8f;
    [Tooltip("'BAŞLA!' yazısının ekstra ne kadar uzun süre görüneceği (saniye)")]
    public float baslaGosterimSuresi = 0.6f;

    [Header("Zıplama Animasyonu")]
    public float zonklamaBuyumeSuresi = 0.15f;
    public float zonklamaOlcek = 1.3f;

    void Awake()
    {
        Instance = this;
        if (sayiYazisi != null) sayiYazisi.gameObject.SetActive(false);
        if (talimatYazisi != null) talimatYazisi.gameObject.SetActive(false);
        if (engelleyiciKatman != null) engelleyiciKatman.SetActive(false);
    }

    // Dışarıdan çağrılacak ANA metod. bittiginde = geri sayım tamamlanınca çalışacak kod
    // (süre sayacını başlatma, tıklanabilirliği açma vs.)
    // YENİ: talimatMetni opsiyonel - her mod kendi kısa "nasıl oynanır" cümlesini geçebilir.
    // Verilmezse (null/boş) talimat yazısı hiç gösterilmez, eski davranış aynen korunur.
    public void GeriSayimBaslat(Action bittiginde, string talimatMetni = null)
    {
        StartCoroutine(GeriSayimCoroutine(bittiginde, talimatMetni));
    }

    IEnumerator GeriSayimCoroutine(Action bittiginde, string talimatMetni)
    {
        if (engelleyiciKatman != null) engelleyiciKatman.SetActive(true);
        if (sayiYazisi != null) sayiYazisi.gameObject.SetActive(true);

        if (talimatYazisi != null && !string.IsNullOrEmpty(talimatMetni))
        {
            talimatYazisi.text = talimatMetni;
            talimatYazisi.gameObject.SetActive(true);
        }

        string[] sayilar = { "3", "2", "1" };
        foreach (string sayi in sayilar)
            yield return SayiyiGosterVeZonkla(sayi, sayiBasinaSure);

        string baslaMetni = MenuYoneticisi.turkceMi ? "BAŞLA!" : "START!";
        yield return SayiyiGosterVeZonkla(baslaMetni, baslaGosterimSuresi);

        if (sayiYazisi != null) sayiYazisi.gameObject.SetActive(false);
        if (talimatYazisi != null) talimatYazisi.gameObject.SetActive(false);
        if (engelleyiciKatman != null) engelleyiciKatman.SetActive(false);

        bittiginde?.Invoke();
    }

    IEnumerator SayiyiGosterVeZonkla(string metin, float gosterimSuresi)
    {
        if (sayiYazisi != null) sayiYazisi.text = metin;

        // Zonklama: 0 -> zonklamaOlcek -> 1 (fırlama/baloncuk hissi, YapbozParcasi'nin
        // spawn animasyonuyla AYNI mantık)
        float buyumeSuresi = zonklamaBuyumeSuresi;
        float kucultmeSuresi = zonklamaBuyumeSuresi * 0.6f;

        float gecenZaman = 0f;
        while (gecenZaman < buyumeSuresi)
        {
            gecenZaman += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(gecenZaman / buyumeSuresi);
            if (sayiYazisi != null)
                sayiYazisi.transform.localScale = Vector3.one * Mathf.Lerp(0f, zonklamaOlcek, t);
            yield return null;
        }

        gecenZaman = 0f;
        while (gecenZaman < kucultmeSuresi)
        {
            gecenZaman += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(gecenZaman / kucultmeSuresi);
            if (sayiYazisi != null)
                sayiYazisi.transform.localScale = Vector3.one * Mathf.Lerp(zonklamaOlcek, 1f, t);
            yield return null;
        }

        if (sayiYazisi != null) sayiYazisi.transform.localScale = Vector3.one;

        float kalanSure = gosterimSuresi - buyumeSuresi - kucultmeSuresi;
        if (kalanSure > 0f)
            yield return new WaitForSecondsRealtime(kalanSure);
    }
}