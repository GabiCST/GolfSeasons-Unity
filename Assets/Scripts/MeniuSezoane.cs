using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MeniuSezoane : MonoBehaviour
{
    public UIDocument sezoaneDoc;
    public GameObject meniuPrincipal; 

    private VisualElement root;
    private int idSalvareCurenta;
    private string[] numeSezoane = { "Primavara", "Vara", "Toamna", "Iarna" };

    private void OnEnable()
    {
        if (sezoaneDoc != null) root = sezoaneDoc.rootVisualElement;

        Button btnInapoi = root.Q<Button>("Inapoi");
        if (btnInapoi != null)
        {
            btnInapoi.clicked -= InapoiLaMeniuPrincipal;
            btnInapoi.clicked += InapoiLaMeniuPrincipal;
        }
    }

    public void SeteazaDate(int idSalvare, int sezonMaximDeblocat)
    {
        idSalvareCurenta = idSalvare;

        if (root == null && sezoaneDoc != null) root = sezoaneDoc.rootVisualElement;

        for (int i = 0; i < numeSezoane.Length; i++)
        {
            string sezon = numeSezoane[i];
            VisualElement cardSezon = root.Q<VisualElement>(sezon);
            
            if (cardSezon != null)
            {
                Button btnJoaca = cardSezon.Q<Button>("Joaca");
                if (btnJoaca != null)
                {
                    btnJoaca.clicked -= delegate { }; 

                    if (i < sezonMaximDeblocat)
                    {
                        btnJoaca.SetEnabled(true);
                        btnJoaca.text = "Joaca";
                        btnJoaca.clicked += () => IncarcaNivel(sezon);
                    }
                    else
                    {
                        btnJoaca.SetEnabled(false);
                        btnJoaca.text = "Blocat";
                    }
                }
            }
        }
    }

    private void IncarcaNivel(string sezonSelectat)
    {
        PlayerPrefs.SetString("SezonSelectat", sezonSelectat);
        PlayerPrefs.SetInt("SalvareCurenta", idSalvareCurenta);
        PlayerPrefs.SetInt("TraseuStart", 1);
        PlayerPrefs.Save();
        
        Global.dinMeniu = true;
        SceneManager.LoadScene("GamePlay");
    }

    private void InapoiLaMeniuPrincipal()
    {
        if (meniuPrincipal != null) meniuPrincipal.SetActive(true);
        gameObject.SetActive(false);
    }
}