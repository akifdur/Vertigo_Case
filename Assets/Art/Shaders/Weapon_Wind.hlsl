void SampleNoise_float(float2 UV, float4 VertexColor, UnityTexture2D NoiseMap, out float Result)
{
    float edgesMask = smoothstep(0.0, 1, VertexColor.x);
    bool useLine = VertexColor.b < 0.5;
    float randomPerIsland = VertexColor.g;
        
    UV.y -= _Time.y * _Scroll_Speed * lerp(0.9, 1.3, randomPerIsland);
    float4 textureSample = SAMPLE_TEXTURE2D_LOD(NoiseMap.tex, NoiseMap.samplerstate, UV, 0);

    float lineTexture = textureSample.x;
    float noiseTexture = textureSample.y * 0.8;
    
    float noiseMask = 1;
    noiseMask *= smoothstep(0.5, 1, sin(UV.y * 2 + _Time.y * 0.1));
    noiseMask *= noiseMask;
    lineTexture += textureSample.y * noiseMask * 0.2;
    
    Result = useLine ? lineTexture : noiseTexture;
    Result *= edgesMask;
}


void DistortMesh_float(float2 UV, float4 VertexColor, out float3 Result)
{
    float t = (UV.y + VertexColor.y) * 6.283 * _Wave_Frequency;
    t += _Time.y * _Wave_Speed;
        
    float distortion = sin(t) * 0.01 * _Wave_Strength;
    
    Result = float3(0, distortion, 0);
}