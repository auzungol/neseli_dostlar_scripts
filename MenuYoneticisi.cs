using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuYoneticisi : MonoBehaviour
{
    [Header("Oyun Yöneticileri")]
    public HafizaOyunuYoneticisi hafizaYoneticisi;
    
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

    [Header("Dil Değişecek Mod Yazıları (TMP)")]
    public TextMeshProUGUI hafizaYazisi;
    public TextMeshProUGUI quizYazisi;
    public TextMeshProUGUI yapbozYazisi;
    public TextMeshProUGUI yemekYazisi;

    [Header("Dil Bayrak Görselleri")]
    public Image dilButonuGorseli;
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

    public void SecenekleriAc() { seceneklerPaneli.SetActive(true); }
    public void SecenekleriKapat() { seceneklerPaneli.SetActive(false); }

    public void DiliDegistir()
    {
        turkceMi = !turkceMi; 

        if (turkceMi)
        {
            // Türkçe Metinler ve Butonlar
            dilYazisi.text = "DİL";
            muzikYazisi.text = "MÜZİK";
            sesYazisi.text = "SES EFEKTLERİ";

            hafizaYazisi.text = "HAFIZA";
            quizYazisi.text = "BİLMECE";
            yapbozYazisi.text = "YAPBOZ";
            yemekYazisi.text = "BESLEME";

            baslaYazisi.text = "BAŞLA";
            seceneklerYazisi.text = "SEÇENEKLER";

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

            hafizaYazisi.text = "MEMORY CARDS";
            quizYazisi.text = "QUIZ";
            yapbozYazisi.text = "PUZZLE";
            yemekYazisi.text = "MATCHING";

            baslaYazisi.text = "START";
            seceneklerYazisi.text = "OPTIONS";

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