sampler2D uBubbleTarget : register(s0);
sampler2D uScreenTarget : register(s1);
sampler2D uNoiseTexture : register(s2);

float uTime;
float2 uResolution;
float2 uScreenPos;
float2 uZoom;

float4 Main(float2 coords : TEXCOORD0) : COLOR0
{    
    coords = floor((coords * uResolution / uZoom) / 2) / uResolution * uZoom * 2;
    float4 bubbleData = tex2D(uBubbleTarget, coords);
    float2 normal = normalize((bubbleData.rg - float2(0.5f, 0.5f)) * 2);
    float value = bubbleData.a;
    if (value <= 0)
        return float4(0, 0, 0, 0);
    float radius = bubbleData.b * 256;
    
    float rimWidth = 9.f;
    float rim = saturate(1 - value * radius / rimWidth);
    rim = pow(rim, 10);
    
    float2 noiseCoords = coords / (uResolution.x / uResolution.y) / uZoom * 10;
    noiseCoords = frac(noiseCoords + float2(uTime * 0.1f, uTime * 0.2f));
    float4 noise = tex2D(uNoiseTexture, noiseCoords);
    
    normal += noise.r * 2.f - 1.f;
    coords -= normal * pow(value, 0.1f) / uResolution * radius * 0.2f;
    
    float pxOffset = 4;
    float cR = tex2D(uScreenTarget, coords + normal * (1 - value) / uResolution * pxOffset).r;
    float cG = tex2D(uScreenTarget, coords).g;
    float cB = tex2D(uScreenTarget, coords - normal * (1 - value) / uResolution * pxOffset).b;
   
    
    float4 color = float4(cR, cG, cB, tex2D(uScreenTarget, coords).a);
    rim += noise * (1 - value) * 0.2f;
    color += rim * 0.5f;
    
    float2 lightingCoords = bubbleData.rg + noise.r * 0.05f;
    float lighting = saturate(1 - distance(lightingCoords, float2(0.5f, 0.f)) * 2);
    float sideLighting = abs(bubbleData.r - 0.5f) * 2;
    sideLighting *= bubbleData.g;
    sideLighting = pow(sideLighting, 2);
    lighting += sideLighting;
    color += lighting * float4(1.f, 0.9f, 0.5f, 1.f);
    
    float shading = saturate(distance(lightingCoords, float2(0.5f, 0.66f)) * 2);
    shading = pow(shading, 0.6f);
    shading = max(shading, 0.5f);
    float3 shadowColor = float3(0.2f, 0.1f, 0.5f);
    color.rgb = lerp(color.rgb, shadowColor, 1 - shading);
    
    int steps = 12;
    color = floor(color * steps) / (steps - 1);

    return color;
}
technique MainTechnique
{
    pass MainPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}