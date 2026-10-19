sampler2D uImage0 : register(s0);

#define TAU 6.283185307f
#define PIOVER2 1.57079632679f

float3 uColor;
float uTexSampleX; //at which point to sample
float uThickness;
bool uFill;
bool uPixelate;

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

float4 Main(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TexCoord;

    float encoded =
        floor(input.Color.r * 255.f + 0.5f) * 16777216.f +
        floor(input.Color.g * 255.f + 0.5f) * 65536.f +
        floor(input.Color.b * 255.f + 0.5f) * 256.f +
        floor(input.Color.a * 255.f + 0.5f);
    float radius = encoded / 65536.f;
    
    if (uPixelate)
        coords = floor((coords * radius)) / radius;
    float2 centered = coords - float2(0.5f, 0.5f);
    float distSq = abs(centered.x * centered.x + centered.y * centered.y);
    if (distSq > 0.25f)
        return float4(0, 0, 0, 0);
    
    if (uFill)
        return float4(uColor, 0.5f);
    
    float distPx = sqrt(distSq) * (2.f * radius);
    float innerRadius = max(radius - uThickness, 0.f);
    if (distPx < innerRadius + uThickness * 0.5f)
        return (0, 0, 0, 0);
 
    coords.x = uTexSampleX;
    coords.y = saturate((distPx - innerRadius) / uThickness);
    
    
    float4 color = tex2D(uImage0, coords);
    color.rgb *= uColor;
    return color;
}
technique MainTechnique
{
    pass MainPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}