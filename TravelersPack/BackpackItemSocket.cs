using System.Collections.Generic;
using UnityEngine;

namespace TravelersPack;

public class BackpackItemSocket : OWItemSocket
{
    private List<OWItem> _storedItems = [];
    private int _storedItemIndex;

    public override bool AcceptsItem(OWItem item)
    {
        return true;
    }

    public override bool IsSocketOccupied()
    {
        return _storedItems.Count > 0;
    }

    public int GetNumberOfStoredItems()
    {
        return _storedItems.Count;
    }

    public override bool PlaceIntoSocket(OWItem item)
    {
        _socketedItem = null;
        if (base.PlaceIntoSocket(item))
        {
            if (!_storedItems.Contains(item))
            {
                _storedItems.Add(item);
                TravelersPack.WriteDebugMessage("Count: " + _storedItems.Count);
                item.SetVisible(false);

                _storedItemIndex = _storedItems.Count - 1;
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    public override OWItem RemoveFromSocket()
    {
        OWItem result = base.RemoveFromSocket();
        if (_storedItems.Contains(result))
        {
            _storedItems.Remove(result);
            TravelersPack.WriteDebugMessage("Count: " + _storedItems.Count);
            result.SetVisible(true);

            if (_storedItems.Count > 0)
            {
                _storedItemIndex = Mathf.Min(_storedItemIndex, _storedItems.Count - 1);
                _socketedItem = _storedItems[_storedItemIndex];
            }
        }
        return result;
    }

    public void CycleCurrentItem(bool forward)
    {
        if (forward)
        {
            _storedItemIndex++;
            if (_storedItemIndex > _storedItems.Count - 1)
            {
                _storedItemIndex = 0;
            }
        }
        else
        {
            _storedItemIndex--;
            if (_storedItemIndex < 0)
            {
                _storedItemIndex = _storedItems.Count - 1;
            }
        }
        _socketedItem = _storedItems[_storedItemIndex];
    }

    public string GetDuplicateNumber(OWItem item)
    {
        int count = 0;
        int targetCount = 0;
        if (_storedItems.Contains(item))
        {
            for (int i = 0; i < _storedItems.Count; i++)
            {
                if (_storedItems[i].GetDisplayName() == item.GetDisplayName())
                {
                    count++;
                }
                if (_storedItems[i] == item)
                {
                    targetCount = count;
                }
            }
        }
        return count > 1 ? $" ({targetCount})" : "";
    }
}
