sampler2D uImage0 : register(s0);
sampler2D uImage1 : register(s1);
sampler2D uImage2 : register(s2);

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
    float dist = length(float2(0.5f, 0.5f) - coords) * 2;
    float2 noiseCoords = coords;
    float mod = frac(input.Color.r);
    float2 noiseCoords2 = coords;
    noiseCoords2 += float2(uTime * -0.2f * mod, uTime * 0.3f * mod);
    noiseCoords2 = frac(noiseCoords2 / 4);
    float4 noise2 = tex2D(uImage2, noiseCoords2);
    
    noiseCoords += noise2.r * 0.3f;
    dist += noise2.r * 0.45f;
    
    noiseCoords += float2(uTime * 0.7f * mod, uTime * -0.8f * mod);
    noiseCoords = frac(noiseCoords * 2);
    float4 noise = tex2D(uImage1, noiseCoords);
    
    float4 color = tex2D(uImage0, coords) * input.Color;
    color += noise.r * (1 - dist) * pow(1 - noise2.r, 0.4f);
    return color * 2;
}
technique MainTechnique
{
    pass MainPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}