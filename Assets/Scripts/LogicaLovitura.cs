using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class LogicaLovitura : MonoBehaviour
{
    public bool controlatDeAI = false;
    public UnityEvent mingeLovita;
    public UI uiManager;
    public Bila bila;
    public Transform cameraRig, Sageata;
    public Gradient culoriPutere;

    private MaterialPropertyBlock probBlock;
    private readonly float multiplicatorPutere = 4.0f, putereMaxima = 6.0f;

    private Transform seg1, seg2, seg3, varf;
    private bool seIncarca = false;
    private float putereCurenta = 0.0f;
    private Vector3 directie;

    private MeshRenderer[] rendereSageata;
    public bool SeIncarca => seIncarca;
    
    private void Start()
    {
        probBlock = new MaterialPropertyBlock();
        if (Sageata != null)
        {
            seg1 = Sageata.Find("segment1");
            seg2 = Sageata.Find("segment2");
            seg3 = Sageata.Find("segment3");
            varf = Sageata.Find("varf");
            rendereSageata = Sageata.GetComponentsInChildren<MeshRenderer>(true);
            Sageata.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!controlatDeAI && Mouse.current != null) GestionareInput();
    }
    
    private void GestionareInput()
    {
        if(Cursor.lockState != CursorLockMode.Locked) return;
        
        if (Mouse.current.leftButton.wasPressedThisFrame && bila.E_Oprita)
        {
            seIncarca = true;
            putereCurenta = 0.0f;
            bila.Opreste();
            if (Sageata) Sageata.gameObject.SetActive(true);
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame && seIncarca)
        {
            seIncarca = false;
            if (putereCurenta > 0.1f) Loveste();
            else if (Sageata) Sageata.gameObject.SetActive(false);
        }

        if (seIncarca) ActualizeazaIndicator();
    }
    
    private void ActualizeazaIndicator()
    {
        float mouseY = Mouse.current.delta.y.ReadValue();
        putereCurenta = Mathf.Clamp(putereCurenta - mouseY * multiplicatorPutere * Time.deltaTime, 0.0f, putereMaxima);
        
        float p = putereCurenta / putereMaxima;
        Color culoare = culoriPutere != null ? culoriPutere.Evaluate(p) : Color.white;
        
        if (rendereSageata != null)
        {
            foreach (var r in rendereSageata)
            {
                r.GetPropertyBlock(probBlock);
                
                probBlock.SetColor("_Color", culoare); 
                probBlock.SetColor("_BaseColor", culoare); 
                
                r.SetPropertyBlock(probBlock);
            }
        }
        
        if (seg1 && seg2 && seg3)
        {
            bool s1Activ = p > 0.01f;
            bool s2Activ = p > 0.33f;
            bool s3Activ = p > 0.66f;

            if (seg1.gameObject.activeSelf != s1Activ) seg1.gameObject.SetActive(s1Activ);
            if (seg2.gameObject.activeSelf != s2Activ) seg2.gameObject.SetActive(s2Activ);
            if (seg3.gameObject.activeSelf != s3Activ) seg3.gameObject.SetActive(s3Activ);

            seg1.localScale = new Vector3(Mathf.Clamp01(p / 0.33f), seg1.localScale.y, seg1.localScale.z);
            seg2.localScale = new Vector3(Mathf.Clamp01((p - 0.33f) / 0.33f), seg2.localScale.y, seg2.localScale.z);
            seg3.localScale = new Vector3(Mathf.Clamp01((p - 0.66f) / 0.34f), seg3.localScale.y, seg3.localScale.z);
        }
        
        directie = Vector3.ProjectOnPlane(cameraRig.forward, Vector3.up).normalized;
        if (Sageata != null) Sageata.SetPositionAndRotation(bila.transform.position, Quaternion.LookRotation(directie));
    }

    private void Loveste()
    {
        if (Sageata) Sageata.gameObject.SetActive(false);
        bila.Loveste((directie + Vector3.up * 0.1f).normalized * putereCurenta);
        if (uiManager != null) uiManager.OnCrosaMingeLovita();
        mingeLovita?.Invoke();
    }
    
    public void LovesteAI(float putereGenerata, Vector3 dir)
    {
        if (!bila.E_Oprita) return;
        putereCurenta = Mathf.Clamp(putereGenerata, 0.1f, putereMaxima);
        directie = dir.normalized;
        Loveste();
    }
}