using System.Collections;
using System.Collections.Generic;
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

    [Header("UI Yazıları (TMP)")]
    public TextMeshProUGUI ilerlemeYazisi;        // "3 / 8" gibi, opsiyonel
    public TextMeshProUGUI tebriklerBaslikYazisi;
    public TextMeshProUGUI tebriklerButonYazisi;

    [Header("Hayvan-Yemek Eşleşmeleri (8 Tane)")]
    public HayvanYemekEslesmesi[] eslesmeler;

    [Header("Ses Efektleri")]
    public AudioSource sesKaynagi;
    public AudioClip dogruSesi;
    public AudioClip yanlisSesi;
    public AudioClip oyunBittiSesi;

    private List<int> hayvanSirasi = new List<int>();
    private int aktifIndex;

    public void EslesmeModunaGirildi()
    {
        TemizleYemekKartlari();

        if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(false);
        if (eslesmeOyunuPaneli != null) eslesmeOyunuPaneli.SetActive(true);
        if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);

        OyunuBaslat();
    }

    void OyunuBaslat()
    {
        hayvanSirasi.Clear();
        for (int i = 0; i < eslesmeler.Length; i++) hayvanSirasi.Add(i);
        KaristirListe(hayvanSirasi);

        aktifIndex = 0;
        SiradakiHayvaniGoster();
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
        }

        if (ilerlemeYazisi != null)
            ilerlemeYazisi.text = (aktifIndex + 1) + " / " + eslesmeler.Length;

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

    // Doğru yemek hedefe bırakıldığında EslesmeYemekKarti bunu çağırır
    public void DogruEslesme()
    {
        if (MenuYoneticisi.sesEfektleriAcik && dogruSesi != null && sesKaynagi != null)
            sesKaynagi.PlayOneShot(dogruSesi);

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

    // Yanlış yemek hedefe bırakıldığında EslesmeYemekKarti bunu çağırır
    public void YanlisEslesme()
    {
        if (MenuYoneticisi.sesEfektleriAcik && yanlisSesi != null && sesKaynagi != null)
            sesKaynagi.PlayOneShot(yanlisSesi);
    }

    IEnumerator OyunuBitir()
    {
        yield return new WaitForSeconds(0.4f);

        if (MenuYoneticisi.sesEfektleriAcik && oyunBittiSesi != null && sesKaynagi != null)
            sesKaynagi.PlayOneShot(oyunBittiSesi);

        if (tebriklerBaslikYazisi != null)
            tebriklerBaslikYazisi.text = MenuYoneticisi.turkceMi ? "TEBRİKLER!" : "CONGRATULATIONS!";

        if (tebriklerButonYazisi != null)
            tebriklerButonYazisi.text = MenuYoneticisi.turkceMi ? "DEVAM" : "CONTINUE";

        if (tebriklerPaneli != null) tebriklerPaneli.SetActive(true);
    }

    public void TebriklerTamamButonunaBasildi()
    {
        TemizleYemekKartlari();

        if (tebriklerPaneli != null) tebriklerPaneli.SetActive(false);
        if (eslesmeOyunuPaneli != null) eslesmeOyunuPaneli.SetActive(false);
        if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
    }

    public void GeriButonunaBasildi()
    {
        TemizleYemekKartlari();

        if (eslesmeOyunuPaneli != null) eslesmeOyunuPaneli.SetActive(false);
        if (oyunSecimGrubu != null) oyunSecimGrubu.SetActive(true);
    }
}