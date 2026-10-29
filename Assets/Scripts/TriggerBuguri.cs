using Unity.MLAgents;
using UnityEngine;

public class TriggerBuguri : MonoBehaviour
{
    public UI uiManager;
    public TriggerGaura triggerGaura;
    private int ultimulNivel = -1;
    private float ultimulTimp = -1f;
    private Transform traseu;
    public Vector3 spawnPos;

    private void Start()
    {
        bool modInvata = Academy.Instance.EnvironmentParameters.GetWithDefault("mod_invata", 0) > 0.5f;
        if(modInvata || (Global.dinMeniu && !Global.validare))
        {
            Destroy(gameObject);
            return;
        }
        traseu = transform.parent;
    }
    private void Update()
    {
        if (uiManager == null) return;
        int nivelCurent = uiManager.GetNivelCurent();
        float timpCurent = uiManager.timpCurent;
        if(!Global.validare)
            if (nivelCurent != ultimulNivel || nivelCurent == 0 || (timpCurent == 0f && ultimulTimp > 0f)) CreareGrid();
        ultimulNivel = nivelCurent;
        ultimulTimp = timpCurent;
    }
    private void CreareGrid()
    {
        if (traseu == null) return;
        Transform punctStart = null;
        foreach(Transform c in traseu.GetComponentInChildren<Transform>(true))
        {
            if(c.CompareTag("Start"))
            {
                punctStart = c;
                break;
            }
        }
        if(punctStart != null) spawnPos = punctStart.transform.position;
        else spawnPos = Vector3.zero;
        Bounds boundsNivel = CalculeazaLimitaTraseu(traseu);
        bool positieGasita = false;
        int incercari = 0;
        Random.InitState((int) System.DateTime.Now.Ticks + gameObject.GetInstanceID());
        while (!positieGasita && incercari < 200)
        {
            float randomX = Random.Range(boundsNivel.min.x, boundsNivel.max.x);
            float randomZ = Random.Range(boundsNivel.min.z, boundsNivel.max.z);

            Vector3 startRay = new(randomX, boundsNivel.max.y + 5f, randomZ);
            if (Physics.Raycast(startRay, Vector3.down, out RaycastHit hit, 50f))
            {
                bool aLovitTeren = hit.collider.CompareTag("Teren") || hit.collider.CompareTag("Gheata") ;
                bool aLovitOOB = hit.collider.CompareTag("OutOfBounds");
                if (aLovitTeren && !aLovitOOB && Vector3.Distance(hit.point, spawnPos) > 0.3f)
                {
                    transform.position = hit.point + new Vector3(0, 0.05f, 0);
                    positieGasita = true;
                }
            }
            incercari++;
        }
    }
    private Bounds CalculeazaLimitaTraseu(Transform nivel)
    {
        Renderer[] renderers = nivel.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(transform.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        foreach (Renderer r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<Bila>(out _))
        {
            if (gameObject.CompareTag("Desincronizare"))
            { 
                if (uiManager != null) uiManager.SeteazaLovituri(0);
            }
            else if (gameObject.CompareTag("Skip"))
            {
                if (uiManager != null) uiManager.aAjunsLaFinal = false;
                if (triggerGaura != null) triggerGaura.TreciLaURmatorulNivel();
            }
            
        }
    }
}
