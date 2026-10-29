using System.IO;
using System.Threading;
using System.Linq;
using UnityEngine;
using Mono.Data.Sqlite;

public class UtilitareBD
{
    private static readonly System.Random rnd = new();
    public static readonly bool EsteModAntrenament = Application.isBatchMode || System.Environment.GetCommandLineArgs().Any(a => a.ToLower().Contains("mlagents")) ;

    public static string InitializareBazaDeDate(string dbName, string destName = null)
    {
        string folderBaze = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Baze");
        string destinationPath = Path.Combine(folderBaze, destName ?? dbName);
        string sourcePath = Path.Combine(Application.streamingAssetsPath, dbName);
        if (!File.Exists(destinationPath))
        {
            System.Threading.Thread.Sleep(rnd.Next(0, 300));
            if (!File.Exists(destinationPath) && File.Exists(sourcePath)) CopiazaFisier(sourcePath, destinationPath);
        }
        return "URI=file:" + destinationPath;
    }
    private static void CopiazaFisier(string source, string dest)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dest));
        for (int i = 0; i < 15; i++)
            {
                try
                {
                    using FileStream fsSource = new(source, FileMode.Open, FileAccess.Read, FileShare.Read);
                    using FileStream fsDest = new(dest, FileMode.Create, FileAccess.Write, FileShare.None);
                    fsSource.CopyTo(fsDest); 
                    return;
                }
                catch (IOException) {  System.Threading.Thread.Sleep(rnd.Next(50, 200));}
            }
    }
    public static void TimeOut(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();

        cmd.CommandText = "PRAGMA journal_mode=WAL;PRAGMA busy_timeout = 30000;";
        cmd.ExecuteNonQuery();

    }
}
