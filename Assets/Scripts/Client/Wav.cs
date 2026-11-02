using System;
using System.IO;
using UnityEngine;

/// <summary>
/// LETTORE WAV PER STREAMINGASSETS.
///
/// I suoni delle armi stanno in `Assets/StreamingAssets/audio/weapons/&lt;id&gt;/` come WAV
/// PCM 16 bit, gli stessi file che rigenera `./run.sh sound`. Da StreamingAssets Unity non
/// importa niente: non ci sono clip pronte, quindi il file si legge e si converte qui.
///
/// Solo PCM 16 bit, mono o stereo: è quello che scrive `tools/audio/make_weapon_sounds.py`.
/// Se un file manca o è in un altro formato si restituisce `null` e il gioco resta muto
/// per quel suono, senza errori: un'arma senza audio non deve fermare una partita.
/// </summary>
public static class Wav
{
    public static AudioClip Load(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            byte[] bytes = File.ReadAllBytes(path);
            var clip = Parse(bytes, Path.GetFileNameWithoutExtension(path));
            if (clip == null) Debug.LogWarning($"Wav: formato non supportato ({path})");
            return clip;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Wav: {path} non leggibile ({e.Message})");
            return null;
        }
    }

    /// <summary>Legge un RIFF/WAVE PCM 16 bit. `name` finisce nel nome della clip (diagnostica).</summary>
    public static AudioClip Parse(byte[] bytes, string name)
    {
        if (bytes.Length < 44) return null;
        if (Tag(bytes, 0) != "RIFF" || Tag(bytes, 8) != "WAVE") return null;

        int channels = 0, rate = 0, bits = 0, dataOffset = -1, dataLength = 0;
        int p = 12;
        while (p + 8 <= bytes.Length)
        {
            string id = Tag(bytes, p);
            int size = BitConverter.ToInt32(bytes, p + 4);
            int body = p + 8;
            if (size < 0 || body + size > bytes.Length) size = bytes.Length - body;

            if (id == "fmt ")
            {
                // formato (2 byte), canali (2), frequenza (4), byte/s (4), allineamento (2), bit (2)
                channels = BitConverter.ToInt16(bytes, body + 2);
                rate = BitConverter.ToInt32(bytes, body + 4);
                bits = BitConverter.ToInt16(bytes, body + 14);
            }
            else if (id == "data")
            {
                dataOffset = body;
                dataLength = size;
            }

            p = body + size + (size & 1);   // i chunk sono allineati a 2 byte
        }

        if (channels <= 0 || rate <= 0 || bits != 16 || dataOffset < 0) return null;

        int frameBytes = channels * 2;
        int frames = dataLength / frameBytes;
        if (frames <= 0) return null;

        var samples = new float[frames * channels];
        for (int i = 0; i < samples.Length; i++)
        {
            short v = BitConverter.ToInt16(bytes, dataOffset + i * 2);
            samples[i] = v / 32768f;
        }

        var clip = AudioClip.Create(name, frames, channels, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static string Tag(byte[] b, int at) =>
        at + 4 <= b.Length ? new string(new[] { (char)b[at], (char)b[at + 1], (char)b[at + 2], (char)b[at + 3] }) : "";
}
