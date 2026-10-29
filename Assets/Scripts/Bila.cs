using UnityEngine;
using System.Collections; 
[RequireComponent(typeof(Rigidbody))]

public class Bila : MonoBehaviour
{
    private Vector3 ultimaPozitie;
    private Rigidbody rb;
    private Coroutine rutinaFrecare;

    private readonly float pragOprire = 0.0009f, pragOprireUng = 0.0025f, timpMaxMiscare = 8.0f, dragLiniar = 0.5f, dragUnghiular = 1.5f, timpTranzitie = 0.5f;
    private const int frameStabil = 10;
    private int frameOprita = 0;
    private float timerMiscare;

    public bool AreStareSalvata { get; private set; }
    public bool E_Oprita { get; private set; } = false;

    private void Awake()
    { 
        rb = GetComponent<Rigidbody>();
        AplicaFrecare(1f);
    }

    private void Update()
    {
        if (E_Oprita) timerMiscare = 0f;
        else if ((timerMiscare += Time.deltaTime) >= timpMaxMiscare) Opreste();
    }

    private void FixedUpdate()
    {
        bool vitezaM = rb.linearVelocity.sqrMagnitude <= pragOprire && rb.angularVelocity.sqrMagnitude <= pragOprireUng;
        frameOprita = vitezaM ? frameOprita + 1 : 0;
        E_Oprita = frameOprita >= frameStabil;
    }

    public void ForteazaPornire() => (E_Oprita, frameOprita, timerMiscare) = (false, 0, 0);
    public void ForteazaOprire()
    {
        E_Oprita = true;
        frameOprita = frameStabil;
        Opreste();
    }

    public void Opreste()
    {
        if (rb && !rb.isKinematic) rb.linearVelocity = rb.angularVelocity = Vector3.zero;
    }
    public void Loveste(Vector3 impuls)
    {
        rb.AddForce(impuls, ForceMode.Impulse);
        AreStareSalvata = true;
        ForteazaPornire();
    }
    public void SalveazaPozitia()
    {
        ultimaPozitie = transform.position;
        AreStareSalvata = true;
    }
    public void RestaureazaPozitia() { if (AreStareSalvata) Teleporteaza(ultimaPozitie); }
    public void Ingheata() { Opreste(); rb.isKinematic = true; }
    public void Dezgheata() { rb.isKinematic = false; Opreste(); }

    public void Teleporteaza(Vector3 pozitie)
    {
        Ingheata();
        transform.position = rb.position = pozitie;
        Dezgheata();
        ForteazaOprire();
    }

    public void AplicaFrecare(float multiplicator)
    {
        if (rutinaFrecare != null) StopCoroutine(rutinaFrecare);
        rb.linearDamping = dragLiniar * multiplicator;
        rb.angularDamping = dragUnghiular * multiplicator;
    }

    public void ReseteazaFrecare()
    {
        if (rutinaFrecare != null) StopCoroutine(rutinaFrecare);
        rutinaFrecare = StartCoroutine(TranzitieFrecare());
    }

    private IEnumerator TranzitieFrecare()
    {
        float t = 0f;
        float startLin = rb.linearDamping, startAng = rb.angularDamping;
        while ((t += Time.deltaTime) < timpTranzitie)
        {
            float progres = t / timpTranzitie;
            rb.linearDamping = Mathf.Lerp(startLin, dragLiniar, progres);
            rb.angularDamping = Mathf.Lerp(startAng, dragUnghiular, progres);
            yield return null;
        }
        AplicaFrecare(1f);
    }

    private void OnTriggerEnter(Collider c) => GestioneazaFrecareSuprafata(c.gameObject, true);
    private void OnTriggerExit(Collider c) => GestioneazaFrecareSuprafata(c.gameObject, false);
    private void OnCollisionEnter(Collision c) => GestioneazaFrecareSuprafata(c.gameObject, true);
         
 
    private void OnCollisionExit(Collision c) => GestioneazaFrecareSuprafata(c.gameObject, false);

    private void GestioneazaFrecareSuprafata(GameObject obj, bool intra)
    {
        string t = obj.tag;
        if (t is "Nisip" or "Balti" or "Gheata" or "Tub")
        {
            if (intra) 
            {
                if((t == "Nisip" || t == "Balti") && Random.value < 0.01f)
                {
                    Vector3 impulsEroare = rb.linearVelocity.magnitude > 0.1f ? rb.linearVelocity.normalized : new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
                    rb.AddForce(impulsEroare * 30f, ForceMode.Impulse);
                    return;
                }
                AplicaFrecare(t switch { "Nisip" => 10f, "Balti" => 6f, _ => 0.7f });
            }
            else ReseteazaFrecare();
        }
    }
}
