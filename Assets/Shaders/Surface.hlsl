#ifndef SURFACE_INCLUDED
#define SURFACE_INCLUDED


struct Surface
{
    float3 worldPos;
    float3 normal;
    float3 viewDir;
    float4 surfaceColor;
};

Surface CreateSurface(float3 wp,float4 sc, float3 n)
{
    Surface surface;
    surface.worldPos = wp;
    //surface.clipPos = cp;
    //surface.viewDir = vd;
    surface.surfaceColor = sc;
    surface.normal = n;
    

    return surface;
}

#endif