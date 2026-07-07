sampler2D uImage0 : register(s0);
sampler2D uImage1 : register(s1);
sampler2D uImage2 : register(s2);

float2 uSize;
float uProgress;
float uTime;

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 Main(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TexCoord;
    coords *= uSize;
    coords = (floor(coords / 2) * 2) / uSize;
    float targetScale = 400;
    float modX = uSize.x / targetScale;
    float modY = uSize.y / targetScale;
    
    float level = uProgress - (0.9f - coords.y * 0.9f);
    float2 waveCoords = coords + float2(uTime * 0.4f, uTime * 0.2f);
    waveCoords *= float2(modX, modY);
    waveCoords = frac(waveCoords * 3);
    float4 wave = tex2D(uImage2, waveCoords);
    level += wave.r * 0.1f - 0.1f;
    
    float4 color = tex2D(uImage0, coords) * input.Color;
    
    float2 veinCoords = coords + float2(uTime * 1.5f, uTime * 0.2f);
    veinCoords += wave.r * 0.25f;
    veinCoords *= float2(modX, modY);
    veinCoords = frac(veinCoords);
    
    float gradientProgress = 1 - (coords.y * (uProgress + coords.y) * 0.5f + 0.5f);
    float3 top = float3(1, 1, 0.5f);
    float3 gradient = top * gradientProgress;

    float4 stuff = tex2D(uImage1, veinCoords);
    float veinStrength = 0.6f;
    color += stuff * (gradientProgress * veinStrength + (1 - veinStrength));
    color.rgb += gradient;
    
    float movingBand = (1 - ((coords.y * modY + wave.r * 0.15f) * 2 + uTime) % 0.7f);
    movingBand -= 0.6666f;
    movingBand = saturate(movingBand * 3);
    color += movingBand * 0.5f;
    
    float outlineWidth = 1 / uSize.y * 2;
    if (level <= 0 && level > -outlineWidth)
    {
        return float4(1, 1, 1, 1);
    }
    else if (level <= -outlineWidth)
    {
        return float4(0, 0, 0, 0);
    }

    int steps = 6;
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