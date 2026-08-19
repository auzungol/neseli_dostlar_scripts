using UnityEngine;

public class MuzikYonetim : MonoBehaviour
{
    private static MuzikYonetim sahnedekiMuzikKutusu;

    void Awake()
    {
        // Eğer sahnede daha önce oluşmuş bir müzik kutusu yoksa, bunu ana müzik kutusu yap
        if (sahnedekiMuzikKutusu == null)
        {
            sahnedekiMuzikKutusu = this;
            
            // Sahne değiştiğinde bu objeyi (MuzikKutusu'nu) yok etme!
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // Eğer sahnede zaten bir müzik kutusu varsa (örneğin ana menüye geri dönüldüyse)
            // sesler üst üste binmesin diye bu sonradan oluşanı yok et
            Destroy(gameObject);
        }
    }
}