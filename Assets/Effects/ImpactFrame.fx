sampler uImage0 : register(s0);
float2 uScreenResolution;
float2 uScreenPosition;
float2 uPosition;
float uIntensity;
float2 uZoom;
float uAngles [8];
float uWidths [8];

float4 Main(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    
    float intensity = uIntensity;
    float2 tx = coords * uScreenResolution;
    float2 delta = (uPosition - uScreenPosition) - tx;
    float dist = 1 - length(delta / min(uScreenResolution.x, uScreenResolution.y));
    dist = pow(dist, 3);
    float blurIntensity = intensity * dist;
    
    int count = 10;
    for (int i = 0; i < count; i++)
    {
        float2 blurCoords = tx - delta * intensity * (0.1f / count) * i;
        blurCoords /= uScreenResolution;
        blurCoords = saturate(blurCoords);
        float4 blur = tex2D(uImage0, blurCoords) * blurIntensity;
        color += blur / count;
    }
    
    float luminance = dot(color.rgb, float3(0.299f, 0.587f, 0.114f));
    color = float4(luminance, luminance, luminance, color.a);
    float contrastFactor = 5;
    float midpoint = 0.5f;
    color.rgb = ((color.rgb - midpoint) * contrastFactor) + midpoint;

    int amountOfRays = 10;
    for (int j = 0; j < 8; j++)
    {
        float angle = atan2(delta.y, delta.x);
        float angleToCheck = uAngles[j];
        if (angleToCheck == 0)
            continue;
        if (abs(angle - angleToCheck) < uWidths[j])
        {
            color += 3 * intensity;
        }
    }
    
    return floor(color * 10) / 9;
}
technique MainTechnique
{
    pass MainPass
    {
        PixelShader = compile ps_3_0 Main();
    }
}