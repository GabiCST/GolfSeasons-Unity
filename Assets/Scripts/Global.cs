using System.Collections.ObjectModel;
public static class Global
{
    public static readonly ReadOnlyCollection<string> Sezoane = new ReadOnlyCollection<string>( new[]{ "Primavara" , "Vara", "Toamna", "Iarna"});
    public static bool dinMeniu = false;
    public static bool validare = false;
    public static bool antrenament = false;
    public static int pidProcesAntrenament = -1;
}