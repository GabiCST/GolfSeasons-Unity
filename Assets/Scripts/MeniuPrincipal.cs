using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class MeniuPrincipal : MonoBehaviour
{
    public UIDocument meniuPrincipalDoc; 
    public GameObject meniuStart;
    public GameObject meniuSezoane;
    public GameObject meniuScoreboard;

    private VisualElement root;

    public class DateSalvare
    {
        public int id_salvare;
        public string nume_salvare;
        public string data_creare;
        public int sezon_maxim_deblocat;
    }

    private void OnEnable()
    {
        if (meniuPrincipalDoc != null)
        {
            root = meniuPrincipalDoc.rootVisualElement;
            SeteazaButonInapoi();
            IncarcaSalvariInUI();
        }
    }

    private void SeteazaButonInapoi()
    {
        Button btnInapoi = root.Q<Button>("btnInapoi");
        if (btnInapoi != null)
        {
            btnInapoi.clicked -= InapoiLaStart; 
            btnInapoi.clicked += InapoiLaStart;
        }
    }

    private void InapoiLaStart()
    {
        if (meniuStart != null) meniuStart.SetActive(true);
        gameObject.SetActive(false);
    }

    private void IncarcaSalvariInUI()
    {
        List<DateSalvare> salvariDinDB = new();
        if(DatabaseManager.instance != null)
        {
            salvariDinDB = DatabaseManager.instance.ExtrageSalvari();
        }

        for(int i = 1; i <= 3; i++)
        {
            int numarSlot = i;
            VisualElement slot = root.Q<VisualElement>("Salvare" + i);
            if (slot == null) continue;

            Label numeLabel = slot.Q<Label>("NumeSalvare");
            Label detaliiLabel = slot.Q<Label>("Detalii");
            Button playBtn = slot.Q<Button>("Play");
            Button scoreBtn = slot.Q<Button>("Scoreboard");
            Button stergeBtn = slot.Q<Button>("Sterge");

            playBtn.clicked -= delegate { }; 
            scoreBtn.clicked -= delegate { };
            stergeBtn.clicked -= delegate { };

            if(i - 1 < salvariDinDB.Count)
            {
                DateSalvare salvareCurenta = salvariDinDB[i - 1];
                numeLabel.text = salvareCurenta.nume_salvare;
                detaliiLabel.style.display = DisplayStyle.Flex;
                detaliiLabel.text = "Data: " + salvareCurenta.data_creare + "\nSezoane: " + salvareCurenta.sezon_maxim_deblocat + "/4";
                
                playBtn.text = "Alege sezon";
                scoreBtn.style.display = DisplayStyle.Flex;
                stergeBtn.style.display = DisplayStyle.Flex;

                playBtn.clicked += () => DeschideSezoane(salvareCurenta.id_salvare, salvareCurenta.sezon_maxim_deblocat);
                scoreBtn.clicked += () => DeschideScoreboard(salvareCurenta.id_salvare);
                stergeBtn.clicked += () => StergeSalvare(salvareCurenta.id_salvare);
            }
            else
            {
                numeLabel.text = "Joc nou";
                detaliiLabel.style.display = DisplayStyle.None;
                playBtn.text = "Start";
                scoreBtn.style.display = DisplayStyle.None;
                stergeBtn.style.display = DisplayStyle.None;

                playBtn.clicked += () => CreeazaJocNou(numarSlot);
            }
        }
    }

    private void DeschideSezoane(int idSalvare, int sezonMaxim)
    {
        if (meniuSezoane != null)
        {
            meniuSezoane.SetActive(true);
            meniuSezoane.GetComponent<MeniuSezoane>().SeteazaDate(idSalvare, sezonMaxim);
            gameObject.SetActive(false);
        }
    }

    private void DeschideScoreboard(int idSalvare)
    {
        if (meniuScoreboard != null)
        {
            meniuScoreboard.SetActive(true);
            meniuScoreboard.GetComponent<MeniuScoreboard>().IncarcaScoreboard(idSalvare);
            gameObject.SetActive(false);
        }
    }

    private void StergeSalvare(int id)
    {
        if (DatabaseManager.instance != null)
        {
            DatabaseManager.instance.StergeSalvare(id);
            IncarcaSalvariInUI();
        }
    }

    private void CreeazaJocNou(int numarSlot)
    {
        int idNou = 99;
        if (DatabaseManager.instance != null) 
            idNou = DatabaseManager.instance.CreeazaSalvare("Joc " + numarSlot);
        
        PlayerPrefs.SetString("SezonSelectat", "Primavara");
        PlayerPrefs.SetInt("SalvareCurenta", idNou);
        PlayerPrefs.SetInt("TraseuStart", 1);
        PlayerPrefs.Save();
        
        Global.dinMeniu = true;
        Global.antrenament = false;
        UnityEngine.SceneManagement.SceneManager.LoadScene("GamePlay");
    }
}