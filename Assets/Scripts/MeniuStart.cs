using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
public class MeniuStart : MonoBehaviour
{
    public GameObject meniuPrincipal;
    public GameObject meniuAntrenare;
    private void OnEnable()
    {
        meniuPrincipal.SetActive(false);
        var root = GetComponent<UIDocument>().rootVisualElement;
        Button btnJoaca = root.Q<Button>("Joc");
        if(btnJoaca != null) btnJoaca.clicked += DeschideMeniuPrincipal;
        Button btnTestare = root.Q<Button>("Testare");
        if(btnTestare != null) btnTestare.clicked += DeschideMeniuTestare;
        Button btnAntrenament = root.Q<Button>("Antrenare");
        if(btnAntrenament != null) btnAntrenament.clicked += DeschideMeniuAntrenament;
    }

    private void DeschideMeniuPrincipal()
    {
        if(meniuPrincipal != null) meniuPrincipal.SetActive(true);
        gameObject.SetActive(false);
    }
    private void DeschideMeniuTestare()
    {
        Global.dinMeniu = true;
        Global.validare = true;
        SceneManager.LoadScene("GamePlay");
    }
    private void DeschideMeniuAntrenament()
    {
        if(meniuAntrenare != null) meniuAntrenare.SetActive(true);
        gameObject.SetActive(false);
    }
}
