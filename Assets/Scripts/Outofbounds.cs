using UnityEngine;

public class DetectieOutOfBounds : MonoBehaviour
{
    public UI scriptUI;

    private void OnCollisionEnter(Collision coliziune)
    {
        if(coliziune.transform.parent != null && coliziune.transform.parent.CompareTag("Pereti"))
        {
            if (scriptUI != null) scriptUI.ultimuPereteLovit = coliziune.gameObject.name;
        }
         VerificaOutOfBounds(coliziune.gameObject);
    }

    private void OnTriggerEnter(Collider altCollider)
    {
        VerificaOutOfBounds(altCollider.gameObject);
    }
    private void VerificaOutOfBounds(GameObject obiectLovit)
    {
        if(obiectLovit.CompareTag("OutOfBounds") && scriptUI != null)
        {
            scriptUI.ResetMingeOutOfBounds();
        }
    }
}