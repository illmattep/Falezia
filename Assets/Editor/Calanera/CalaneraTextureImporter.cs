using UnityEditor;
using UnityEngine;

namespace Falezia.EditorTools.Calanera
{
    /// <summary>
    /// Impostazioni d'importazione della libreria materiali (doc 29), decise dal NOME del file:
    ///   *_a.png  albedo, sRGB
    ///   *_n.png  normal map
    ///   *_m.png  maschera lineare (R metallo · G occlusione · B altezza · A levigatezza)
    /// Tutte: ripetizione, mipmap, anisotropico 8, 2048. Nessuna impostazione a mano.
    /// </summary>
    public class CalaneraTextureImporter : AssetPostprocessor
    {
        private const string Root = "Assets/Art/Calanera/Textures/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var ti = (TextureImporter)assetImporter;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);

            ti.wrapMode = TextureWrapMode.Repeat;
            ti.mipmapEnabled = true;
            ti.anisoLevel = 8;
            ti.filterMode = FilterMode.Trilinear;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;

            // i segni (insegne, macchie, schiuma) sono decal: clamp, alpha vera, niente ripetizione
            bool segno = assetPath.Contains("/Textures/segni/");
            if (segno)
            {
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency = true;
                ti.maxTextureSize = 1024;
            }

            if (name.EndsWith("_n"))
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.sRGBTexture = false;
            }
            else if (name.EndsWith("_m"))
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = false;
                ti.alphaSource = TextureImporterAlphaSource.FromInput;
                ti.alphaIsTransparency = false;
                // la maschera porta l'altezza nel blu: niente mip "trasparenti"
                ti.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
            }
            else
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = true;
                if (!segno) ti.alphaSource = TextureImporterAlphaSource.None;
            }
        }
    }
}
