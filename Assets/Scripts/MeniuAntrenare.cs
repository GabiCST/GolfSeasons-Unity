using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
 
public class MeniuAntrenare : MonoBehaviour
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
    private static extern bool FreeConsole();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
    private const uint ENABLE_PROCESSED_INPUT = 0x0001;
    private const uint GENERIC_READ = 0x80000000;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_READ = 0x1;
    private const uint FILE_SHARE_WRITE = 0x2;
    private const uint OPEN_EXISTING = 3;
    private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

    private const int STD_INPUT_HANDLE = -10;
    private const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
    private const uint ENABLE_EXTENDED_FLAGS = 0x0080;

    private const uint MB_OK = 0x00000000;
    private const uint MB_YESNO = 0x00000004;
    private const uint MB_ICONERROR = 0x00000010;
    private const uint MB_ICONWARNING = 0x00000030;
    private const int IDNO = 7;

    private const string GUID_CONSOLE_CLASIC = "{B23D10C0-E52E-411E-9D5B-C09FDF709C7D}";
    private const string REGISTRY_CALE_DELEGARE = @"Console\%%Startup%%";

    public GameObject meniuStart;

    private RadioButtonGroup alegereDependente, alegereArhitecturaInstalare;
    private RadioButtonGroup radioProcesare, alegereMoment, alegereMediu;
    private TextField inputNume, inputNumeA;
    private IntegerField inputNumar;
    private Button btnStartAntrenament, btnInapoi;

    private const string NumeFisierFlagInstalare = "instalare_ok.flag";

    private string projectPath;
    private string tempBatPath;

    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        alegereDependente = root.Q<RadioButtonGroup>("ADependente");
        alegereArhitecturaInstalare = root.Q<RadioButtonGroup>("AArhitectura");
        radioProcesare = root.Q<RadioButtonGroup>("AProcesare");
        alegereMoment = root.Q<RadioButtonGroup>("AMoment");
        alegereMediu = root.Q<RadioButtonGroup>("AMediu");

        inputNume = root.Q<TextField>("TNume");
        inputNumeA = root.Q<TextField>("TNumeA");
        inputNumar = root.Q<IntegerField>("TNumar");

        btnStartAntrenament = root.Q<Button>("BtnAntrenament");
        btnInapoi = root.Q<Button>("BtnInapoi");

        if (btnStartAntrenament != null)
        {
            btnStartAntrenament.clicked -= VerificareSiIncepere;
            btnStartAntrenament.clicked += VerificareSiIncepere;
            btnStartAntrenament.SetEnabled(true);
        }
        if (btnInapoi != null)
        {
            btnInapoi.clicked -= InapoiLaMeniulDeStart;
            btnInapoi.clicked += InapoiLaMeniulDeStart;
        }

        if (alegereDependente != null)
        {
            alegereDependente.UnregisterValueChangedCallback(OnDependenteSchimbat);
            alegereDependente.RegisterValueChangedCallback(OnDependenteSchimbat);
            ActualizeazaStareArhitecturaInstalare(alegereDependente.value);
        }

        if (alegereMediu != null)
        {
            alegereMediu.UnregisterValueChangedCallback(OnMediuSchimbat);
            alegereMediu.RegisterValueChangedCallback(OnMediuSchimbat);
            ConfigureazaOptiuniMediuDupaContext();
            ActualizeazaStareNumarInstante(alegereMediu.value);
        }

        projectPath = Directory.GetParent(Application.dataPath).FullName;
        tempBatPath = Path.Combine(projectPath, "start_antrenament_temp.bat");
    }

    private void ConfigureazaOptiuniMediuDupaContext()
    {
        if (alegereMediu == null) return;

        var butoaneRadio = alegereMediu.Query<RadioButton>().ToList();
        if (butoaneRadio.Count < 3) return;

        bool suntemInEditor = Application.isEditor;

        butoaneRadio[0].SetEnabled(suntemInEditor);
        butoaneRadio[1].SetEnabled(true);
        butoaneRadio[2].SetEnabled(!suntemInEditor);

        if ((alegereMediu.value == 0 && !suntemInEditor) || (alegereMediu.value == 2 && suntemInEditor))
        {
            alegereMediu.value = -1;
        }
    }

    private void OnMediuSchimbat(ChangeEvent<int> evt)
    {
        ActualizeazaStareNumarInstante(evt.newValue);
    }

    private void ActualizeazaStareNumarInstante(int valMediu)
    {
        if (inputNumar == null) return;
        inputNumar.SetEnabled(valMediu == 1);
    }

    private void OnDependenteSchimbat(ChangeEvent<int> evt)
    {
        ActualizeazaStareArhitecturaInstalare(evt.newValue);
    }

    private void ActualizeazaStareArhitecturaInstalare(int valDependente)
    {
        if (alegereArhitecturaInstalare == null) return;
        bool trebuieInstalat = valDependente == 0;
        alegereArhitecturaInstalare.SetEnabled(trebuieInstalat);
    }

    private void VerificareSiIncepere()
    {
        int valDependente = alegereDependente != null ? alegereDependente.value : -1;
        int valArhitecturaInstalare = alegereArhitecturaInstalare != null ? alegereArhitecturaInstalare.value : -1;
        int valHardware = radioProcesare != null ? radioProcesare.value : -1;
        int valMod = alegereMoment != null ? alegereMoment.value : -1;
        int valMediu = alegereMediu != null ? alegereMediu.value : -1;
        int instante = inputNumar != null ? inputNumar.value : 0;

        string numeAntrenament = inputNume != null ? inputNume.value : "";
        string numeInit = inputNumeA != null ? inputNumeA.value : "";

        if (string.IsNullOrWhiteSpace(numeAntrenament))
        {
            MessageBox(IntPtr.Zero, "Introduceti numele antrenarii!", "Date incomplete", MB_OK | MB_ICONERROR);
            return;
        }

        if (valDependente == -1)
        {
            MessageBox(IntPtr.Zero, "Selectati daca doriti sa se instaleze automat dependentele!", "Date incomplete", MB_OK | MB_ICONERROR);
            return;
        }

        if (valDependente == 0 && valArhitecturaInstalare == -1)
        {
            MessageBox(IntPtr.Zero, "Selectati arhitectura pe care doriti sa se instaleze dependentele (CPU / GPU Standard / GPU nou (seria RTX 5000+))!", "Date incomplete", MB_OK | MB_ICONERROR);
            return;
        }

        if (valHardware == -1)
        {
            MessageBox(IntPtr.Zero, "Selectati pe ce doriti sa se realizeze antrenarea modelului!", "Date incomplete", MB_OK | MB_ICONERROR);
            return;
        }

        if (valMod == -1)
        {
            MessageBox(IntPtr.Zero, "Alegeti daca doriti sa incepeti antrenamentul de la 0 sau sa-l continuati!", "Date incomplete", MB_OK | MB_ICONERROR);
            return;
        }

        if (valMediu == -1)
        {
            MessageBox(IntPtr.Zero, "Alegeti mediul de antrenare!", "Date incomplete", MB_OK | MB_ICONERROR);
            return;
        }

        if ((valMediu == 0 && !Application.isEditor) || (valMediu == 2 && Application.isEditor))
        {
            MessageBox(IntPtr.Zero, "Aceasta optiune de mediu nu este valida in contextul curent (Editor / build). Selectati din nou mediul de antrenare!", "Date incorecte", MB_OK | MB_ICONERROR);
            return;
        }

        string numeExeAntrenare = "GolfSeasons.exe";
        string caleRelativaExeAntrenare = Application.isEditor
            ? Path.Combine("..", "Golfseasons_Build", numeExeAntrenare)
            : numeExeAntrenare;
        string caleBuild = Path.Combine(projectPath, caleRelativaExeAntrenare);
        bool folositeBuild = valMediu == 1 || valMediu == 2;

        if (valMediu == 1)
        {
            if (instante <= 0)
            {
                MessageBox(IntPtr.Zero, "Introduceti un numar valid de instante!", "Date incorecte", MB_OK | MB_ICONERROR);
                return;
            }

            if (instante > 20)
            {
                int dialogResult = MessageBox(IntPtr.Zero, $"Atentie! {instante} instante pot crea instabilitate sistemului.\n\nContinuati?", "Avertizare", MB_YESNO | MB_ICONWARNING);
                if (dialogResult == IDNO) return;
            }
        }

        if (folositeBuild && !File.Exists(caleBuild))
        {
            MessageBox(IntPtr.Zero, $"Nu exista build pentru a rula instantele.\n\nCale asteptata:\n{caleBuild}", "Build lipsa", MB_OK | MB_ICONERROR);
            return;
        }

        string caleFlagInstalare = Path.Combine(projectPath, NumeFisierFlagInstalare);
        if (File.Exists(caleFlagInstalare)) File.Delete(caleFlagInstalare);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("title AntrenamentGolfSeasons");
        sb.AppendLine($"cd /d \"{projectPath}\"");
        sb.AppendLine();
        sb.AppendLine("py --version >nul 2>&1");
        sb.AppendLine("if %ERRORLEVEL% EQU 0 goto python_ok");
        sb.AppendLine();
        sb.AppendLine("echo Python nu a fost gasit in sistem (sau nu a fost adaugat in variabilele PATH).");
        sb.AppendLine("echo Pentru a crea mediul virtual este necesar Python (recomandat 3.10.x).");
        sb.AppendLine();
        sb.AppendLine(":ask_python");
        sb.AppendLine("set /p instal_py=\"Doriti sa descarcam si sa instalam automat Python 3.10.11? (da/nu): \"");
        sb.AppendLine("if /i \"%instal_py%\"==\"da\" goto install_python");
        sb.AppendLine("if /i \"%instal_py%\"==\"nu\" goto cancel_python");
        sb.AppendLine("echo Raspuns invalid. Va rugam scrieti 'da' sau 'nu'.");
        sb.AppendLine("goto ask_python");
        sb.AppendLine();
        sb.AppendLine(":install_python");
        sb.AppendLine("echo.");
        sb.AppendLine("echo Se descarca Python 3.10.11...");
        sb.AppendLine("curl -# -o python_installer.exe https://www.python.org/ftp/python/3.10.11/python-3.10.11-amd64.exe");
        sb.AppendLine("echo.");
        sb.AppendLine("echo Se deschide installer-ul Python. Urmati pasii de instalare normal.");
        sb.AppendLine("echo IMPORTANT: la primul ecran, bifati optiunea \"Add python.exe to PATH\" inainte de a apasa Install!");
        sb.AppendLine("start /wait python_installer.exe");
        sb.AppendLine("del python_installer.exe");
        sb.AppendLine("echo.");
        sb.AppendLine("echo Instalarea s-a terminat!");
        sb.AppendLine();
        sb.AppendLine("REM Chiar daca s-a bifat \"Add to PATH\", acea bifa actualizeaza doar registry-ul Windows.");
        sb.AppendLine("REM Procesul jocului a pornit INAINTE de instalare, deci PATH-ul lui din memorie e tot cel vechi.");
        sb.AppendLine("REM Cautam calea reala de instalare direct din registry si o injectam manual in sesiunea curenta,");
        sb.AppendLine("REM ca sa functioneze imediat, fara sa mai fie nevoie de restart la joc.");
        sb.AppendLine("set \"PYDIR=\"");
        sb.AppendLine("for /f \"tokens=2,*\" %%A in ('reg query \"HKCU\\SOFTWARE\\Python\\PythonCore\\3.10\\InstallPath\" /ve 2^>nul ^| find \"REG_SZ\"') do set \"PYDIR=%%B\"");
        sb.AppendLine("if not defined PYDIR for /f \"tokens=2,*\" %%A in ('reg query \"HKLM\\SOFTWARE\\Python\\PythonCore\\3.10\\InstallPath\" /ve 2^>nul ^| find \"REG_SZ\"') do set \"PYDIR=%%B\"");
        sb.AppendLine("if not defined PYDIR for /f \"tokens=2,*\" %%A in ('reg query \"HKCU\\SOFTWARE\\Python\\PythonCore\\3.10-32\\InstallPath\" /ve 2^>nul ^| find \"REG_SZ\"') do set \"PYDIR=%%B\"");
        sb.AppendLine("if defined PYDIR set \"PATH=%PYDIR%;%PYDIR%Scripts;%PATH%\"");
        sb.AppendLine();
        sb.AppendLine("py --version >nul 2>&1");
        sb.AppendLine("if %ERRORLEVEL% EQU 0 goto python_ok");
        sb.AppendLine();
        sb.AppendLine("echo Nu am putut detecta automat noua instalare de Python.");
        sb.AppendLine("echo Inchideti complet jocul, redeschideti-l si apasati din nou 'Start Antrenament'.");
        sb.AppendLine("pause");
        sb.AppendLine("exit");
        sb.AppendLine();
        sb.AppendLine(":cancel_python");
        sb.AppendLine("echo Instalare anulata. Procesul de antrenament se va opri.");
        sb.AppendLine("pause");
        sb.AppendLine("exit");
        sb.AppendLine();
        sb.AppendLine(":python_ok");
        sb.AppendLine();

        sb.AppendLine("if exist \"ml_env\\Scripts\\activate.bat\" (");
        sb.AppendLine("    call \"ml_env\\Scripts\\activate.bat\"");
        sb.AppendLine(") else (");
        if (valDependente == 0)
        {
            sb.AppendLine("    echo Mediul Python nu exista. Se creeaza..");
            sb.AppendLine("    py -3.10 -m venv ml_env");
            sb.AppendLine("    call \"ml_env\\Scripts\\activate.bat\"");
        }
        else
        {
            sb.AppendLine("    echo Mediul Python nu a fost gasit!");
            sb.AppendLine("    echo Selectati \"Da\" la instalarea dependentelor sau instalati-le manual.");
            sb.AppendLine("    pause");
            sb.AppendLine("    exit");
        }
        sb.AppendLine(")");
        sb.AppendLine();

        if (valDependente == 0)
        {
            sb.AppendLine("python -m pip install --upgrade pip >nul");
            sb.AppendLine();

            if (valArhitecturaInstalare == 0)
            {
                sb.AppendLine("echo Se instaleaza versiunea pentru CPU");
                sb.AppendLine("pip install torch==2.1.1 torchvision==0.16.1 --index-url https://download.pytorch.org/whl/cpu");
            }
            else if (valArhitecturaInstalare == 1)
            {
                sb.AppendLine("echo Se instaleaza versiunea pentru GPU standard");
                sb.AppendLine("pip install torch==2.1.1 torchvision==0.16.1 --index-url https://download.pytorch.org/whl/cu121");
            }
            else if (valArhitecturaInstalare == 2)
            {
                sb.AppendLine("echo Se instaleaza versiunea pentru GPU nou");
                sb.AppendLine("pip install --pre torch --index-url https://download.pytorch.org/whl/cu130");
            }

            sb.AppendLine();
            sb.AppendLine("pip install mlagents==1.1.0 \"protobuf<3.21\"");

            sb.AppendLine();
            sb.AppendLine("pip install onnx onnxscript onnx_ir ml_dtypes --upgrade --no-deps");
            sb.AppendLine();
        }

        sb.AppendLine("if exist \"Patch\\encoders.py\" (");
        sb.AppendLine("    copy /Y \"Patch\\encoders.py\" \"ml_env\\Lib\\site-packages\\mlagents\\trainers\\torch_entities\\encoders.py\" >nul");
        sb.AppendLine("    copy /Y \"Patch\\distributions.py\" \"ml_env\\Lib\\site-packages\\mlagents\\trainers\\torch_entities\\distributions.py\" >nul");
        sb.AppendLine("    copy /Y \"Patch\\curiosity_reward_provider.py\" \"ml_env\\Lib\\site-packages\\mlagents\\trainers\\torch_entities\\components\\reward_providers\\curiosity_reward_provider.py\" >nul");
        sb.AppendLine("    copy /Y \"Patch\\utils.py\" \"ml_env\\Lib\\site-packages\\mlagents\\trainers\\torch_entities\\utils.py\" >nul");
        sb.AppendLine("    copy /Y \"Patch\\model_serialization.py\" \"ml_env\\Lib\\site-packages\\mlagents\\trainers\\torch_entities\\model_serialization.py\" >nul");
        sb.AppendLine(")");
        sb.AppendLine();

        sb.AppendLine($"echo ok > \"{NumeFisierFlagInstalare}\"");
        sb.AppendLine();

        sb.AppendLine("set \"PATH=%~dp0ml_env\\Scripts;%PATH%\"");
        sb.AppendLine("set PYTHONWARNINGS=ignore");
        sb.AppendLine("set GLOG_minloglevel=2");
        sb.AppendLine("set TORCH_CPP_LOG_LEVEL=ERROR");
        sb.AppendLine("set C10_LOG_LEVEL=ERROR");
        sb.AppendLine(valHardware == 1 ? "set CUDA_VISIBLE_DEVICES=0" : "set CUDA_VISIBLE_DEVICES=-1");
        sb.AppendLine();

        string cmd = $"mlagents-learn config_ant.yaml --run-id={numeAntrenament}";
        if (valMod == 0) cmd += " --force";
        else if (valMod == 1) cmd += " --resume";

        if (!string.IsNullOrWhiteSpace(numeInit)) cmd += $" --initialize-from={numeInit}";

        if (valHardware == 0) cmd += " --torch-device cpu";
        else if (valHardware == 1) cmd += " --torch-device cuda";

        if (valMediu == 1)
        {
            cmd += $" --env=\"{caleRelativaExeAntrenare}\" --num-envs={instante} --no-graphics";
        }
        else if (valMediu == 2)
        {
            cmd += $" --env=\"{caleRelativaExeAntrenare}\" --num-envs=1 --width 1920 --height 1080 --env-args -screen-fullscreen 1 -screen-width 1920 -screen-height 1080 -window-mode exclusivefullscreen";
        }

        sb.AppendLine($"call {cmd}");
        sb.AppendLine("echo Antrenamentul s-a oprit. Se inchid instantele ramase");
        sb.AppendLine("taskkill /IM GolfSeasons.exe /F >nul 2>&1");

        string caleLoguri = folositeBuild ? "build" : "editor";
        sb.AppendLine("if exist \"ImportLoguri.exe\" (");
        sb.AppendLine($"    \"ImportLoguri.exe\" {caleLoguri}");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine("echo done > \"antrenament_finalizat.flag\"");
        sb.AppendLine();

        sb.AppendLine($"del /q \"{NumeFisierFlagInstalare}\" >nul 2>&1");
        sb.AppendLine();

        sb.AppendLine("pause");
        sb.AppendLine("(goto) 2>nul & del \"%~f0\"");

        File.WriteAllText(tempBatPath, sb.ToString());

        string valoareOriginalaConsole = null;
        string valoareOriginalaTerminal = null;
        bool cheieFortata = false;

        try
        {
            using (RegistryKey cheie = Registry.CurrentUser.CreateSubKey(REGISTRY_CALE_DELEGARE))
            {
                if (cheie != null)
                {
                    valoareOriginalaConsole = cheie.GetValue("DelegationConsole") as string;
                    valoareOriginalaTerminal = cheie.GetValue("DelegationTerminal") as string;

                    cheie.SetValue("DelegationConsole", GUID_CONSOLE_CLASIC);
                    cheie.SetValue("DelegationTerminal", GUID_CONSOLE_CLASIC);
                    cheieFortata = true;

                    LogFisier($"Registry delegare consola fortat pe conhost clasic. Valori originale: Console={valoareOriginalaConsole ?? "null"}, Terminal={valoareOriginalaTerminal ?? "null"}");
                }
            }
        }
        catch (System.Exception ex)
        {
            LogFisier($"Nu s-a putut forta consola clasica prin registry: {ex}");
        }

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{tempBatPath}\"",
            UseShellExecute = true,
            CreateNoWindow = false
        };

        Process procesPornit = Process.Start(psi);

        if (cheieFortata)
        {
            StartCoroutine(RestaureazaRegistryDupaDelay(valoareOriginalaConsole, valoareOriginalaTerminal));
        }

        if (procesPornit != null)
        {
            Global.pidProcesAntrenament = procesPornit.Id;
        }

        if (valMediu == 0)
        {
            if (procesPornit != null)
            {
                StartCoroutine(DezactiveazaQuickEditMode(procesPornit.Id));
            }
            StartCoroutine(AsteaptaPornireaServerului(valDependente == 0, caleFlagInstalare));
        }
        else if (valMediu == 1 || valMediu == 2)
        {
            StartCoroutine(DezactiveazaSiIesire(procesPornit != null ? procesPornit.Id : -1));
        }
    }

    private void LogFisier(string mesaj)
    {
        try
        {
            string caleLog = Path.Combine(projectPath, "quickedit_debug.log");
            File.AppendAllText(caleLog, $"[{DateTime.Now:HH:mm:ss.fff}] {mesaj}\n");
        }
        catch { }
    }

    private IEnumerator RestaureazaRegistryDupaDelay(string valConsole, string valTerminal)
    {
        yield return new WaitForSecondsRealtime(1f);

        try
        {
            using (RegistryKey cheie = Registry.CurrentUser.CreateSubKey(REGISTRY_CALE_DELEGARE))
            {
                if (cheie != null)
                {
                    if (valConsole != null) cheie.SetValue("DelegationConsole", valConsole);
                    else cheie.DeleteValue("DelegationConsole", false);

                    if (valTerminal != null) cheie.SetValue("DelegationTerminal", valTerminal);
                    else cheie.DeleteValue("DelegationTerminal", false);
                }
            }
            LogFisier("Registry delegare consola restaurat la valorile originale.");
        }
        catch (System.Exception ex)
        {
            LogFisier($"Nu s-a putut restaura registry: {ex}");
        }
    }

    private IEnumerator DezactiveazaSiIesire(int pid)
    {
        if (pid != -1)
        {
            yield return StartCoroutine(DezactiveazaQuickEditMode(pid));
        }

        LogFisier("Se inchide Editorul / se face Quit dupa dezactivarea QuickEdit.");

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator DezactiveazaQuickEditMode(int pid)
    {
        LogFisier($"START coroutine pentru PID {pid}");

        yield return new WaitForSecondsRealtime(0.5f);
        LogFisier("Trecut de primul yield (0.5s)");

        const int maxIncercari = 10;
        bool reusit = false;

        for (int incercare = 0; incercare < maxIncercari && !reusit; incercare++)
        {
            LogFisier($"Incercarea {incercare + 1}/{maxIncercari}");

            try
            {
                bool okFree1 = FreeConsole();
                LogFisier($"FreeConsole(initial)={okFree1}, LastError={Marshal.GetLastWin32Error()}");

                bool okAttach = AttachConsole((uint)pid);
                LogFisier($"AttachConsole({pid})={okAttach}, LastError={Marshal.GetLastWin32Error()}");

                if (okAttach)
                {
                    IntPtr handleInput = CreateFile("CONIN$", GENERIC_READ | GENERIC_WRITE,
                        FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero);
                    bool handleValid = handleInput != INVALID_HANDLE_VALUE && handleInput != IntPtr.Zero;
                    LogFisier($"CreateFile(CONIN$) handleValid={handleValid}, LastError={Marshal.GetLastWin32Error()}");

                    if (handleValid)
                    {
                        bool okGet = GetConsoleMode(handleInput, out uint modCurent);
                        LogFisier($"GetConsoleMode ok={okGet}, mod=0x{modCurent:X4}, QuickEditActiv={(modCurent & ENABLE_QUICK_EDIT_MODE) != 0}");

                        if (okGet)
                        {
                            uint modNou = (modCurent & ~ENABLE_QUICK_EDIT_MODE) | ENABLE_EXTENDED_FLAGS | ENABLE_PROCESSED_INPUT;
                            bool okSet = SetConsoleMode(handleInput, modNou);
                            LogFisier($"SetConsoleMode ok={okSet}, modNou=0x{modNou:X4}, LastError={Marshal.GetLastWin32Error()}");

                            if (GetConsoleMode(handleInput, out uint modVerificare))
                            {
                                bool quickEditOprit = (modVerificare & ENABLE_QUICK_EDIT_MODE) == 0;
                                LogFisier($"Verificare: mod=0x{modVerificare:X4}, quickEditOprit={quickEditOprit}");
                                if (quickEditOprit) reusit = true;
                            }
                        }

                        CloseHandle(handleInput);
                    }

                    bool okFree2 = FreeConsole();
                    LogFisier($"FreeConsole(final)={okFree2}");
                }
            }
            catch (System.Exception ex)
            {
                LogFisier($"EXCEPTIE: {ex}");
            }

            if (!reusit)
            {
                yield return new WaitForSecondsRealtime(0.3f);
            }
        }

        LogFisier(reusit ? "SUCCES: QuickEdit dezactivat." : "ESEC: QuickEdit NU a fost dezactivat dupa toate incercarile.");
    }

    private IEnumerator AsteaptaPornireaServerului(bool seInstaleazaDependente, string caleFlagInstalare)
    {
        if (btnStartAntrenament != null) btnStartAntrenament.SetEnabled(false);

        if (seInstaleazaDependente)
        {
            while (!File.Exists(caleFlagInstalare))
            {
                yield return new WaitForSeconds(1f);
            }
        }

        yield return new WaitForSeconds(6f);

        Global.dinMeniu = true;
        Global.antrenament = true;
        Global.validare = false;
        SceneManager.LoadScene("GamePlay");
    }

    private void InapoiLaMeniulDeStart()
    {
        if (meniuStart != null) meniuStart.SetActive(true);
        gameObject.SetActive(false);
    }
}