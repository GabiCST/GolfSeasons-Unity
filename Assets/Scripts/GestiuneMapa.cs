using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

public class DateTraseu
{
    public string anotimp, numeTraseu;
    public Transform punctStart;
    public Collider triggerFinal;

    public SetariNivel setariDB;
}

[DefaultExecutionOrder(-100)]
public class GestiuneMapa : MonoBehaviour
{
    public Transform Mapa;

    public Dictionary<Collider, DateTraseu> HartaTrasee = new();
    public List<DateTraseu> TraseeOrdonate = new();
    public Dictionary<int, DateTraseu> TraseeByID = new();
    public Dictionary<string, List<DateTraseu>> TraseeBySezon = new();
    private void Start() => ConstruiesteHarta();
   

    void ConstruiesteHarta()
    {
        int ordine = 1;
        foreach (Transform anotimp in Mapa)
        {
            foreach (Transform traseu in anotimp)
            {
                SetariNivel setariDBExtrase = DatabaseManager.instance != null ? DatabaseManager.instance.VerificaSiExtrageNivel(anotimp.name, traseu.name, ordine) : null;
                DateTraseu nivelNou = new()
                {
                    anotimp = anotimp.name,
                    numeTraseu = traseu.name,
                    setariDB = setariDBExtrase
                };
                foreach (Transform copil in traseu)
                {
                    if (copil.CompareTag("Start")) nivelNou.punctStart = copil;
                    else if (copil.CompareTag("TriggerFinal")) nivelNou.triggerFinal = copil.GetComponent<Collider>();
                }
                if (nivelNou.punctStart != null && nivelNou.triggerFinal != null)
                {
                    HartaTrasee.Add(nivelNou.triggerFinal, nivelNou);
                    TraseeOrdonate.Add(nivelNou);
                    ordine++;
                }

            }
        }
        TraseeOrdonate.Sort((a, b) => (a.setariDB != null && b.setariDB != null) ? a.setariDB.ordine.CompareTo(b.setariDB.ordine) : 0);
        TraseeByID.Clear();
        TraseeBySezon.Clear();
        foreach(var t in TraseeOrdonate)
        {
            if(t.setariDB != null) TraseeByID[t.setariDB.id_nivel] = t;
            if(!TraseeBySezon.ContainsKey(t.anotimp)) TraseeBySezon[t.anotimp] = new List<DateTraseu>();
            TraseeBySezon[t.anotimp].Add(t);
        }
    }
}
 
