using UnityEditor;
using UnityEngine;

/// <summary>
/// Import delle texture della mappa e del cielo: ripetizione, mipmap, niente compressione
/// aggressiva. Le impostazioni le decide il codice, così rigenerando le immagini
/// (`tools/map/make_map_pbr.py`, `make_map_textures.py`) non si deve toccare niente a mano.
///
/// Convenzione dei nomi in Resources/MapTex:
///   <tipo>_c  colore (sRGB) · <tipo>_n  normal map · <tipo>_m  maschera lineare
///   (R metallo, G occlusione, A levigatezza) · grass  erba con alfa per l'alpha-test.
/// </summary>
public class MapTextureImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/MapTex/") && !assetPath.Contains("/Resources/Sky/")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 4;
        importer.sRGBTexture = true;

        bool sky = assetPath.Contains("/Resources/Sky/");
        importer.maxTextureSize = sky ? 4096 : 1024;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        if (sky) return;

        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        if (name.EndsWith("_n"))
        {
            // normal map in convenzione OpenGL (quella di Unity): l'import la decodifica giusta
            importer.textureType = TextureImporterType.NormalMap;
            importer.sRGBTexture = false;
            importer.anisoLevel = 8;
        }
        else if (name.EndsWith("_m"))
        {
            // dati, non colore: niente conversione sRGB
            importer.sRGBTexture = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
        }
        else if (name == "grass")
        {
            // l'erba non deve sparire in lontananza: i mipmap conservano la copertura dell'alfa
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipMapsPreserveCoverage = true;
            importer.alphaTestReferenceValue = 0.45f;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
        else if (name.EndsWith("_c"))
        {
            importer.anisoLevel = 8;   // pavimenti e muri visti di taglio
        }
    }
}
