using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System.IO;
using System.Reflection;
using OWML.Common.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TravelersPack;

public class TravelersPack : ModBehaviour
{
    private readonly bool debugEnabled = false;

    public static TravelersPack Instance;

    public int MaxItems { get; private set; }
    public bool MarkerEnabled { get; private set; }
    public bool IsWarpingBackToEye => NHAPI != null && NHAPI.IsWarpingBackToEye();

    public bool PlacingEnabled = true;
    
    public INewHorizons NHAPI;
    private AssetBundle _assetBundle;
    private BackpackController _backpack;
    private ScreenPrompt _unpackPrompt;
    private FirstPersonManipulator _manipulator;
    private InputConsts.InputCommandType _unpackInput;
    private int _itemLimit;

    private bool InGame => (LoadManager.GetCurrentScene() == OWScene.SolarSystem ||
        LoadManager.GetCurrentScene() == OWScene.EyeOfTheUniverse) && !IsWarpingBackToEye;

    public void Awake()
    {
        Instance = this;
    }

    public void Start()
    {
        new Harmony("Etherpod.TravelersPack").PatchAll(Assembly.GetExecutingAssembly());

        _unpackInput = ModHelper.RebindingHelper.RegisterRebindable("Unpack Binding", 
            "The button that places the backpack down at your feet.", 
            Key.X,GamepadBinding.DPadDown, 
            false, 0f);

        _assetBundle = AssetBundle.LoadFromFile(Path.Combine(ModHelper.Manifest.ModFolderPath, "assets/travelerspack"));
        MarkerEnabled = ModHelper.Config.GetSettingsValue<bool>("enableMapMarker");
        QSBHelper.Initialize();
        
        if (ModHelper.Interaction.ModExists("xen.NewHorizons"))
        {
            NHAPI = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
        }
        
        OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen); // We start on title screen
        LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        LoadManager.OnStartSceneLoad += OnStartSceneLoad;

        enabled = false;
    }

    public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
    {
        if ((newScene != OWScene.SolarSystem && newScene != OWScene.EyeOfTheUniverse) ||
            IsWarpingBackToEye)
        {
            enabled = false;
            return;
        }

        if (!QSBHelper.InMultiplayer || QSBHelper.IsHost)
        {
            MaxItems = _itemLimit;
        }
        
        GameObject pack = (GameObject)_assetBundle.LoadAsset("Assets/TravelersPack/Backpack.prefab");
        AssetBundleUtilities.ReplaceShaders(pack);
        _backpack = Instantiate(pack).GetComponent<BackpackController>();

        _unpackPrompt = new ScreenPrompt(GetSelectedInput(), "Place Traveler's Pack");
        
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
        if (IsWarpingBackToEye) return;
        
        if (previousScene == OWScene.SolarSystem || previousScene == OWScene.EyeOfTheUniverse)
        {
            Locator.GetPromptManager()?.RemoveScreenPrompt(_unpackPrompt);
            PlacingEnabled = true;
            _backpack = null;
            _unpackPrompt = null;
            _manipulator = null;
        }
    }

    private void Update()
    {
        if (InGame && _unpackPrompt != null)
        {
            _unpackPrompt.SetVisibility(false);

            if (_backpack.IsVisible() || !_backpack.IsPackOwner() || !PlacingEnabled
                || !OWInput.IsInputMode(InputMode.Character)
                || (PlayerState.InDreamWorld() && !PlayerState.IsWearingSuit()))
            {
                return;
            }

            bool readyToPlace = Locator.GetPlayerController().IsGrounded()
                && !FocusedOnInteractible();

            if (readyToPlace && OWInput.IsNewlyPressed(GetSelectedInput(), InputMode.Character))
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

    public static IInputCommands GetSelectedInput()
    {
        return InputLibrary.GetInputCommand(Instance._unpackInput);
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
        _itemLimit = Mathf.FloorToInt(config.GetSettingsValue<float>("itemLimit"));
        MarkerEnabled = config.GetSettingsValue<bool>("enableMapMarker");

        if (_unpackPrompt != null)
        {
            _unpackPrompt._commandIdList[0] = GetSelectedInput().CommandType;
            _unpackPrompt.RefreshCommandList();

            Locator.GetPromptManager().TriggerRebuild(_unpackPrompt);
        }

        if (_backpack != null)
        {
            _backpack.RefreshPromptCommand();
            _backpack.GetComponent<BackpackDistanceMarker>().RefreshOwnVisibility();
        }
    }

    public void SetItemLimitRemote(int limit)
    {
        MaxItems = limit;
        if (_backpack != null)
        {
            _backpack.GetItemSocket().SetMaxItemsRemote(limit);
        }
    }
    
    public override object GetApi()
    {
        return new TravelersPackAPI();
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