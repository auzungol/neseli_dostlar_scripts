using UnityEngine;
using TMPro;

public class PauseController : MonoBehaviour
{
    public static PauseController Instance;

    [Header("Paneller")]
    public GameObject pauseMenuPanel;      // PauseMenuPanel'in kendisi
    public GameObject seceneklerPaneli;    // MenuYoneticisi'ndeki AYNI Ayarlar paneli
    public MenuYoneticisi menuYoneticisi;  // YENİ: Ayarlar açılırken dil butonunu kilitlemek için

    [Header("Dil Değişecek Yazılar (TMP)")]
    [Tooltip("\"OYUN DURAKLATILDI\" başlığı")]
    public TextMeshProUGUI baslikYazisi;
    [Tooltip("\"DEVAM ET\" buton yazısı")]
    public TextMeshProUGUI devamEtYazisi;
    [Tooltip("\"AYARLAR\" buton yazısı")]
    public TextMeshProUGUI ayarlarYazisi;
    [Tooltip("\"ANA MENÜ\" buton yazısı")]
    public TextMeshProUGUI anaMenuYazisi;

    private System.Action anaMenuAksiyonu; // O an hangi modun "Ana Menü'ye Dön" mantığı çalışacak

    void Awake()
    {
        Instance = this;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

        // Sahne başlarken mevcut dile göre yazıları hazırla (henüz DiliDegistir hiç
        // çağrılmamış olsa bile panel açıldığında doğru dilde görünsün diye).
        DiliGuncelle();
    }

    // Her mod, kendi GeriButonunaBasildi/OyunSecimineDon metodunu buraya "callback" olarak yollar
    public void Ac(System.Action modunGeriDonusu)
    {
        anaMenuAksiyonu = modunGeriDonusu;
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            pauseMenuPanel.transform.SetAsLastSibling(); // her zaman en önde görünsün
        }
        Time.timeScale = 0f;
    }

    public void DevamEtButonunaBasildi()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    // Sağ üstteki X butonu da Devam Et ile birebir aynı işi yapıyor
    public void KapatButonunaBasildi()
    {
        DevamEtButonunaBasildi();
    }

    public void AyarlarButonunaBasildi()
    {
        // YENİ: Oyun içindeyken dil değiştirilirse mevcut turdaki metinler (ipuçları vb.)
        // hemen güncellenmiyor, bir sonraki tura kadar eski dilde kalıyordu - kafa
        // karıştırıcıydı. Kökten çözmek yerine, oyun içi duraklatma menüsünden Ayarlar
        // açılınca dil butonunu tamamen kilitleyip soluklaştırıyoruz.
        if (menuYoneticisi != null)
            menuYoneticisi.DilButonunuKilitle(true);

        if (seceneklerPaneli != null)
        {
            seceneklerPaneli.SetActive(true);
            seceneklerPaneli.transform.SetAsLastSibling();
        }
    }

    public void AnaMenuButonunaBasildi()
    {
        Time.timeScale = 1f; // sahne/panel değişmeden önce MUTLAKA resetlenmeli
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        anaMenuAksiyonu?.Invoke(); // o modun kendi geri dönüş mantığını çalıştırır
    }

    // --- YENİ: MenuYoneticisi.DiliDegistir() bunu çağırır, dil değişince
    //           duraklatma menüsündeki tüm yazılar anında güncellensin diye.
    //           Panel o an kapalı (inactive) olsa bile .text ataması çalışır,
    //           bir sonraki açılışta doğru dilde görünür. ---
    public void DiliGuncelle()
    {
        if (MenuYoneticisi.turkceMi)
        {
            if (baslikYazisi != null) baslikYazisi.text = "OYUN DURAKLATILDI";
            if (devamEtYazisi != null) devamEtYazisi.text = "DEVAM ET";
            if (ayarlarYazisi != null) ayarlarYazisi.text = "AYARLAR";
            if (anaMenuYazisi != null) anaMenuYazisi.text = "ANA MENÜ";
        }
        else
        {
            if (baslikYazisi != null) baslikYazisi.text = "GAME PAUSED";
            if (devamEtYazisi != null) devamEtYazisi.text = "RESUME";
            if (ayarlarYazisi != null) ayarlarYazisi.text = "SETTINGS";
            if (anaMenuYazisi != null) anaMenuYazisi.text = "MAIN MENU";
        }
    }
}