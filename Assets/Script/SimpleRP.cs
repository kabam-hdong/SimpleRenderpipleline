using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class SimpleRP : RenderPipeline
{
    // Start is called before the first frame update

    private CameraRender _camRender;
    public SimpleRP()
    {
        GraphicsSettings.lightsUseLinearIntensity = true;
        _camRender = new CameraRender();
    }
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        for (int n = 0; n < cameras.Length; n++)
        {
            Camera cam = cameras[n];
            _camRender.RenderSingleCamera(context,cam);
        }
    }
}
