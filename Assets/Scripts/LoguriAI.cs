using UnityEngine;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
public class LoguriAI : MonoBehaviour
{
    public static LoguriAI instance; 
    private int processId;
    private string logFolder;
    private readonly ConcurrentQueue<LogEntry> coadaLoguri = new();
    private CancellationTokenSource cancellationTokenSource;
    private Task taskScriere;
    public bool LogInEditor = true;
    private struct LogEntry
    {
        public int idInstanta;
        public int lovituri;
        public string tipBug, sezon, traseu;
        public Vector3 pozitieBilaPrec, pozitieBilaCurenta;
        public System.DateTime dataOra;
    }
    private bool Loguri()
    {
        if (UtilitareBD.EsteModAntrenament || Global.validare) return true; 
#if UNITY_EDITOR 
        return LogInEditor;
#else
        return false;
#endif
    }
    private void Awake()
    {
        if (!Loguri())
        {
            Destroy(this);
            return;
        }
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            processId = System.Diagnostics.Process.GetCurrentProcess().Id;
            string folderJoc = Directory.GetParent(Application.dataPath).FullName;
            logFolder = Path.Combine(folderJoc, "LoguriAgenti");

            if (!Directory.Exists(logFolder)) Directory.CreateDirectory(logFolder);
            cancellationTokenSource = new CancellationTokenSource();
            taskScriere = Task.Run(() => WorkerScriere(cancellationTokenSource.Token));
        }
        else Destroy(gameObject);
    }
    private void OnDestroy()
    {
        cancellationTokenSource?.Cancel();
        try { taskScriere?.Wait(2000); } catch { }
        cancellationTokenSource?.Dispose();
    }
    public void InregistreazaBug(int idInstanta, string tipBug, int lovituri, string sezon, string traseu, Vector3 pozitieBilaPred, Vector3 pozitieCurenta)
    {
        coadaLoguri.Enqueue(new LogEntry
        {
            idInstanta = idInstanta,
            tipBug = tipBug,
            lovituri = lovituri,
            sezon = sezon,
            traseu = traseu,
            pozitieBilaPrec = pozitieBilaPred,
            pozitieBilaCurenta = pozitieCurenta,
            dataOra = System.DateTime.Now
        });
    }
    private string FormateazaLog(LogEntry entry)
    {
        System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
        string prec = $"Vector3({entry.pozitieBilaPrec.x.ToString("F3",inv)}, {entry.pozitieBilaPrec.y.ToString("F3",inv)}, {entry.pozitieBilaPrec.z.ToString("F3",inv)})";
        string curent = $"Vector3({entry.pozitieBilaCurenta.x.ToString("F3",inv)}, {entry.pozitieBilaCurenta.y.ToString("F3",inv)}, {entry.pozitieBilaCurenta.z.ToString("F3",inv)})";
        return $"{entry.dataOra};{entry.tipBug};{entry.lovituri};{entry.sezon};{entry.traseu};{prec};{curent}\n";
    }
    private void WorkerScriere(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (coadaLoguri.TryDequeue(out LogEntry entry)) 
                {
                    string csvPath = Path.Combine(logFolder, $"loguri_agent_{processId}_{entry.idInstanta}.csv");
                    if(!File.Exists(csvPath)) File.WriteAllText(csvPath, "DataOra;TipBug;NumarLovituri;Sezon;Traseu;PozitiePrecedenta;PozitieCurenta\n");
                    File.AppendAllText(csvPath, FormateazaLog(entry));
                }
                else Thread.Sleep(50);
            }
            catch (System.Exception) { Thread.Sleep(500); }

        }
        while (coadaLoguri.TryDequeue(out LogEntry entry))
        {
            string csvPath = Path.Combine(logFolder, $"loguri_agent_{processId}_{entry.idInstanta}.csv");
            if(!File.Exists(csvPath)) File.WriteAllText(csvPath, "DataOra;TipBug;NumarLovituri;Sezon;Traseu;PozitiePrecedenta;PozitieCurenta\n");
            File.AppendAllText(csvPath, FormateazaLog(entry));
        } 
    }
}
