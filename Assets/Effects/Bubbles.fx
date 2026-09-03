sampler2D uImage0 : register(s0);
sampler2D uImage1 : register(s1);

float uTime;
float2 uResolution;
float2 uScreenPos;
float2 uZoom;

cbuffer BubbleBuffer : register(b0)
{
    uniform float3 uBubbles[128];
}
uint uBubbleCount;

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
};

//normalized 0-1 to closest bubble
float Field(float2 p, float threshold, out float2 normal)
{
    float result = 0;
    normal = float2(0, 0);
    float2 delta = float2(0, 0);
    float eps = 1e-6;
    //why the fuck does it not want to iterate over 128 but its totally okay with 64 twice
    [loop]
    for (uint i = 0; i < 64; i++)
    {
        if (i > uBubbleCount)
            break;
        delta = p - uBubbles[i].xy;
        float r = uBubbles[i].z;
        float dist = length(delta);
        float threshRadius = r / sqrt(max(threshold, eps));
        float n = saturate(1 - dist / threshRadius);
        result = max(result, n);
        normal += delta * n;
    }
    if (uBubbleCount > 64)
    {
        [loop]
        for (uint j = 64; j < 128; j++)
        {
            if (j > uBubbleCount)
                break;
            delta = p - uBubbles[j].xy;
            float r = uBubbles[j].z;
            float dist = length(delta);
            float threshRadius = r / sqrt(max(threshold, eps));
            float n = saturate(1 - dist / threshRadius);
            result = max(result, n);
            normal += delta * n;
        }
    }
    normal = normalize(-normal);
    return result;
}

float4 Main(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TexCoord;
    
    float2 worldPos = (coords - float2(0.5f, 0.5f)) / uZoom + float2(0.5f, 0.5f);
    worldPos *= uResolution;
    worldPos += uScreenPos;
    //worldPos = floor(worldPos / 2) * 2;
    float2 normal;
    float threshold = 0.9f;
    float value = Field(worldPos, threshold, normal);
    float invValue = value > 0 ? (1 - value) : 0;
    
    float2 noiseCoords = worldPos / uResolution;
    noiseCoords += float2(uTime * 0.01f, uTime * 0.02f);
    float4 noise = tex2D(uImage1, frac(noiseCoords * 16));
    
    value -= noise.r * 0.2f;
    
    float2 viewDir = float2(0, 1);
    viewDir.x += sin(uTime * 0.05f) * 0.1f;
    float f = saturate(1 - dot(viewDir, normal));
    float fauxFresnel = pow(f, 3);
    
    float2 lightDir = float2(-1, -1);
    float highlight = saturate(dot(normal, -lightDir));
    highlight *= (noise.r * 0.2f + 0.9f);
    highlight = pow(highlight, 9);
    float backlight = saturate(dot(normal, lightDir));
    backlight = pow(backlight, 0.7f);
    backlight *= (noise.r * 0.2f + 0.9f);
    
    float rim = pow(invValue, 4);
    
    float pxOffset = 10;
    float cR = tex2D(uImage0, coords + normal * rim / uResolution * pxOffset).r;
    float cG = tex2D(uImage0, coords).g;
    float cB = tex2D(uImage0, coords - normal * rim / uResolution * pxOffset).b;

    coords += normal / uResolution * value * 200;
    
    float4 color = float4(cR, cG, cB, tex2D(uImage0, coords).a);
    color += rim;
    color += float4(0.5f, 1, 0.5f, 1) * fauxFresnel * invValue * 0.4f;
    color += noise.r * float4(1, 0.6f, 0.3f, 1) * highlight * invValue * 0.35f;
    color += noise.r * float4(0.3f, 0.6f, 1, 1) * backlight * invValue * 0.35f;

    return color;
}
technique MainTechnique
{
    pass MainPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}