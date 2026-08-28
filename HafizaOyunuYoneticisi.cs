using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshPro kullanmak için bu şart!

public class HafizaOyunuYoneticisi : MonoBehaviour
{
    [Header("Sahne Kurulumu")]
    public GameObject kartPrefab;      
    public Transform kartMasasi;       
    public GameObject tebriklerPaneli;     // YENİ: Bitiş ekranı
    public GameObject hafizaOyunuPaneli;   // YENİ: Çıkışta kapatmak için
    public GameObject oyunSecimGrubu;      // YENİ: Çıkışta açmak için

    [Header("UI Yazıları (TMP)")]
    public TextMeshProUGUI sureYazisi;            // YENİ: Sağ paneldeki süre
    public TextMeshProUGUI enIyiSureYazisi;       // YENİ: Sağ paneldeki rekor
    public TextMeshProUGUI tebriklerSureYazisi;   // YENİ: Bitiş ekranındaki skor
    public TextMeshProUGUI tebriklerBaslikYazisi; // YENİ: "TEBRİKLER!" yazısı
    public TextMeshProUGUI tebriklerButonYazisi;  // YENİ: "DEVAM" butonu yazısı
    [Tooltip("Tebrikler ekranında REKOR yazısı - Yapboz'daki gibi eklendi. Sağdaki enIyiSureYazisi " +
             "ile AYNI formatı kullanır.")]
    public TextMeshProUGUI tebriklerRekorYazisi;

    [Header("Hayvan Listesi (8 Tane Sürükle)")]
    public Sprite[] hayvanGorselleri;

    [Header("Ses Efektleri")]
    public AudioSource sesKaynagi;
    public AudioClip kartAcmaSesi;
    public AudioClip dogruEslesmeSesi;
    public AudioClip yanlisEslesmeSesi;
    public AudioClip oyunBittiSesi; // YENİ: İsteğe bağlı alkış/zafer sesi

    private List<Kart> masadakiKartlar = new List<Kart>();
    private Kart ilkSecilenKart;
    private Kart ikinciSecilenKart;
    private bool kontrolEdiliyor = false;

    // --- YENİ DEĞİŞKENLER ---
    private int eslesenCiftSayisi = 0;
    private float gecenSure = 0f;
    private bool oyunDevamEdiyor = false;
    private float enIyiSure = 0f;

    void Start()
    {
        // Unity hafızasından eski rekoru çek (Eğer hiç rekor yoksa 0 gelir)
        enIyiSure = PlayerPrefs.GetFloat("HafizaEnIyiSure", 0f);
        EnIyiSureyiEkranaYaz();
    }

    void Update()
    {
        // Oyun başladıysa sayacı saniye saniye akıt
        if (oyunDevamEdiyor)
        {
            gecenSure += Time.deltaTime;
            if (sureYazisi != null)
            {
                // YENİ: Dil İngilizce mi Türkçe mi kontrol et! (Türkçe: SN, İngilizce: S)
                string sureKelimesi = MenuYoneticisi.turkceMi ? "SÜRE" : "TIME";
                string saniyeKisaltma = MenuYoneticisi.turkceMi ? " SN" : " S";
                
                sureYazisi.text = sureKelimesi + "\n" + gecenSure.ToString("F1") + saniyeKisaltma;
            }
        }
    }

    // YENİ: Diğer 3 modun "...ModunaGirildi" metotlarıyla TUTARLI tek bir giriş noktası -
    // Ana Menü'deki mod seçim carousel'inin "BAŞLA" butonu bunu çağıracak.
    public void HafizaModunaGirildi()
    {
        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(
                ortadaCagrilacak: () => {
                    if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(false);
                    if (hafizaOyunuPaneli != null) hafizaOyunuPaneli.SetActive(true);
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
            if (hafizaOyunuPaneli != null) hafizaOyunuPaneli.SetActive(true);
            OyunuBaslat();
            oyunDevamEdiyor = true;
        }
    }

    public void OyunuBaslat()
    {
        MasayiTemizle();
        KartlariDagit();
        
        // Değişkenleri sıfırla - DİKKAT: oyunDevamEdiyor artık BURADA true yapılmıyor,
        // 3-2-1-BAŞLA geri sayımı bitene kadar süre saymaya başlamamalı.
        gecenSure = 0f;
        eslesenCiftSayisi = 0;

        // YENİ FIX: Update() henüz çalışmadığı için sureYazisi'nin Inspector'daki varsayılan
        // "New Text" içeriği geri sayım boyunca görünür kalıyordu - Update() ile AYNI formatla
        // burada bir kere elle yazdırıyoruz.
        if (sureYazisi != null)
        {
            string sureKelimesi = MenuYoneticisi.turkceMi ? "SÜRE" : "TIME";
            string saniyeKisaltma = MenuYoneticisi.turkceMi ? " SN" : " S";
            sureYazisi.text = sureKelimesi + "\n" + gecenSure.ToString("F1") + saniyeKisaltma;
        }
        
        if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
        
        enIyiSure = PlayerPrefs.GetFloat("HafizaEnIyiSure", 0f);
        EnIyiSureyiEkranaYaz();
    }

    // Bu fonksiyonu public yaptık ki dil değişince dışarıdan da çağırabilelim!
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

    void MasayiTemizle()
    {
        foreach (Transform child in kartMasasi)
        {
            Destroy(child.gameObject);
        }
        masadakiKartlar.Clear();
        ilkSecilenKart = null;
        ikinciSecilenKart = null;
        kontrolEdiliyor = false;
    }

    void KartlariDagit()
    {
        List<int> kartIDListesi = new List<int>();
        for (int i = 0; i < hayvanGorselleri.Length; i++)
        {
            kartIDListesi.Add(i); 
            kartIDListesi.Add(i); 
        }

        for (int i = 0; i < kartIDListesi.Count; i++)
        {
            int temp = kartIDListesi[i];
            int rastgeleIndex = Random.Range(i, kartIDListesi.Count);
            kartIDListesi[i] = kartIDListesi[rastgeleIndex];
            kartIDListesi[rastgeleIndex] = temp;
        }

        for (int i = 0; i < 16; i++)
        {
            GameObject yeniKartObje = Instantiate(kartPrefab, kartMasasi);
            Kart kartScript = yeniKartObje.GetComponent<Kart>();
            
            int id = kartIDListesi[i];
            kartScript.KartKur(id, hayvanGorselleri[id], this);
            
            kartScript.kartButonu.onClick.AddListener(() => kartScript.KartaTiklandi());
            
            masadakiKartlar.Add(kartScript);
        }
    }

    public bool TiklamaMuzunmu()
    {
        return !kontrolEdiliyor;
    }

    public void KartSecildi(Kart secilenKart)
    {
        if (MenuYoneticisi.sesEfektleriAcik && kartAcmaSesi != null)
            sesKaynagi.PlayOneShot(kartAcmaSesi);

        if (ilkSecilenKart == null)
        {
            ilkSecilenKart = secilenKart;
        }
        else if (ikinciSecilenKart == null && secilenKart != ilkSecilenKart)
        {
            ikinciSecilenKart = secilenKart;
            StartCoroutine(EslesmeyiKontrolEt());
        }
    }

    IEnumerator EslesmeyiKontrolEt()
    {
        kontrolEdiliyor = true; 
        yield return new WaitForSeconds(0.8f); 

        // GÜVENLİK KONTROLÜ: Bekleme sırasında MasayiTemizle() çağrıldıysa
        // (örn. oyuncu duraklatıp Ana Menü'ye döndüyse) kartlar zaten Destroy
        // edilmiş ve referanslar null'lanmış olabilir. Bu durumda sessizce çık,
        // NullReferenceException fırlatma.
        if (ilkSecilenKart == null || ikinciSecilenKart == null)
        {
            kontrolEdiliyor = false;
            yield break;
        }

        if (ilkSecilenKart.kartID == ikinciSecilenKart.kartID)
        {
            // DOĞRU EŞLEŞME!
            if (MenuYoneticisi.sesEfektleriAcik && dogruEslesmeSesi != null)
                sesKaynagi.PlayOneShot(dogruEslesmeSesi);

            ilkSecilenKart.KartiYokEt();
            ikinciSecilenKart.KartiYokEt();

            // YENİ: Eşleşme sayısını artır. 8 çift eşleştiyse oyun bitmiştir!
            eslesenCiftSayisi++;
            if (eslesenCiftSayisi >= 8)
            {
                StartCoroutine(OyunuBitir());
            }
        }
        else
        {
            // YANLIŞ EŞLEŞME!
            if (MenuYoneticisi.sesEfektleriAcik && yanlisEslesmeSesi != null)
                sesKaynagi.PlayOneShot(yanlisEslesmeSesi);

            ilkSecilenKart.KartiKapat();
            ikinciSecilenKart.KartiKapat();
        }

        ilkSecilenKart = null;
        ikinciSecilenKart = null;
        kontrolEdiliyor = false;
    }

    IEnumerator OyunuBitir()
    {
        oyunDevamEdiyor = false; // Sayacı durdur!
        yield return new WaitForSeconds(0.5f); // Son kart yok olsun diye yarım saniye bekle

        if (MenuYoneticisi.sesEfektleriAcik && oyunBittiSesi != null)
            sesKaynagi.PlayOneShot(oyunBittiSesi);

        // Rekor Kontrolü: Daha önce rekor yoksa VEYA bu süre eski rekordan daha kısaysa yeni rekor!
        if (enIyiSure == 0f || gecenSure < enIyiSure)
        {
            enIyiSure = gecenSure;
            PlayerPrefs.SetFloat("HafizaEnIyiSure", enIyiSure); // Telefona/bilgisayara kaydet
            PlayerPrefs.Save();
            EnIyiSureyiEkranaYaz();
        }

        // --- YENİ EKLENEN KISIM: TEBRİKLER EKRANINI DİLE GÖRE ÇEVİR! ---
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

        if (tebriklerPaneli != null)
            tebriklerPaneli.SetActive(true);
    }

    // YENİ: Tebrikler ekranındaki "TAMAM" butonuna bu fonksiyonu bağlayacağız!
    public void TebriklerTamamButonunaBasildi()
    {
        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(() => {
                if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
                if (hafizaOyunuPaneli != null) hafizaOyunuPaneli.SetActive(false);
                if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
            });
        }
        else
        {
            if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
            if (hafizaOyunuPaneli != null) hafizaOyunuPaneli.SetActive(false);
            if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
        }
    }

    // --- YENİ: Duraklatma menüsündeki "Ana Menü'ye Dön" butonunun çağırdığı metod ---
    public void GeriButonunaBasildi()
    {
        // KRİTİK: Arka planda bekleyen EslesmeyiKontrolEt/OyunuBitir coroutine'leri
        // varsa, MasayiTemizle() kartları Destroy ettikten SONRA bunlar uyanıp
        // yok olmuş kart referanslarına erişmeye çalışıp NullReferenceException
        // fırlatabiliyordu. Önce coroutine'leri durdurup sonra temizliyoruz.
        StopAllCoroutines();
        oyunDevamEdiyor = false;

        if (GecisYoneticisi.Instance != null)
        {
            GecisYoneticisi.Instance.GecisYap(() => {
                MasayiTemizle();
                if (hafizaOyunuPaneli != null) hafizaOyunuPaneli.SetActive(false);
                if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
            });
        }
        else
        {
            MasayiTemizle();
            if (hafizaOyunuPaneli != null) hafizaOyunuPaneli.SetActive(false);
            if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
        }
    }

    public void DuraklatButonunaBasildi()
    {
        // FIX: Duraklatınca oyunDevamEdiyor'u da false yapıyoruz - önceden sadece
        // Time.timeScale=0 ile duruyordu, "mantıksal olarak" hâlâ true kalıyordu.
        // Yeniden Başlat sırasında bulutlar kapanana kadarki kısa pencerede Update()
        // eski süre değerini artırmaya devam ediyordu, bu bug'ı çözer.
        oyunDevamEdiyor = false;
        PauseController.Instance.Ac(GeriButonunaBasildi, HafizaModunaGirildi);
    }
}