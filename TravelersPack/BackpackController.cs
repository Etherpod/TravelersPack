using UnityEngine;

namespace TravelersPack;

public class BackpackController : MonoBehaviour
{
    [SerializeField] private BackpackInteractVolume _interactVolume = null;
    [SerializeField] private BackpackItemSocket _socket = null;
    [SerializeField] private GameObject _meshParent = null;
    [SerializeField] private OWAudioSource _oneShotAudio = null;

    private bool _interactionEnabled = false;
    private bool _interactVolumeFocus = false;
    private bool _visible = false;
    private ScreenPrompt _cycleItemPrompt;
    private ScreenPrompt _packUpPrompt;
    private ScreenPrompt _emptyPrompt;
    private AudioClip _equipPackClip;

    private void Awake()
    {
        _interactVolume.OnPressInteract += OnPressInteract;
        _interactVolume.OnGainFocus += OnGainFocus;
        _interactVolume.OnLoseFocus += OnLoseFocus;
        _cycleItemPrompt = new ScreenPrompt(InputLibrary.toolOptionLeft, InputLibrary.toolOptionRight,
            "<CMD1><CMD2> Cycle Selected Item", 
            ScreenPrompt.MultiCommandType.CUSTOM_BOTH, 0, ScreenPrompt.DisplayState.Normal, false);
        _packUpPrompt = new ScreenPrompt(InputLibrary.interactSecondary, "Pack up", 0, ScreenPrompt.DisplayState.Normal);
        _emptyPrompt = new ScreenPrompt("No items stored");
        _equipPackClip = TravelersPack.LoadAudio("Assets/TravelersPack/EquipPack.ogg");
    }

    private void Start()
    {
        _emptyPrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
        SetVisibility(false);
    }

    private void Update()
    {
        OWItem item = Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem();

        _cycleItemPrompt.SetVisibility(false);
        _packUpPrompt.SetVisibility(false);
        _emptyPrompt.SetVisibility(false);

        // Player is holding something to store
        if (item != null)
        {
            if (!_interactionEnabled)
            {
                SetInteractVisibility(true);
                _interactionEnabled = true;
            }
            _interactVolume.ChangePrompt("Store " + item.GetDisplayName());
        }
        // Player wants to remove something
        else
        {
            // Backpack has items
            if (_socket.IsSocketOccupied())
            {
                if (!_interactionEnabled)
                {
                    SetInteractVisibility(true);
                    _interactionEnabled = true;
                }

                _interactVolume.ChangePrompt("Remove " + _socket.GetSocketedItem().GetDisplayName() + _socket.GetDuplicateNumber(_socket.GetSocketedItem()));

                // Cycling items
                if (_interactVolumeFocus && _socket.GetNumberOfStoredItems() > 1 && OWInput.IsInputMode(InputMode.Character))
                {
                    _cycleItemPrompt.SetVisibility(true);
                    if (OWInput.IsNewlyPressed(InputLibrary.toolOptionRight))
                    {
                        _socket.CycleCurrentItem(true);
                        _oneShotAudio.PlayOneShot(AudioType.Menu_LeftRight);
                    }
                    else if (OWInput.IsNewlyPressed(InputLibrary.toolOptionLeft))
                    {
                        _socket.CycleCurrentItem(false);
                        _oneShotAudio.PlayOneShot(AudioType.Menu_LeftRight);
                    }
                }
            }
            // Backpack is empty
            else
            {
                _emptyPrompt.SetVisibility(true);
                if (_interactionEnabled)
                {
                    SetInteractVisibility(false);
                    _interactionEnabled = false;
                }
            }
        }

        if (_interactVolumeFocus && OWInput.IsInputMode(InputMode.Character))
        {
            _packUpPrompt.SetVisibility(true);
            if (OWInput.IsNewlyPressed(InputLibrary.interactSecondary))
            {
                SetVisibility(false);
                _oneShotAudio.PlayOneShot(_equipPackClip);
            }
        }
    }

    private void OnPressInteract()
    {
        if (!_interactionEnabled) return;

        OWItem item = Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem();
        if (item != null)
        {
            Locator.GetToolModeSwapper().GetItemCarryTool().SocketItem(_socket);
            _oneShotAudio.PlayOneShot(AudioType.ToolTranslatorUnequip);
        }
        else
        {
            item = _socket.RemoveFromSocket();
            Locator.GetToolModeSwapper().GetItemCarryTool().PickUpItemInstantly(item);
            _oneShotAudio.PlayOneShot(AudioType.ToolTranslatorEquip);
        }
    }

    private void OnGainFocus()
    {
        _interactVolumeFocus = true;
        Locator.GetPromptManager().AddScreenPrompt(_emptyPrompt, PromptPosition.Center, false);
        Locator.GetPromptManager().AddScreenPrompt(_cycleItemPrompt, PromptPosition.Center, false);
        Locator.GetPromptManager().AddScreenPrompt(_packUpPrompt, PromptPosition.Center, false);
    }

    private void OnLoseFocus()
    {
        _interactVolumeFocus = false;
        Locator.GetPromptManager().RemoveScreenPrompt(_emptyPrompt, PromptPosition.Center);
        Locator.GetPromptManager().RemoveScreenPrompt(_cycleItemPrompt, PromptPosition.Center);
        Locator.GetPromptManager().RemoveScreenPrompt(_packUpPrompt, PromptPosition.Center);
    }

    public void SetVisibility(bool visible)
    {
        if (!visible)
        {
            SetInteractVisibility(false);
            _interactVolume.LoseFocus();
            _interactionEnabled = false;
        }
        _meshParent.SetActive(visible);
        _interactVolume.SetInteractionEnabled(visible);
        enabled = visible;
        _visible = visible;
    }

    public bool IsVisible()
    {
        return _visible;
    }

    private void SetInteractVisibility(bool visible)
    {
        _interactVolume.UpdateInteractionVisibility(visible);
    }

    public void PlaceBackpack()
    {
        if (Physics.Raycast(Locator.GetPlayerTransform().position, -Locator.GetPlayerTransform().up, 
            out RaycastHit hit, 2f, OWLayerMask.physicalMask))
        {
            transform.position = hit.point;
            transform.up = hit.normal;
            Vector3 playerForward = -Locator.GetPlayerCameraController().transform.right;
            Vector3 projected = -Vector3.ProjectOnPlane(playerForward, transform.up);
            transform.LookAt(transform.position + projected, transform.up);
            transform.parent = hit.collider.GetAttachedOWRigidbody().transform;
            SetVisibility(true);
            _oneShotAudio.PlayOneShot(AudioType.LandingGrass);
        }
    }

    private void OnDestroy()
    {
        _interactVolume.OnPressInteract -= OnPressInteract;
        _interactVolume.OnGainFocus -= OnGainFocus;
        _interactVolume.OnLoseFocus -= OnLoseFocus;
    }
}
