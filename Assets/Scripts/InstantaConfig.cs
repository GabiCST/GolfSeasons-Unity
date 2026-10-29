using UnityEngine;

public class InstantaConfig : MonoBehaviour
{
    public UI uiManager;
    public AgentG agent;
    public TriggerGaura triggerGaura;
    public GestiuneMapa managerHarta;
    public LogicaLovitura logicaLovitura;

private void Awake()
{
    Referinte();
}
private void Referinte()
{
    TriggerBuguri[] buguri = GetComponentsInChildren<TriggerBuguri>(true);
    foreach (var bug in buguri)
    {
        bug.uiManager = uiManager;
        bug.triggerGaura = triggerGaura;
    }
    if (agent != null)
    {
        agent.uiManager = uiManager;
        agent.scriptHoleTrigger = triggerGaura;
        agent.managerHarta = managerHarta;
        agent.logicaLovitura = logicaLovitura;
    }
    if (triggerGaura != null)
    {
        triggerGaura.uiManager = uiManager;
        triggerGaura.agent = agent;
        triggerGaura.managerHarta = managerHarta;
    }
    if (uiManager != null)
    {
        uiManager.agent = agent;
        uiManager.managerHarta = managerHarta;
        uiManager.scriptHoleTrigger = triggerGaura;
        uiManager.logicaAi = logicaLovitura;
    }
    if (logicaLovitura != null)
    {
        logicaLovitura.uiManager = uiManager;
    }
}
}