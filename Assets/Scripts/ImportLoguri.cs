using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Mono.Data.Sqlite;

namespace ImportLoguri
{
    class Program
    {
        static void Main(string[] args)
        {
            string bazaPath = AppDomain.CurrentDomain.BaseDirectory;
            string dbPath = Path.Combine(bazaPath, "LoguriAI.db");
            string folder = Path.Combine(bazaPath, "LoguriAgenti");

            if (!Directory.Exists(folder)) return;

            using (var conn = new SqliteConnection($"URI=file:{dbPath}"))
            {
                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS Loguri (
                        id_log INTEGER PRIMARY KEY AUTOINCREMENT,
                        Data_ora TEXT NOT NULL,
                        Tip_bug TEXT NOT NULL,
                        Numar_lovituri INTEGER NOT NULL,
                        Sezon TEXT NOT NULL,
                        Traseu TEXT NOT NULL,
                        Pozitie_bila_precedenta_X REAL NOT NULL,
                        Pozitie_bila_precedenta_Y REAL NOT NULL,
                        Pozitie_bila_precedenta_Z REAL NOT NULL,
                        Pozitie_bila_curenta_X REAL NOT NULL,
                        Pozitie_bila_curenta_Y REAL NOT NULL,
                        Pozitie_bila_curenta_Z REAL NOT NULL
                    )";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_verificare ON Loguri (Traseu)";
                    cmd.ExecuteNonQuery();
                }

                var fisiereCsv = new List<string>(Directory.GetFiles(folder, "*loguri_agent*.csv"));
                if (fisiereCsv.Count == 0) return;

                int buguriImportate = 0;

                foreach (var fisier in fisiereCsv)
                {
                    var linii = File.ReadAllLines(fisier);
                    if (linii.Length == 0) continue;

                    var headers = linii[0].Split(';');
                    int idxTipBug = Array.IndexOf(headers, "TipBug");
                    if (idxTipBug < 0) continue;

                    int idxDataOra = Array.IndexOf(headers, "DataOra");
                    int idxNumarLovituri = Array.IndexOf(headers, "NumarLovituri");
                    int idxSezon = Array.IndexOf(headers, "Sezon");
                    int idxTraseu = Array.IndexOf(headers, "Traseu");
                    int idxPozPrec = Array.IndexOf(headers, "PozitiePrecedenta");
                    int idxPozCurenta = Array.IndexOf(headers, "PozitieCurenta");

                    for (int i = 1; i < linii.Length; i++)
                    {
                        var valori = linii[i].Split(';');
                        if (valori.Length < headers.Length) continue;

                        string dataOra = idxDataOra >= 0 ? valori[idxDataOra] : "";
                        string tipBug = idxTipBug >= 0 ? valori[idxTipBug] : "";
                        int numarLovituri = 0;
                        if (idxNumarLovituri >= 0) int.TryParse(valori[idxNumarLovituri], out numarLovituri);
                        string sezon = idxSezon >= 0 ? valori[idxSezon] : "";
                        string traseu = idxTraseu >= 0 ? valori[idxTraseu] : "";

                        var (px, py, pz) = ParseVector3(idxPozPrec >= 0 ? valori[idxPozPrec] : null);
                        var (cx, cy, cz) = ParseVector3(idxPozCurenta >= 0 ? valori[idxPozCurenta] : null);
                        if (px == null || cx == null) continue;

                        bool esteDuplicat = false;
                        using (var selCmd = conn.CreateCommand())
                        {
                            selCmd.CommandText = @"SELECT Pozitie_bila_precedenta_X, Pozitie_bila_precedenta_Y, Pozitie_bila_precedenta_Z
                                FROM Loguri WHERE Tip_bug = @tipBug AND Sezon = @sezon AND Traseu = @traseu";
                            selCmd.Parameters.Add(new SqliteParameter("@tipBug", tipBug));
                            selCmd.Parameters.Add(new SqliteParameter("@sezon", sezon));
                            selCmd.Parameters.Add(new SqliteParameter("@traseu", traseu));

                            using (var reader = selCmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    double bx = reader.GetDouble(0);
                                    double by = reader.GetDouble(1);
                                    double bz = reader.GetDouble(2);
                                    double dist = Math.Sqrt(Math.Pow(bx - px.Value, 2) + Math.Pow(by - py.Value, 2) + Math.Pow(bz - pz.Value, 2));
                                    if (dist < 0.1) { esteDuplicat = true; break; }
                                }
                            }
                        }

                        if (!esteDuplicat)
                        {
                            using (var insCmd = conn.CreateCommand())
                            {
                                insCmd.CommandText = @"INSERT INTO Loguri (Data_ora, Tip_bug, Numar_lovituri, Sezon, Traseu,
                                    Pozitie_bila_precedenta_X, Pozitie_bila_precedenta_Y, Pozitie_bila_precedenta_Z,
                                    Pozitie_bila_curenta_X, Pozitie_bila_curenta_Y, Pozitie_bila_curenta_Z)
                                    VALUES (@dataOra, @tipBug, @numarLovituri, @sezon, @traseu, @px, @py, @pz, @cx, @cy, @cz)";
                                insCmd.Parameters.Add(new SqliteParameter("@dataOra", dataOra));
                                insCmd.Parameters.Add(new SqliteParameter("@tipBug", tipBug));
                                insCmd.Parameters.Add(new SqliteParameter("@numarLovituri", numarLovituri));
                                insCmd.Parameters.Add(new SqliteParameter("@sezon", sezon));
                                insCmd.Parameters.Add(new SqliteParameter("@traseu", traseu));
                                insCmd.Parameters.Add(new SqliteParameter("@px", px.Value));
                                insCmd.Parameters.Add(new SqliteParameter("@py", py.Value));
                                insCmd.Parameters.Add(new SqliteParameter("@pz", pz.Value));
                                insCmd.Parameters.Add(new SqliteParameter("@cx", cx.Value));
                                insCmd.Parameters.Add(new SqliteParameter("@cy", cy.Value));
                                insCmd.Parameters.Add(new SqliteParameter("@cz", cz.Value));
                                insCmd.ExecuteNonQuery();
                            }
                            buguriImportate++;
                        }
                    }
                }
                
                Console.WriteLine($"Numar buguri gasite: {buguriImportate}, baza de date a fost salvata cu numele LoguriAI");
            }
        }

        static (double?, double?, double?) ParseVector3(string vStr)
        {
            if (string.IsNullOrEmpty(vStr)) return (null, null, null);
            var match = Regex.Match(vStr, @"Vector3\((.*?),\s*(.*?),\s*(.*?)\)");
            if (!match.Success) return (null, null, null);

            if (double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double y) &&
                double.TryParse(match.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double z))
            {
                return (x, y, z);
            }
            return (null, null, null);
        }
    }
}