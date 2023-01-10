using UnityEngine;

public static class ShaderIDCache
{
    public static readonly int PositionsId = Shader.PropertyToID("_Positions");
    public static readonly int DensityId = Shader.PropertyToID("_Density");
    public static readonly int ResolutionId = Shader.PropertyToID("_Resolution");
    public static readonly int RotationId = Shader.PropertyToID("_Rotation");
    
    public static readonly int HeightMapId = Shader.PropertyToID("_HeightMap");
    
    public static readonly int NoiseMapHeightId = Shader.PropertyToID("_NoiseMapHeight");
    public static readonly int TipColorId = Shader.PropertyToID("_TipColor");
}
