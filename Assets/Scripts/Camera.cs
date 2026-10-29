using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRig : MonoBehaviour
{
    public float sensibilitate = 1f;
    public LogicaLovitura logicaLovitura;
    public Transform bila;

    private readonly Vector3 offset = new(0f, .28f, -1f);
    private readonly Vector3 rotatieOffset = new(30, 120f, 0f);

    private float rotX, rotY;

    void Start() => SeteazaCursor(true);

    void LateUpdate()
    { 
        if (MeniuESC.MeniuDeschis) return;

        if (Mouse.current?.leftButton.wasPressedThisFrame == true) SeteazaCursor(true);
        
        if (Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
             
            float factorSensibilitate = sensibilitate * 0.1f; 

            if (logicaLovitura && logicaLovitura.SeIncarca)
            {
                rotX += delta.x * (factorSensibilitate * 0.5f); 
                rotY = Mathf.Lerp(rotY, 0f, Time.unscaledDeltaTime * 10f);
            }
            else
            {
                rotX += delta.x * factorSensibilitate;
                rotY = Mathf.Clamp(rotY - (delta.y * factorSensibilitate), 0f, 45f);
            }
        }
        
        transform.localEulerAngles = new Vector3(rotY + rotatieOffset.x, rotX + rotatieOffset.y, rotatieOffset.z);
        if (bila) transform.position = bila.position + transform.rotation * offset;
    }
    
    private void SeteazaCursor(bool blocat)
    {
        Cursor.lockState = blocat ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !blocat;
    }
}