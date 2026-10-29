using System.Collections.Generic;
using Unity.Mathematics;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
public class AgentG : Agent
{
    public UI uiManager;
    public Bila bila;
    public GestiuneMapa managerHarta;
    public TriggerGaura scriptHoleTrigger;
    public Transform cameraRig;
    public Vector3 directieLovitura = Vector3.forward;
    public LogicaLovitura logicaLovitura;

    private Vector3 pozitiePrecedenta, pozitieInceput, ultimaPozitieValida, pozitieInainteDeTunneling;
    private Rigidbody rbBila;
    private DateTraseu traseuCurentC;
    private readonly HashSet<Vector2Int> celuleDescoperite = new HashSet<Vector2Int>();
    public Transform senzorAgent;

    public bool afiseazaDebugLog = true, afiseazaGizmos = true, modValidare = false;
    private bool modInvata = false;

    private int idNivelPrecedent, lovituriPrecedente, lovituriFacuteInEpisod, lovituriFaraProgres = 0, sezonMaxC = 1, idSalvareC, idxSezonCurent = 0;
    private float distantaMaxLovitura = 0f, ultimaPutereFolosita, timpEpisod, timpUltimRefresh = -999f, limitaLovituriCurent = 60f, limitaTimpCurent = 300f;
    private bool nivelEsuatPrecedent, nivelFinalizatPrecedent, aGasitBugAcestEpisod = false, denivelare = false, aGasitDesincronizare = false, verificareSoftLockLovituraCurenta = false;

    private readonly Vector3[] directiiTest = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };
    private const float PutereSlaba = 2.0f, PutereMaxima = 6.0f, intervalRefreshSezon = 120f, dimensiuneCelulaGrid = 2.0f;
    private NavMeshPath traseuOptim;
    private int indexPunct = 0;

    private int numarEsecuriConsecutive = 0;
    private bool esteInitializat = false;
    private bool glitchViteza = false;
    private static int instante = 0;

    private bool verificareTunneling = false;
    private bool tunnelingProcesat = false;

    public int idInstantaL { get; private set; }
    public int idSalvareCurenta => idSalvareC;
    private void Awake()
    {
        if (Global.dinMeniu && !Global.validare && !Global.antrenament)
        {
            if (TryGetComponent<Unity.MLAgents.Policies.BehaviorParameters>(out var bp)) bp.enabled = false;
            if (TryGetComponent<Unity.MLAgents.DecisionRequester>(out var dr)) dr.enabled = false;
            this.enabled = false;
        }
        else if (Global.validare) modValidare = true;
    }
    protected override void OnDisable()
    {
        if (esteInitializat) base.OnDisable();
    }
    public override void Initialize()
    {
        if (!this.enabled || (Global.dinMeniu) && !Global.validare && !Global.antrenament) return;
        idInstantaL = System.Threading.Interlocked.Increment(ref instante);
        int pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        idSalvareC = (pid * 1000) + idInstantaL;
        rbBila = bila.GetComponent<Rigidbody>();
        if (rbBila == null) rbBila = bila.GetComponentInChildren<Rigidbody>();
        AsiguraCaSalvareaExista();
        if (uiManager != null) uiManager.seteazaIdSalvare(idSalvareC);
        Application.runInBackground = true;
        if (logicaLovitura == null) logicaLovitura = FindFirstObjectByType<LogicaLovitura>();
        if (logicaLovitura != null) logicaLovitura.controlatDeAI = true;
        if (logicaLovitura != null)
        {
            logicaLovitura.mingeLovita.RemoveListener(InregistreazaLovitura);
            logicaLovitura.mingeLovita.AddListener(InregistreazaLovitura);
        }
        if (!UtilitareBD.EsteModAntrenament)
        {
            PlayerPrefs.SetInt("SalvareCurenta", idSalvareC);
            PlayerPrefs.SetString("SezonSelectat", "Primavara");
            PlayerPrefs.SetInt("TraseuStart", 1);
            PlayerPrefs.Save();
            SalveazaSnapshotStare();
        }
        if (Global.validare) modValidare = true;
        modInvata = !Global.validare && Academy.Instance.EnvironmentParameters.GetWithDefault("mod_invata", 0f) > 0.5f;
        esteInitializat = true;
    }
    private void InregistreazaLovitura() => lovituriFacuteInEpisod++;
    private void AsiguraCaSalvareaExista()
    {
        if (DatabaseManager.instance == null) return;
        var salvari = DatabaseManager.instance.ExtrageSalvari();
        if (salvari.Find(s => s.id_salvare == idSalvareC) == null)
        {
            string nume = $"Agent_PID{idSalvareC}";
            DatabaseManager.instance.CreeazaSalvare(idSalvareC, nume);
        }
    }

    private void OnDestroy()
    {

        if (DatabaseManager.instance != null && !UtilitareBD.EsteModAntrenament) DatabaseManager.instance.StergeSalvare(idSalvareC);
    }
    public override void OnEpisodeBegin()
    {
        verificareTunneling = false;
        tunnelingProcesat = false;
        glitchViteza = false;
        celuleDescoperite.Clear();
        aGasitDesincronizare = false; denivelare = false; distantaMaxLovitura = 0f; lovituriFaraProgres = 0; aGasitBugAcestEpisod = false;
        ultimaPutereFolosita = 0f; lovituriFacuteInEpisod = 0; timpEpisod = 0f; directieLovitura = Vector3.forward;
        int idNivelCurent = uiManager != null ? uiManager.GetNivelCurent() : -1;
        if (idNivelCurent != idNivelPrecedent)
        {
            numarEsecuriConsecutive = 0;

        }
        if (uiManager != null) uiManager.ResetareNivelComplet();
        if (bila != null) pozitieInceput = pozitiePrecedenta = bila.transform.position;
        if (scriptHoleTrigger != null)
        {
            scriptHoleTrigger.ultimulTriggerAtins = null;
            scriptHoleTrigger.proceseazaGaura = false;
        }

        traseuCurentC = ObtineTraseuCurent();
        CalculareTraseuOptim();

        idxSezonCurent = traseuCurentC != null ? Global.Sezoane.IndexOf(traseuCurentC.anotimp) : 0;

        limitaLovituriCurent = Mathf.Max(1f, ObtineLimitaLovituri());
        limitaTimpCurent = Mathf.Max(1f, ObtineLimitaTimp());


        RefreshSezonMaxF();
        SalveazaSnapshotStare();
    }
    private void CalculareTraseuOptim()
    {
        if (traseuCurentC == null || traseuCurentC.triggerFinal == null || bila == null) return;
        traseuOptim = new NavMeshPath();
        if (NavMesh.SamplePosition(bila.transform.position, out NavMeshHit hitStart, 1.0f, NavMesh.AllAreas) && NavMesh.SamplePosition(traseuCurentC.triggerFinal.transform.position, out NavMeshHit hitEnd, 1.0f, NavMesh.AllAreas))
        {
            NavMesh.CalculatePath(hitStart.position, hitEnd.position, NavMesh.AllAreas, traseuOptim);
        }
        indexPunct = 1;
    }
    private void FixedUpdate()
    {
        if (bila == null) return;
        if (modInvata && traseuOptim != null && indexPunct < traseuOptim.corners.Length)
        {
            if (Vector3.Distance(bila.transform.position, traseuOptim.corners[indexPunct]) < 1.0f)
            {
                AddReward(0.1f);
                indexPunct++;
            }
        }
        if (senzorAgent != null)
        {
            senzorAgent.position = bila.transform.position;
            senzorAgent.rotation = quaternion.identity;
        }

        Vector3 pozitieCurenta = bila.transform.position;
        Vector3 deplasare = pozitieCurenta - pozitiePrecedenta;
        float distanta = deplasare.magnitude;

        if (!bila.E_Oprita)
        {
            if (uiManager != null && !uiManager.nivelEsuat && !uiManager.nivelFinalizat)
            {
                var coordonateGrid = new Vector2Int(Mathf.FloorToInt(pozitieCurenta.x / dimensiuneCelulaGrid), Mathf.FloorToInt(pozitieCurenta.z / dimensiuneCelulaGrid));
                if (celuleDescoperite.Add(coordonateGrid)) AddReward(0.005f);
            }
            float distantaParcursaP = (pozitieCurenta - pozitieInceput).sqrMagnitude;
            if (distantaParcursaP > distantaMaxLovitura * distantaMaxLovitura) distantaMaxLovitura = Mathf.Sqrt(distantaParcursaP);

            if (distanta > 0.05f && !verificareTunneling && !tunnelingProcesat)
            {
                if (Physics.Raycast(pozitiePrecedenta, deplasare.normalized, out RaycastHit hit, distanta, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (!hit.collider.CompareTag("Teren") && !hit.collider.CompareTag("Player"))
                    {
                        verificareTunneling = true;
                        pozitieInainteDeTunneling = pozitiePrecedenta;
                    }
                }
            }
        }
        else ultimaPozitieValida = pozitieCurenta;
        pozitiePrecedenta = pozitieCurenta;
        if (afiseazaDebugLog) Debug.DrawRay(pozitieCurenta, directieLovitura.normalized * 3f, Color.red);
    }
    public override void CollectObservations(VectorSensor sensor)
    {
        if (uiManager == null || bila == null)
        {
            for (int i = 0; i < 14; i++) sensor.AddObservation(0f);
            return;
        }

        sensor.AddObservation((float)uiManager.GetNivelCurent() / 40f);
        sensor.AddObservation((float)uiManager.numarLovituri / limitaLovituriCurent);
        sensor.AddObservation(uiManager.timpCurent / limitaTimpCurent);

        sensor.AddObservation(uiManager.nivelEsuat ? 1f : 0f);
        sensor.AddObservation(uiManager.nivelFinalizat ? 1f : 0f);

        sensor.AddObservation(bila.E_Oprita ? 1f : 0f);
        sensor.AddObservation((float)sezonMaxC / 4f);
        sensor.AddObservation((float)ObtineSezonCurentIndex() / 3f);
        sensor.AddObservation((float)ObtineNivelInSezon() / 9f);


        if (traseuCurentC != null && traseuCurentC.triggerFinal != null)
        {
            Vector3 vectorCatreGaura = traseuCurentC.triggerFinal.transform.position - bila.transform.position;
            vectorCatreGaura.y = 0;
            Vector3 directieGauraNorm = vectorCatreGaura.normalized;
            Vector3 directiaMeaNorm = directieLovitura.normalized;

            sensor.AddObservation(Mathf.Clamp01(vectorCatreGaura.magnitude / 100f));
            sensor.AddObservation(Vector3.Dot(directiaMeaNorm, directieGauraNorm));
            sensor.AddObservation(Vector3.Cross(directiaMeaNorm, directieGauraNorm).y);

            Vector3 offsetFataDeStart = traseuCurentC.punctStart != null ? bila.transform.position - traseuCurentC.punctStart.position : Vector3.zero;
            sensor.AddObservation(Mathf.Clamp(offsetFataDeStart.x / 50f, -1f, 1f));
            sensor.AddObservation(Mathf.Clamp(offsetFataDeStart.z / 50f, -1f, 1f));
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (uiManager == null) return;
        VerificaTunneling();
        ProceseazaOutOfBoundsB();
        if (modInvata) OnActionInvatare(actions);
        else OnActionDescoperireSauValidare(actions);
    }
    private void OnActionInvatare(ActionBuffers actions)
    {
        if (uiManager.nivelFinalizat || uiManager.aAjunsLaFinal)
        {
            uiManager.aAjunsLaFinal = false;
            uiManager.nivelFinalizat = false;
            numarEsecuriConsecutive = 0;
            AddReward(5.0f);
            EndEpisode();
            return;
        }
        if (uiManager.nivelEsuat)
        {
            DetecteazaIncalcariSiAcordaReward();
            if (bila.E_Oprita || uiManager.nivelFinalizat)
            {
                AddReward(-0.1f);
                GestioneazaEsec();
                EndEpisode();
                return;
            }
        }
        traseuCurentC = ObtineTraseuCurent();
        DiscreteActions(actions);
        timpEpisod += Time.fixedDeltaTime;

    }

    private void OnActionDescoperireSauValidare(ActionBuffers actions)
    {
        if (uiManager.nivelFinalizat || uiManager.aAjunsLaFinal)
        {
            VerificaDesincronizare(uiManager.GetNivelCurent(), uiManager.numarLovituri);
            DetecteazaIncalcariSiAcordaReward();
            uiManager.aAjunsLaFinal = false;
            uiManager.nivelFinalizat = false;
            numarEsecuriConsecutive = 0;
            float bonusFinalizare = aGasitBugAcestEpisod ? 3f : 0.35f;
            if (traseuCurentC != null && managerHarta != null)
            {
                int indexCurent = managerHarta.TraseeOrdonate.IndexOf(traseuCurentC);
                bool eUltimulDinSezon = indexCurent + 1 >= managerHarta.TraseeOrdonate.Count || traseuCurentC.anotimp != managerHarta.TraseeOrdonate[indexCurent + 1].anotimp;
                if (eUltimulDinSezon) bonusFinalizare += 1f;
            }
            AddReward(bonusFinalizare);
            EndEpisode();
            return;
        }
        if (uiManager.nivelEsuat)
        {
            DetecteazaIncalcariSiAcordaReward();
            if (bila.E_Oprita || uiManager.nivelFinalizat)
            {
                AddReward(aGasitBugAcestEpisod ? 0f : -0.05f);
                GestioneazaEsec();
                EndEpisode();
            }
            return;
        }
        traseuCurentC = ObtineTraseuCurent();
        DiscreteActions(actions);
        timpEpisod += Time.fixedDeltaTime;
        DetecteazaIncalcariSiAcordaReward();
        if (lovituriFacuteInEpisod > ObtineLimitaLovituri() * 2)
        {
            AddReward(-0.1f);
            GestioneazaEsec();
            EndEpisode();
        }
    }
    private void DiscreteActions(ActionBuffers actions)
    {
        int actiunePutere = actions.DiscreteActions[0];
        int actiuneRotatie = actions.DiscreteActions[1];
        if (bila.E_Oprita)
        {
            if (actiuneRotatie == 1) directieLovitura = Quaternion.Euler(0, -5f, 0) * directieLovitura;
            else if (actiuneRotatie == 2) directieLovitura = Quaternion.Euler(0, 5f, 0) * directieLovitura;
        }
        ExecutaActiune(actiunePutere);
    }
    private void ExecutaActiune(int actiune)
    {
        if (uiManager == null || bila == null) return;
        switch (actiune)
        {
            case 1: LovituraAI(PutereSlaba); break;
            case 2: LovituraAI(PutereMaxima * 0.5f); break;
            case 3: LovituraAI(PutereMaxima); break;
        }
    }
    private void LovituraAI(float putere)
    {
        verificareTunneling = false;
        tunnelingProcesat = false;
        if (!bila.E_Oprita || uiManager.nivelEsuat || uiManager.nivelFinalizat) return;
        AddReward(-0.001f);

        if (logicaLovitura != null) logicaLovitura.LovesteAI(putere, directieLovitura.normalized);
        ultimaPutereFolosita = putere;
        verificareSoftLockLovituraCurenta = false;
        if (Vector3.Distance(bila.transform.position, pozitieInceput) < 0.5f) AddReward(-0.001f);
    }
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var d = actionsOut.DiscreteActions;
        d[0] = d[1] = 0;
        if (Keyboard.current == null) return;
        if (Keyboard.current.aKey.isPressed) d[1] = 1;
        else if (Keyboard.current.dKey.isPressed) d[1] = 2;
        if (Keyboard.current.digit1Key.isPressed) d[0] = 1;
        else if (Keyboard.current.digit2Key.isPressed) d[0] = 2;
        else if (Keyboard.current.digit3Key.isPressed) d[0] = 3;
    }

    private void ProceseazaOutOfBoundsB()
    {
        if (uiManager != null && uiManager.aAtinsOutOfBoundsAcestFrame)
        {
            AddReward(-0.01f);
            uiManager.aAtinsOutOfBoundsAcestFrame = false;
            uiManager.ultimuPereteLovit = "Niciunul";
            if (bila != null) pozitieInceput = bila.transform.position;
            lovituriFaraProgres = 0;
        }
    }

    private void GestioneazaEsec()
    {
        if (modInvata || UtilitareBD.EsteModAntrenament || Application.isEditor || modValidare)
        {
            string motiv = uiManager.timpCurent > limitaTimpCurent ? "Timp" : "Lovituri";
            numarEsecuriConsecutive++;
            if (numarEsecuriConsecutive >= 4)
            {
                numarEsecuriConsecutive = 0;
                if (scriptHoleTrigger != null) scriptHoleTrigger.TreciLaURmatorulNivel();
                else if (uiManager != null) uiManager.ResetareNivelComplet();
            }
            else uiManager?.ResetareNivelComplet();
        }
        else if (uiManager != null)
        {
            uiManager.incercariCurente = 0;
            uiManager.ResetareNivelComplet();
        }
    }
    private void DetecteazaIncalcariSiAcordaReward()
    {
        int idNivelCurent = uiManager.GetNivelCurent();
        int lovituriCurente = uiManager.numarLovituri;
        VerificaTunneling();
        VerificaFizica();
        VerificaOutOfBounds();
        VerificaEroriProgres(idNivelCurent, lovituriCurente);
        VerificaSoftlock(lovituriCurente);
        VerificaDesincronizare(idNivelCurent, lovituriCurente);
        VerificaProgresDupaEsec();
        SalveazaSnapshotStare();
    }
    private void VerificaTunneling()
    {
        if (!verificareTunneling || tunnelingProcesat) return;
        
        bool urmeazaSaCada = false;
        if (bila != null)
        {
            urmeazaSaCada = !Physics.Raycast(bila.transform.position, Vector3.down, 50f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        bool esteOOB = uiManager.aAtinsOutOfBoundsAcestFrame || (bila != null && bila.transform.position.y < -25f) || urmeazaSaCada;
        
        tunnelingProcesat = true;

        if (esteOOB)
        {
            aGasitBug("TUNNELING_MINOR_OOB", 1.5f, pozitieInainteDeTunneling, false);
        }
        else 
        {
            bool aObtinutAvantaj = false;
            if (traseuCurentC?.triggerFinal != null && bila != null)
            {
                float distantaInitiala = Vector3.Distance(pozitieInainteDeTunneling, traseuCurentC.triggerFinal.transform.position);
                float distantaCurenta = Vector3.Distance(bila.transform.position, traseuCurentC.triggerFinal.transform.position);
                aObtinutAvantaj = distantaCurenta < distantaInitiala - 0.1f;
            }
            
            aGasitBug(aObtinutAvantaj ? "TUNNELING_MAJOR" : "TUNNELING_MINOR", aObtinutAvantaj ? 3.5f : 2.0f, pozitieInainteDeTunneling, false);
        }
    }
    

    private void VerificaOutOfBounds()
    {
        if (bila == null) return;
        if (bila.transform.position.y < -50f && !uiManager.aAtinsOutOfBoundsAcestFrame)
        {
            aGasitBug("LIPSA_OUTOFBOUNDS", 0.35f);
            uiManager.ResetMingeOutOfBounds();
        }
    }
    private void VerificaFizica()
    {
        if (bila == null || rbBila == null) return;
        Vector3 viteza = rbBila.linearVelocity;

        if (viteza.magnitude > 18.0f && !verificareTunneling)
        {
            if (!glitchViteza)
            {
                aGasitBugFizica("GLITCH_VITEZA", 0.1f);
                glitchViteza = true;
            }
            return;
        }
        if (!bila.E_Oprita)
        {
            if (!denivelare && viteza.y > 3.5f && viteza.y < 8.0f)
            {
                denivelare = true;
                aGasitBugAcestEpisod = true;
                AddReward(0.1f);
                LogBugInDB("DENIVELARE");
            }
            return;
        }

        if (Physics.Raycast(bila.transform.position, Vector3.down, out RaycastHit hit, 0.05f))
        {

            if (hit.collider.transform.parent != null && hit.collider.transform.parent.CompareTag("Pereti"))
            {
                aGasitBug("MERS_PE_PERETI", 2.5f);
            }
        }
        else if (bila.transform.position.y < -1f && bila.transform.position.y > -5.0 && !uiManager.aAtinsOutOfBoundsAcestFrame)
        {
            aGasitBug("Cadere_SUB_PODEA", 3.0f);
        }

    }
    private void VerificaEroriProgres(int idNivelCurent, int lovituriCurente)
    {
        if (idNivelCurent != idNivelPrecedent)
        {
            bool trecereNormala = uiManager.aAjunsLaFinal;
            managerHarta.TraseeByID.TryGetValue(idNivelPrecedent, out DateTraseu traseuVechi);
            managerHarta.TraseeByID.TryGetValue(idNivelCurent, out DateTraseu traseuNou);
            if (trecereNormala && scriptHoleTrigger != null && scriptHoleTrigger.ultimulTriggerAtins != null)
            {
                if (managerHarta.HartaTrasee.TryGetValue(scriptHoleTrigger.ultimulTriggerAtins, out DateTraseu traseuGauraLovit) && traseuVechi != null && traseuGauraLovit != traseuVechi)
                {
                    bool sezonDiferit = traseuVechi.anotimp != traseuGauraLovit.anotimp;
                    aGasitBug(sezonDiferit ? "GAURA_GRESITA_SEZON_DIFERIT" : "GAURA_GRESITA", sezonDiferit ? 7.0f : 4.0f, traseuVechi, ultimaPozitieValida);
                    return;
                }
            }


            if (!nivelFinalizatPrecedent && !nivelEsuatPrecedent && !trecereNormala)
            {
                aGasitBug("PROGRES_FARA_GAURA", 2.0f, traseuVechi, ultimaPozitieValida);
                return;
            }

            uiManager.aAjunsLaFinal = false;
            if (traseuVechi != null && traseuNou != null)
            {
                List<DateTraseu> niveleSezonVechi = managerHarta.TraseeBySezon.GetValueOrDefault(traseuVechi.anotimp, new List<DateTraseu>());
                List<DateTraseu> niveleSezonNou = managerHarta.TraseeBySezon.GetValueOrDefault(traseuNou.anotimp, new List<DateTraseu>());

                int localIdxVechi = niveleSezonVechi.IndexOf(traseuVechi);
                int localIdxNou = niveleSezonNou.IndexOf(traseuNou);
                int limitaNiveluri = modValidare ? 9 : 6;
                int startNivelUrmator = modValidare ? 7 : 0;
                bool trecereOrdonata = false;

                if (traseuVechi.anotimp == traseuNou.anotimp)
                {
                    trecereOrdonata = (localIdxNou == localIdxVechi + 1);
                }
                else if (localIdxVechi == limitaNiveluri && localIdxNou == startNivelUrmator) trecereOrdonata = true;
                if (!trecereOrdonata && !aGasitBugAcestEpisod)
                {
                    aGasitBug("INCALCARE_ORDINE_NIVELURI", 4.0f, traseuVechi, ultimaPozitieValida);
                    return;
                }
            }
        }

    }

    private void VerificaSoftlock(int lovituriCurente)
    {
        if (bila == null) return;

        if (lovituriCurente > lovituriPrecedente)
        {
            verificareSoftLockLovituraCurenta = false;
        }
        if (!bila.E_Oprita || verificareSoftLockLovituraCurenta) return;

        verificareSoftLockLovituraCurenta = true;
        if ((bila.transform.position - pozitieInceput).sqrMagnitude <= 0.01f)
        {

            lovituriFaraProgres++;
            if (!Global.validare) AddReward(-0.02f);
            if (lovituriFaraProgres >= 7)
            {
                if (VerificaDacaBilaEBlocata()) aGasitBug("SOFTLOCK", 1.5f);
                else aGasitBugAcestEpisod = true;
                lovituriFaraProgres = 0;
                GestioneazaEsec();
                EndEpisode();
            }
        }
        else
        {
            pozitieInceput = bila.transform.position;
            lovituriFaraProgres = 0;
            distantaMaxLovitura = 0f;
        }

    }
    private void VerificaDesincronizare(int idNivelCurent, int lovituriCurente)
    {
        if (idNivelCurent != idNivelPrecedent || !(uiManager.timpCurent > 0.1f)) return;

        bool seIncarcaNivelNou = scriptHoleTrigger.proceseazaGaura;
        if (!seIncarcaNivelNou && lovituriCurente < lovituriFacuteInEpisod && !aGasitDesincronizare)
        {
            if (lovituriFacuteInEpisod - lovituriCurente >= 1)
            {
                aGasitDesincronizare = true;
                aGasitBugAcestEpisod = true;
                if (!modValidare) AddReward(2.0f);
                LogBugInDB("DESINCRONIZARE_UI_DB");
                lovituriFacuteInEpisod = lovituriCurente;
                if (!modValidare)
                {
                    if (UtilitareBD.EsteModAntrenament) GestioneazaEsec();
                    EndEpisode();
                }
            }
        }
    }
    private void VerificaProgresDupaEsec()
    {
        if (nivelEsuatPrecedent && uiManager.nivelFinalizat && !nivelFinalizatPrecedent)
        {
            aGasitBugAcestEpisod = true;
            if (!modValidare) AddReward(3.0f);
            LogBugInDB("PROGRES_DUPA_ESEC");
        }
    }
    private bool VerificaDacaBilaEBlocata()
    {
        int directiiBlocate = 0;
        foreach (var dir in directiiTest)
        {
            if (Physics.Raycast(bila.transform.position, dir, 0.4f)) directiiBlocate++;
        }
        return directiiBlocate >= 4;
    }
    private void aGasitBug(string tip, float reward, Vector3? pozitiePrec = null, bool reseteazaEpisod = true)
    {
        aGasitBugAcestEpisod = true;
        if (!modValidare) AddReward(reward);
        LogBugInDB(tip, null, pozitiePrec);
        if (reseteazaEpisod)
        {
            if (modValidare || UtilitareBD.EsteModAntrenament) GestioneazaEsec();
            EndEpisode();
        }
    }
    private void aGasitBug(string tip, float reward, DateTraseu traseu, Vector3 pozitiePrec, bool reseteazaEpisod = true)
    {
        aGasitBugAcestEpisod = true;
        if (!modValidare) AddReward(reward);
        LogBugInDB(tip, traseu, pozitiePrec);
        if (reseteazaEpisod)
        {
            if (modValidare || UtilitareBD.EsteModAntrenament) GestioneazaEsec();
            EndEpisode();
        }
    }
    private void aGasitBugFizica(string tip, float reward)
    {
        aGasitBugAcestEpisod = true;
        if (!modValidare) AddReward(reward);
        LogBugInDB(tip, null, ultimaPozitieValida);
    }
    public void InregistreazaDeblocareSezon(int indexSezonNou)
    {
        DateTraseu traseuCurentC = ObtineTraseuCurent();
        if (traseuCurentC == null) return;
        sezonMaxC = indexSezonNou;
        List<DateTraseu> traseeSezonVechi = managerHarta.TraseeBySezon.GetValueOrDefault(traseuCurentC.anotimp, new List<DateTraseu>());
        int localIdxVechi = traseeSezonVechi.IndexOf(traseuCurentC);
        int limitaNiveluriIdx = modValidare ? 9 : 6;
        if (localIdxVechi == limitaNiveluriIdx)
        {
            int idxAnotimpAsteptat = Global.Sezoane.IndexOf(traseuCurentC.anotimp) + 2;
            if (sezonMaxC > idxAnotimpAsteptat) aGasitBug("DEBLOCARE_SEZOANE_MULTIPLE", 5.0f);
        }
        else aGasitBug("DEBLOCARE_SEZON_FARA_PARGURGEREA_TUTUROR_NIVELURILOR", 5.0f);

    }
    private void LogBugInDB(string tipBug, DateTraseu traseu = null, Vector3? pozitiePrecedenta = null)
    {
        if (LoguriAI.instance == null)
        {
            return;
        }
        if (bila != null)
        {
            DateTraseu traseuCurent = traseu ?? traseuCurentC;
            string numeAnotimp = traseuCurent != null ? traseuCurent.anotimp : "Necunoscut";
            string numeTraseu = traseuCurent != null ? traseuCurent.numeTraseu : "Necunoscut";
            Vector3 pozitiePrec = pozitiePrecedenta ?? ultimaPozitieValida;
            LoguriAI.instance.InregistreazaBug(idInstantaL, tipBug, lovituriFacuteInEpisod, numeAnotimp, numeTraseu, pozitiePrec, bila.transform.position);
        }
    }
    private void SalveazaSnapshotStare()
    {
        if (uiManager == null) return;
        idNivelPrecedent = uiManager.GetNivelCurent();
        lovituriPrecedente = uiManager.numarLovituri;
        nivelEsuatPrecedent = uiManager.nivelEsuat;
        nivelFinalizatPrecedent = uiManager.nivelFinalizat;
        if (bila != null) ultimaPozitieValida = bila.transform.position;
        DateTraseu anotimpTraseu = ObtineTraseuCurent();
        idxSezonCurent = anotimpTraseu != null ? Global.Sezoane.IndexOf(anotimpTraseu.anotimp) : Global.Sezoane.IndexOf(PlayerPrefs.GetString("SezonSelectat", "Primavara"));
        RefreshSezonMax();
    }

    private void RefreshSezonMaxF()
    {
        timpUltimRefresh = -999f;
        RefreshSezonMax();
    }
    private void RefreshSezonMax()
    {
        if (Time.time - timpUltimRefresh < intervalRefreshSezon) return;
        timpUltimRefresh = Time.time;
        if (DatabaseManager.instance == null) return;
        var salvare = DatabaseManager.instance.ExtrageSalvari().Find(s => s.id_salvare == idSalvareC);
        sezonMaxC = salvare?.sezon_maxim_deblocat ?? 1;
    }
    private int ObtineLimitaLovituri() => 30;
    private float ObtineLimitaTimp() => 150f;
    private int ObtineSezonCurentIndex() => idxSezonCurent;
    private int ObtineNivelInSezon()
    {
        if (managerHarta == null || traseuCurentC == null) return 1;
        List<DateTraseu> traseeSezonCurent = managerHarta.TraseeBySezon.GetValueOrDefault(traseuCurentC.anotimp, new List<DateTraseu>());
        int localIndex = traseeSezonCurent.IndexOf(traseuCurentC);

        return localIndex >= 0 ? localIndex + 1 : 1;
    }
    private DateTraseu ObtineTraseuCurent()
    {
        if (managerHarta == null || uiManager == null) return null;
        managerHarta.TraseeByID.TryGetValue(uiManager.GetNivelCurent(), out DateTraseu traseu);
        return traseu;
    }
}


