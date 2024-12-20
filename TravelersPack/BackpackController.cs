using UnityEngine;

namespace TravelersPack;

public class BackpackController : MonoBehaviour
{
    public delegate void BackpackPlaceEvent();
    public event BackpackPlaceEvent OnPlaceBackpack;
    public event BackpackPlaceEvent OnRetrieveBackpack;

    [SerializeField] private BackpackInteractVolume _interactVolume = null;
    [SerializeField] private BackpackItemSocket _socket = null;
    [SerializeField] private GameObject _meshParent = null;
    [SerializeField] private OWAudioSource _oneShotAudio = null;

    private bool _interactionEnabled = false;
    private bool _interactVolumeFocus = false;
    private bool _visible = false;
    private bool _ownsPack;
    private ScreenPrompt _cycleItemPrompt;
    private ScreenPrompt _packUpPrompt;
    private ScreenPrompt _emptyPrompt;
    private ScreenPrompt _fullPrompt;
    private AudioClip _equipPackClip;

    private void Awake()
    {
        _interactVolume.OnPressInteract += OnPressInteract;
        _interactVolume.OnGainFocus += OnGainFocus;
        _interactVolume.OnLoseFocus += OnLoseFocus;
        _cycleItemPrompt = new ScreenPrompt(InputLibrary.toolOptionLeft, InputLibrary.toolOptionRight,
            "<CMD1><CMD2> Cycle Selected Item", 
            ScreenPrompt.MultiCommandType.CUSTOM_BOTH, 0, ScreenPrompt.DisplayState.Normal, false);
        _packUpPrompt = new ScreenPrompt(TravelersPack.GetSelectedInput(), "Pack up", 0, ScreenPrompt.DisplayState.Normal);
        _emptyPrompt = new ScreenPrompt("No items stored");
        _fullPrompt = new ScreenPrompt("Backpack is full");
        _equipPackClip = TravelersPack.LoadAudio("Assets/TravelersPack/EquipPack.ogg");
        _ownsPack = QSBHelper.IsHost;
    }

    private void Start()
    {
        _emptyPrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
        _fullPrompt.SetDisplayState(ScreenPrompt.DisplayState.GrayedOut);
        SetVisibility(false);
    }

    private void Update()
    {
        OWItem item = Locator.GetToolModeSwapper().GetItemCarryTool().GetHeldItem();

        _cycleItemPrompt.SetVisibility(false);
        _packUpPrompt.SetVisibility(false);
        _emptyPrompt.SetVisibility(false);
        _fullPrompt.SetVisibility(false);

        // Player is holding something to store
        if (item != null)
        {
            if (_socket.CanAddItems())
            {
                if (!_interactionEnabled)
                {
                    SetInteractVisibility(true);
                    _interactionEnabled = true;
                }
                _interactVolume.ChangePrompt("Store " + item.GetDisplayName());
            }
            else if (OWInput.IsInputMode(InputMode.Character))
            {
                _fullPrompt.SetVisibility(true);
                if (_interactionEnabled)
                {
                    SetInteractVisibility(false);
                    _interactionEnabled = false;
                }
            }
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
            else if (OWInput.IsInputMode(InputMode.Character))
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
            if (OWInput.IsNewlyPressed(TravelersPack.GetSelectedInput()))
            {
                RetrieveBackpack(Locator.GetPlayerTransform(), true);
                if (QSBHelper.InMultiplayer)
                {
                    QSBHelper.SendRetrieveMessage();
                }
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
            if (QSBHelper.InMultiplayer)
            {
                QSBHelper.SendTakeItemMessage(_socket.GetCurrentItemIndex());
            }
            else
            {
                RemoveCurrentItem();
            }
        }
    }

    private void OnGainFocus()
    {
        _interactVolumeFocus = true;
        Locator.GetPromptManager().AddScreenPrompt(_emptyPrompt, PromptPosition.Center, false);
        Locator.GetPromptManager().AddScreenPrompt(_fullPrompt, PromptPosition.Center, false);
        Locator.GetPromptManager().AddScreenPrompt(_cycleItemPrompt, PromptPosition.Center, false);
        Locator.GetPromptManager().AddScreenPrompt(_packUpPrompt, PromptPosition.Center, false);
    }

    private void OnLoseFocus()
    {
        _interactVolumeFocus = false;
        Locator.GetPromptManager().RemoveScreenPrompt(_emptyPrompt, PromptPosition.Center);
        Locator.GetPromptManager().RemoveScreenPrompt(_fullPrompt, PromptPosition.Center);
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

    public bool IsPackOwner()
    {
        return _ownsPack;
    }

    public void SetSocketItemIndex(int index)
    {
        _socket.SetItemIndex(index);
    }

    public void RemoveCurrentItem()
    {
        Locator.GetToolModeSwapper().GetItemCarryTool().StartUnsocketItem(_socket);
        _oneShotAudio.PlayOneShot(AudioType.ToolTranslatorEquip);
    }

    private void SetInteractVisibility(bool visible)
    {
        _interactVolume.UpdateInteractionVisibility(visible);
    }

    public void PlaceBackpack(Transform playerTransform)
    {
        if (Physics.Raycast(playerTransform.position, -playerTransform.up,
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

            OnPlaceBackpack?.Invoke();
        }
    }

    public void RetrieveBackpack(Transform playerTransform, bool ownsPack)
    {
        SetVisibility(false);
        _oneShotAudio.PlayOneShot(_equipPackClip);
        transform.parent = playerTransform;
        transform.localPosition = Vector3.zero;
        _ownsPack = ownsPack;

        OnRetrieveBackpack?.Invoke();
    }

    public void RefreshPromptCommand()
    {
        _packUpPrompt._commandIdList[0] = TravelersPack.GetSelectedInput().CommandType;
        _packUpPrompt.RefreshCommandList();

        Locator.GetPromptManager().TriggerRebuild(_packUpPrompt);
    }

    private void OnDestroy()
    {
        _interactVolume.OnPressInteract -= OnPressInteract;
        _interactVolume.OnGainFocus -= OnGainFocus;
        _interactVolume.OnLoseFocus -= OnLoseFocus;
    }
}
