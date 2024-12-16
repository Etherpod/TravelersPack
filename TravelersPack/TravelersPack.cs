﻿using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace TravelersPack;

public class TravelersPack : ModBehaviour
{
    private readonly bool debugEnabled = true;

    public static TravelersPack Instance;

    public bool MarkerEnabled { get; private set; }

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
        new Harmony("Etherpod.TravelersPack").PatchAll(Assembly.GetExecutingAssembly());

        _assetBundle = AssetBundle.LoadFromFile(Path.Combine(ModHelper.Manifest.ModFolderPath, "assets/travelerspack"));
        MarkerEnabled = ModHelper.Config.GetSettingsValue<bool>("enableMapMarker");
        QSBHelper.Initialize();

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

        GameObject pack = (GameObject)_assetBundle.LoadAsset("Assets/TravelersPack/Backpack.prefab");
        AssetBundleUtilities.ReplaceShaders(pack);
        _backpack = Instantiate(pack).GetComponent<BackpackController>();

        _unpackPrompt = new ScreenPrompt(InputLibrary.autopilot, "Place Traveler's Pack", 0, ScreenPrompt.DisplayState.Normal);

        ModHelper.Events.Unity.RunWhen(() => Locator._promptManager != null, () =>
        {
            Locator.GetPromptManager().AddScreenPrompt(_unpackPrompt, PromptPosition.UpperLeft, false);
            _manipulator = Locator.GetPlayerCamera().GetComponent<FirstPersonManipulator>();
            if (QSBHelper.InMultiplayer)
            {
                QSBHelper.Start();
            }
            enabled = true;
        });
    }

    public void OnStartSceneLoad(OWScene previousScene, OWScene newScene)
    {
        if (previousScene == OWScene.SolarSystem || previousScene == OWScene.EyeOfTheUniverse)
        {
            Locator.GetPromptManager().RemoveScreenPrompt(_unpackPrompt);
        }
    }

    private void Update()
    {
        if (InGame)
        {
            _unpackPrompt.SetVisibility(false);

            if (_backpack.IsVisible() || !_backpack.IsPackOwner() 
                || !OWInput.IsInputMode(InputMode.Character)
                || (PlayerState.InDreamWorld() && !PlayerState.IsWearingSuit()))
            {
                return;
            }

            bool readyToPlace = Locator.GetPlayerController().IsGrounded()
                && !FocusedOnInteractible();

            if (readyToPlace && OWInput.IsNewlyPressed(InputLibrary.autopilot, InputMode.Character))
            {
                _backpack.PlaceBackpack(Locator.GetPlayerTransform());
                if (QSBHelper.InMultiplayer)
                {
                    QSBHelper.SendUnpackMessage();
                }
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

    public BackpackController GetBackpack()
    {
        return _backpack;
    }

    public static void WriteDebugMessage(object msg)
    {
        if (Instance.debugEnabled)
        {
            Instance.ModHelper.Console.WriteLine(msg.ToString());
        }
    }

    public override void Configure(IModConfig config)
    {
        MarkerEnabled = config.GetSettingsValue<bool>("enableMapMarker");
        if (_backpack != null)
        {
            _backpack.GetComponent<BackpackDistanceMarker>().RefreshOwnVisibility();
        }
    }
}

[HarmonyPatch]
public static class TravelersPackPatches
{
    private static bool usingBackpack = false;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemTool), nameof(ItemTool.SocketItem))]
    public static void DisableInsertAudioPrefix(OWItemSocket socket)
    {
        usingBackpack = socket is BackpackItemSocket;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemTool), nameof(ItemTool.StartUnsocketItem))]
    public static void DisableRemoveAudioPrefix(OWItemSocket socket)
    {
        usingBackpack = socket is BackpackItemSocket;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerAudioController), nameof(PlayerAudioController.PlayInsertItem))]
    public static bool DisableItemInsertAudio()
    {
        if (usingBackpack)
        {
            usingBackpack = false;
            return false;
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerAudioController), nameof(PlayerAudioController.PlayRemoveItem))]
    public static bool DisableItemRemoveAudio()
    {
        if (usingBackpack)
        {
            usingBackpack = false;
            return false;
        }
        return true;
    }
}