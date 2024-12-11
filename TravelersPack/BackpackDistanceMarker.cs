using System;
using UnityEngine;

namespace TravelersPack;

public class BackpackDistanceMarker : HUDDistanceMarker
{
    [SerializeField] private Transform _markerPointerTransform;
    [SerializeField] private BackpackController _backpack;

    private bool _lastRingWorldState = false;
    private bool _lastQuantumMoonState = false;
    private bool _lastCloakState = false;
    private OuterFogWarpVolume _outerFogWarpVolume;

    public override void Start()
    {
        base.Start();
        if (Locator.GetRingWorldController() != null)
        {
            Locator.GetRingWorldController().OnPlayerEnter += RefreshOwnVisibility;
            Locator.GetRingWorldController().OnPlayerExit += RefreshOwnVisibility;
        }
        if (_backpack != null)
        {
            _backpack.OnPlaceBackpack += OnPlaceBackpack;
            _backpack.OnRetrieveBackpack += OnRetrieveBackpack;
        }
    }

    public override void InitCanvasMarker()
    {
        _markerTarget = _markerPointerTransform;
        _markerLabel = "BACKPACK";
        _markerRadius = 0.5f;
        base.InitCanvasMarker();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Locator.GetRingWorldController() != null)
        {
            Locator.GetRingWorldController().OnPlayerEnter -= RefreshOwnVisibility;
            Locator.GetRingWorldController().OnPlayerExit -= RefreshOwnVisibility;
        }
    }

    private void OnPlaceBackpack()
    {
        _lastRingWorldState = Locator.GetRingWorldController() != null && Locator.GetRingWorldController().isPlayerInside;
        _lastQuantumMoonState = _quantumMoon != null && _quantumMoon.IsPlayerInside();
        _lastCloakState = Locator.GetCloakFieldController() != null && Locator.GetCloakFieldController().isPlayerInsideCloak;
        if (PlayerState.GetOuterFogWarpVolume() != null && PlayerState.GetOuterFogWarpVolume().IsOuterWarpVolume())
        {
            _outerFogWarpVolume = (OuterFogWarpVolume)PlayerState.GetOuterFogWarpVolume();
            _canvasMarker.SetOuterFogWarpVolume(_outerFogWarpVolume);
        }
        RefreshOwnVisibility();
        if (_outerFogWarpVolume != null)
        {
            Locator.GetMarkerManager().RequestFogMarkerUpdate();
        }
    }

    private void OnRetrieveBackpack()
    {
        RefreshOwnVisibility();
    }

    public override void RefreshOwnVisibility()
    {
        if (!_backpack.IsVisible() || !TravelersPack.Instance.MarkerEnabled)
        {
            _isVisible = false;
        }
        else
        {
            bool insideEye = Locator.GetEyeStateManager() != null && Locator.GetEyeStateManager().IsInsideTheEye();
            bool onQuantumMoon = _quantumMoon != null && (_quantumMoon.IsPlayerInside() || _lastQuantumMoonState);
            bool playerInRingWorld = Locator.GetRingWorldController() != null && Locator.GetRingWorldController().isPlayerInside;
            bool packInRingWorld = Locator.GetRingWorldController() != null && _lastRingWorldState;
            bool playerAndPackInCloak = true;
            if (Locator.GetCloakFieldController() != null)
            {
                playerAndPackInCloak = Locator.GetCloakFieldController().isPlayerInsideCloak == _lastCloakState;
            }
            _isVisible = !insideEye && !onQuantumMoon && !_translatorEquipped && !_inConversation && (_isWearingHelmet || _atFlightConsole)
                && playerInRingWorld == packInRingWorld && playerAndPackInCloak;
        }

        if (_canvasMarker != null)
        {
            _canvasMarker.SetVisibility(_isVisible);
        }
    }
}
