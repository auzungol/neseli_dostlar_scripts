using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TahminYoneticisi : MonoBehaviour
{
    [System.Serializable]
    public class TahminHayvani
    {
        public string hayvanAdi; // Sadece Inspector'da tanımak için, aynı zamanda cevap kontrolünde kullanılmıyor
        public Sprite hayvanSprite; // Hem cevap seçeneğinde hem final "reveal" görselinde kullanılır

        [Tooltip("EslesmeYoneticisi'ndeki AYNI hayvanın 'Gorsel Olcek' değerini buraya kopyala - " +
                 "TEK referans, hem ödül görseli hem cevap seçeneği bunu kullanır. Artık ayrı bir " +
                 "'kart içi doluluk oranı' YOK - cevap seçenekleri artık sabit bir kutuya sıkıştırılmıyor, " +
                 "doğal oranlarıyla ortak bir yer çizgisi üzerinde duruyorlar.")]
        public float gorselOlcek = 1f;

        [Tooltip("PNG'nin alt kenarı ile karakterin GERÇEK ayak noktası arasındaki boşluk, ORİJİNAL " +
                 "kaynak görsel piksel cinsinden (örn. 1024px'lik bir PNG'de 250 gibi). Her hayvanın " +
                 "PNG'si içinde farklı miktarda boşluk bırakılmış olabilir - bu değer olmadan cevap " +
                 "seçeneklerinde hayvanların 'ayakları' aynı hizada durmaz. Photoshop/piksel analiziyle " +
                 "ölçülür, elle girilir.")]
        public float tabanBoslugu = 0f;

        [TextArea]
        [Tooltip("Sırayla verilecek ipuçları (TÜRKÇE). 3-4 tane yeterli. Örn: 'Bataklıkta yaşarım', '4 ayaklı sürüngenim'...")]
        public string[] ipuclari;

        [TextArea]
        [Tooltip("Aynı ipuçlarının İNGİLİZCE hali, AYNI SIRADA. Dil İngilizce'ye çevrilince bunlar gösterilir.")]
        public string[] ipuclariEN;
    }

    [Header("Sahne Kurulumu")]
    public GameObject secenekKartPrefab;      // Image + TahminSecenekKarti.cs olan prefab

    [Tooltip("3 SABİT konum (Yapboz'daki havuzSlotKonumlari ile AYNI mantık). Layout Group YOK - " +
             "her slot'un kendi anchoredPosition'ı, hayvanın 'ayak' hizasını belirler. Üçünün de " +
             "Y konumu AYNI olmalı ki tüm hayvanlar aynı yer çizgisinde dursun.")]
    public RectTransform[] secenekSlotlari;

    [Tooltip("Cevap seçeneklerindeki hayvanların, ödül görseline (hayvanGorseli) göre GENEL boyut " +
             "çarpanı. 1 = ödül görseliyle aynı boyut, 0.5 = yarısı kadar. Tüm hayvanlar için ortak " +
             "tek bir değer - her hayvanın KENDİ gorselOlcek'i zaten aralarındaki oranı koruyor.")]
    [Range(0.1f, 1.5f)]
    public float secenekGenelCarpan = 0.55f;

    public Image hayvanGorseli;                // Doğru cevapta büyüyerek beliren ödül görseli
    public TextMeshProUGUI ipucuMetniAlani;    // Verilen ipuçlarının biriktiği metin alanı
    public Button sonrakiIpucuButonu;
    public Image[] yildizIkonlari;             // 3 tane - kazanılan yıldıza göre renklenir

    [Header("Paneller")]
    public GameObject tahminOyunuPaneli;
    public GameObject tebriklerPaneli;
    public GameObject oyunSecimGrubu;

    [Header("UI Yazıları (TMP)")]
    public TextMeshProUGUI sureYazisi;
    public TextMeshProUGUI enIyiSureYazisi;
    public TextMeshProUGUI tebriklerBaslikYazisi;
    public TextMeshProUGUI tebriklerButonYazisi;
    public TextMeshProUGUI tebriklerSureYazisi;
    public TextMeshProUGUI tebriklerYildizYazisi;

    [Header("Hayvanlar (8 Tane)")]
    public TahminHayvani[] hayvanlar;

    [Header("Ayarlar")]
    [Tooltip("Yıldız rengi/görünürlüğü: kazanılan yıldız bu renkte, kazanılmayan soluk kalır")]
    public Color yildizDoluRengi = Color.white;
    public Color yildizBosRengi = new Color(1f, 1f, 1f, 0.25f);

    [Header("Ses Efektleri")]
    public AudioSource sesKaynagi;
    public AudioClip dogruSesi;
    public AudioClip yanlisSesi;
    public AudioClip alkisSesi;
    public AudioClip oyunBittiSesi;

    private List<int> hayvanSirasi = new List<int>();
    private int aktifIndex;
    private int aktifIpucuIndex; // Şu ana kadar gösterilen ipucu sayısı (1'den başlar)
    private int toplamYildiz = 0;

    private float gecenSure = 0f;
    private bool oyunDevamEdiyor = false;
    private float enIyiSure = 0f;

    private const string RekorAnahtari = "TahminEnIyiSure";

    // Şu an sahnede duran seçenek objelerini, HANGİ hayvana ait olduklarıyla birlikte takip
    // ediyoruz - böylece OnValidate() içinde secenekGenelCarpan Play modunda değiştirilince
    // hepsini ANINDA yeniden boyutlandırabiliyoruz (kapalı kutuda tahmin yürütmek yerine
    // canlı, WYSIWYG bir ayarlama deneyimi için).
    private class AktifSecenek
    {
        public TahminSecenekKarti kart;
        public TahminHayvani hayvan;
    }
    private List<AktifSecenek> aktifSecenekler = new List<AktifSecenek>();

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

    public void TahminModunaGirildi()
    {
        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(() => {
                if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(false);
                if (tahminOyunuPaneli != null) tahminOyunuPaneli.SetActive(true);
                if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);

                enIyiSure = PlayerPrefs.GetFloat(RekorAnahtari, 0f);
                EnIyiSureyiEkranaYaz();

                OyunuBaslat();
            });
        }
        else
        {
            if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(false);
            if (tahminOyunuPaneli != null) tahminOyunuPaneli.SetActive(true);
            if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);

            enIyiSure = PlayerPrefs.GetFloat(RekorAnahtari, 0f);
            EnIyiSureyiEkranaYaz();

            OyunuBaslat();
        }
    }

    void OyunuBaslat()
    {
        hayvanSirasi.Clear();
        for (int i = 0; i < hayvanlar.Length; i++) hayvanSirasi.Add(i);
        KaristirListe(hayvanSirasi);

        aktifIndex = 0;
        toplamYildiz = 0;
        gecenSure = 0f;
        oyunDevamEdiyor = true;

        SiradakiHayvaniGoster();
    }

    void SiradakiHayvaniGoster()
    {
        TemizleSecenekKartlari();

        aktifIpucuIndex = 1; // İlk ipucu otomatik gösteriliyor

        int hayvanIndex = hayvanSirasi[aktifIndex];
        TahminHayvani hayvan = hayvanlar[hayvanIndex];

        // Ödül görselini gizle (bir sonraki doğru cevaba kadar)
        if (hayvanGorseli != null)
        {
            hayvanGorseli.sprite = hayvan.hayvanSprite;
            hayvanGorseli.transform.localScale = Vector3.zero; // Zıplama animasyonu 0'dan başlıyor, hedef ölçek DogruTahmin'de ayarlanacak
            hayvanGorseli.transform.SetAsLastSibling(); // Her zaman en önde görünsün (tahta panelin arkasında kalmasın)
        }

        // Yıldızları sıfırla (soluk hale getir)
        YildizlariGuncelle(0);

        // İlk ipucuyu yaz
        string[] ipuclariAktif = IpuclariGetir(hayvan);
        if (ipucuMetniAlani != null)
            ipucuMetniAlani.text = "1. " + ipuclariAktif[0];

        if (sonrakiIpucuButonu != null)
            sonrakiIpucuButonu.interactable = ipuclariAktif.Length > 1;

        // 3 seçenek hazırla: 1 doğru + 2 yanlış (başka hayvanlardan, tekrarsız)
        List<TahminHayvani> secenekler = new List<TahminHayvani> { hayvan };

        List<int> digerIndexler = new List<int>();
        for (int i = 0; i < hayvanlar.Length; i++)
            if (i != hayvanIndex) digerIndexler.Add(i);
        KaristirListe(digerIndexler);

        for (int i = 0; i < digerIndexler.Count && secenekler.Count < 3; i++)
            secenekler.Add(hayvanlar[digerIndexler[i]]);

        KaristirListe(secenekler); // Doğru cevabın yeri de karışsın

        // YENİ: Layout Group YOK. Her seçenek, KENDİ SABİT slot'unun altına yerleştirilir.
        // Boyut = hayvanın gorselOlcek'i * genel çarpan - böylece her hayvan doğal oranını
        // korur (zürafa uzun kalır, panda geniş kalır) ama hepsi aynı yer çizgisinde durur.
        int slotSayisi = Mathf.Min(secenekler.Count, secenekSlotlari != null ? secenekSlotlari.Length : 0);
        for (int i = 0; i < slotSayisi; i++)
        {
            GameObject secenekObje = Instantiate(secenekKartPrefab, secenekSlotlari[i]);
            RectTransform rt = secenekObje.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = Vector2.zero;
            }

            TahminSecenekKarti kart = secenekObje.GetComponent<TahminSecenekKarti>();
            bool dogruMu = (secenekler[i] == hayvan);
            float efektifOlcek = secenekler[i].gorselOlcek * secenekGenelCarpan;
            kart.KartiKur(secenekler[i].hayvanSprite, dogruMu, this, efektifOlcek, secenekler[i].tabanBoslugu);

            aktifSecenekler.Add(new AktifSecenek { kart = kart, hayvan = secenekler[i] });
        }
    }

    // YENİ: secenekGenelCarpan (ya da başka bir Inspector alanı) Play modundayken elle
    // değiştirildiğinde Unity bunu otomatik çağırır. Şu an ekranda duran hayvanlar varsa,
    // onları YENİDEN Instantiate etmeden, sadece boyutlarını anında güncelliyoruz -
    // böylece "değeri değiştir, Play'e gir, bak, çık, tekrar dene" döngüsüne hiç gerek kalmaz,
    // Inspector'da değeri sürüklerken sonucu CANLI görürsünüz.
    void OnValidate()
    {
        if (!Application.isPlaying || aktifSecenekler == null) return;

        foreach (AktifSecenek secenek in aktifSecenekler)
        {
            if (secenek.kart == null || secenek.hayvan == null) continue;
            float efektifOlcek = secenek.hayvan.gorselOlcek * secenekGenelCarpan;
            secenek.kart.OlcegiGuncelle(efektifOlcek, secenek.hayvan.tabanBoslugu);
        }
    }

    // Dil Türkçe ise Türkçe ipuçlarını, İngilizce ise İngilizce ipuçlarını döner.
    // İngilizce dizi boşsa (doldurulmadıysa) Türkçe'ye geri düşer, uygulama çökmez.
    string[] IpuclariGetir(TahminHayvani hayvan)
    {
        if (!MenuYoneticisi.turkceMi && hayvan.ipuclariEN != null && hayvan.ipuclariEN.Length > 0)
            return hayvan.ipuclariEN;
        return hayvan.ipuclari;
    }

    void TemizleSecenekKartlari()
    {
        foreach (AktifSecenek secenek in aktifSecenekler)
        {
            if (secenek.kart != null) Destroy(secenek.kart.gameObject);
        }
        aktifSecenekler.Clear();
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

    // Sıradaki ipucu butonuna bağlanacak
    public void SonrakiIpucuButonunaBasildi()
    {
        int hayvanIndex = hayvanSirasi[aktifIndex];
        TahminHayvani hayvan = hayvanlar[hayvanIndex];
        string[] ipuclariAktif = IpuclariGetir(hayvan);

        if (aktifIpucuIndex >= ipuclariAktif.Length) return;

        aktifIpucuIndex++;
        if (ipucuMetniAlani != null)
            ipucuMetniAlani.text += "\n" + aktifIpucuIndex + ". " + ipuclariAktif[aktifIpucuIndex - 1];

        if (sonrakiIpucuButonu != null)
            sonrakiIpucuButonu.interactable = aktifIpucuIndex < ipuclariAktif.Length;
    }

    // Yanlış bir seçeneğe tıklanınca TahminSecenekKarti bunu çağırır
    public void YanlisTahmin()
    {
        if (MenuYoneticisi.sesEfektleriAcik && yanlisSesi != null && sesKaynagi != null)
            sesKaynagi.PlayOneShot(yanlisSesi);
    }

    // Doğru seçeneğe tıklanınca TahminSecenekKarti bunu çağırır
    public void DogruTahmin()
    {
        int yildiz = aktifIpucuIndex <= 1 ? 3 : (aktifIpucuIndex == 2 ? 2 : 1);
        toplamYildiz += yildiz;

        if (MenuYoneticisi.sesEfektleriAcik && dogruSesi != null && sesKaynagi != null)
            sesKaynagi.PlayOneShot(dogruSesi);

        YildizlariGuncelle(yildiz);
        StartCoroutine(OduluGosterVeDevamEt(yildiz));
    }

    void YildizlariGuncelle(int kazanilanYildiz)
    {
        if (yildizIkonlari == null) return;
        for (int i = 0; i < yildizIkonlari.Length; i++)
        {
            if (yildizIkonlari[i] == null) continue;
            yildizIkonlari[i].color = (i < kazanilanYildiz) ? yildizDoluRengi : yildizBosRengi;
        }
    }

    IEnumerator OduluGosterVeDevamEt(int yildiz)
    {
        // Ödül görselini zıplayarak büyüt - hedef ölçek, o hayvanın kendi gorselOlcek'ine göre
        if (hayvanGorseli != null)
        {
            float hedefOlcek = hayvanlar[hayvanSirasi[aktifIndex]].gorselOlcek;

            float sure = 0.35f;
            float gecen = 0f;
            while (gecen < sure)
            {
                gecen += Time.deltaTime;
                float t = gecen / sure;
                float olcek = Mathf.Lerp(0f, hedefOlcek * 1.15f, t);
                hayvanGorseli.transform.localScale = Vector3.one * olcek;
                yield return null;
            }
            gecen = 0f;
            sure = 0.15f;
            while (gecen < sure)
            {
                gecen += Time.deltaTime;
                float t = gecen / sure;
                hayvanGorseli.transform.localScale = Vector3.one * Mathf.Lerp(hedefOlcek * 1.15f, hedefOlcek, t);
                yield return null;
            }
            hayvanGorseli.transform.localScale = Vector3.one * hedefOlcek;
        }

        if (MenuYoneticisi.sesEfektleriAcik && alkisSesi != null && sesKaynagi != null)
            sesKaynagi.PlayOneShot(alkisSesi);

        yield return new WaitForSeconds(1.4f);

        aktifIndex++;
        if (aktifIndex >= hayvanSirasi.Count)
        {
            StartCoroutine(OyunuBitir());
        }
        else
        {
            SiradakiHayvaniGoster();
        }
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

    IEnumerator OyunuBitir()
    {
        oyunDevamEdiyor = false;

        if (MenuYoneticisi.sesEfektleriAcik && oyunBittiSesi != null && sesKaynagi != null)
            sesKaynagi.PlayOneShot(oyunBittiSesi);

        if (enIyiSure == 0f || gecenSure < enIyiSure)
        {
            enIyiSure = gecenSure;
            PlayerPrefs.SetFloat(RekorAnahtari, enIyiSure);
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

        if (tebriklerYildizYazisi != null)
        {
            int maksYildiz = hayvanlar.Length * 3;
            string yildizMetni = MenuYoneticisi.turkceMi ? "TOPLAM YILDIZ: " : "TOTAL STARS: ";
            tebriklerYildizYazisi.text = yildizMetni + toplamYildiz + " / " + maksYildiz;
        }

        if (tebriklerPaneli != null)
        {
            tebriklerPaneli.SetActive(true);
            tebriklerPaneli.transform.SetAsLastSibling(); // HayvanGorseli her zaman en önde durabiliyor, bunu üstüne alalım
        }

        yield break;
    }

    public void TebriklerTamamButonunaBasildi()
    {
        TemizleSecenekKartlari();

        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(() => {
                if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
                if (tahminOyunuPaneli != null) tahminOyunuPaneli.SetActive(false);
                if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
            });
        }
        else
        {
            if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
            if (tahminOyunuPaneli != null) tahminOyunuPaneli.SetActive(false);
            if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
        }
    }

    public void GeriButonunaBasildi()
    {
        StopAllCoroutines();
        oyunDevamEdiyor = false;
        TemizleSecenekKartlari();

        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(() => {
                if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
                if (tahminOyunuPaneli != null) tahminOyunuPaneli.SetActive(false);
                if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
            });
        }
        else
        {
            if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
            if (tahminOyunuPaneli != null) tahminOyunuPaneli.SetActive(false);
            if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
        }
    }

    public void DuraklatButonunaBasildi()
    {
        PauseController.Instance.Ac(GeriButonunaBasildi);
    }
}