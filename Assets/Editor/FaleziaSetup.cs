using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Falezia.EditorTools
{
    /// <summary>
    /// Prepara la scena di gioco: un GameObject con GameRoot, salvato in Assets/Scenes/Main.unity
    /// e messo come prima scena del build. Si può lanciare da menu (Falezia → Crea scena)
    /// o in batch: Unity -batchmode -quit -executeMethod Falezia.EditorTools.FaleziaSetup.CreateScene
    /// </summary>
    public static class FaleziaSetup
    {
        [MenuItem("Falezia/Crea scena di gioco")]
        public static void CreateScene()
        {
            Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var root = new GameObject("GameRoot");
            var game = root.AddComponent<GameRoot>();
            game.sunIntensity = 1f;              // bilanciato per il tonemap ACES
            game.mapFile = "calanera.json";

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true)
            };

            Debug.Log("Falezia: Assets/Scenes/Main.unity creata con GameRoot");
        }
    }
}
