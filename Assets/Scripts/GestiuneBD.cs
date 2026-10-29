using System.IO;
using UnityEngine;
using System.Data;
using System.Collections.Generic;
using Mono.Data.Sqlite;
using System;
[DefaultExecutionOrder(-100)]
public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager instance;
    private string dbPath;
    private SqliteConnection conexiune;
    private readonly object lockDb = new();
    private readonly System.Random rand = new();
    private void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject); 
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scena, UnityEngine.SceneManagement.LoadSceneMode mod)
    {
        bool esteMeniu = scena.name == "MainMenu";

        if (esteMeniu)
        {

            if (!UtilitareBD.EsteModAntrenament)
            {
                Global.antrenament = false;
                Global.validare = false;
            }

            InchideConexiune();
            dbPath = null;
            return;
        }

        RecalculeazaDbPathDacaNecesar();
    }

    private void RecalculeazaDbPathDacaNecesar()
    {

        string numeScena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (numeScena == "MainMenu") return;

        bool testareDirectaEditor = Application.isEditor && !Global.dinMeniu;
        bool modIzolat = UtilitareBD.EsteModAntrenament || Global.antrenament || Global.validare || testareDirectaEditor;

        string dbPathNou = modIzolat
            ? UtilitareBD.InitializareBazaDeDate("db_golfseasons.db", $"db_golfseasons_{System.Diagnostics.Process.GetCurrentProcess().Id}.db")
            : UtilitareBD.InitializareBazaDeDate("db_golfseasons.db");

        if (dbPathNou == dbPath && conexiune != null && conexiune.State == ConnectionState.Open) return;

        dbPath = dbPathNou;
        InchideConexiune();
        DeschideConexiune();

    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        InchideConexiune();
    }
    private void OnApplicationQuit() => InchideConexiune();

    private void DeschideConexiune()
    {
        System.Random rand = new();
        try
        {
            conexiune = new SqliteConnection(dbPath);
            conexiune.Open();
            UtilitareBD.TimeOut(conexiune);
        }
        catch (System.Exception)
        {
            try { conexiune?.Close(); } catch { }
            conexiune = null;
            System.Threading.Thread.Sleep(rand.Next(200, 1000));
        }

    }
    private void InchideConexiune()
    {
        if (conexiune == null) return;
        try { conexiune.Close(); conexiune.Dispose(); SqliteConnection.ClearAllPools(); } catch { }
        conexiune = null;
    }
    private SqliteConnection ObtineConexiune()
    {
        string numeScena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (numeScena == "MainMenu")
        {
            if (dbPath == null)
            {

                dbPath = UtilitareBD.InitializareBazaDeDate("db_golfseasons.db");
            }
        }
        else
        {

            RecalculeazaDbPathDacaNecesar();
        }

        if (conexiune == null || conexiune.State != ConnectionState.Open) DeschideConexiune();
        return conexiune;
    }
    private void ExecutaComanda(string query, Action<SqliteCommand> parametrii = null)
    {
        lock (lockDb)
        {
            using var cmd = ObtineConexiune().CreateCommand();
            cmd.CommandText = query;
            parametrii?.Invoke(cmd);
            cmd.ExecuteNonQuery();
        }
    }
    public void CreeazaSalvare(int idSalvare, string numeSalvare) =>
        ExecutaComanda(@"INSERT OR IGNORE INTO Salvari (id_salvare, nume_salvare, data_creare, sezon_maxim_deblocat) VALUES (@id, @nume,@data,1)",
            cmd => { cmd.Parameters.AddWithValue("@id", idSalvare); cmd.Parameters.AddWithValue("@nume", numeSalvare); cmd.Parameters.AddWithValue("@data", System.DateTime.Now.ToString("dd/MM/yyyy")); });

    public int CreeazaSalvare(string numeSalvare)
    {
        lock (lockDb)
        {
            var connection = ObtineConexiune();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO Salvari (nume_salvare, data_creare, sezon_maxim_deblocat) VALUES (@nume, @data, 1); SELECT last_insert_rowid();";
            cmd.Parameters.Add(new SqliteParameter("@nume", numeSalvare));
            cmd.Parameters.Add(new SqliteParameter("@data", System.DateTime.Now.ToString("dd/MM/yyyy")));
            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
    public SetariNivel VerificaSiExtrageNivel(string numeAnotimp, string numeTraseu, int ordineScena)
    {
        lock (lockDb)
        {
            var connection = ObtineConexiune();
            int idAnotimp = GetOrInsert(connection, "Tip_nivele", "nume", numeAnotimp, "id_tip_nivel");
            int idTraseu = GetOrInsert(connection, "Trasee", "nume", numeTraseu, "id_traseu");

            SetariNivel setari = ExtrageDateNivel(connection, idAnotimp, idTraseu);

            if (setari == null)
            {
                CreazaNivelDefault(connection, idAnotimp, idTraseu, ordineScena, 3, 60);
                setari = ExtrageDateNivel(connection, idAnotimp, idTraseu);
            }
            else if (setari.ordine != ordineScena)
            {
                if (!UtilitareBD.EsteModAntrenament)
                    ExecutaComanda("UPDATE Nivele SET Ordine = @ordine WHERE id_nivel = @idNivel",
                    cmd => { cmd.Parameters.AddWithValue("@ordine", ordineScena); cmd.Parameters.AddWithValue("@idNivel", setari.id_nivel); });
                setari.ordine = ordineScena;
            }

            return setari;
        }
    }
    public void NivelInceput(int idSalvare, int idNivel)
    {
        lock (lockDb)
        {
            using var cmd = ObtineConexiune().CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO Progres_Nivele (id_nivel, id_salvare, Cel_mai_bun_scor, Cel_mai_bun_timp) VALUES (@idN, @idS, 99, 999)";
            cmd.Parameters.Add(new SqliteParameter("@idS", idSalvare));
            cmd.Parameters.Add(new SqliteParameter("@idN", idNivel));
            cmd.ExecuteNonQuery();
        }
    }
    public List<MeniuPrincipal.DateSalvare> ExtrageSalvari()
    {
        lock (lockDb)
        {
            List<MeniuPrincipal.DateSalvare> salvari = new();
            var connection = ObtineConexiune();
            if (connection == null) return salvari;
            using var cmd = connection.CreateCommand();

            cmd.CommandText = "SELECT id_salvare, nume_salvare, data_creare, sezon_maxim_deblocat FROM Salvari ORDER BY id_salvare ASC LIMIT 3";
            using (IDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    salvari.Add(new MeniuPrincipal.DateSalvare
                    {
                        id_salvare = reader.GetInt32(0),
                        nume_salvare = reader.GetString(1),
                        data_creare = reader.GetString(2),
                        sezon_maxim_deblocat = reader.GetInt32(3)
                    });
                }
            }
            return salvari;
        }
    }

    public void SalveazaProgresNivel(int idSalvare, int idNivel, int lovituri, int timpSecunde)
    {
        lock (lockDb)
        {
            using var cmdcheck = ObtineConexiune().CreateCommand();
            cmdcheck.CommandText = "SELECT Cel_mai_bun_scor FROM Progres_Nivele WHERE id_salvare = @idS AND id_nivel = @idN";
            cmdcheck.Parameters.Add(new SqliteParameter("@idS", idSalvare)); cmdcheck.Parameters.Add(new SqliteParameter("@idN", idNivel));
            var result = cmdcheck.ExecuteScalar();

            if (result != null && lovituri >= System.Convert.ToInt32(result)) return;

            string sql = result == null ? "INSERT INTO Progres_Nivele (id_nivel, id_salvare, Cel_mai_bun_scor, Cel_mai_bun_timp) VALUES (@idN, @idS, @scor, @timp)"
                    : "UPDATE Progres_Nivele SET Cel_mai_bun_scor = @scor, Cel_mai_bun_timp = @timp WHERE id_salvare = @idS AND id_nivel = @idN";
            ExecutaComanda(sql, cmd => { cmd.Parameters.AddWithValue("@idS", idSalvare); cmd.Parameters.AddWithValue("@idN", idNivel); cmd.Parameters.AddWithValue("@scor", lovituri); cmd.Parameters.AddWithValue("@timp", timpSecunde); });
        }
    }
    public void DeblocheazaSezon(int idSalvare, string anotimpUrmator)
    {
        lock (lockDb)
        {
            int idx = anotimpUrmator switch { "Vara" => 2, "Toamna" => 3, "Iarna" => 4, _ => 1 };
            ExecutaComanda("UPDATE Salvari SET sezon_maxim_deblocat = @idx WHERE id_salvare = @idS AND sezon_maxim_deblocat < @idx",
             cmd => { cmd.Parameters.AddWithValue("@idx", idx); cmd.Parameters.AddWithValue("@idS", idSalvare); });

        }
    }
    public struct DateScoreBoard { public int par, scor_best, timp_par, timp_best; }
    public List<DateScoreBoard> ExtrageDateScoreBoard(string numeAnotimp, int idSalvare)
    {
        lock (lockDb)
        {
            List<DateScoreBoard> dateNivele = new();
            using var cmd = ObtineConexiune().CreateCommand();

            cmd.CommandText = @"
                SELECT n.Scor_par, IFNULL(p.Cel_mai_bun_scor, 0) as scor, n.Timp_par, IFNULL(p.Cel_mai_bun_timp, 0) as timpScurs
                FROM Nivele n
                INNER JOIN Tip_nivele tn ON n.id_tip_nivel = tn.id_tip_nivel
                INNER JOIN Restrictii r ON n.id_restrictie = r.id_restrictie
                LEFT JOIN Progres_Nivele p ON p.id_nivel = n.id_nivel AND p.id_salvare = @idS
                WHERE tn.nume = @nume
                        ORDER BY n.Ordine ASC LIMIT 10";
            cmd.Parameters.Add(new SqliteParameter("@nume", numeAnotimp));
            cmd.Parameters.Add(new SqliteParameter("@idS", idSalvare));

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) dateNivele.Add(new DateScoreBoard { par = reader.GetInt32(0), scor_best = reader.GetInt32(1), timp_par = reader.GetInt32(2), timp_best = reader.GetInt32(3) });
            return dateNivele;
        }
    }
    public void StergeSalvare(int idSalvare)
    {
        ExecutaComanda("DELETE FROM Progres_Nivele WHERE id_salvare = @idS", cmd => cmd.Parameters.AddWithValue("@idS", idSalvare));
        ExecutaComanda("DELETE FROM Salvari WHERE id_salvare = @idS", cmd => cmd.Parameters.AddWithValue("@idS", idSalvare));
    }
    private int GetOrInsert(SqliteConnection conn, string tabel, string coloanaNume, string valoare, string coloanaId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"INSERT OR IGNORE INTO {tabel} ({coloanaNume}) VALUES (@val);
                            SELECT {coloanaId} FROM {tabel} WHERE {coloanaNume} = @val;";
        cmd.Parameters.AddWithValue("@val", valoare);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }
    private void CreazaNivelDefault(SqliteConnection conn, int idTip, int idTraseu, int ordine, int scorPar, int timpPar)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO Restrictii(limita_incercari, limita_timp) VALUES(12,120);
            INSERT INTO Nivele (id_tip_nivel, id_traseu, id_restrictie, Cod_nivel, Ordine, Scor_par, Timp_par) 
            VALUES (@idTip, @idTraseu, last_insert_rowid(), (SELECT IFNULL(MAX(Cod_nivel), 0) + 1 FROM Nivele), @ordine, @scorPar, @timpPar)";
        cmd.Parameters.AddWithValue("@idTip", idTip); cmd.Parameters.AddWithValue("@idTraseu", idTraseu);
        cmd.Parameters.AddWithValue("@ordine", ordine); cmd.Parameters.AddWithValue("@scorPar", scorPar); cmd.Parameters.AddWithValue("@timpPar", timpPar);
        cmd.ExecuteNonQuery();
    }
    private SetariNivel ExtrageDateNivel(SqliteConnection conn, int idTip, int idTraseu)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT n.id_nivel, n.Ordine, n.Scor_par, n.Timp_par, r.limita_incercari, IFNULL(r.limita_timp,120)
            FROM Nivele n INNER JOIN Restrictii r ON n.id_restrictie = r.id_restrictie
            WHERE n.id_tip_nivel = @idTip AND n.id_traseu = @idTraseu";
        cmd.Parameters.AddWithValue("@idTip", idTip); cmd.Parameters.AddWithValue("@idTraseu", idTraseu);
        using IDataReader reader = cmd.ExecuteReader();
        if (reader.Read())
            return new SetariNivel { id_nivel = reader.GetInt32(0), ordine = reader.GetInt32(1), par = reader.GetInt32(2), timp = reader.GetInt32(3), limitaLovituri = reader.GetInt32(4), limitaTimp = reader.IsDBNull(5) ? 120 : reader.GetInt32(5) };
        return null;
    }
    public void SezonDeblocat(int idSalvare, int indexSezon)
    {
        lock (lockDb)
        {
            ExecutaComanda("UPDATE Salvari SET sezon_maxim_deblocat = @idx WHERE id_salvare = @idS",
            cmd => { cmd.Parameters.AddWithValue("@idx", indexSezon); cmd.Parameters.AddWithValue("@idS", idSalvare); });
        }
    }
}