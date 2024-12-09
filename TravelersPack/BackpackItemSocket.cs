using System.Collections.Generic;
using UnityEngine;

namespace TravelersPack;

public class BackpackItemSocket : OWItemSocket
{
    private List<OWItem> _storedItems = [];
    private int _storedItemIndex;
    private readonly int _maxItems = 9;

    public override bool AcceptsItem(OWItem item)
    {
        return true;
    }

    public override bool IsSocketOccupied()
    {
        return _storedItems.Count > 0;
    }

    public bool CanAddItems()
    {
        return _storedItems.Count < _maxItems;
    }

    public int GetNumberOfStoredItems()
    {
        return _storedItems.Count;
    }

    public int GetMaxItems()
    {
        return _maxItems;
    }

    public override bool PlaceIntoSocket(OWItem item)
    {
        _socketedItem = null;

        if (!AcceptsItem(item) || _socketedItem != null)
        {
            return false;
        }
        _socketedItem = item;
        _socketedItem.SocketItem(this._socketTransform, this._sector);
        //_socketedItem.PlaySocketAnimation();
        if (OnSocketablePlaced != null)
        {
            OnSocketablePlaced(_socketedItem);
        }
        enabled = true;

        if (!_storedItems.Contains(item))
        {
            _storedItems.Add(item);
            item.gameObject.SetActive(false);

            _storedItemIndex = _storedItems.Count - 1;
        }
        return true;
    }

    public override OWItem RemoveFromSocket()
    {
        //OWItem result = base.RemoveFromSocket();

        _removedItem = _socketedItem;
        _socketedItem = null;
        if (OnSocketableRemoved != null && _removedItem != null)
        {
            OnSocketableRemoved(_removedItem);
        }
        //_removedItem.PlayUnsocketAnimation();
        _removedItem.SetColliderActivation(true);
        enabled = true;
        OWItem result = _removedItem;

        if (_storedItems.Contains(result))
        {
            _storedItems.Remove(result);
            result.gameObject.SetActive(true);

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
