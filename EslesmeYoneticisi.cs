using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EslesmeYoneticisi : MonoBehaviour
{
    [System.Serializable]
    public class HayvanYemekEslesmesi
    {
        public string hayvanAdi; // Sadece Inspector'da tanımak için
        public Sprite hayvanSprite;
        public Sprite dogruYemekSprite;

        [Tooltip("Bu hayvanın görseli diğerlerine göre çok büyük/küçük duruyorsa buradan ince ayar yap " +
                 "(1 = normal, 0.7 = %30 küçült, 1.3 = %30 büyüt). Playtest ederek gözle ayarla.")]
        public float gorselOlcek = 1f;

        [Tooltip("Sprite içindeki boşluk (transparan padding) hayvandan hayvana farklı olduğu için, " +
                 "aynı ölçekte bile görseller hizasız durabilir. Buradan hayvana özel bir X/Y kayması " +
                 "verip diğerleriyle aynı hizaya getir. (0,0) = kayma yok, HayvanGorseli'nin sahnedeki " +
                 "orijinal konumunda kalır.")]
        public Vector2 gorselKonumOfset = Vector2.zero;

        [Header("Tanıtım Cümlesi (TR)")]
        [Tooltip("Türkçe ünlü uyumu (ım/im/um/üm) elle girilmeli, otomatik üretilemez. " +
                 "Örnek (panda için): \"Ben bir pandayım ve \" " +
                 "- Devamına doğru yemeğin adı otomatik eklenip sonuna \" yerim.\" gelecek.")]
        public string cumleOnEki = "Ben bir ...yım ve ";

        [Header("Tanıtım Cümlesi (EN)")]
        [Tooltip("İngilizce cümlenin \"yemeğin adı\" gelmeden önceki tam hâli, fiil dahil. " +
                 "Örnek (panda için): \"I am a panda and I eat \" " +
                 "- Devamına yemek adı eklenip sonuna sadece \".\" gelecek.")]
        public string cumleOnEkiEN = "I am a ... and I eat ";

        [Tooltip("Sprite dosya adından otomatik türetilemiyor (sprite'lar iki dilde ortak kullanılıyor) - " +
                 "elle gir. Örnek: \"bamboo\"")]
        public string yemekAdiEN;
    }

    [Header("Sahne Kurulumu")]
    public GameObject yemekKartPrefab;         // Image + EslesmeYemekKarti.cs olan prefab
    public RectTransform oyunAlani;            // Kartın sürüklenirken geçici olarak taşınacağı, EN ÜSTTE duran alan
    public RectTransform yemekSecenekleriGrubu; // Horizontal Layout Group'lu, 3 kartı barındıran container
    public RectTransform hedefBolge;           // Yemeğin bırakılacağı alan (hayvan görselinin kendisi olabilir)
    public Image hayvanGorseli;

    [Header("Paneller")]
    public GameObject eslesmeOyunuPaneli;
    public GameObject tebriklerPaneli;
    public GameObject oyunSecimGrubu;

    [Header("Duraklat Butonu")]
    [Tooltip("Diğer modlardaki gibi - sadece gerçek oyun ekranında görünür.")]
    public GameObject duraklatButonu;

    [Header("Sağ Bilgi Paneli")]
    [Tooltip("SÜRE/REKOR yazılarının olduğu panel.")]
    public GameObject sagBilgiPaneli;

    [Header("UI Yazıları (TMP)")]
    public TextMeshProUGUI ilerlemeYazisi;        // "3 / 8" gibi, opsiyonel
    public TextMeshProUGUI cumleYazisi;           // "Ben bir pandayım ve ...... yerim."
    public TextMeshProUGUI sureYazisi;            // Sağ paneldeki akan süre
    public TextMeshProUGUI enIyiSureYazisi;       // Sağ paneldeki rekor
    public TextMeshProUGUI tebriklerSureYazisi;   // Bitiş ekranındaki skor
    public TextMeshProUGUI tebriklerBaslikYazisi;
    public TextMeshProUGUI tebriklerButonYazisi;

    [Header("Hayvan-Yemek Eşleşmeleri (8 Tane)")]
    public HayvanYemekEslesmesi[] eslesmeler;

    [Header("Cümle Efekti Ayarları")]
    public float harfBasinaSure = 0.06f;
    public float sonrakiHayvanaGecisBeklemesi = 1.2f;

    [Header("Ses Efektleri")]
    public AudioSource sesKaynagi;
    public AudioClip dogruSesi;
    public AudioClip yanlisSesi;
    public AudioClip oyunBittiSesi;

    private const string BOSLUK_PLACEHOLDER = "......";
    private const string REKOR_ANAHTARI = "EslesmeEnIyiSure";

    private List<int> hayvanSirasi = new List<int>();
    private int aktifIndex;

    private Vector2 hayvanGorseliTemelKonum;
    private bool temelKonumAlindi = false;

    private float gecenSure = 0f;
    private bool oyunDevamEdiyor = false;
    private float enIyiSure = 0f;

    void Awake()
    {
        // Hayvan görselinin sahnede elle yerleştirdiğin orijinal konumunu bir kere kaydet.
        // Sonradan her hayvan için gorselKonumOfset bu konuma eklenip/çıkarılacak.
        if (hayvanGorseli != null)
        {
            hayvanGorseliTemelKonum = hayvanGorseli.rectTransform.anchoredPosition;
            temelKonumAlindi = true;
        }
    }

    void Update()
    {
        if (oyunDevamEdiyor)
        {
            gecenSure += Time.deltaTime;
            if (sureYazisi != null)
            {
                string sureKelimesi = MenuYoneticisi.turkceMi ? "SÜRE" : "TIME";
                string saniyeKisaltma = MenuYoneticisi.turkceMi ? " SN" : " S";
                sureYazisi.text = sureKelimesi + "\n" + gecenSure.ToString("F1") + saniyeKisaltma;
            }
        }
    }

    public void EslesmeModunaGirildi()
    {
        TemizleYemekKartlari();

        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(
                ortadaCagrilacak: () => {
                    if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(false);
                    if (eslesmeOyunuPaneli != null) eslesmeOyunuPaneli.SetActive(true);
                    if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);

                    if (duraklatButonu != null) duraklatButonu.SetActive(true);
                    if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(true);

                    OyunuBaslat();
                },
                tamamlaninca: () => {
                    // Bulutlar TAMAMEN açıldıktan SONRA geri sayım başlar.
                    if (GeriSayimYoneticisi.Instance != null)
                    {
                        GeriSayimYoneticisi.Instance.GeriSayimBaslat(() => {
                            oyunDevamEdiyor = true;
                        });
                    }
                    else
                    {
                        oyunDevamEdiyor = true;
                    }
                }
            );
        }
        else
        {
            if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(false);
            if (eslesmeOyunuPaneli != null) eslesmeOyunuPaneli.SetActive(true);
            if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);

            if (duraklatButonu != null) duraklatButonu.SetActive(true);
            if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(true);

            OyunuBaslat();
            oyunDevamEdiyor = true;
        }
    }

    void OyunuBaslat()
    {
        hayvanSirasi.Clear();
        for (int i = 0; i < eslesmeler.Length; i++) hayvanSirasi.Add(i);
        KaristirListe(hayvanSirasi);

        aktifIndex = 0;

        // DİKKAT: oyunDevamEdiyor artık BURADA true yapılmıyor - 3-2-1-BAŞLA geri sayımı
        // bitene kadar süre saymaya başlamamalı.
        gecenSure = 0f;

        // YENİ FIX: sureYazisi'nin Inspector'daki varsayılan "New Text" içeriği geri sayım
        // boyunca görünür kalmasın diye, Update() ile AYNI formatla burada bir kere yazdırıyoruz.
        if (sureYazisi != null)
        {
            string sureKelimesi = MenuYoneticisi.turkceMi ? "SÜRE" : "TIME";
            string saniyeKisaltma = MenuYoneticisi.turkceMi ? " SN" : " S";
            sureYazisi.text = sureKelimesi + "\n" + gecenSure.ToString("F1") + saniyeKisaltma;
        }

        enIyiSure = PlayerPrefs.GetFloat(REKOR_ANAHTARI, 0f);
        EnIyiSureyiEkranaYaz();

        SiradakiHayvaniGoster();
    }

    public void EnIyiSureyiEkranaYaz()
    {
        if (enIyiSureYazisi != null)
        {
            string rekorKelimesi = MenuYoneticisi.turkceMi ? "REKOR" : "BEST";
            string saniyeKisaltma = MenuYoneticisi.turkceMi ? " SN" : " S";

            if (enIyiSure > 0f)
                enIyiSureYazisi.text = rekorKelimesi + "\n" + enIyiSure.ToString("F1") + saniyeKisaltma;
            else
                enIyiSureYazisi.text = rekorKelimesi + "\n--";
        }
    }

    void SiradakiHayvaniGoster()
    {
        TemizleYemekKartlari();

        int hayvanIndex = hayvanSirasi[aktifIndex];
        HayvanYemekEslesmesi hayvan = eslesmeler[hayvanIndex];

        if (hayvanGorseli != null)
        {
            hayvanGorseli.sprite = hayvan.hayvanSprite;
            hayvanGorseli.rectTransform.localScale = Vector3.one * hayvan.gorselOlcek;
            if (temelKonumAlindi)
                hayvanGorseli.rectTransform.anchoredPosition = hayvanGorseliTemelKonum + hayvan.gorselKonumOfset;
        }

        if (ilerlemeYazisi != null)
            ilerlemeYazisi.text = (aktifIndex + 1) + " / " + eslesmeler.Length;

        if (cumleYazisi != null)
        {
            string onEk = MenuYoneticisi.turkceMi ? hayvan.cumleOnEki : hayvan.cumleOnEkiEN;
            string sonEk = MenuYoneticisi.turkceMi ? " yerim." : ".";
            cumleYazisi.text = onEk + BOSLUK_PLACEHOLDER + sonEk;
        }

        // 1 doğru + 2 yanlış (başka hayvanların yemeklerinden) seçenek hazırla
        List<Sprite> secenekler = new List<Sprite> { hayvan.dogruYemekSprite };

        List<int> digerIndexler = new List<int>();
        for (int i = 0; i < eslesmeler.Length; i++)
            if (i != hayvanIndex) digerIndexler.Add(i);
        KaristirListe(digerIndexler);

        for (int i = 0; i < digerIndexler.Count && secenekler.Count < 3; i++)
            secenekler.Add(eslesmeler[digerIndexler[i]].dogruYemekSprite);

        KaristirListe(secenekler); // Doğru cevabın sırası da karışsın

        for (int i = 0; i < secenekler.Count; i++)
        {
            GameObject kartObje = Instantiate(yemekKartPrefab, yemekSecenekleriGrubu);
            EslesmeYemekKarti kart = kartObje.GetComponent<EslesmeYemekKarti>();
            bool dogruMu = (secenekler[i] == hayvan.dogruYemekSprite);
            kart.KartiKur(secenekler[i], dogruMu, this);
        }
    }

    void TemizleYemekKartlari()
    {
        if (yemekSecenekleriGrubu != null)
        {
            for (int i = yemekSecenekleriGrubu.childCount - 1; i >= 0; i--)
                Destroy(yemekSecenekleriGrubu.GetChild(i).gameObject);
        }

        // Sürüklenip oyunAlani'na taşınmış ama henüz yok olmamış bir kart kalmışsa onu da temizle
        if (oyunAlani != null)
        {
            for (int i = oyunAlani.childCount - 1; i >= 0; i--)
            {
                Transform child = oyunAlani.GetChild(i);
                if (child.GetComponent<EslesmeYemekKarti>() != null)
                    Destroy(child.gameObject);
            }
        }
    }

    void KaristirListe<T>(List<T> liste)
    {
        for (int i = 0; i < liste.Count; i++)
        {
            int rastgeleIndex = Random.Range(i, liste.Count);
            T gecici = liste[i];
            liste[i] = liste[rastgeleIndex];
            liste[rastgeleIndex] = gecici;
        }
    }

#if UNITY_EDITOR
    // Play modundayken Inspector'dan bir hayvanın "Gorsel Olcek" ya da "Gorsel Konum Ofset"
    // değerini değiştirdiğinde, eğer o hayvan şu an ekranda gösteriliyorsa, değişiklik
    // anında sahnede uygulanır. Böylece slider'ı/alanı değiştirip gözle en uygun değeri bulabilirsin.
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        if (hayvanGorseli == null || hayvanSirasi == null || hayvanSirasi.Count == 0) return;
        if (aktifIndex < 0 || aktifIndex >= hayvanSirasi.Count) return;
        if (!temelKonumAlindi) return;

        int hayvanIndex = hayvanSirasi[aktifIndex];
        if (hayvanIndex < 0 || hayvanIndex >= eslesmeler.Length) return;

        HayvanYemekEslesmesi hayvan = eslesmeler[hayvanIndex];
        hayvanGorseli.rectTransform.localScale = Vector3.one * hayvan.gorselOlcek;
        hayvanGorseli.rectTransform.anchoredPosition = hayvanGorseliTemelKonum + hayvan.gorselKonumOfset;
    }
#endif

    // Sprite dosya adından cümlede kullanılabilecek yemek adını türetir (SADECE TR).
    // "Bambu" -> "bambu", "Deniz_Yosunu" -> "deniz yosunu"
    // İngilizce için sprite adı kullanılamaz (sprite'lar iki dilde ortak) - o yüzden
    // hayvan.yemekAdiEN alanı elle doldurulup doğrudan kullanılıyor.
    string YemekAdiniGetirTR(Sprite yemekSprite)
    {
        if (yemekSprite == null) return "";

        string ad = yemekSprite.name;
        ad = ad.Replace("_", " ").Replace("-", " ").Trim();
        if (ad.Length == 0) return ad;

        CultureInfo trKultur = CultureInfo.GetCultureInfo("tr-TR");
        ad = char.ToLower(ad[0], trKultur) + ad.Substring(1);
        return ad;
    }

    // Doğru yemek hedefe bırakıldığında EslesmeYemekKarti bunu çağırır
    public void DogruEslesme(Sprite yenenYemekSprite)
    {
        if (MenuYoneticisi.sesEfektleriAcik && dogruSesi != null && sesKaynagi != null)
            sesKaynagi.PlayOneShot(dogruSesi);

        // Cümle yazı efekti sırasında süre sayacı duraklasın.
        oyunDevamEdiyor = false;

        // Tur bitti - kalan kartların (sürüklenen dahil) hepsini temizle,
        // cümle efekti sırasında başka kart sürüklenemesin.
        TemizleYemekKartlari();

        StartCoroutine(DogruEslesmeAkisi(yenenYemekSprite));
    }

    IEnumerator DogruEslesmeAkisi(Sprite yenenYemekSprite)
    {
        int hayvanIndex = hayvanSirasi[aktifIndex];
        HayvanYemekEslesmesi hayvan = eslesmeler[hayvanIndex];

        string yemekAdi = MenuYoneticisi.turkceMi
            ? YemekAdiniGetirTR(yenenYemekSprite)
            : hayvan.yemekAdiEN;

        if (cumleYazisi != null)
        {
            string onEk = MenuYoneticisi.turkceMi ? hayvan.cumleOnEki : hayvan.cumleOnEkiEN;
            string sonEk = MenuYoneticisi.turkceMi ? " yerim." : ".";
            string suankiYazi = "";

            cumleYazisi.text = onEk + sonEk;

            for (int i = 0; i < yemekAdi.Length; i++)
            {
                suankiYazi += yemekAdi[i];
                cumleYazisi.text = onEk + suankiYazi + sonEk;
                yield return new WaitForSeconds(harfBasinaSure);
            }
        }

        yield return new WaitForSeconds(sonrakiHayvanaGecisBeklemesi);

        aktifIndex++;
        if (aktifIndex >= hayvanSirasi.Count)
        {
            StartCoroutine(OyunuBitir());
        }
        else
        {
            // Sayacı tekrar başlat - oyun devam ediyor.
            oyunDevamEdiyor = true;
            SiradakiHayvaniGoster();
        }
    }

    // Yanlış yemek hedefe bırakıldığında EslesmeYemekKarti bunu çağırır
    public void YanlisEslesme()
    {
        if (MenuYoneticisi.sesEfektleriAcik && yanlisSesi != null && sesKaynagi != null)
            sesKaynagi.PlayOneShot(yanlisSesi);
    }

    IEnumerator OyunuBitir()
    {
        oyunDevamEdiyor = false;
        yield return new WaitForSeconds(0.4f);

        if (MenuYoneticisi.sesEfektleriAcik && oyunBittiSesi != null && sesKaynagi != null)
            sesKaynagi.PlayOneShot(oyunBittiSesi);

        if (enIyiSure == 0f || gecenSure < enIyiSure)
        {
            enIyiSure = gecenSure;
            PlayerPrefs.SetFloat(REKOR_ANAHTARI, enIyiSure);
            PlayerPrefs.Save();
            EnIyiSureyiEkranaYaz();
        }

        if (tebriklerBaslikYazisi != null)
            tebriklerBaslikYazisi.text = MenuYoneticisi.turkceMi ? "TEBRİKLER!" : "CONGRATULATIONS!";

        if (tebriklerButonYazisi != null)
            tebriklerButonYazisi.text = MenuYoneticisi.turkceMi ? "DEVAM" : "CONTINUE";

        if (tebriklerSureYazisi != null)
        {
            string sureMetni = MenuYoneticisi.turkceMi ? "SÜRENİZ: " : "YOUR TIME: ";
            string saniyeMetni = MenuYoneticisi.turkceMi ? " SANİYE!" : " SECONDS!";
            tebriklerSureYazisi.text = sureMetni + gecenSure.ToString("F1") + saniyeMetni;
        }

        if (tebriklerPaneli != null) tebriklerPaneli.SetActive(true);

        if (duraklatButonu != null) duraklatButonu.SetActive(false);
        if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(false);
    }

    public void TebriklerTamamButonunaBasildi()
    {
        TemizleYemekKartlari();

        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(() => {
                if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
                if (eslesmeOyunuPaneli != null) eslesmeOyunuPaneli.SetActive(false);
                if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
            });
        }
        else
        {
            if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
            if (eslesmeOyunuPaneli != null) eslesmeOyunuPaneli.SetActive(false);
            if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
        }
    }

    public void GeriButonunaBasildi()
    {
        // KRİTİK: OyunuBitir coroutine'i (WaitForSeconds 0.4f) hâlâ beklerken
        // duraklatıp Ana Menü'ye dönülürse, coroutine uyanınca artık kapanmış
        // panellere/temizlenmiş karta erişmeye çalışabilir. Önce durduruyoruz.
        StopAllCoroutines();

        oyunDevamEdiyor = false;
        TemizleYemekKartlari();

        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(() => {
                if (eslesmeOyunuPaneli != null) eslesmeOyunuPaneli.SetActive(false);
                if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);

                if (duraklatButonu != null) duraklatButonu.SetActive(false);
                if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(false);
            });
        }
        else
        {
            if (eslesmeOyunuPaneli != null) eslesmeOyunuPaneli.SetActive(false);
            if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);

            if (duraklatButonu != null) duraklatButonu.SetActive(false);
            if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(false);
        }
    }

    // --- Duraklatma menüsündeki "Ana Menü'ye Dön" butonunun çağırdığı metod ---
    public void DuraklatButonunaBasildi()
    {
        PauseController.Instance.Ac(GeriButonunaBasildi);
    }
}