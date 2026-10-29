using UnityEngine;
using UnityEngine.InputSystem;

public class CameraAntrenament : MonoBehaviour
{
    public float vitezaMiscare = 50f; 
    public float multiplicatorViteza = 5f;
    public float sensibilitateMouse = 0.1f;
    public float factorNetezire = 15f; 
    public float distantaUmbre = 40f; 
    public int targetFrameRate = 60;

    private float rotatieX = 0f;
    private float rotatieY = 0f;
    private float targetRotatieX = 0f;
    private float targetRotatieY = 0f;

    private AgentG[] agentiInScena;
    private int indexAgentCurent = 0;

    public void Start()
    {
        rotatieY = transform.eulerAngles.y; 
        rotatieX = transform.eulerAngles.x;
        targetRotatieX = rotatieX;
        targetRotatieY = rotatieY;

        agentiInScena = FindObjectsByType<AgentG>(FindObjectsSortMode.None);

        QualitySettings.shadowDistance = distantaUmbre;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;

        if (agentiInScena != null && agentiInScena.Length > 0)
        {
            indexAgentCurent = 0;
            PozitioneazaCameraPeAgent(agentiInScena[indexAgentCurent]);
        }
    } 

    private void Update()
    {
        if(Mouse.current == null || Keyboard.current == null) return;

        if (agentiInScena != null && agentiInScena.Length > 0)
        {
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame) TeleporteazaLaAgent(1);
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame) TeleporteazaLaAgent(-1);
        }

        float scroll = Mouse.current.scroll.y.ReadValue();
        if (scroll > 0f) vitezaMiscare += 20f;
        else if (scroll < 0f) vitezaMiscare = Mathf.Max(10f, vitezaMiscare - 20f);

        if(Mouse.current.rightButton.isPressed)
        {
            Cursor.lockState = CursorLockMode.Locked;
            
            Vector2 deltaMouse = Mouse.current.delta.ReadValue(); 
            deltaMouse.x = Mathf.Clamp(deltaMouse.x, -50f, 50f);
            deltaMouse.y = Mathf.Clamp(deltaMouse.y, -50f, 50f);

            targetRotatieY += deltaMouse.x * sensibilitateMouse;
            targetRotatieX -= deltaMouse.y * sensibilitateMouse;
            targetRotatieX = Mathf.Clamp(targetRotatieX, -90f, 90f); 
            rotatieX = Mathf.Lerp(rotatieX, targetRotatieX, Time.unscaledDeltaTime * factorNetezire);
            rotatieY = Mathf.Lerp(rotatieY, targetRotatieY, Time.unscaledDeltaTime * factorNetezire);
            
            transform.eulerAngles = new Vector3(rotatieX, rotatieY, 0f);
            
            Vector3 directie = Vector3.zero;
            if (Keyboard.current.wKey.isPressed) directie += transform.forward;
            if (Keyboard.current.sKey.isPressed) directie -= transform.forward;
            if (Keyboard.current.aKey.isPressed) directie -= transform.right;
            if (Keyboard.current.dKey.isPressed) directie += transform.right;
            if (Keyboard.current.eKey.isPressed) directie += transform.up;
            if (Keyboard.current.qKey.isPressed) directie -= transform.up;
            
            float vitezaCurenta = vitezaMiscare;
            if (Keyboard.current.leftShiftKey.isPressed) vitezaCurenta *= multiplicatorViteza; 
            transform.position += directie.normalized * vitezaCurenta * Time.unscaledDeltaTime;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            targetRotatieX = rotatieX;
            targetRotatieY = rotatieY;
        }
    }

    private void TeleporteazaLaAgent(int directiePasi)
    {
        indexAgentCurent += directiePasi;
        
        if (indexAgentCurent >= agentiInScena.Length) indexAgentCurent = 0;
        else if (indexAgentCurent < 0) indexAgentCurent = agentiInScena.Length - 1;

        PozitioneazaCameraPeAgent(agentiInScena[indexAgentCurent]);
    }

    private void PozitioneazaCameraPeAgent(AgentG agentTinta)
    {
        if (agentTinta == null) return;

        transform.position = agentTinta.transform.position + new Vector3(0, 10f, -15f);

        targetRotatieX = 30f;
        targetRotatieY = 0f;
        rotatieX = targetRotatieX;
        rotatieY = targetRotatieY;
        
        transform.eulerAngles = new Vector3(rotatieX, rotatieY, 0f);
    }
}