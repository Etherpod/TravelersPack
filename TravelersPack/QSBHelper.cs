using System.Linq;

namespace TravelersPack;

public static class QSBHelper
{
    public static bool InMultiplayer => _api != null && _api.GetIsInMultiplayer();
    public static bool IsHost => _api == null || _api.GetIsHost();

    private static IQSBAPI _api;
    private static BackpackController _backpack;

    private static uint[] ConnectedIDs => _api.GetPlayerIDs().Where(id => id != _api.GetLocalPlayerID()).ToArray();

    public static void Initialize()
    {
        _api = TravelersPack.Instance.ModHelper.Interaction.TryGetModApi<IQSBAPI>("Raicuparta.QuantumSpaceBuddies");
        if (_api != null)
        {
            _api.RegisterHandler<uint>("unpack", ReceiveUnpack);
            _api.RegisterHandler<uint>("retrieve", ReceiveRetrieve);
            _api.RegisterHandler<int>("take-item", ReceiveTakeItem);
        }
    }

    public static void Start()
    {
        _backpack = TravelersPack.Instance.GetBackpack();
    }

    public static void SendUnpackMessage()
    {
        foreach (uint id in ConnectedIDs)
        {
            _api.SendMessage("unpack", _api.GetLocalPlayerID(), id, false);
        }
    }

    private static void ReceiveUnpack(uint from, uint target)
    {
        if (_api.GetPlayerBody(target) == null)
        {
            return;
        }
        _backpack.PlaceBackpack(_api.GetPlayerBody(target).transform);
    }

    public static void SendRetrieveMessage()
    {
        foreach (uint id in ConnectedIDs)
        {
            _api.SendMessage("retrieve", _api.GetLocalPlayerID(), id, false);
        }
    }

    private static void ReceiveRetrieve(uint from, uint target)
    {
        if (_api.GetPlayerBody(target) == null)
        {
            return;
        }
        _backpack.RetrieveBackpack(_api.GetPlayerBody(target).transform, false);
    }

    public static void SendTakeItemMessage(int index)
    {
        foreach (uint id in _api.GetPlayerIDs())
        {
            _api.SendMessage("take-item", index, id, true);
        }
    }

    private static void ReceiveTakeItem(uint from, int index)
    {
        _backpack.SetSocketItemIndex(index);
        if (from == _api.GetLocalPlayerID())
        {
            TravelersPack.Instance.ModHelper.Events.Unity.FireInNUpdates(_backpack.RemoveCurrentItem, 5);
        }
    }
}
