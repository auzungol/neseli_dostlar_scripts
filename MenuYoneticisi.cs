using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuYoneticisi : MonoBehaviour
{
    [Header("Oyun Yöneticileri")]
    public HafizaOyunuYoneticisi hafizaYoneticisi;
    public YapbozYoneticisi yapbozYoneticisi;   // YENİ: REKOR yazısını dil değişince güncellemek için
    public TahminYoneticisi tahminYoneticisi;   // YENİ: REKOR yazısını dil değişince güncellemek için
    public PauseController pauseController;     // YENİ: Duraklatma menüsü yazılarını dil değişince güncellemek için
    
    [Header("Grup ve Paneller")]
    public GameObject anaMenuGrubu;
    public GameObject oyunSecimGrubu;
    public GameObject seceneklerPaneli;

    [Header("Dil Değişecek Menü Yazıları (TMP)")]
    public TextMeshProUGUI dilYazisi;
    public TextMeshProUGUI muzikYazisi;
    public TextMeshProUGUI sesYazisi;
    public TextMeshProUGUI baslaYazisi;
    public TextMeshProUGUI seceneklerYazisi;
    public TextMeshProUGUI ayarlarYazisi;


    [Header("Dil Değişecek Mod Yazıları (TMP)")]
    public TextMeshProUGUI hafizaYazisi;
    public TextMeshProUGUI quizYazisi;
    public TextMeshProUGUI yapbozYazisi;
    public TextMeshProUGUI yemekYazisi;

    [Header("Dil Değişecek İpucu Buton Yazıları (TMP)")]
    [Tooltip("Yapboz modundaki İPUCU butonunun İÇİNDEKİ TMP Text objesi")]
    public TextMeshProUGUI ipucuButonuYazisi;
    [Tooltip("Bilmece modundaki SONRAKİ İPUCU butonunun İÇİNDEKİ TMP Text objesi")]
    public TextMeshProUGUI sonrakiIpucuButonuYazisi;
    [Tooltip("Oyun seçim ekranındaki mod carousel'inin SEÇ butonunun İÇİNDEKİ TMP Text objesi")]
    public TextMeshProUGUI modSecYazisi;
    [Tooltip("Yapboz'daki hayvan seçim carousel'inin SEÇ butonunun İÇİNDEKİ TMP Text objesi")]
    public TextMeshProUGUI yapbozSecYazisi;

    [Header("Dil Bayrak Görselleri")]
    public Image dilButonuGorseli;
    [Tooltip("Dil butonunun Button component'i - oyun içi duraklatma menüsünden Ayarlar açıldığında " +
             "bunu kilitleyip soluklaştırıyoruz (dil o an değiştirilirse ekrandaki yazılar yarım " +
             "kalabiliyor, sonraki tura kadar güncellenmiyor - bu yüzden mod içindeyken dil " +
             "değiştirilmesini tamamen engelliyoruz).")]
    public Button dilButonu;
    public Sprite turkceBayrak;
    public Sprite ingilizceBayrak;

    // ----- YENİ EKLENEN KISIM: LOGO GÖRSELİ -----
    [Header("Oyun Logosu Görselleri")]
    public Image logoGorseli;
    public Sprite logoTR_Sprite;
    public Sprite logoEN_Sprite;
    // --------------------------------------------

    [Header("Müzik ve Ses Efekti Görselleri")]
    public Image muzikButonuGorseli;
    public Sprite muzikAcikSprite;
    public Sprite muzikKapaliSprite;
    public Image sesButonuGorseli;
    public Sprite sesAcikSprite;
    public Sprite sesKapaliSprite;

    private bool muzikAcik = true;
    public static bool sesEfektleriAcik = true; 
    
    // DİKKAT: Bunu public static yaptık ki Hafıza Oyunu da bu dil ayarını okuyabilsin!
    public static bool turkceMi = true; 

    void Start()
    {
        anaMenuGrubu.SetActive(true);
        oyunSecimGrubu.SetActive(false);
        seceneklerPaneli.SetActive(false);
    }

    public void BaslaButonunaBasildi()
    {
        anaMenuGrubu.SetActive(false);  
        oyunSecimGrubu.SetActive(true);  
    }

    public void ModEkranindanGeriDon()
    {
        oyunSecimGrubu.SetActive(false); 
        anaMenuGrubu.SetActive(true);    
    }

    public void SecenekleriAc()
    {
        // Ana menüden normal açılışta dil butonu HER ZAMAN aktif olmalı -
        // bir önceki açılış duraklatma menüsünden olup kilitlenmiş olabilir, sıfırlıyoruz.
        DilButonunuKilitle(false);
        seceneklerPaneli.SetActive(true);
    }
    public void SecenekleriKapat() { seceneklerPaneli.SetActive(false); }

    // YENİ: Oyun içi duraklatma menüsünden Ayarlar açılırken PauseController bunu çağırır.
    // kilitli=true -> buton tıklanamaz + yarı saydam. kilitli=false -> normal, tam opak.
    public void DilButonunuKilitle(bool kilitli)
    {
        if (dilButonu != null)
            dilButonu.interactable = !kilitli;

        if (dilButonuGorseli != null)
        {
            Color renk = dilButonuGorseli.color;
            renk.a = kilitli ? 0.4f : 1f;
            dilButonuGorseli.color = renk;
        }

        if (dilYazisi != null)
        {
            Color renk = dilYazisi.color;
            renk.a = kilitli ? 0.4f : 1f;
            dilYazisi.color = renk;
        }
    }

    public void DiliDegistir()
    {
        turkceMi = !turkceMi; 

        if (turkceMi)
        {
            // Türkçe Metinler ve Butonlar
            dilYazisi.text = "DİL";
            muzikYazisi.text = "MÜZİK";
            sesYazisi.text = "SES EFEKTLERİ";
            ayarlarYazisi.text = "AYARLAR";

            hafizaYazisi.text = "HAFIZA";
            quizYazisi.text = "BİLMECE";
            yapbozYazisi.text = "YAPBOZ";
            yemekYazisi.text = "BESLEME";

            baslaYazisi.text = "BAŞLA";
            seceneklerYazisi.text = "SEÇENEKLER";

            if (ipucuButonuYazisi != null) ipucuButonuYazisi.text = "İPUCU";
            if (sonrakiIpucuButonuYazisi != null) sonrakiIpucuButonuYazisi.text = "SONRAKİ İPUCU";
            if (modSecYazisi != null) modSecYazisi.text = "SEÇ";
            if (yapbozSecYazisi != null) yapbozSecYazisi.text = "SEÇ";

            dilButonuGorseli.sprite = turkceBayrak;

            // Logoyu Türkçe Yap!
            logoGorseli.sprite = logoTR_Sprite;
        }
        else
        {
            // İngilizce Metinler ve Butonlar
            dilYazisi.text = "LANGUAGE";
            muzikYazisi.text = "MUSIC";
            sesYazisi.text = "SOUND FX";
            ayarlarYazisi.text = "OPTIONS"; 

            hafizaYazisi.text = "MEMORY CARDS";
            quizYazisi.text = "QUIZ";
            yapbozYazisi.text = "PUZZLE";
            yemekYazisi.text = "MATCHING";

            baslaYazisi.text = "START";
            seceneklerYazisi.text = "OPTIONS";

            if (ipucuButonuYazisi != null) ipucuButonuYazisi.text = "HINT";
            if (sonrakiIpucuButonuYazisi != null) sonrakiIpucuButonuYazisi.text = "NEXT HINT";
            if (modSecYazisi != null) modSecYazisi.text = "SELECT";
            if (yapbozSecYazisi != null) yapbozSecYazisi.text = "SELECT";

            dilButonuGorseli.sprite = ingilizceBayrak;

            // Logoyu İngilizce Yap!
            logoGorseli.sprite = logoEN_Sprite;
        }

        // DİKKAT: Bunu if-else bloğunun dışına, en aşağıya aldık! 
        // Böylece dil hem Türkçe hem İngilizce olduğunda tabelayı anında güncelleyecek.
        if (hafizaYoneticisi != null)
        {
            hafizaYoneticisi.EnIyiSureyiEkranaYaz();
        }

        // YENİ: Yapboz ve Bilmece'nin REKOR yazıları da aynı şekilde anında güncellensin -
        // önceden sadece hafizaYoneticisi çağrılıyordu, oyun ortasında dil değiştirilince
        // bu ikisinin REKOR yazısı bir sonraki oyuna kadar eski dilde takılı kalıyordu.
        if (yapbozYoneticisi != null)
        {
            yapbozYoneticisi.EnIyiSureyiEkranaYaz();
        }
        if (tahminYoneticisi != null)
        {
            tahminYoneticisi.EnIyiSureyiEkranaYaz();
        }

        // YENİ: Duraklatma menüsündeki tüm yazılar (OYUN DURAKLATILDI, DEVAM ET,
        // AYARLAR, ANA MENÜ) da anında güncellensin.
        if (pauseController != null)
        {
            pauseController.DiliGuncelle();
        }
    }

    public void MuzigiAcKapat()
    {
        muzikAcik = !muzikAcik;
        muzikButonuGorseli.sprite = muzikAcik ? muzikAcikSprite : muzikKapaliSprite;
        GameObject muzikObjesi = GameObject.Find("MuzikKutusu");
        if (muzikObjesi != null) { muzikObjesi.GetComponent<AudioSource>().mute = !muzikAcik; }
    }

    public void SesEfektleriniAcKapat()
    {
        sesEfektleriAcik = !sesEfektleriAcik;
        sesButonuGorseli.sprite = sesEfektleriAcik ? sesAcikSprite : sesKapaliSprite;
    }
}