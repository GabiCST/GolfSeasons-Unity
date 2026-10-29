using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections;
using System.IO;

public class MeniuESC : MonoBehaviour
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool WriteConsoleInput(IntPtr hConsoleInput, INPUT_RECORD[] lpBuffer, uint nLength, out uint lpNumberOfEventsWritten);

    [StructLayout(LayoutKind.Sequential)]
    private struct KEY_EVENT_RECORD
    {
        public bool bKeyDown;
        public ushort wRepeatCount;
        public ushort wVirtualKeyCode;
        public ushort wVirtualScanCode;
        public char UnicodeChar;
        public uint dwControlKeyState;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct INPUT_RECORD
    {
        [FieldOffset(0)] public ushort EventType;
        [FieldOffset(4)] public KEY_EVENT_RECORD KeyEvent;
    }

    private const ushort KEY_EVENT = 0x0001;
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x1;
    private const uint FILE_SHARE_WRITE = 0x2;
    private const uint OPEN_EXISTING = 3;
    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    private const string NumeFisierMarcajFinalizare = "antrenament_finalizat.flag";

    delegate bool ConsoleCtrlDelegate(uint CtrlType);
    const uint CTRL_C_EVENT = 0;

    private UIDocument uiDocument;
    private VisualElement meniuContainer;
    private Button btnRevenire;
    private Button btnMeniu;
    private Button btnIesire;
    private Button btnOprire;

    private bool esteDeschis = false;
    public static bool MeniuDeschis { get; private set; } = false;

#if UNITY_EDITOR
    private static bool? starePrecedentaGizmos = null;
#endif

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null || uiDocument.visualTreeAsset == null) return;

        var root = uiDocument.rootVisualElement;
        if (root == null) return;

        meniuContainer = root.Q<VisualElement>("MeniuESC");
        btnRevenire = root.Q<Button>("Revenire");
        btnMeniu = root.Q<Button>("Meniu");
        btnIesire = root.Q<Button>("Iesire");
        btnOprire = root.Q<Button>("Oprire");

        if (meniuContainer == null) return;

        meniuContainer.style.display = DisplayStyle.None;

        if (btnRevenire != null) btnRevenire.clicked += OnRevenireClicked;
        if (btnMeniu != null) btnMeniu.clicked += OnMeniuClicked;
        if (btnIesire != null) btnIesire.clicked += OnIesireClicked;
        if (btnOprire != null) btnOprire.clicked += OnOprireClicked;

        ActualizeazaStareButonOprire();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMeniu();
        }
    }

    private void ActualizeazaStareButonOprire()
    {
        if (btnOprire != null) btnOprire.SetEnabled(Global.antrenament);
    }

    private void ToggleMeniu()
    {
        if (meniuContainer == null) return;

        esteDeschis = !esteDeschis;
        MeniuDeschis = esteDeschis;
        meniuContainer.style.display = esteDeschis ? DisplayStyle.Flex : DisplayStyle.None;

        if (esteDeschis)
        {
            ActualizeazaStareButonOprire();
            meniuContainer.BringToFront();
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            Time.timeScale = 0f;
            SeteazaGizmos(false);
        }
        else
        {
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            Time.timeScale = 1f;
            SeteazaGizmos(true);
        }
    }

#if UNITY_EDITOR
    private static void SeteazaGizmos(bool valoare)
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
            var tipGameView = assembly.GetType("UnityEditor.GameView");
            var gameView = UnityEditor.EditorWindow.GetWindow(tipGameView);
            var fieldGizmos = tipGameView.GetField("m_Gizmos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fieldGizmos != null)
            {
                if (starePrecedentaGizmos == null)
                {
                    starePrecedentaGizmos = (bool)fieldGizmos.GetValue(gameView);
                }

                fieldGizmos.SetValue(gameView, valoare ? (starePrecedentaGizmos ?? true) : false);
                gameView.Repaint();
            }
        }
        catch { }
    }
#else
    private static void SeteazaGizmos(bool valoare) { }
#endif

    private void OnRevenireClicked()
    {
        ToggleMeniu();
    }

    private void OnOprireClicked()
    {
        if (!Global.antrenament) return;
        if (btnOprire != null) btnOprire.SetEnabled(false);
        OpresteProcesCMD();
    }

    private void OnMeniuClicked()
    {
        Time.timeScale = 1f;
        MeniuDeschis = false;

        if (Global.antrenament)
        {
            OpresteProcesCMD();
        }

        SceneManager.LoadScene("MainMenu");
    }

    private void OnIesireClicked()
    {
        Time.timeScale = 1f;
        MeniuDeschis = false;
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void OpresteProcesCMD()
    {
        StartCoroutine(OpresteProcesCMDCoroutine());
    }

    private IEnumerator OpresteProcesCMDCoroutine()
    {
        int pid = Global.pidProcesAntrenament;
        Process procesAntrenament = null;

        if (pid > 0)
        {
            try
            {
                procesAntrenament = Process.GetProcessById(pid);
                if (procesAntrenament.HasExited) procesAntrenament = null;
            }
            catch (System.ArgumentException)
            {
                procesAntrenament = null;
            }
        }

        if (procesAntrenament == null)
        {
            StartCoroutine(ForteazaOprireDacaTotRuleaza());
            yield break;
        }

        bool atasat = false;
        try
        {
            FreeConsole();
            atasat = AttachConsole((uint)procesAntrenament.Id);

            if (atasat)
            {
                SetConsoleCtrlHandler(null, true);
                GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);
            }
        }
        catch { }

        if (atasat)
        {
            yield return new WaitForSecondsRealtime(0.6f);

            try
            {
                TrimiteRaspunsNuTerminaBatch();
            }
            catch { }

            try
            {
                FreeConsole();
                SetConsoleCtrlHandler(null, false);
            }
            catch { }
        }

        StartCoroutine(ForteazaOprireDacaTotRuleaza());
    }

    private void TrimiteRaspunsNuTerminaBatch()
    {
        IntPtr handleInput = CreateFile("CONIN$", GENERIC_READ | GENERIC_WRITE,
            FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);

        if (handleInput == IntPtr.Zero || handleInput == INVALID_HANDLE_VALUE) return;

        try
        {
            var evenimente = new INPUT_RECORD[]
            {
                CreeazaEvenimentTasta('N', true),
                CreeazaEvenimentTasta('N', false),
                CreeazaEvenimentTasta('\r', true),
                CreeazaEvenimentTasta('\r', false),
            };

            WriteConsoleInput(handleInput, evenimente, (uint)evenimente.Length, out uint nrScrise);
        }
        finally
        {
            CloseHandle(handleInput);
        }
    }

    private static INPUT_RECORD CreeazaEvenimentTasta(char caracter, bool apasata)
    {
        return new INPUT_RECORD
        {
            EventType = KEY_EVENT,
            KeyEvent = new KEY_EVENT_RECORD
            {
                bKeyDown = apasata,
                wRepeatCount = 1,
                wVirtualKeyCode = 0,
                wVirtualScanCode = 0,
                UnicodeChar = caracter,
                dwControlKeyState = 0
            }
        };
    }

    private IEnumerator ForteazaOprireDacaTotRuleaza()
    {
        const float timpAsteptareSecunde = 25f;
        const float intervalVerificare = 1f;
        float timpScurs = 0f;

        int pid = Global.pidProcesAntrenament;
        if (pid <= 0) yield break;

        string caleMarcajFinalizare = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName, NumeFisierMarcajFinalizare);

        while (timpScurs < timpAsteptareSecunde)
        {
            if (File.Exists(caleMarcajFinalizare))
            {
                try { File.Delete(caleMarcajFinalizare); } catch { }
                Global.pidProcesAntrenament = -1;
                yield break;
            }

            bool inceTotRuleaza;
            try
            {
                Process p = Process.GetProcessById(pid);
                inceTotRuleaza = !p.HasExited;
            }
            catch (System.ArgumentException)
            {
                inceTotRuleaza = false;
            }

            if (!inceTotRuleaza)
            {
                Global.pidProcesAntrenament = -1;
                yield break;
            }

            yield return new WaitForSecondsRealtime(intervalVerificare);
            timpScurs += intervalVerificare;
        }

        try
        {
            ProcessStartInfo psiKill = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/PID {pid} /T /F",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psiKill);
        }
        catch { }

        Global.pidProcesAntrenament = -1;
    }

    private void OnDestroy()
    {
        if (btnRevenire != null) btnRevenire.clicked -= OnRevenireClicked;
        if (btnMeniu != null) btnMeniu.clicked -= OnMeniuClicked;
        if (btnIesire != null) btnIesire.clicked -= OnIesireClicked;
        if (btnOprire != null) btnOprire.clicked -= OnOprireClicked;
    }
}