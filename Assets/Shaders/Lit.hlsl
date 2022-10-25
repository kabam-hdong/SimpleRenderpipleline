#ifndef SIMPLERP_LIT_INCLUDED
#define SIMPLERP_LIT_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Surface.hlsl"

CBUFFER_START(UnityPerFrame)
    float4x4 unity_MatrixVP;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
CBUFFER_END

#define MAX_VISIBLE_LIGHTS 4

CBUFFER_START(_LightBuffer)
    float4  _VisibleLightColors[MAX_VISIBLE_LIGHTS];
    float4  _VisibleLightDirectionsOrPos[MAX_VISIBLE_LIGHTS];
    float4  _VisibleLightAttens[MAX_VISIBLE_LIGHTS];
    float4  _visibleSpotLightDir[MAX_VISIBLE_LIGHTS];
    
CBUFFER_END

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M  unity_WorldToObject

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"


UNITY_INSTANCING_BUFFER_START(PerInstance)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Color)
UNITY_INSTANCING_BUFFER_END(PerInstance)

float3 DiffuseLight(int index, Surface surface)
{
    float3 worldPos = surface.worldPos;
    float3 normal = surface.normal;
    float4 lightColor = _VisibleLightColors[index];
    float4 lightDirOrPos = _VisibleLightDirectionsOrPos[index];
    float3 lightVector = lightDirOrPos.xyz - worldPos * lightDirOrPos.w;
    float4 lightAtten = _VisibleLightAttens[index];
    float3 spotLightDir = _visibleSpotLightDir[index].xyz;

    
    float3 lightDir = normalize(lightVector);
    
    float diffuse = saturate(dot(normal, lightDir));

    float rangeFade = dot(lightVector, lightVector) * lightAtten.x;
    rangeFade = saturate(1.0 - rangeFade * rangeFade);
    rangeFade *= rangeFade;

    float spotFade = dot(spotLightDir, lightDir);
    spotFade = saturate(spotFade * lightAtten.z + lightAtten.w);
    spotFade *= spotFade;
    
    float distanceSqr = max(dot(lightVector, lightVector),0.00001);
    diffuse *= spotFade * rangeFade/distanceSqr;
    
    return diffuse * lightColor;
}

struct VertexInput {
    float4 pos : POSITION;
    float3 normal: NORMAL;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput {
    float4 clipPos :    SV_POSITION;
    float3 normal:      TEXCOORD0;
    float3 worldPos:    TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


VertexOutput LitPassVertex (VertexInput input) {
    VertexOutput output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float4 worldPos = mul(UNITY_MATRIX_M, float4(input.pos.xyz, 1.0));
    float3 normal = mul((float3x3) UNITY_MATRIX_M, input.normal);
    output.normal = normal;
    output.clipPos = mul(unity_MatrixVP, worldPos);
    output.worldPos = worldPos;
    return output;
}

float4 LitPassFragment (VertexOutput input) : SV_TARGET {
    UNITY_SETUP_INSTANCE_ID(input);
    float3 normal = normalize(input.normal);
    float4 albedo = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _Color);
    float3 worldPos = input.worldPos;

    Surface surface = CreateSurface(worldPos, albedo, normal);
    

    
    float3 diffuseColor = 0;
    for(int n = 0; n < MAX_VISIBLE_LIGHTS; n++)
    {
        diffuseColor += DiffuseLight(n, surface);
    }
    
    float3 finalColor = diffuseColor * albedo;
    return float4(finalColor, 1);
}

#endif 