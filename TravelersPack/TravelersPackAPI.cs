using System;

namespace TravelersPack;

public class TravelersPackAPI : ITravelersPackAPI
{
    public bool AddItem(OWItem item)
    {
        if (TravelersPack.Instance.GetBackpack() != null)
        {
            return TravelersPack.Instance.GetBackpack().AddItem(item);
        }
        return false;
    }

    public bool RemoveItem(OWItem item)
    {
        if (TravelersPack.Instance.GetBackpack() != null)
        {
            return TravelersPack.Instance.GetBackpack().RemoveItem(item);
        }
        return false;
    }

    public OWItem[] RemoveAllItems()
    {
        BackpackController pack = TravelersPack.Instance.GetBackpack();
        if (pack != null)
        {
            OWItem[] removedItems = pack.GetItemSocket().GetStoredItems();
            if (removedItems.Length > 0)
            {
                foreach (OWItem item in pack.GetItemSocket().GetStoredItems())
                {
                    pack.RemoveItem(item);
                }
                return removedItems;
            }
        }
        return null;
    }

    public OWItem RemoveSelectedItem()
    {
        BackpackController pack = TravelersPack.Instance.GetBackpack();
        if (pack != null && pack.GetItemSocket().GetNumberOfStoredItems() != 0)
        {
            OWItem item = pack.GetItemSocket().GetSocketedItem();
            pack.RemoveCurrentItem();
            return item;
        }
        return null;
    }

    public OWItem[] GetStoredItems()
    {
        BackpackController pack = TravelersPack.Instance.GetBackpack();
        if (pack != null && pack.GetItemSocket().GetNumberOfStoredItems() != 0)
        {
            return pack.GetItemSocket().GetStoredItems();
        }
        return null;
    }

    public OWItem GetSelectedItem()
    {
        BackpackController pack = TravelersPack.Instance.GetBackpack();
        if (pack != null && pack.GetItemSocket().GetNumberOfStoredItems() != 0)
        {
            return pack.GetItemSocket().GetSocketedItem();
        }
        return null;
    }
}
