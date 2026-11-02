using System.Reflection;

namespace Falezia.Control;

/// <summary>
/// Il pannello, incorporato nell'eseguibile come risorsa. Così il deploy è un file
/// solo: non c'è una cartella wwwroot da ricordarsi di copiare.
/// </summary>
public static class PanelHtml
{
    public static string Value { get; } = Load();

    private static string Load()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("Falezia.Control.Panel.html");
        if (stream == null)
            return "<!doctype html><meta charset=utf-8><h1>Pannello non incorporato</h1>" +
                   "<p>Manca la risorsa Falezia.Control.Panel.html nella build.</p>";
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
