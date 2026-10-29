using UnityEngine;
using UnityEngine.SceneManagement;
[DefaultExecutionOrder(-1000)]
public class Antrenare : MonoBehaviour
{
    private void Awake()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        bool esteInstantaDeAntrenare = System.Array.Exists(args, a => a.StartsWith("--mlagents-port") || a.Contains("no-graphics"));
        if(esteInstantaDeAntrenare)
        {
            Global.antrenament = true;
            Global.validare = false;
            SceneManager.LoadScene("GamePlay");
        }
    }
}
