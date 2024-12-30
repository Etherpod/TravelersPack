public interface ITravelersPackAPI
{
    /// <summary>
    /// Adds an item to the backpack if the pack isn't full.
    /// </summary>
    /// <param name="item">The item to place inside of the backpack.</param>
    /// <returns>True if the item was successfully added, false if the backpack is full already.</returns>
    public bool AddItem(OWItem item);

    /// <summary>
    /// Removes an item from the backpack and places it at the backpack's location.
    /// </summary>
    /// <param name="item">The item to remove from the backpack.</param>
    /// <returns>True if the item was successfully destroyed, false if the backpack does not contain the item.</returns>
    public bool RemoveItem(OWItem item);

    /// <summary>
    /// Removes every item from the backpack and places them at the backpack's location.
    /// </summary>
    /// <returns>An array of every removed item.</returns>
    public OWItem[] RemoveAllItems();

    /// <summary>
    /// Removes the currently selected item and places it at the backpack's location.
    /// </summary>
    /// <returns>The removed item, or null if the backpack is empty.</returns>
    public OWItem RemoveSelectedItem();

    /// <summary>
    /// Gets every item currently stored in the backpack.
    /// </summary>
    /// <returns>An array of the stored items, or null if the backpack is empty.</returns>
    public OWItem[] GetStoredItems();

    /// <summary>
    /// Gets the currently selected item.
    /// </summary>
    /// <returns>The currently selected item, or null if the backpack is empty.</returns>
    public OWItem GetSelectedItem();

    /// <summary>
    /// Sets whether the backpack is able to be placed on the ground. Disabling it still allows it to be picked up.
    /// </summary>
    public void SetPlacingEnabled(bool enabled);
}