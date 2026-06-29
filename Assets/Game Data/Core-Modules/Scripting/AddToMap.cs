using MTAssets.EasyMinimapSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddToMap : MonoBehaviour
{
    public MinimapItem minimapitem;
    private void OnEnable()
    {
        //if (MinmapManager.instance.MiniMap.MinimapReneder.isMinimapItemAddedToHighlight(minimapitem) == false)
        //    MinmapManager.instance.MiniMap.MinimapReneder.AddMinimapItemToBeHighlighted(minimapitem);
            
        if (MinmapManager.instance.LargemapRender.isMinimapItemAddedToHighlight(minimapitem) == false)
            MinmapManager.instance.LargemapRender.AddMinimapItemToBeHighlighted(minimapitem);


    }
    private void OnDisable()
    {
        //if (GangsterStory.instance.MiniMap.MinimapReneder.isMinimapItemAddedToHighlight(minimapitem) == true)
        //    GangsterStory.instance.MiniMap.MinimapReneder.RemoveMinimapItemOfHighlight(minimapitem);

        if (MinmapManager.instance.LargemapRender.isMinimapItemAddedToHighlight(minimapitem) == true)
            MinmapManager.instance.LargemapRender.RemoveMinimapItemOfHighlight(minimapitem);
    }
}