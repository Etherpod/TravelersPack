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
    private ScreenPrompt _unpackPrompt;
    private FirstPersonManipulator _manipulator;
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
        LoadManager.OnStartSceneLoad += OnStartSceneLoad;

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

        GameObject pack = (GameObject)_assetBundle.LoadAsset("Assets/TravelersPack/Backpack.prefab");
        AssetBundleUtilities.ReplaceShaders(pack);
        _backpack = Instantiate(pack).GetComponent<BackpackController>();

        _unpackPrompt = new ScreenPrompt(InputLibrary.interactSecondary, "Place Traveler's Pack", 0, ScreenPrompt.DisplayState.Normal);

        ModHelper.Events.Unity.RunWhen(() => Locator._promptManager != null, () =>
        {
            Locator.GetPromptManager().AddScreenPrompt(_unpackPrompt, PromptPosition.UpperLeft, false);
            _manipulator = Locator.GetPlayerCamera().GetComponent<FirstPersonManipulator>();
            enabled = true;
        });
    }

    public void OnStartSceneLoad(OWScene previousScene, OWScene newScene)
    {
        if (previousScene == OWScene.SolarSystem || previousScene == OWScene.SolarSystem)
        {
            Locator.GetPromptManager().RemoveScreenPrompt(_unpackPrompt);
        }
    }

    private void Update()
    {
        if (InGame)
        {
            _unpackPrompt.SetVisibility(false);

            if (_backpack.IsVisible() || !OWInput.IsInputMode(InputMode.Character)
                || PlayerState.InDreamWorld())
            {
                return;
            }

            bool readyToPlace = Locator.GetPlayerController().IsGrounded()
                && !FocusedOnInteractible();

            if (readyToPlace && OWInput.IsNewlyPressed(InputLibrary.interactSecondary, InputMode.Character))
            {
                _backpack.PlaceBackpack();
            }

            _unpackPrompt.SetVisibility(true);

            if (readyToPlace)
            {
                _unpackPrompt.SetDisplayState(ScreenPrompt.DisplayState.Normal);
            }
            else
            {
                _unpackPrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
            }
        }
    }

    public bool FocusedOnInteractible()
    {
        return _manipulator.HasFocusedInteractible()
            || _manipulator.GetFocusedNomaiText() != null
            || _manipulator.GetFocusedItemSocket() != null
            || _manipulator.GetFocusedOWItem() != null;
    }

    public static AudioClip LoadAudio(string filepath)
    {
        return (AudioClip)Instance._assetBundle.LoadAsset(filepath);
    }

    public static void WriteDebugMessage(object msg)
    {
        Instance.ModHelper.Console.WriteLine(msg.ToString());
    }
}

[HarmonyPatch]
public static class TravelersPackPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemTool), nameof(ItemTool.SocketItem))]
    public static bool DisableSocketAudio(ItemTool __instance, OWItemSocket socket)
    {
        if (socket is BackpackItemSocket)
        {
            //Locator.GetPlayerAudioController().PlayInsertItem(this._heldItem.GetItemType());
            socket.PlaceIntoSocket(__instance._heldItem);
            __instance._heldItem = null;
            Locator.GetToolModeSwapper().UnequipTool();
            return false;
        }

        return true;
    }
}