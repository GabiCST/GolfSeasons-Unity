using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class MeniuScoreboard : MonoBehaviour
{
    public UIDocument scoreboardDoc;
    public GameObject meniuPrincipal; 

    private VisualElement root;

    private void OnEnable()
    {
        if (scoreboardDoc != null) root = scoreboardDoc.rootVisualElement;

        Button btnInapoi = root.Q<Button>("Inapoi");
        if (btnInapoi != null)
        {
            btnInapoi.clicked -= InapoiLaMeniuPrincipal;
            btnInapoi.clicked += InapoiLaMeniuPrincipal;
        }
    }

    public void IncarcaScoreboard(int idSalvare)
    {
        if (root == null && scoreboardDoc != null) root = scoreboardDoc.rootVisualElement;

        string[] toateSezoanele = { "Primavara", "Vara", "Toamna", "Iarna" };
        foreach (string sezon in toateSezoanele)
        {
            VisualElement randAnotimp = root.Q<VisualElement>("Rand_" + sezon);
            if (randAnotimp != null)
            {
                var toateRandurile = randAnotimp.Children().ToList();

                if (toateRandurile.Count >= 4 && DatabaseManager.instance != null)
                {
                    VisualElement randParLovituri = toateRandurile[0];
                    VisualElement randScorLovituri = toateRandurile[1];
                    VisualElement randParTimp = toateRandurile[2];
                    VisualElement randScorTimp = toateRandurile[3];

                    List<DatabaseManager.DateScoreBoard> dateBD = DatabaseManager.instance.ExtrageDateScoreBoard(sezon, idSalvare);
                    
                    int totalParLovituri = 0, totalScorLovituri = 0;
                    int totalParTimp = 0, totalScorTimp = 0;

                    for (int i = 0; i < dateBD.Count; i++)
                    {
                        int numarNivel = i + 1;
                        Label lblParLov = randParLovituri.Q<Label>("Nivel" + numarNivel);
                        if (lblParLov != null) { lblParLov.text = dateBD[i].par.ToString(); totalParLovituri += dateBD[i].par; }
                        
                        Label lblScorLov = randScorLovituri.Q<Label>("Nivel" + numarNivel);
                        if (lblScorLov != null) { lblScorLov.text = dateBD[i].scor_best > 0 ? dateBD[i].scor_best.ToString() : "-"; if (dateBD[i].scor_best > 0) totalScorLovituri += dateBD[i].scor_best; }

                        Label lblParTimp = randParTimp.Q<Label>("Nivel" + numarNivel);
                        if (lblParTimp != null) { lblParTimp.text = dateBD[i].timp_par.ToString(); totalParTimp += dateBD[i].timp_par; }
                        
                        Label lblScorTimp = randScorTimp.Q<Label>("Nivel" + numarNivel);
                        if (lblScorTimp != null) { lblScorTimp.text = dateBD[i].timp_best > 0 ? dateBD[i].timp_best.ToString() : "-"; if (dateBD[i].timp_best > 0) totalScorTimp += dateBD[i].timp_best; }
                    }

                    Label lblTotalParlov = randParLovituri.Q<Label>("Lovituri");
                    if (lblTotalParlov != null) lblTotalParlov.text = totalParLovituri.ToString();
                    
                    Label lblScorParlov = randScorLovituri.Q<Label>("Lovituri");
                    if (lblScorParlov != null) lblScorParlov.text = totalScorLovituri > 0 ? totalScorLovituri.ToString() : "-";

                    Label lblTotalParTimp = randParTimp.Q<Label>("Total");
                    if (lblTotalParTimp != null) lblTotalParTimp.text = totalParTimp.ToString();
                    
                    Label lblTotalScorTimp = randScorTimp.Q<Label>("Total");
                    if (lblTotalScorTimp != null) lblTotalScorTimp.text = totalScorTimp > 0 ? totalScorTimp.ToString() + "s" : "-";
                }
            }
        }
    }

    private void InapoiLaMeniuPrincipal()
    {
        if (meniuPrincipal != null) meniuPrincipal.SetActive(true);
        gameObject.SetActive(false);
    }
}