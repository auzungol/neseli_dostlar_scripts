using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class YapbozYoneticisi : MonoBehaviour
{
    [System.Serializable]
    public class HayvanYapbozSeti
    {
        public string hayvanAdi; // Sadece Inspector'da tanımak için

        [Header("3x2 Kolay Mod (6 parça)")]
        [Tooltip("Sırasıyla: r1c1, r1c2, r1c3, r2c1, r2c2, r2c3")]
        public Sprite[] parcalar3x2;
        [Tooltip("YapbozKalibratoru aracıyla otomatik doldurulur - elle girmene gerek yok")]
        public Vector2[] konumlar3x2;
        public Vector2[] boyutlar3x2;

        [Header("4x3 Zor Mod (12 parça)")]
        [Tooltip("Sırasıyla: r1c1..r1c4, r2c1..r2c4, r3c1..r3c4")]
        public Sprite[] parcalar4x3;
        [Tooltip("YapbozKalibratoru aracıyla otomatik doldurulur - elle girmene gerek yok")]
        public Vector2[] konumlar4x3;
        public Vector2[] boyutlar4x3;
    }

    private struct ParcaVerisi
    {
        public Sprite sprite;
        public Vector2 hedefKonum;
        public Vector2 boyut;
    }

    [Header("Sahne Kurulumu")]
    public GameObject parcaPrefab;
    public GameObject yuvaPrefab;
    public RectTransform oyunAlani;

    [Tooltip("Havuzda parçaların duracağı, elle yerleştirilmiş sabit slot konumları. " +
             "Bunlar TÜM hayvanlar için ortak kullanılır (kesimden bağımsız, sadece bekleme alanı).")]
    public RectTransform[] havuzSlotKonumlari;

    [Header("Paneller")]
    public GameObject zorlukSecimPaneli;
    public GameObject hayvanSecimPaneli;
    public GameObject yapbozOyunuPaneli;
    public GameObject tebriklerPaneli;
    public GameObject oyunSecimGrubu;

    [Header("Duraklat Butonu")]
    [Tooltip("Sadece gerçek oyun ekranında (OyunAlani) görünmeli - Zorluk/Hayvan seçim ekranlarında gizlenir.")]
    public GameObject duraklatButonu;

    [Header("Sağ Bilgi Paneli")]
    [Tooltip("SÜRE/REKOR yazılarının olduğu panel. Sahnede varsayılan kapalı başlıyorsa hiçbir kod onu açmıyordu - " +
             "duraklatButonu ile aynı mantıkla, sadece gerçek oyun ekranında görünür.")]
    public GameObject sagBilgiPaneli;

    [Header("İpucu Butonu")]
    public Button ipucuButonu;

    [Header("UI Yazıları (TMP)")]
    public TextMeshProUGUI sureYazisi;
    public TextMeshProUGUI enIyiSureYazisi;
    public TextMeshProUGUI tebriklerSureYazisi;
    public TextMeshProUGUI tebriklerBaslikYazisi;
    public TextMeshProUGUI tebriklerButonYazisi;
    [Tooltip("Tebrikler ekranında REKOR yazısı - diğer modlarla tutarlı olsun diye eklendi. " +
             "Sağdaki SagBilgiPaneli'ndeki enIyiSureYazisi ile AYNI formatı kullanır.")]
    public TextMeshProUGUI tebriklerRekorYazisi;

    [Header("Hayvan Yapboz Setleri (8 Tane - HayvanSecildi(index) ile aynı sırada olmalı!)")]
    public HayvanYapbozSeti[] hayvanSetleri;

    [Header("Ayarlar")]
    public float hizalamaToleransi = 60f;
    [Range(0f, 1f)] public float yuvaBaslangicAlfasi = 0.35f;
    [Range(0f, 1f)] public float ipucuOnizlemeAlfasi = 0.9f;
    public float ipucuOnizlemeSuresi = 2.5f;
    [Range(0.3f, 1f)]
    [Tooltip("Havuzda beklerken parçaların, gerçek (kalibre edilmiş) boyutlarına göre ne kadar " +
             "küçük görüneceği - 1 = gerçek boyut, 0.75 = %25 küçük. Sürüklemeye başlayınca " +
             "gerçek boyutuna büyürler.")]
    public float havuzParcaOlcegi = 0.75f;

    [Header("Ses Efektleri")]
    public AudioSource sesKaynagi;
    public AudioClip parcaAlmaSesi;
    public AudioClip dogruYereOturmaSesi;
    public AudioClip oyunBittiSesi;

    private List<YapbozParcasi> aktifParcalar = new List<YapbozParcasi>();
    private List<GameObject> aktifYuvalar = new List<GameObject>();
    private List<ParcaVerisi> bekleyenParcaKuyrugu = new List<ParcaVerisi>();

    private int yerlesenParcaSayisi = 0;
    private int toplamParcaSayisi = 0;

    private float gecenSure = 0f;
    private bool oyunDevamEdiyor = false;
    private float enIyiSure = 0f;

    private int aktifSatirSayisi;
    private int aktifSutunSayisi;

    private int secilenSatirGecici;
    private int secilenSutunGecici;

    // YENİ: "Yeniden Başlat" için son seçilen hayvanı hatırlıyoruz
    private int sonSecilenHayvanIndex;

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

    public void YapbozModunaGirildi()
    {
        MasayiTemizle();

        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(() => {
                if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(false);
                if (yapbozOyunuPaneli != null) yapbozOyunuPaneli.SetActive(true);
                if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
                if (hayvanSecimPaneli != null) hayvanSecimPaneli.SetActive(false);
                if (zorlukSecimPaneli != null) zorlukSecimPaneli.SetActive(true);

                if (duraklatButonu != null) duraklatButonu.SetActive(false);
                if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(false);
            });
        }
        else
        {
            if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(false);
            if (yapbozOyunuPaneli != null) yapbozOyunuPaneli.SetActive(true);
            if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
            if (hayvanSecimPaneli != null) hayvanSecimPaneli.SetActive(false);
            if (zorlukSecimPaneli != null) zorlukSecimPaneli.SetActive(true);

            if (duraklatButonu != null) duraklatButonu.SetActive(false);
            if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(false);
        }
    }

    public void Zorluk3x2Secildi()
    {
        secilenSatirGecici = 2;
        secilenSutunGecici = 3;
        ZorlukSecildiOrtak();
    }

    public void Zorluk4x3Secildi()
    {
        secilenSatirGecici = 3;
        secilenSutunGecici = 4;
        ZorlukSecildiOrtak();
    }

    void ZorlukSecildiOrtak()
    {
        if (zorlukSecimPaneli != null) zorlukSecimPaneli.SetActive(false);
        if (hayvanSecimPaneli != null) hayvanSecimPaneli.SetActive(true);

        if (duraklatButonu != null) duraklatButonu.SetActive(false);
        if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(false);
    }

    public void HayvanSecildi(int hayvanIndex)
    {
        // YENİ: "Yeniden Başlat" butonunun hangi hayvanla tekrar başlayacağını bilmesi için
        // son seçilen hayvanı hatırlıyoruz.
        sonSecilenHayvanIndex = hayvanIndex;

        // Bulut geçişiyle sarmaladık - SEÇ'e basar basmaz bulutlar ANINDA kapanmaya başlasın,
        // altında OyunuBaslat()'ın ağır işlemleri (parça/yuva oluşturma) örtülü kalsın.
        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(
                ortadaCagrilacak: () => {
                    if (hayvanSecimPaneli != null) hayvanSecimPaneli.SetActive(false);
                    OyunuBaslat(secilenSatirGecici, secilenSutunGecici, hayvanIndex);
                },
                tamamlaninca: () => {
                    // Bulutlar TAMAMEN açıldıktan SONRA geri sayım başlar - böylece
                    // geri sayım hiçbir zaman bulut animasyonuyla çakışmaz.
                    if (GeriSayimYoneticisi.Instance != null)
                    {
                        GeriSayimYoneticisi.Instance.GeriSayimBaslat(() => {
                            // Geri sayım bitince süre sayacı GERÇEKTEN burada başlar.
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
            if (hayvanSecimPaneli != null) hayvanSecimPaneli.SetActive(false);
            OyunuBaslat(secilenSatirGecici, secilenSutunGecici, hayvanIndex);
            oyunDevamEdiyor = true;
        }
    }

    void OyunuBaslat(int satirSayisi, int sutunSayisi, int hayvanIndex)
    {
        aktifSatirSayisi = satirSayisi;
        aktifSutunSayisi = sutunSayisi;

        if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);

        MasayiTemizle();

        HayvanYapbozSeti secilenHayvan = hayvanSetleri[hayvanIndex];
        bool zorMod = (satirSayisi == 3);

        Sprite[] parcalar = zorMod ? secilenHayvan.parcalar4x3 : secilenHayvan.parcalar3x2;
        Vector2[] konumlar = zorMod ? secilenHayvan.konumlar4x3 : secilenHayvan.konumlar3x2;
        Vector2[] boyutlar = zorMod ? secilenHayvan.boyutlar4x3 : secilenHayvan.boyutlar3x2;

        if (konumlar == null || konumlar.Length != parcalar.Length)
        {
            Debug.LogError("'" + secilenHayvan.hayvanAdi + "' için " + (zorMod ? "4x3" : "3x2") +
                " konum verisi eksik veya parça sayısıyla uyuşmuyor! YapbozKalibratoru ile kaydetmeyi unuttun mu?");
            return;
        }

        YuvalariOlustur(parcalar, konumlar, boyutlar);
        ParcaKuyruguHazirla(parcalar, konumlar, boyutlar);
        HavuzuDoldur();

        gecenSure = 0f;
        yerlesenParcaSayisi = 0;
        toplamParcaSayisi = parcalar.Length;
        // DİKKAT: oyunDevamEdiyor BURADA true yapılmıyor artık - 3-2-1-BAŞLA geri sayımı
        // bitene kadar süre saymaya başlamamalı. Geri sayımın "bittiginde" callback'inde set edilecek.

        // YENİ FIX: Update() henüz çalışmadığı için (oyunDevamEdiyor hâlâ false) sureYazisi
        // TMP objesinin Inspector'daki varsayılan "New Text" içeriği geri sayım boyunca
        // görünür kalıyordu. Update()'in kullandığı AYNI formatla, süre 0.0 iken bir kere
        // burada elle yazdırıyoruz - geri sayım sırasında "SÜRE / 0.0 SN" görünsün.
        if (sureYazisi != null)
        {
            string sureKelimesi = MenuYoneticisi.turkceMi ? "SÜRE" : "TIME";
            string saniyeKisaltma = MenuYoneticisi.turkceMi ? " SN" : " S";
            sureYazisi.text = sureKelimesi + "\n" + gecenSure.ToString("F1") + saniyeKisaltma;
        }

        if (ipucuButonu != null) ipucuButonu.interactable = true;
        if (duraklatButonu != null) duraklatButonu.SetActive(true);
        if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(true);

        string rekorAnahtari = RekorAnahtariGetir(satirSayisi, sutunSayisi);
        enIyiSure = PlayerPrefs.GetFloat(rekorAnahtari, 0f);
        EnIyiSureyiEkranaYaz();
    }

    string RekorAnahtariGetir(int satirSayisi, int sutunSayisi)
    {
        return "YapbozEnIyiSure_" + sutunSayisi + "x" + satirSayisi;
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

    Vector2 HavuzSlotKonumuGetir(int slotIndex)
    {
        return havuzSlotKonumlari[slotIndex].anchoredPosition;
    }

    void MasayiTemizle()
    {
        if (oyunAlani != null)
        {
            for (int i = oyunAlani.childCount - 1; i >= 0; i--)
            {
                Transform child = oyunAlani.GetChild(i);
                if (child.GetComponent<YapbozParcasi>() != null)
                    Destroy(child.gameObject);
            }
        }
        foreach (GameObject yuva in aktifYuvalar)
        {
            if (yuva != null) Destroy(yuva);
        }
        aktifParcalar.Clear();
        aktifYuvalar.Clear();
        bekleyenParcaKuyrugu.Clear();
        yerlesenParcaSayisi = 0;
    }

    void YuvalariOlustur(Sprite[] parcalar, Vector2[] konumlar, Vector2[] boyutlar)
    {
        for (int i = 0; i < parcalar.Length; i++)
        {
            GameObject yeniYuva = Instantiate(yuvaPrefab, oyunAlani);
            RectTransform rt = yeniYuva.GetComponent<RectTransform>();
            rt.sizeDelta = boyutlar[i];
            rt.anchoredPosition = konumlar[i];

            Image img = yeniYuva.GetComponent<Image>();
            img.sprite = parcalar[i];
            img.color = new Color(1f, 1f, 1f, yuvaBaslangicAlfasi);
            img.raycastTarget = false;

            aktifYuvalar.Add(yeniYuva);
        }
    }

    void ParcaKuyruguHazirla(Sprite[] parcalar, Vector2[] konumlar, Vector2[] boyutlar)
    {
        bekleyenParcaKuyrugu.Clear();

        for (int i = 0; i < parcalar.Length; i++)
        {
            bekleyenParcaKuyrugu.Add(new ParcaVerisi
            {
                sprite = parcalar[i],
                hedefKonum = konumlar[i],
                boyut = boyutlar[i]
            });
        }

        KaristirListe(bekleyenParcaKuyrugu);
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

    // Mevcut havuz grubunda kaç parça var ve kaçı doğru yerine oturdu -
    // grubun HEPSİ oturmadan yeni grup gelmiyor.
    private int suankiHavuzBoyutu = 0;
    private int suankiHavuzdaYerlesen = 0;

    void HavuzuDoldur()
    {
        suankiHavuzBoyutu = 0;
        suankiHavuzdaYerlesen = 0;

        for (int slot = 0; slot < havuzSlotKonumlari.Length; slot++)
        {
            if (ParcayiHavuzSlotunaYerlestir(slot))
                suankiHavuzBoyutu++;
        }
    }

    bool ParcayiHavuzSlotunaYerlestir(int slotIndex)
    {
        if (bekleyenParcaKuyrugu.Count == 0) return false;

        ParcaVerisi veri = bekleyenParcaKuyrugu[0];
        bekleyenParcaKuyrugu.RemoveAt(0);

        GameObject yeniParcaObje = Instantiate(parcaPrefab, oyunAlani);
        RectTransform rt = yeniParcaObje.GetComponent<RectTransform>();
        rt.sizeDelta = veri.boyut;
        rt.anchoredPosition = HavuzSlotKonumuGetir(slotIndex);

        YapbozParcasi parcaScript = yeniParcaObje.GetComponent<YapbozParcasi>();
        parcaScript.ParcayiKur(veri.sprite, veri.hedefKonum, this, slotIndex, havuzParcaOlcegi);

        if (MenuYoneticisi.sesEfektleriAcik && parcaAlmaSesi != null)
            sesKaynagi.PlayOneShot(parcaAlmaSesi);

        aktifParcalar.Add(parcaScript);
        return true;
    }

    public void ParcaYerlestirildi(YapbozParcasi parca)
    {
        if (MenuYoneticisi.sesEfektleriAcik && dogruYereOturmaSesi != null)
            sesKaynagi.PlayOneShot(dogruYereOturmaSesi);

        yerlesenParcaSayisi++;
        suankiHavuzdaYerlesen++;

        // Mevcut grubun hepsi bitti mi? Bittiyse sıradaki grubu getir.
        if (yerlesenParcaSayisi < toplamParcaSayisi && suankiHavuzdaYerlesen >= suankiHavuzBoyutu)
        {
            HavuzuDoldur();
        }

        if (yerlesenParcaSayisi >= toplamParcaSayisi)
        {
            StartCoroutine(OyunuBitir());
        }
    }

    public void IpucuButonunaBasildi()
    {
        if (!oyunDevamEdiyor) return;
        StartCoroutine(IpucuOnizlemesiGoster());
    }

    IEnumerator IpucuOnizlemesiGoster()
    {
        if (ipucuButonu != null) ipucuButonu.interactable = false;

        yield return StartCoroutine(YuvalarinAlfasiniAyarla(ipucuOnizlemeAlfasi, 0.3f));
        yield return new WaitForSeconds(ipucuOnizlemeSuresi);
        yield return StartCoroutine(YuvalarinAlfasiniAyarla(yuvaBaslangicAlfasi, 0.3f));

        if (ipucuButonu != null) ipucuButonu.interactable = true;
    }

    IEnumerator YuvalarinAlfasiniAyarla(float hedefAlfa, float sure)
    {
        List<Image> yuvaGorselleri = new List<Image>();
        foreach (GameObject yuva in aktifYuvalar)
        {
            if (yuva == null) continue;
            Image img = yuva.GetComponent<Image>();
            if (img != null) yuvaGorselleri.Add(img);
        }

        if (yuvaGorselleri.Count == 0) yield break;

        float baslangicAlfa = yuvaGorselleri[0].color.a;
        float gecenZaman = 0f;

        while (gecenZaman < sure)
        {
            gecenZaman += Time.deltaTime;
            float t = gecenZaman / sure;
            float yeniAlfa = Mathf.Lerp(baslangicAlfa, hedefAlfa, t);

            foreach (Image img in yuvaGorselleri)
                img.color = new Color(1f, 1f, 1f, yeniAlfa);

            yield return null;
        }

        foreach (Image img in yuvaGorselleri)
            img.color = new Color(1f, 1f, 1f, hedefAlfa);
    }

    IEnumerator OyunuBitir()
    {
        oyunDevamEdiyor = false;
        if (ipucuButonu != null) ipucuButonu.interactable = false;
        yield return new WaitForSeconds(0.4f);

        if (MenuYoneticisi.sesEfektleriAcik && oyunBittiSesi != null)
            sesKaynagi.PlayOneShot(oyunBittiSesi);

        string rekorAnahtari = RekorAnahtariGetir(aktifSatirSayisi, aktifSutunSayisi);
        if (enIyiSure == 0f || gecenSure < enIyiSure)
        {
            enIyiSure = gecenSure;
            PlayerPrefs.SetFloat(rekorAnahtari, enIyiSure);
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

        if (tebriklerRekorYazisi != null)
        {
            string rekorKelimesi = MenuYoneticisi.turkceMi ? "REKOR: " : "BEST: ";
            string saniyeKisaltma = MenuYoneticisi.turkceMi ? " SN" : " S";
            tebriklerRekorYazisi.text = rekorKelimesi + enIyiSure.ToString("F1") + saniyeKisaltma;
        }

        if (tebriklerPaneli != null) tebriklerPaneli.SetActive(true);
    }

    public void TebriklerTamamButonunaBasildi()
    {
        MasayiTemizle();

        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(() => {
                if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
                if (hayvanSecimPaneli != null) hayvanSecimPaneli.SetActive(false);
                if (zorlukSecimPaneli != null) zorlukSecimPaneli.SetActive(false);
                if (yapbozOyunuPaneli != null) yapbozOyunuPaneli.SetActive(false);
                if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);

                if (duraklatButonu != null) duraklatButonu.SetActive(false);
                if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(false);
            });
        }
        else
        {
            if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
            if (hayvanSecimPaneli != null) hayvanSecimPaneli.SetActive(false);
            if (zorlukSecimPaneli != null) zorlukSecimPaneli.SetActive(false);
            if (yapbozOyunuPaneli != null) yapbozOyunuPaneli.SetActive(false);
            if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);

            if (duraklatButonu != null) duraklatButonu.SetActive(false);
            if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(false);
        }
    }

    public void GeriButonunaBasildi()
    {
        if (oyunDevamEdiyor)
        {
            oyunDevamEdiyor = false;
            MasayiTemizle();
            if (hayvanSecimPaneli != null) hayvanSecimPaneli.SetActive(false);
            if (zorlukSecimPaneli != null) zorlukSecimPaneli.SetActive(true);
        }
        else if (hayvanSecimPaneli != null && hayvanSecimPaneli.activeSelf)
        {
            hayvanSecimPaneli.SetActive(false);
            if (zorlukSecimPaneli != null) zorlukSecimPaneli.SetActive(true);
        }
        else
        {
            MasayiTemizle();
            if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
            if (yapbozOyunuPaneli != null) yapbozOyunuPaneli.SetActive(false);
            if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
        }
    }

    // --- YENİ: ZorlukSecimPaneli'ndeki kendi Geri butonuna bağlanacak ---
    public void ZorlukSecimindenGeriDon()
    {
        if (zorlukSecimPaneli != null) zorlukSecimPaneli.SetActive(false);

        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(() => {
                // KRİTİK FIX: Mod seçim ekranına dönüyoruz - YapbozOyunPaneli'nin KENDİSİNİ de
                // kapatmazsak (ArkaPlan dahil tüm içeriği) aktif kalıp oyunSecimGrubu'nun ÜSTÜNE
                // biniyor. ArkaPlan tam ekran + Raycast Target açık olduğundan tüm tıklamaları
                // yutuyordu - "zorluk modundan geri dönünce hiçbir yere tıklanamıyor" bugu buydu.
                if (yapbozOyunuPaneli != null) yapbozOyunuPaneli.SetActive(false);
                if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);

                if (duraklatButonu != null) duraklatButonu.SetActive(false);
                if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(false);
            });
        }
        else
        {
            if (yapbozOyunuPaneli != null) yapbozOyunuPaneli.SetActive(false);
            if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);

            if (duraklatButonu != null) duraklatButonu.SetActive(false);
            if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(false);
        }
    }

    // --- YENİ: HayvanSecimPaneli'ndeki kendi Geri butonuna bağlanacak ---
    public void HayvanSecimindenGeriDon()
    {
        if (hayvanSecimPaneli != null) hayvanSecimPaneli.SetActive(false);
        if (zorlukSecimPaneli != null) zorlukSecimPaneli.SetActive(true);

        if (duraklatButonu != null) duraklatButonu.SetActive(false);
        if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(false);
    }

    // --- YENİ: Oyun ekranındaki Duraklat menüsünün "Ana Menü'ye Dön" butonu
    //           artık GeriButonunaBasildi (bir adım geri) yerine DOĞRUDAN
    //           mod seçim ekranına atsın diye eklendi ---
    public void OyunSecimineDon()
    {
        oyunDevamEdiyor = false;
        MasayiTemizle();

        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(() => {
                if (yapbozOyunuPaneli != null) yapbozOyunuPaneli.SetActive(false);
                if (hayvanSecimPaneli != null) hayvanSecimPaneli.SetActive(false);
                if (zorlukSecimPaneli != null) zorlukSecimPaneli.SetActive(false);
                if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
                if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);

                if (duraklatButonu != null) duraklatButonu.SetActive(false);
                if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(false);
            });
        }
        else
        {
            if (yapbozOyunuPaneli != null) yapbozOyunuPaneli.SetActive(false);
            if (hayvanSecimPaneli != null) hayvanSecimPaneli.SetActive(false);
            if (zorlukSecimPaneli != null) zorlukSecimPaneli.SetActive(false);
            if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
            if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);

            if (duraklatButonu != null) duraklatButonu.SetActive(false);
            if (sagBilgiPaneli != null) sagBilgiPaneli.SetActive(false);
        }
    }

    public void DuraklatButonunaBasildi()
    {
        // DİKKAT: artık GeriButonunaBasildi değil, OyunSecimineDon çağrılıyor -
        // duraklatma menüsündeki "Ana Menü'ye Dön" butonu doğrudan mod seçim ekranına gitsin diye.
        // Yeniden Başlat, son seçilen hayvanla HayvanSecildi'yi tekrar çağırır.
        // FIX: Duraklatınca oyunDevamEdiyor'u da false yapıyoruz - Yeniden Başlat sırasında
        // bulutlar kapanana kadarki kısa pencerede Update() eski süre değerini artırmaya
        // devam etmesin diye.
        oyunDevamEdiyor = false;
        PauseController.Instance.Ac(OyunSecimineDon, () => HayvanSecildi(sonSecilenHayvanIndex));
    }
}