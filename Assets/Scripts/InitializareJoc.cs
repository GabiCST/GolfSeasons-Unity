using UnityEngine;
[DefaultExecutionOrder(-200)]
public class InitializareJoc : MonoBehaviour
{
    public GameObject Jucator;
    public GameObject Antrenament;
    public GameObject Testare;
    public GameObject MeniuEsc;
    private void Awake()
    {
         MeniuEsc?.SetActive(true);

        if(Global.dinMeniu && Global.validare)
        {
            Jucator?.SetActive(false);
            Antrenament?.SetActive(false);
            Testare?.SetActive(true);
        }
        else if(Global.dinMeniu && Global.antrenament)
        {
            Jucator?.SetActive(false);
            Antrenament?.SetActive(true);
            Testare?.SetActive(false);
        }
        else if(Global.dinMeniu)
        {
            Jucator?.SetActive(true);
            Antrenament?.SetActive(false);
            Testare?.SetActive(false);
        } 
        else
        {
            Jucator?.SetActive(false);
            Antrenament?.SetActive(true);
            Testare?.SetActive(false);
        }
    }

}