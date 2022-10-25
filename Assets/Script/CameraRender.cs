using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRender : MonoBehaviour
{
    
    
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    const int maxVisibleLights = 4;
    
    //setup light properties
    private static int visibleLightColorId = Shader.PropertyToID("_VisibleLightColors");
    private static int visibleLightDirectionOrPosId = Shader.PropertyToID("_VisibleLightDirectionsOrPos");
    private static int VisibleLightAttensId = Shader.PropertyToID("_VisibleLightAttens");
    private static int visibleSpotLightDirId = Shader.PropertyToID("_visibleSpotLightDir");
             
    private Vector4[] visibleLightColor = new Vector4[maxVisibleLights];
    private Vector4[] visibleLightDirectionOrPos = new Vector4[maxVisibleLights];
    private Vector4[] visibleLightAttens = new Vector4[maxVisibleLights];
    private Vector4[] visibleSpotLightDir = new Vector4[maxVisibleLights];
    
    //setup unsupport shaders
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };
    
    static Material errorMaterial;

    private CullingResults _cullingResults;
    
    private ScriptableRenderContext _context;
    private Camera _camera;

    private CommandBuffer _camBuffer;

    private DrawingSettings _drawingSettings;
    private FilteringSettings _filterSettings;
    private SortingSettings _sortingSettings;
    
    public void RenderSingleCamera(ScriptableRenderContext c, Camera cam)
    {
        _context = c;
        _camera = cam;
        
        SetupSceneCam();
        
        if (!Cull(_camera))
        {
            return;
        }
        _context.SetupCameraProperties(_camera);
        _camBuffer = new CommandBuffer()
        {
            name = _camera.name
        };
        CameraClearFlags clearFlags = _camera.clearFlags;
        _camBuffer.ClearRenderTarget(
            (clearFlags & CameraClearFlags.Depth) != 0,
            (clearFlags & CameraClearFlags.Color) != 0,
            _camera.backgroundColor
        );

        SetupLights();
        SendInfoToGPU();
        _context.ExecuteCommandBuffer(_camBuffer);
        _camBuffer.Clear();

        DrawVisuableOpaque();
        _context.DrawSkybox(_camera);
        DrawVisuableTrans();
        DrawUnsupprtShaders();
        _context.Submit();
    }

    private void DrawUnsupprtShaders()
    {
        if (errorMaterial == null) {
            errorMaterial =
                new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        var drawingSettings = new DrawingSettings(
            legacyShaderTagIds[0], new SortingSettings(_camera)
        ) {
            overrideMaterial = errorMaterial
        };
        for (int i = 1; i < legacyShaderTagIds.Length; i++) {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        var filteringSettings = FilteringSettings.defaultValue;
        _context.DrawRenderers(
            _cullingResults, ref drawingSettings, ref filteringSettings
        );
    }
    private void DrawVisuableTrans()
    {
        _sortingSettings.criteria = SortingCriteria.CommonTransparent;
        _drawingSettings.sortingSettings = _sortingSettings;
        _filterSettings.renderQueueRange = RenderQueueRange.transparent;
        
        _context.DrawRenderers(_cullingResults, ref _drawingSettings,ref _filterSettings);
    }

    private void DrawVisuableOpaque()
    {
        _sortingSettings = new SortingSettings(_camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        _drawingSettings = new DrawingSettings(unlitShaderTagId, _sortingSettings);
        _filterSettings = new FilteringSettings(RenderQueueRange.opaque);
        _context.DrawRenderers(_cullingResults, ref _drawingSettings, ref _filterSettings);
    }

    private void SendInfoToGPU()
    {
        _camBuffer.BeginSample("Render Camera");
        _camBuffer.SetGlobalVectorArray(visibleLightColorId, visibleLightColor);
        _camBuffer.SetGlobalVectorArray(visibleLightDirectionOrPosId, visibleLightDirectionOrPos);
        _camBuffer.SetGlobalVectorArray(VisibleLightAttensId, visibleLightAttens);
        _camBuffer.SetGlobalVectorArray(visibleSpotLightDirId, visibleSpotLightDir);
        _camBuffer.EndSample("Render Camera");
    }
    
    private void SetupLights()
    {
        for (int n = 0; n < maxVisibleLights; n++)
        {
            visibleLightColor[n] = Color.clear;
            visibleLightDirectionOrPos[n] = Vector4.zero;
            visibleLightAttens[n] = Vector4.zero;
            visibleSpotLightDir[n] = Vector4.zero;
        }
        for (int n = 0; n < _cullingResults.visibleLights.Length; n++)
        {
            if (n == maxVisibleLights)
            {
                break;
            }

            Vector4 atten = Vector4.zero;
            atten.w = 1;
            
            VisibleLight light = _cullingResults.visibleLights[n];
            visibleLightColor[n] = light.finalColor;
            Vector4 v;
            if (light.lightType == LightType.Directional)
            {
                v = light.localToWorldMatrix.GetColumn(2);
                v.x = -v.x;
                v.y = -v.y;
                v.z = -v.z;
                visibleLightDirectionOrPos[n] = v;
            }
            else
            {
                v = light.localToWorldMatrix.GetColumn(3);
                atten.x = 1.0f / Mathf.Max(light.range * light.range, 0.00001f); 
                visibleLightDirectionOrPos[n] = v;
                if (light.lightType == LightType.Spot)
                {
                    v = light.localToWorldMatrix.GetColumn(2);
                    v.x = -v.x;
                    v.y = -v.y;
                    v.z = -v.z;
                    visibleSpotLightDir[n] = v;

                    float outterRad = Mathf.Deg2Rad * 0.5f * light.spotAngle;
                    float outterCos = Mathf.Cos(outterRad);
                    float outterTan = Mathf.Tan(outterRad);
                    float innerCos =
                        Mathf.Cos(Mathf.Atan(((46f / 64f) * outterTan)));
                    float angelRange = Mathf.Max(innerCos - outterCos, 0.001f);
                    atten.z = 1 / angelRange;
                    atten.w = -outterCos * atten.z;
                }
                
            }
           
            visibleLightAttens[n] = atten;
        }
    }
    
    private void SetupSceneCam()
    {
        if (_camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(_camera);
        }
    }
    
    private bool Cull(Camera cam)
    {
        ScriptableCullingParameters cullingParameters;
        if (!cam.TryGetCullingParameters(out cullingParameters))
        {
            return false;
        }

        _cullingResults = _context.Cull(ref cullingParameters);
        return true;

    }
    
    

}
