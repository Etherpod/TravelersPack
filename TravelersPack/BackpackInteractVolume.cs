using System;
using System.Reflection;

namespace TravelersPack;

public class BackpackInteractVolume : InteractReceiver
{
    private bool _showInteraction = false;
    private bool _promptsOnScreen = false;

    public override void UpdatePromptVisibility()
    {
        if (_showInteraction)
        {
            base.UpdatePromptVisibility();
            return;
        }

		_screenPrompt.SetVisibility(false);
		_noCommandIconPrompt.SetVisibility(false);
    }

    public void UpdateInteractionVisibility(bool visible)
    {
        _showInteraction = visible;
    }
}
