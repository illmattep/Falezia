using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Falezia.Sim
{
    /// <summary>
    /// Un lettore JSON minimo, senza dipendenze.
    ///
    /// Perché non System.Text.Json: la simulazione deve compilare **ovunque** — console
    /// per i test, server, Godot e Unity. Unity non include System.Text.Json nel profilo
    /// .NET Standard, e aggiungere un pacchetto NuGet per leggere una mappa è un rischio
    /// in più. Questo parser legge il formato delle mappe (oggetti, array, stringhe,
    /// numeri, booleani, null) in ~200 righe e non ha nessun riferimento esterno.
    /// </summary>
    public sealed class Json
    {
        public enum Kind { Object, Array, String, Number, Bool, Null }

        public Kind Type;
        public Dictionary<string, Json> Fields;   // Type == Object
        public List<Json> Items;                  // Type == Array
        public string Str = "";
        public double Num;
        public bool Bool;

        public int Count => Items?.Count ?? 0;
        public IReadOnlyList<Json> Array => Items ?? throw new InvalidDataException("atteso un array");
        public bool IsNull => Type == Kind.Null;

        /// <summary>Campo di un oggetto. Manca? È un errore del file, non un valore zero.</summary>
        public Json this[string key]
        {
            get
            {
                if (Fields != null && Fields.TryGetValue(key, out var v)) return v;
                throw new InvalidDataException($"campo JSON mancante: '{key}'");
            }
        }

        public bool TryGet(string key, out Json value)
        {
            value = null;
            return Fields != null && Fields.TryGetValue(key, out value);
        }

        public int Int() => (int)Math.Round(Num, MidpointRounding.AwayFromZero);
        public float Float() => (float)Num;
        public string Text() => Str ?? "";

        // ------------------------------------------------------------------ parser

        public static Json Parse(string text)
        {
            if (text == null) throw new InvalidDataException("JSON nullo");
            int i = 0;
            var v = ReadValue(text, ref i);
            Skip(text, ref i);
            if (i < text.Length) throw new InvalidDataException($"carattere inatteso in posizione {i}");
            return v;
        }

        private static void Skip(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
        }

        private static Json ReadValue(string s, ref int i)
        {
            Skip(s, ref i);
            if (i >= s.Length) throw new InvalidDataException("JSON troncato");

            char c = s[i];
            if (c == '{') return ReadObject(s, ref i);
            if (c == '[') return ReadArray(s, ref i);
            if (c == '"') return new Json { Type = Kind.String, Str = ReadString(s, ref i) };
            if (c == 't') { Expect(s, ref i, "true"); return new Json { Type = Kind.Bool, Bool = true }; }
            if (c == 'f') { Expect(s, ref i, "false"); return new Json { Type = Kind.Bool, Bool = false }; }
            if (c == 'n') { Expect(s, ref i, "null"); return new Json { Type = Kind.Null }; }
            return new Json { Type = Kind.Number, Num = ReadNumber(s, ref i) };
        }

        private static Json ReadObject(string s, ref int i)
        {
            var obj = new Json { Type = Kind.Object, Fields = new Dictionary<string, Json>() };
            i++;                     // {
            Skip(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return obj; }

            while (true)
            {
                Skip(s, ref i);
                if (i >= s.Length || s[i] != '"') throw new InvalidDataException($"chiave attesa in posizione {i}");
                string key = ReadString(s, ref i);

                Skip(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new InvalidDataException($"':' atteso dopo '{key}'");
                i++;

                obj.Fields[key] = ReadValue(s, ref i);

                Skip(s, ref i);
                if (i >= s.Length) throw new InvalidDataException("oggetto non chiuso");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return obj; }
                throw new InvalidDataException($"',' o '}}' atteso in posizione {i}");
            }
        }

        private static Json ReadArray(string s, ref int i)
        {
            var arr = new Json { Type = Kind.Array, Items = new List<Json>() };
            i++;                     // [
            Skip(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return arr; }

            while (true)
            {
                arr.Items.Add(ReadValue(s, ref i));
                Skip(s, ref i);
                if (i >= s.Length) throw new InvalidDataException("array non chiuso");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return arr; }
                throw new InvalidDataException($"',' o ']' atteso in posizione {i}");
            }
        }

        private static string ReadString(string s, ref int i)
        {
            i++;                     // "
            var sb = new System.Text.StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }

                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new InvalidDataException("sequenza \\u tronca");
                        sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                        i += 4;
                        break;
                    default: throw new InvalidDataException($"escape sconosciuto '\\{e}'");
                }
            }
            throw new InvalidDataException("stringa non chiusa");
        }

        private static double ReadNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' ||
                                    s[i] == '.' || s[i] == 'e' || s[i] == 'E')) i++;
            string slice = s.Substring(start, i - start);
            if (!double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                throw new InvalidDataException($"numero non valido: '{slice}'");
            return v;
        }

        private static void Expect(string s, ref int i, string word)
        {
            if (i + word.Length > s.Length || string.CompareOrdinal(s, i, word, 0, word.Length) != 0)
                throw new InvalidDataException($"atteso '{word}' in posizione {i}");
            i += word.Length;
        }
    }
}
