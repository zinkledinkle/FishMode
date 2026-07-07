sampler2D uImage0 : register(s0);
sampler2D uImage1 : register(s1);
sampler2D uImage2 : register(s2);

float2 uSize;
float4 uBaseColor;
float uTime;
float uZoom;

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 Main(VertexShaderOutput input) : COLOR0
{
    float height = (1 - input.Color.r);
    float layer = input.Color.g;
    layer = lerp(0.2f, 1, layer);
    float panX = input.Color.b;
    float alpha = input.Color.a;
    
    float2 coords = input.TexCoord;
    
    coords *= uSize;
    coords = (floor(coords / 2) * 2) / uSize;
    coords *= uSize;
    
    float farScale = 3;
    float layerScale = lerp(farScale, 1, 1 - layer);
    float zoominess = lerp(1 / uZoom, 1, layer);
    float finalScale = layerScale * zoominess;
    
    coords = (coords - uSize * 0.5f) * finalScale + uSize * 0.5f;
    coords /= uSize;
    
    float targetScale = 400;
    float modX = uSize.x / targetScale;
    float modY = uSize.y / targetScale;
        
    float parallaxamt = 0.7f;
    coords.x += panX * parallaxamt * modX * layer;

    float4 color = tex2D(uImage0, coords) * uBaseColor;
    
    float level = coords.y - height;
    float gradient = (1 - level);
    gradient = saturate(gradient * 6 - 5);
    color.rgb += float3(0.1f, 0.7f, 0.4f) * gradient;
    color.rgb *= pow(1 - level, 3);
    color.rgb = lerp(color.rgb, color.rgb * float3(0.05f, 0, 0.3f), layer);
    
    float2 waveCoords = coords + float2(uTime * 0.05f + layer * 0.02f, uTime * -0.03f - layer * 0.02f);
    waveCoords *= float2(modX, modY);
    float4 wave = tex2D(uImage2, frac(waveCoords / 2));
    level += wave.r * 0.05f;
    level += sin(uTime * 4 + coords.x * 25 + layer * 5) * 0.015f;
    
    float2 veinCoords = frac(coords / 3) + float2(uTime * -0.01f, uTime * 0.005f);
    veinCoords.y += input.Color.r;
    veinCoords += wave.r * 0.03f;
    veinCoords += sin(wave.r + uTime * 0.01f + coords.x * coords.y) * 0.05f;
    veinCoords = frac(veinCoords * float2(modX, modY));
    float4 vein = tex2D(uImage1, veinCoords);
    color += vein * pow(gradient, 3) * 0.4f;
    
    float outlineWidth = 1 / uSize.y * 3 * (2 + layer);
    
    if (level <= 0 && level > -outlineWidth)
    {
        return float4(1, 1, 1, 1) * alpha;
    }
    else if (level <= -outlineWidth)
    {
        return float4(0, 0, 0, 0);
    }
    
    int steps = 15;
    color = floor(color * steps) / (steps - 1);
    
    return color * alpha;
}
technique MainTechnique
{
    pass MainPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}