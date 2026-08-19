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
                 "aynı görseller kullanıldığı için aynı kalibrasyon geçerli olacaktır.")]
        public float gorselOlcek = 1f;

        [Tooltip("Cevap kartındaki (küçük) hayvan görselinin, kart kutusunun ne kadarını dolduracağı - " +
                 "1 = kutuyu tam kullan, 0.7 = biraz daha küçük göster. 0.1-1 arasında sıkıştırılır, " +
                 "asla kutudan taşamaz. 'gorselOlcek' ile KARIŞTIRMA, o ödül görseli (büyük, " +
                 "TahminYoneticisi.hayvanGorseli) için ayrı bir alan. Bu alan SecenekKarti prefabındaki " +
                 "HayvanGorseli kutusu referans alınarak hesaplanmalı (TahminGorselKalibratoru'nda " +
                 "'Kart İçin mi?' kutusunu işaretleyip kaydet).")]
        public float secenekGorselOlcek = 1f;

        [TextArea]
        [Tooltip("Sırayla verilecek ipuçları (TÜRKÇE). 3-4 tane yeterli. Örn: 'Bataklıkta yaşarım', '4 ayaklı sürüngenim'...")]
        public string[] ipuclari;

        [TextArea]
        [Tooltip("Aynı ipuçlarının İNGİLİZCE hali, AYNI SIRADA. Dil İngilizce'ye çevrilince bunlar gösterilir.")]
        public string[] ipuclariEN;
    }

    [Header("Sahne Kurulumu")]
    public GameObject secenekKartPrefab;      // Image + TahminSecenekKarti.cs olan prefab
    public RectTransform secenekGrubu;         // Horizontal Layout Group'lu, 3 kartı barındıran container
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
        if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(false);
        if (tahminOyunuPaneli != null) tahminOyunuPaneli.SetActive(true);
        if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);

        enIyiSure = PlayerPrefs.GetFloat(RekorAnahtari, 0f);
        EnIyiSureyiEkranaYaz();

        OyunuBaslat();
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

        // 3 seçenek hazırla: 1 doğru + 2 yanlış (başka hayvanlardan, tekrarsız) - kendi ölçekleriyle birlikte
        List<TahminHayvani> secenekler = new List<TahminHayvani> { hayvan };

        List<int> digerIndexler = new List<int>();
        for (int i = 0; i < hayvanlar.Length; i++)
            if (i != hayvanIndex) digerIndexler.Add(i);
        KaristirListe(digerIndexler);

        for (int i = 0; i < digerIndexler.Count && secenekler.Count < 3; i++)
            secenekler.Add(hayvanlar[digerIndexler[i]]);

        KaristirListe(secenekler); // Doğru cevabın yeri de karışsın

        // Kartlar HEP EŞİT sabit boyutta (Layout Element'te elle ayarladığın Preferred Width/Height) -
        // ama sprite'ların kendi içindeki boşluk/oran farklı olduğu için görsel eşitliği
        // her hayvanın kendi gorselOlcek kalibrasyonu sağlıyor.
        for (int i = 0; i < secenekler.Count; i++)
        {
            GameObject kartObje = Instantiate(secenekKartPrefab, secenekGrubu);
            TahminSecenekKarti kart = kartObje.GetComponent<TahminSecenekKarti>();
            bool dogruMu = (secenekler[i] == hayvan);
            kart.KartiKur(secenekler[i].hayvanSprite, dogruMu, this, secenekler[i].secenekGorselOlcek);
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
        if (secenekGrubu == null) return;
        for (int i = secenekGrubu.childCount - 1; i >= 0; i--)
            Destroy(secenekGrubu.GetChild(i).gameObject);
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

        if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
        if (tahminOyunuPaneli != null) tahminOyunuPaneli.SetActive(false);
        if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
    }

    public void GeriButonunaBasildi()
    {
        StopAllCoroutines();
        oyunDevamEdiyor = false;
        TemizleSecenekKartlari();

        if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
        if (tahminOyunuPaneli != null) tahminOyunuPaneli.SetActive(false);
        if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
    }
}