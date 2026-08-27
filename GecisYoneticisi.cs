using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Sahnede HER ZAMAN AKTİF, tek bir obje üzerinde durmalı (PauseController ile aynı mantık).
// Sol ve sağ taraf için BİRDEN FAZLA bulut parçasını (kendi aralarında elle konumlandırılmış -
// kaydırılmış/döndürülmüş, üst üste binen bir küme oluşturacak şekilde) birlikte kaydırarak
// ekranı kapatıp, "ortadaCagrilacak" callback'i (asıl panel değişimi) ekran tam kapalıyken
// çalıştırıp, sonra tekrar açar. Tek bir dev gerilmiş bulut yerine birden fazla parça
// kullanmak, buluşma noktasında görsel yoğunluk sağlıyor (gerilme kaynaklı incelmeyi önlüyor).
public class GecisYoneticisi : MonoBehaviour
{
    public static GecisYoneticisi Instance;

    [Header("Bulut Görselleri")]
    [Tooltip("SOL taraftaki bulut parçaları (2-3 tane önerilir). Sahnede ELLE, KAPALI " +
             "(ekranı kapsayan) haldeyken nasıl durmalarını istiyorsanız öyle konumlandırın - " +
             "üst üste binsinler, farklı Y/rotasyon kullanabilirsiniz. Script bu konumu " +
             "'kapalı' pozisyon olarak kaydedip, hepsini BİRLİKTE sola kaydırarak gizleyecek.")]
    public RectTransform[] solBulutlar;

    [Tooltip("SAĞ taraftaki bulut parçaları - aynı mantık, sağa kaydırılarak gizlenecek.")]
    public RectTransform[] sagBulutlar;

    [Header("Zamanlama")]
    public float kayisSuresi = 0.4f;
    [Tooltip("Ekran TAM kapalıyken, panel değişiminden önce/sonra bekleme payı (saniye)")]
    public float kapaliBeklemeSuresi = 0.15f;

    [Header("Gizli (Ekran Dışı) Mesafe")]
    [Tooltip("Bulutların GÖRÜNMEZ olduğu andaki, kapalı konumlarına göre ne kadar UZAĞA " +
             "kayacağı (Canvas local birim). Sabit bir değer - rect.width okumaya bağlı DEĞİL, " +
             "önceki 'ilk frame'lerde yanlış boyut okunuyor' sınıfı buglardan kaçınmak için.")]
    public float gizliMesafe = 3000f;

    // Her parçanın ELLE yerleştirilmiş "kapalı" pozisyonu (Awake'te bir kere kaydedilir)
    private Vector2[] solKapaliPos;
    private Vector2[] sagKapaliPos;

    void Awake()
    {
        Instance = this;

        if (solBulutlar != null)
        {
            solKapaliPos = new Vector2[solBulutlar.Length];
            for (int i = 0; i < solBulutlar.Length; i++)
                if (solBulutlar[i] != null) solKapaliPos[i] = solBulutlar[i].anchoredPosition;
        }
        if (sagBulutlar != null)
        {
            sagKapaliPos = new Vector2[sagBulutlar.Length];
            for (int i = 0; i < sagBulutlar.Length; i++)
                if (sagBulutlar[i] != null) sagKapaliPos[i] = sagBulutlar[i].anchoredPosition;
        }

        BulutlariAninda(acik: true);
    }

    void BulutlariAninda(bool acik)
    {
        float delta = acik ? gizliMesafe : 0f;

        if (solBulutlar != null)
        {
            for (int i = 0; i < solBulutlar.Length; i++)
            {
                if (solBulutlar[i] == null) continue;
                Vector2 baz = solKapaliPos[i];
                solBulutlar[i].anchoredPosition = new Vector2(baz.x - delta, baz.y);
            }
        }
        if (sagBulutlar != null)
        {
            for (int i = 0; i < sagBulutlar.Length; i++)
            {
                if (sagBulutlar[i] == null) continue;
                Vector2 baz = sagKapaliPos[i];
                sagBulutlar[i].anchoredPosition = new Vector2(baz.x + delta, baz.y);
            }
        }
    }

    // Dışarıdan çağrılacak ANA metod. ortadaCagrilacak = ekran tam kapalıyken çalışacak kod
    // (panel SetActive değişimleri, oyun başlatma vs.)
    public void GecisYap(Action ortadaCagrilacak)
    {
        StartCoroutine(GecisCoroutine(ortadaCagrilacak));
    }

    IEnumerator GecisCoroutine(Action ortadaCagrilacak)
    {
        yield return KaydirCoroutine(kapaniyor: true);

        yield return new WaitForSecondsRealtime(kapaliBeklemeSuresi);
        ortadaCagrilacak?.Invoke();
        yield return new WaitForSecondsRealtime(kapaliBeklemeSuresi);

        yield return KaydirCoroutine(kapaniyor: false);
    }

    IEnumerator KaydirCoroutine(bool kapaniyor)
    {
        // Şu anki "delta" değerini her parçanın baz pozisyonuna göre ölç (X farkı üzerinden)
        float solBaslangicDelta = (solBulutlar != null && solBulutlar.Length > 0 && solBulutlar[0] != null)
            ? solKapaliPos[0].x - solBulutlar[0].anchoredPosition.x : 0f;
        float sagBaslangicDelta = (sagBulutlar != null && sagBulutlar.Length > 0 && sagBulutlar[0] != null)
            ? sagBulutlar[0].anchoredPosition.x - sagKapaliPos[0].x : 0f;

        float hedefDelta = kapaniyor ? 0f : gizliMesafe;

        float gecenZaman = 0f;
        while (gecenZaman < kayisSuresi)
        {
            gecenZaman += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(gecenZaman / kayisSuresi);
            t = t * t * (3f - 2f * t); // smoothstep

            float solDelta = Mathf.Lerp(solBaslangicDelta, hedefDelta, t);
            float sagDelta = Mathf.Lerp(sagBaslangicDelta, hedefDelta, t);

            if (solBulutlar != null)
                for (int i = 0; i < solBulutlar.Length; i++)
                {
                    if (solBulutlar[i] == null) continue;
                    Vector2 baz = solKapaliPos[i];
                    solBulutlar[i].anchoredPosition = new Vector2(baz.x - solDelta, baz.y);
                }
            if (sagBulutlar != null)
                for (int i = 0; i < sagBulutlar.Length; i++)
                {
                    if (sagBulutlar[i] == null) continue;
                    Vector2 baz = sagKapaliPos[i];
                    sagBulutlar[i].anchoredPosition = new Vector2(baz.x + sagDelta, baz.y);
                }

            yield return null;
        }

        BulutlariAninda(acik: !kapaniyor);
    }
}