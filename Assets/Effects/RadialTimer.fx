sampler2D uImage0 : register(s0);

float uProgress;

#define TAU 6.283185307f
#define PIOVER2 1.57079632679f

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 Main(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TexCoord;
    float2 centered = coords - float2(0.5f, 0.5f);
    float distSq = abs(centered.x * centered.x + centered.y * centered.y);
    if (distSq > 0.25f)
        return float4(0, 0, 0, 0);
    float angle = frac((atan2(centered.y, centered.x) + PIOVER2) / TAU);
    float thresh = (1 - uProgress);
    if (angle < thresh)
        return float4(0, 0, 0, 0);
    float4 color = tex2D(uImage0, coords);
    return color * input.Color;
}
technique MainTechnique
{
    pass MainPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}