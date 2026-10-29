using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Linq;

public class UI : MonoBehaviour
{
    public float timpCurent = 0f;
    public AgentG agent;
    public LogicaLovitura logicaAi;
    public bool aAjunsLaFinal = false;
    public GestiuneMapa managerHarta;
    public TriggerGaura scriptHoleTrigger;
    public Bila bila;

    public TextMeshProUGUI labelLovituri, labelSecunde, labelMesaj, labelLovituriPar, labelSecundePar;

    public UIDocument scoreboardDoc;

    public int incercariCurente = 0, maxIncercari = 4, numarLovituri = 0;
    public bool aAtinsOutOfBoundsAcestFrame = false, nivelFinalizat = false, nivelEsuat = false;
    public string ultimuPereteLovit = "Niciunul";
    private bool cronometruPornit = true, isScoreboardVisible = false;

    private int idSalvareCurenta, traseuCurentIndex = 0, limitaLovituriNivel, lovituriPrecedente, idNivelCurentDB;

    private float timpDeLaUltimaLovitura = 1f, limitaTimpNivel;
    private VisualElement scoreboardRoot;
    private string sezonCurent;
    private DateTraseu traseuCurentObiect;
    private readonly WaitForSeconds waitMesajAI = new(1), waitMesajJucator = new(4f), waitResetareEsec = new(2f);

    private void Start()
    {
        ActualizeazaText();
        if (labelMesaj != null) labelMesaj.text = "";
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
        if (UtilitareBD.EsteModAntrenament || (!Global.dinMeniu && Application.isEditor))
        {
            sezonCurent = "Primavara";
            idSalvareCurenta = System.Diagnostics.Process.GetCurrentProcess().Id;
            traseuCurentIndex = 0;
        }
        else
        {
            sezonCurent = PlayerPrefs.GetString("SezonSelectat", "Primavara");
            idSalvareCurenta = PlayerPrefs.GetInt("SalvareCurenta", 1);
            traseuCurentIndex = PlayerPrefs.GetInt("TraseuStart", 1) - 1;
        }
        if (scoreboardDoc != null)
        {
            scoreboardRoot = scoreboardDoc.rootVisualElement;
            scoreboardRoot.style.display = DisplayStyle.None;
        }
        ActualizeazaDateNivelCurent();
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.rKey.wasPressedThisFrame)
        {
            ResetareNivelComplet();
        }
        if (kb.lKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        bool esteInAntrenament = (agent != null && !agent.modValidare);

        if (!esteInAntrenament)
        {
            if (Global.validare) SetareTimp(5f);
            else if (kb.fKey.isPressed) SetareTimp(10f);
            else SetareTimp(1f);
        }
        if (kb.zKey.wasPressedThisFrame && bila != null && bila.AreStareSalvata)
            IntoarceLaStareaPrecedenta();
        timpDeLaUltimaLovitura += Time.deltaTime;

        if (kb.tabKey.wasPressedThisFrame)
        {
            ArataScoreboard(true);
        }
        if (kb.tabKey.wasReleasedThisFrame)
        {
            ArataScoreboard(false);
        }
    }
    public void seteazaIdSalvare(int idNou)
    {
        idSalvareCurenta = idNou;
    }
    private void FixedUpdate()
    {
        timpDeLaUltimaLovitura += Time.fixedDeltaTime;
        if (cronometruPornit && !nivelEsuat && !nivelFinalizat)
        {
            timpCurent += Time.deltaTime;
            if (labelSecunde != null) labelSecunde.text = $"{Mathf.RoundToInt(timpCurent)} secunde";
            if (timpCurent > limitaTimpNivel)
            {
                EsuareNivel();
            }
        }
    }
    private void SetareTimp(float timp)
    {
        Time.timeScale = timp;
        Time.fixedDeltaTime = 0.02f * timp;
        Time.maximumDeltaTime = 0.33f;
    }
    public void SeteazaIndexTraseuNou(int indexNou, string anotimpNou)
    {
        StopAllCoroutines();
        traseuCurentIndex = indexNou;
        sezonCurent = anotimpNou;
        incercariCurente = 0;
        ActualizeazaDateNivelCurent();
    }
    private void ActualizeazaDateNivelCurent()
    {
        if (managerHarta == null) return;
        List<DateTraseu> traseeSezon = managerHarta.TraseeBySezon.GetValueOrDefault(sezonCurent, new List<DateTraseu>());
        if (agent != null)
        {
            if (!agent.modValidare && (traseuCurentIndex > 6 || traseuCurentIndex < 0)) traseuCurentIndex = 0;
            else if (agent.modValidare && (traseuCurentIndex < 7 || traseuCurentIndex > 9)) traseuCurentIndex = 7;
        }

        if (traseuCurentIndex >= 0 && traseuCurentIndex < traseeSezon.Count)
        {
            traseuCurentObiect = traseeSezon[traseuCurentIndex];
            if (traseuCurentObiect.setariDB != null)
            {
                idNivelCurentDB = traseuCurentObiect.setariDB.id_nivel;

                bool esteInAntrenament = (agent != null && !agent.modValidare);
                if ((Global.dinMeniu == false || UtilitareBD.EsteModAntrenament || Application.isEditor) && DatabaseManager.instance != null) DatabaseManager.instance.NivelInceput(idSalvareCurenta, idNivelCurentDB);
                limitaLovituriNivel = esteInAntrenament || Global.validare ? 50 : traseuCurentObiect.setariDB.limitaLovituri;
                limitaTimpNivel = esteInAntrenament || Global.validare ? 300 : traseuCurentObiect.setariDB.limitaTimp;
                nivelEsuat = false;
                nivelFinalizat = false;
                cronometruPornit = true;
                if (labelLovituriPar != null) labelLovituriPar.text = "Par Lovituri: " + traseuCurentObiect.setariDB.par;
                if (labelSecundePar != null) labelSecundePar.text = "Par Timp: " + traseuCurentObiect.setariDB.timp + "s";
            }
        }
    }
    public void ResetareNivelComplet()
    {
        StopAllCoroutines();
        numarLovituri = 0;
        timpCurent = 0f;
        ultimuPereteLovit = "Niciunul";
        nivelEsuat = false;
        nivelFinalizat = false;
        cronometruPornit = true;

        if (labelMesaj != null) labelMesaj.text = "";
        ActualizeazaText();
        if (traseuCurentObiect != null && bila != null)
        {
            bila.Teleporteaza(traseuCurentObiect.punctStart.position);
            bila.SalveazaPozitia();
        }
    }
    private void ArataScoreboard(bool show)
    {
        if (scoreboardRoot == null) return;
        isScoreboardVisible = show;

        if (show)
        {
            scoreboardRoot.style.display = DisplayStyle.Flex;
            Button btnInapoi = scoreboardRoot.Q<Button>("Inapoi");
            if (btnInapoi != null) btnInapoi.style.display = DisplayStyle.None;
            PopuleazaScoreboard();
        }
        else scoreboardRoot.style.display = DisplayStyle.None;
    }
    private void PopuleazaScoreboard()
    {
        foreach (string sezon in Global.Sezoane)
        {
            VisualElement randAnotimp = scoreboardRoot.Q<VisualElement>("Rand_" + sezon);
            if (randAnotimp != null)
            {
                if (sezon == sezonCurent)
                {
                    randAnotimp.style.display = DisplayStyle.Flex;
                    ActualizeazRandScoreboard(sezon, randAnotimp);

                }
                else randAnotimp.style.display = DisplayStyle.None;
            }
        }
    }
    private void ActualizeazRandScoreboard(string sezon, VisualElement randAnotimp)
    {
        var toateRandurile = randAnotimp.Children().ToList();
        if (toateRandurile.Count < 4 || DatabaseManager.instance == null) return;
        VisualElement randParLovituri = toateRandurile[0];
        VisualElement randScorLovituri = toateRandurile[1];
        VisualElement randParTimp = toateRandurile[2];
        VisualElement randScorTimp = toateRandurile[3];

        List<DatabaseManager.DateScoreBoard> dateBD = DatabaseManager.instance.ExtrageDateScoreBoard(sezon, idSalvareCurenta);
        int totalParLovituri = 0;
        int totalScorLovituri = 0;
        int totalParTimp = 0;
        int totalScorTimp = 0;
        for (int i = 0; i < dateBD.Count; i++)
        {
            int numarNivel = i + 1;
            Label lblParLov = randParLovituri.Q<Label>("Nivel" + numarNivel);
            if (lblParLov != null)
            {
                lblParLov.text = dateBD[i].par.ToString();
                totalParLovituri += dateBD[i].par;
            }
            Label lblScorLov = randScorLovituri.Q<Label>("Nivel" + numarNivel);
            if (lblScorLov != null)
            {
                if (dateBD[i].scor_best > 0 && dateBD[i].scor_best < 99)
                {
                    lblScorLov.text = dateBD[i].scor_best.ToString();
                    totalScorLovituri += dateBD[i].scor_best;
                }
                else lblScorLov.text = "-";
            }

            Label lblParTimp = randParTimp.Q<Label>("Nivel" + numarNivel);
            if (lblParTimp != null)
            {
                lblParTimp.text = dateBD[i].timp_par.ToString();
                totalParTimp += dateBD[i].timp_par;
            }
            Label lblScorTimp = randScorTimp.Q<Label>("Nivel" + numarNivel);
            if (lblScorTimp != null)
            {
                if (dateBD[i].timp_best > 0 && dateBD[i].timp_best < 999)
                {
                    lblScorTimp.text = dateBD[i].timp_best.ToString();
                    totalScorTimp += dateBD[i].timp_best;
                }
                else lblScorTimp.text = "-";
            }
        }
        Label lblTotalParlov = randParLovituri.Q<Label>("Lovituri");
        if (lblTotalParlov != null) lblTotalParlov.text = totalParLovituri.ToString();
        Label lblScorParlov = randScorLovituri.Q<Label>("Lovituri");
        if (lblScorParlov != null) lblScorParlov.text = totalScorLovituri > 0 ? totalScorLovituri.ToString() : "-";

        Label lblTotalParTimp = randParTimp.Q<Label>("Total");
        if (lblTotalParTimp != null) lblTotalParTimp.text = totalParTimp.ToString() + "s";
        Label lblTotalScorTimp = randScorTimp.Q<Label>("Total");
        if (lblTotalScorTimp != null) lblTotalScorTimp.text = totalScorTimp > 0 ? totalScorTimp.ToString() + "s" : "-";
    }
    public int OpresteSiReturneazaTimpSecunde()
    {
        cronometruPornit = false;
        return Mathf.RoundToInt(timpCurent);
    }
    public void ReseteazaTimp()
    {
        timpCurent = 0f;
        cronometruPornit = true;
    }
    public void SeteazaLovituri(int numar)
    {
        numarLovituri = numar;
        ActualizeazaText();
    }
    private void ActualizeazaText()
    {
        if (labelLovituri != null) labelLovituri.text = "Lovituri: " + numarLovituri.ToString();
    }

    public void AfiseazaMesajFinal()
    {
        StartCoroutine(MesajFinalCoroutine());
    }

    private IEnumerator MesajFinalCoroutine()
    {
        if (labelMesaj != null)
        {
            labelMesaj.gameObject.SetActive(true);
            if (sezonCurent == "Iarna") labelMesaj.text = "Felicitari!\nAi terminat jocul!";
            else labelMesaj.text = "Sezon finalizat!";
        }
        if (logicaAi != null && logicaAi.controlatDeAI)
        {
            yield return waitMesajAI;
            if (labelMesaj != null) labelMesaj.text = "";
            SeteazaIndexTraseuNou(7, sezonCurent);
            ResetareNivelComplet();
            yield break;
        }
        yield return waitMesajJucator;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        PlayerPrefs.SetInt("DeschideSezoane", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenu");
    }

    public void OnCrosaMingeLovita()
    {
        if (bila != null)
        {
            bila.SalveazaPozitia();
        }

        lovituriPrecedente = numarLovituri;
        numarLovituri++;
        timpDeLaUltimaLovitura = 0f;
        ActualizeazaText();
        if (numarLovituri > limitaLovituriNivel)
        {
            EsuareNivel();
        }
    }

    private void IntoarceLaStareaPrecedenta()
    {
        if (bila != null)
        {
            bila.RestaureazaPozitia();
            nivelEsuat = false;
            cronometruPornit = true;
        }

        numarLovituri++;
        ActualizeazaText();
    }

    public void ResetMingeOutOfBounds()
    {
        aAtinsOutOfBoundsAcestFrame = true;
        if (bila != null)
        {
            bila.RestaureazaPozitia();
        }
    }
    private void EsuareNivel()
    {
        if (nivelEsuat) return;
        nivelEsuat = true;
        cronometruPornit = false;
        if (labelMesaj != null)
        {
            string motiv = (timpCurent > limitaTimpNivel) ? "Timp expirat!" : "Prea multe lovituri!";
            labelMesaj.text = "Nivel esuat!\n" + motiv;
        }
        if (agent == null)
        {
            StartCoroutine(ResetareAutomataDupaEsec());
        }
    }
    private IEnumerator ResetareAutomataDupaEsec()
    {
        yield return waitResetareEsec;
        if (nivelEsuat)
        {
            incercariCurente++;
            if (incercariCurente < maxIncercari) ResetareNivelComplet();
            else
            {
                incercariCurente = 0;
                if (scriptHoleTrigger != null) scriptHoleTrigger.TreciLaURmatorulNivel();
                else ResetareNivelComplet();
            }

        }
    }
    public int GetNivelCurent()
    {
        return idNivelCurentDB;
    }
}