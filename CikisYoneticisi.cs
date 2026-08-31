using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Bu scripti boş bir objeye (örn. Canvas'ın altına "CikisYoneticisi" adıyla) ekle.
// İki işi var:
//  1) Ana menüdeki "Çıkış" butonunun OnClick'ine CikisYap() metodunu bağla.
//  2) Android geri tuşuna (ki Input System'de Escape tuşu olarak gelir) SADECE
//     Ana Menü ekranındayken tepki verir - kısa sürede İKİ KEZ basılırsa oyundan çıkar,
//     tek basışta ekranda kısa bir uyarı yazısı gösterir ("Çıkmak için tekrar basın").
public class CikisYoneticisi : MonoBehaviour
{
    [Header("Ana Menü Kontrolü")]
    [Tooltip("Geri tuşu SADECE bu obje aktifken (yani ana menüdeyken) çıkış mantığını çalıştırır. " +
             "Oyun içindeyken/bir alt menüdeyken geri tuşu bu scripti hiç tetiklemez.")]
    public GameObject anaMenuGrubu;

    [Header("Uyarı Yazısı (opsiyonel)")]
    [Tooltip("Boş bırakabilirsin - atarsan, ilk geri tuşuna basışta kısa süreliğine " +
             "\"Çıkmak için tekrar basın\" yazısı gösterilir.")]
    public TextMeshProUGUI uyariYazisi;
    public float uyariGosterimSuresi = 2f;

    [Tooltip("İki geri tuşu basışı arasında bu süre (saniye) içinde ikinci basış gelirse çıkar.")]
    public float cikisIcinSure = 2f;

    float sonBasmaZamani = -999f;

    void Update()
    {
        // Ana menüde değilsek geri tuşu bu scripti hiç ilgilendirmesin.
        if (anaMenuGrubu == null || !anaMenuGrubu.activeInHierarchy) return;

        // YENİ Input System'de Android'in fiziksel geri tuşu, sanal bir Keyboard
        // cihazında Escape tuşu basışı olarak gelir - bu yüzden Keyboard.current'ı
        // kontrol ediyoruz (Input.GetKeyDown kullanmıyoruz, proje "Input System
        // Package (New)" moduna ayarlı olduğu için o eski API build'de hata verir).
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            GeriTusunaBasildi();
        }
    }

    void GeriTusunaBasildi()
    {
        if (Time.time - sonBasmaZamani <= cikisIcinSure)
        {
            CikisYap();
        }
        else
        {
            sonBasmaZamani = Time.time;
            if (uyariYazisi != null)
            {
                CancelInvoke(nameof(UyariyiGizle));
                uyariYazisi.text = MenuYoneticisi.turkceMi ? "Çıkmak için tekrar basın" : "Press again to exit";
                uyariYazisi.gameObject.SetActive(true);
                Invoke(nameof(UyariyiGizle), uyariGosterimSuresi);
            }
        }
    }

    void UyariyiGizle()
    {
        if (uyariYazisi != null) uyariYazisi.gameObject.SetActive(false);
    }

    // Ana menüdeki "Çıkış" butonunun OnClick'ine bunu bağla.
    public void CikisYap()
    {
#if UNITY_EDITOR
        // Editor'de Application.Quit() hiçbir şey yapmaz - test ederken Play modundan
        // çıkmak için bunu kullanıyoruz.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
