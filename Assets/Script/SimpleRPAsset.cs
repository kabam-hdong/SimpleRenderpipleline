using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Simple Pipeline")]
public class SimpleRPAsset : RenderPipelineAsset 
{
    protected override RenderPipeline CreatePipeline()
    {
        return new SimpleRP();
    }
}
