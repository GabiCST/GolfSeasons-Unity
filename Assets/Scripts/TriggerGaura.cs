using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;

[RequireComponent(typeof(Bila))]
public class TriggerGaura : MonoBehaviour
{
    public GestiuneMapa managerHarta;
    public UI uiManager;
    private Bila bila;

    public AgentG agent;
    public bool proceseazaGaura = false;
    public Collider ultimulTriggerAtins = null;
    private readonly WaitForSeconds waitProceseazaGaura = new(0.5f);
    private bool EsteModValidare => agent != null && agent.modValidare;

    void Start()
    {
        bila = GetComponent<Bila>();
        string sezonSelectat = UtilitareBD.EsteModAntrenament ? "Primavara" : PlayerPrefs.GetString("SezonSelectat", "Primavara");
        if (managerHarta != null && managerHarta.TraseeOrdonate.Count > 0)
        {
            int indexTraseu = EsteModValidare ? 7 : 0;
            IncarcaTraseuSpecific(sezonSelectat, indexTraseu, false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("TriggerFinal") && !proceseazaGaura)
        {
            managerHarta.HartaTrasee.TryGetValue(other, out DateTraseu traseuGaura);
            managerHarta.TraseeByID.TryGetValue(uiManager.GetNivelCurent(), out DateTraseu traseuCurent);
            bool modInvare = Academy.Instance.EnvironmentParameters.GetWithDefault("mod_invata", 0) > 0.5f;
            if (modInvare && traseuGaura != traseuCurent) return;

            ultimulTriggerAtins = other;
            proceseazaGaura = true;
            StartCoroutine(ProceseazaGaura(other));
        }
    }

    public void TreciLaURmatorulNivel()
    {
        managerHarta.TraseeByID.TryGetValue(uiManager.GetNivelCurent(), out DateTraseu traseuCurent);
        if (traseuCurent == null)
        {
            string sezonCurent = UtilitareBD.EsteModAntrenament ? "Primavara" : PlayerPrefs.GetString("SezonSelectat", "Primavara");
            var traseeSezon = managerHarta.TraseeBySezon.GetValueOrDefault(sezonCurent, new List<DateTraseu>());
            if (traseeSezon.Count > 0) traseuCurent = traseeSezon[0];
        }
        if (traseuCurent != null)
        {
            int localIndexCurent = ObtineTraseeSezon(traseuCurent.anotimp).IndexOf(traseuCurent);
            SchimbaNivelul(traseuCurent, localIndexCurent, false);
        }
    }

    private IEnumerator ProceseazaGaura(Collider triggerGaura)
    {
        if (!UtilitareBD.EsteModAntrenament)
            yield return waitProceseazaGaura;

        bila.Ingheata();
        if (managerHarta.HartaTrasee.TryGetValue(triggerGaura, out DateTraseu traseuCurent))
        {
            int localIndexCurent = ObtineTraseeSezon(traseuCurent.anotimp).IndexOf(traseuCurent);

            if (DatabaseManager.instance != null && traseuCurent.setariDB != null)
                DatabaseManager.instance.SalveazaProgresNivel(ObtineIdSalvare(), traseuCurent.setariDB.id_nivel, uiManager.numarLovituri, uiManager.OpresteSiReturneazaTimpSecunde());

            if (uiManager != null)
            {
                uiManager.aAjunsLaFinal = true;
                uiManager.nivelFinalizat = true;
            }
            SchimbaNivelul(traseuCurent, localIndexCurent, true);
        }
    }

    private void SchimbaNivelul(DateTraseu traseuCurent, int localIndexCurent, bool esteDinGaura)
    {
        var ordineaSezoanelor = Global.Sezoane;
        int idxSezon = ordineaSezoanelor.IndexOf(traseuCurent.anotimp);
        int indexNivelUrmator = localIndexCurent + 1;
        string sezonDeIncarcat = traseuCurent.anotimp;
        int totalInSezon = ObtineTraseeSezon(traseuCurent.anotimp).Count;

        if (Academy.Instance.EnvironmentParameters.GetWithDefault("mod_invata", 0) > 0.5f)
        {
            if (indexNivelUrmator > 6)
            {
                sezonDeIncarcat = ordineaSezoanelor[(idxSezon + 1) % ordineaSezoanelor.Count];
                indexNivelUrmator = 0;
            }
            IncarcaTraseuSpecific(sezonDeIncarcat, indexNivelUrmator, esteDinGaura);
            return;
        }
        else if (UtilitareBD.EsteModAntrenament)
        {
            int limitaNiveluri = 6;
            int sezonA = -1;
            float sansaBug = Random.value;
            if (localIndexCurent < limitaNiveluri)
            {
                if (sansaBug < 0.01f) indexNivelUrmator = localIndexCurent + 2;
                else if (sansaBug >= 0.01f && sansaBug < 0.02 && agent != null)
                {
                    int sezonBD = ((idxSezon + 1) % 4) + 1;
                    if (DatabaseManager.instance != null) DatabaseManager.instance.SezonDeblocat(ObtineIdSalvare(), sezonBD);
                    sezonA = sezonBD;
                    sezonDeIncarcat = ordineaSezoanelor[(idxSezon + 1) % ordineaSezoanelor.Count];
                    indexNivelUrmator = 0;
                }
            }
            else if (localIndexCurent == 9 && traseuCurent.anotimp == "Primavara" && agent != null)
            {
                bool sezonMultiplu = sansaBug < .33f;
                int salt = sezonMultiplu ? 2 : 1;
                int sezonBD = ((idxSezon + salt) % 4) + 1;
                if (DatabaseManager.instance != null) DatabaseManager.instance.SezonDeblocat(ObtineIdSalvare(), sezonBD);
                sezonA = sezonBD;
                sezonDeIncarcat = ordineaSezoanelor[(idxSezon + 1) % ordineaSezoanelor.Count];
                indexNivelUrmator = 0;
            }
            else if (localIndexCurent == limitaNiveluri && agent != null)
            {
                bool sezonMultiplu = (idxSezon == 0 || idxSezon == 1) && (sansaBug < .33f);
                int salt = sezonMultiplu ? 2 : 1;
                int sezonBD = ((idxSezon + salt) % 4) + 1;
                if (DatabaseManager.instance != null) DatabaseManager.instance.SezonDeblocat(ObtineIdSalvare(), sezonBD);
                sezonA = sezonBD;
                sezonDeIncarcat = ordineaSezoanelor[(idxSezon + 1) % ordineaSezoanelor.Count];
                indexNivelUrmator = 0;
            }
            if (indexNivelUrmator > limitaNiveluri)
            {
                sezonDeIncarcat = ordineaSezoanelor[(idxSezon + 1) % ordineaSezoanelor.Count];
                indexNivelUrmator = 0;
            }
            if (sezonA != -1 && agent != null) agent.InregistreazaDeblocareSezon(sezonA);
            IncarcaTraseuSpecific(sezonDeIncarcat, indexNivelUrmator, esteDinGaura);
            return;
        }
        else if (Global.validare)
        {
            int limitaNiveluri = totalInSezon - 1;
            float sansaBug = Random.value;

            if (sansaBug < 0.50f && traseuCurent.anotimp == "Vara" && localIndexCurent == 7)
                indexNivelUrmator = 9;

            if (indexNivelUrmator > limitaNiveluri)
            {
                string sezonUrmator = ordineaSezoanelor[(idxSezon + 1) % ordineaSezoanelor.Count];
                int sezonDB = ((idxSezon + 1) % 4) + 1;
                if (DatabaseManager.instance != null) DatabaseManager.instance.SezonDeblocat(ObtineIdSalvare(), sezonDB);
                sezonDeIncarcat = sezonUrmator;
                indexNivelUrmator = 7;
            }

            if (sansaBug < 0.50f && traseuCurent.anotimp == "Primavara" && localIndexCurent == 9 && esteDinGaura)
            {
                int sezonDB = ((idxSezon + 2) % 4) + 1;
                if (DatabaseManager.instance != null) DatabaseManager.instance.SezonDeblocat(ObtineIdSalvare(), sezonDB);
                if (agent != null) agent.InregistreazaDeblocareSezon(sezonDB);
            }

            IncarcaTraseuSpecific(sezonDeIncarcat, indexNivelUrmator, esteDinGaura);
            return;
        }
        else
        {
            int limitaNiveluri = 6;
            if (localIndexCurent >= limitaNiveluri)
            {
                if (traseuCurent.anotimp == "Iarna" && uiManager != null) uiManager.AfiseazaMesajFinal();
                else
                {
                    string sezonUrmator = ordineaSezoanelor[(idxSezon + 1) % ordineaSezoanelor.Count];
                    int indexUrmator = EsteModValidare ? 7 : 0;
                    if (DatabaseManager.instance != null) DatabaseManager.instance.DeblocheazaSezon(ObtineIdSalvare(), sezonUrmator);
                    IncarcaTraseuSpecific(sezonUrmator, indexUrmator, esteDinGaura);
                }
            }
            else IncarcaTraseuSpecific(traseuCurent.anotimp, indexNivelUrmator, esteDinGaura);
        }
    }

    private void IncarcaTraseuSpecific(string sezon, int indexLocal, bool esteDinGaura)
    {
        List<DateTraseu> traseeSezonNou = ObtineTraseeSezon(sezon);
        int totalNou = traseeSezonNou.Count;
        int indexFinal = Mathf.Clamp(indexLocal, 0, totalNou - 1);

        if (UtilitareBD.EsteModAntrenament && sezon == "Primavara" && indexLocal == 0 && DatabaseManager.instance != null)
            DatabaseManager.instance.SezonDeblocat(ObtineIdSalvare(), 1);

        if (esteDinGaura)
        {
            bila.Dezgheata();
            proceseazaGaura = false;
        }
        if (uiManager != null)
        {
            uiManager.SeteazaLovituri(0);
            uiManager.ReseteazaTimp();
            uiManager.SeteazaIndexTraseuNou(indexLocal, sezon);
        }
        if (traseeSezonNou.Count == 0) return;

        bila.Teleporteaza(traseeSezonNou[indexFinal].punctStart.position);
        bila.SalveazaPozitia();
    }

    private List<DateTraseu> ObtineTraseeSezon(string sezon)
    {
        return managerHarta.TraseeBySezon.GetValueOrDefault(sezon, new List<DateTraseu>());
    }

    private int ObtineIdSalvare()
    {
        if (UtilitareBD.EsteModAntrenament)
            return agent != null ? agent.idSalvareCurenta : System.Diagnostics.Process.GetCurrentProcess().Id;
        return PlayerPrefs.GetInt("SalvareCurenta", 1);
    }
}