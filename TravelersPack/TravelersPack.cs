using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace TravelersPack;

public class TravelersPack : ModBehaviour
{
    public static TravelersPack Instance;

    private AssetBundle _assetBundle;
    private BackpackController _backpack;
    private bool InGame => LoadManager.GetCurrentScene() == OWScene.SolarSystem ||
        LoadManager.GetCurrentScene() == OWScene.EyeOfTheUniverse;

    public void Awake()
    {
        Instance = this;
    }

    public void Start()
    {
        // Starting here, you'll have access to OWML's mod helper.
        ModHelper.Console.WriteLine($"My mod {nameof(TravelersPack)} is loaded!", MessageType.Success);

        new Harmony("Etherpod.TravelersPack").PatchAll(Assembly.GetExecutingAssembly());

        _assetBundle = AssetBundle.LoadFromFile(Path.Combine(ModHelper.Manifest.ModFolderPath, "assets/travelerspack"));

        // Example of accessing game code.
        OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen); // We start on title screen
        LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;

        enabled = false;
    }

    public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
    {
        if (newScene != OWScene.SolarSystem && newScene != OWScene.EyeOfTheUniverse)
        {
            enabled = false;
            return;
        }

        ModHelper.Console.WriteLine("Loaded into solar system!", MessageType.Success);

        GameObject cube = (GameObject)_assetBundle.LoadAsset("Assets/Backpack.prefab");
        AssetBundleUtilities.ReplaceShaders(cube);
        _backpack = Instantiate(cube).GetComponent<BackpackController>();

        enabled = true;
    }

    private void Update()
    {
        if (InGame)
        {
            if ((Locator.GetPlayerController()?.IsGrounded() ?? false)
                && OWInput.IsNewlyPressed(InputLibrary.interactSecondary, InputMode.Character)
                && !_backpack.IsVisible())
            {
                WriteDebugMessage("ah");
                _backpack.transform.position = Locator.GetPlayerTransform().position;
                _backpack.transform.parent = Locator.GetPlayerSectorDetector().GetLastEnteredSector().transform;
                _backpack.SetVisibility(true);
            }
        }
    }

    public static void WriteDebugMessage(object msg)
    {
        Instance.ModHelper.Console.WriteLine(msg.ToString());
    }
}
