sampler2D uImage0 : register(s0);
sampler2D uImage1 : register(s1);

float uTime;
float3 uColor;

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 Main(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TexCoord;
    float dist = length(float2(0.5f, 0.5f) - coords) * 2;
    float2 noiseCoords = coords * dist;
    float mod = frac(input.Color.r);
    noiseCoords += float2(uTime * 0.3f * mod, uTime * -0.4f * mod);
    noiseCoords = frac(noiseCoords);
    float4 noise = tex2D(uImage1, noiseCoords);
    
    float4 color = tex2D(uImage0, coords) * float4(uColor, 1);
    color += pow(noise.r, 3) * color.a * 0.8f;
    
    float width = 0.05f;
    if (dist > 1 - width && dist < 1)
        return float4(1, 1, 1, 1);
    
    int steps = 10;
    color = floor(color * steps) / (steps - 1);
    
    color = lerp(color, color * float4(0.7f, 0.2f, 1, 1), pow(coords.y, 2));
    color = lerp(color, color * float4(2, 2, 0.8f, 1), pow(1 - coords.y, 2));
    
    return color;
}
technique MainTechnique
{
    pass MainPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}