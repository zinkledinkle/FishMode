sampler2D uImage0 : register(s0);
sampler2D uImage1 : register(s1);
sampler2D uImage2 : register(s2);

float2 uSize;
float2 uPosition;
float4 uBaseColor;
float uTime;
float uZoom;

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 sampleBlur(sampler2D s, float2 uv, float blurPx, float2 texel)
{
    if (blurPx <= 0)
        return tex2D(s, uv);
    float2 offset = texel * blurPx;
    float4 color = tex2D(s, uv) * 0.4f;
    color += tex2D(s, uv + float2(offset.x, 0)) * 0.15f;
    color += tex2D(s, uv + float2(-offset.x, 0)) * 0.15f;
    color += tex2D(s, uv + float2(0, offset.y)) * 0.15f;
    color += tex2D(s, uv + float2(0, -offset.y)) * 0.15f;
    return color;
}

float4 Main(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TexCoord;
    float4 color = tex2D(uImage0, coords) * input.Color;
    
    float level = 0.6f;
    
    level += uPosition.y * 0.05f;
    
    coords.y = 1 - coords.y;
    coords.x -= 0.5f;
    coords.y -= uPosition.y;
    float xOffset = uPosition.x;
    coords.x += xOffset;

    coords.y = level + (coords.y - level) / uZoom;
    coords.x = (coords.x - xOffset) / uZoom + xOffset;
    
    float gradientHeight = 0.1f;
    if (coords.y > level)
    {
        if (coords.y < level + gradientHeight)
        {
            return float4(0.5f, 0.6f, 0.8f, 1) * pow((1 - (coords.y - level) / gradientHeight), 4);
        }
        return float4(0, 0, 0, 0);
    }
    coords.y /= level;
    float distance = (1 - coords.y);
    coords.x /= distance;
    
    float blurRange = 0.3f / uZoom;
    float blurFactor = saturate((blurRange - distance) / blurRange);
    blurFactor = pow(blurFactor, 1.5f);
    
    float2 tx = 1 / uSize.xy;

    float2 waveCoords = coords - float2(uTime * 0.08f, uTime * 0.06f / distance);
    waveCoords.y /= 2 * distance;
    float4 waves = sampleBlur(uImage2, frac(waveCoords / 2), 100 * blurFactor, tx);
    
    float2 veinCoords = coords + float2(uTime * 0.1f, uTime * 0.03f / distance);
    veinCoords += waves.r * 0.5f;
    float4 vein = sampleBlur(uImage1, frac(veinCoords / 6), 100 * blurFactor, tx);
    vein.rgb = float3(0.3f, 0.9f, 0.4f) * vein.r;  
    color += vein;
    
    float edgeFalloff = saturate(distance * 5);
    color.rgb *= (edgeFalloff / 3 + 0.6666666f);
    
    float sunShine = (abs(0.5f - (input.TexCoord.x + (waves.r * 2 - 1) * 0.1f)) * 2) / (distance * 0.5f);
    sunShine = 1 - saturate(sunShine);
    sunShine = pow(sunShine, 1.8f);
    sunShine *= edgeFalloff;
    float veinLuminosity = (vein.r + vein.g + vein.b) / 3;
    color += sunShine * float4(0.9f, 0.8f, 0.2f, 1) * veinLuminosity * 3;
    
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