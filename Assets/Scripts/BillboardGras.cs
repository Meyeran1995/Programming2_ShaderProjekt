using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BillboardGras : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Color grasColor;
    [SerializeField] private Color tipColor;
    [Space]
    [SerializeField, Min(0f)] private float quadWidth;
    [SerializeField, Min(0f)] private float quadHeight;
    [Space]
    [SerializeField, Min(0f)] private float density;
    [SerializeField, Range(1, 6)] private int numberOfQuads;
    [Header("Noise")]
    [SerializeField] private Texture noiseMapHeight;
    [SerializeField, Min(0f)] private float positionalNoiseStrength;
    [Header("References")]
    [SerializeField] private ComputeShader grasPositionCompute;
    [SerializeField] private Material grassMaterial;
    [SerializeField] private Terrain terrain;
    
    public ComputeShader Compute => grasPositionCompute;
    public ComputeBuffer PositionsBuffer { get; private set; }
    public ComputeBuffer ArgsBuffer { get; private set; }

    public Material[] GrasMaterials => materials.ToArray();

    private InspectorUpdateListener inspectorUpdater;
    private List<Material> materials;

    public void UpdateMaterialList()
    {
        int materialCount = materials.Count;
        
        if(numberOfQuads == materialCount) return;
        
        if (numberOfQuads > materialCount)
        {
            for (int i = numberOfQuads - materialCount; i > 0; i--)
            {
                materials.Add(new Material(grassMaterial));
            }
            return;
        }
        
        int materialsToDestroy = materialCount - numberOfQuads;
        
        for (int i = materialCount - 1; materialsToDestroy > 0; i--, materialsToDestroy--)
        {
            Destroy(materials[i]);
            materials.RemoveAt(i);
        }
    }

    public void DispatchBuffer(int resolutionSquared, int groups)
    {
        PositionsBuffer?.Release();
        //A compute buffer contains arbitrary untyped data. We have to specify the exact size of each element in bytes, via a second argument.
        //We need to store 3D position vectors, which consist of three float numbers, so the element size is three times the size of a float (four bytes).
        PositionsBuffer = new ComputeBuffer(resolutionSquared, 4 * sizeof(float));
        grasPositionCompute.SetBuffer(0, ShaderIDCache.PositionsId, PositionsBuffer);
        
        grasPositionCompute.Dispatch(0, groups, groups, 1);
    }
    
    private void Awake()
    {
        var terrainData = terrain.terrainData;
        var terrainHeight = Mathf.CeilToInt(terrainData.size.y);
        
        inspectorUpdater = new InspectorUpdateListener(
            Mathf.CeilToInt(terrainData.size.x), terrainHeight,
            numberOfQuads, quadWidth, quadHeight, 
            density,positionalNoiseStrength, this);
        
        grasPositionCompute.SetTexture(0, ShaderIDCache.HeightMapId, terrainData.heightmapTexture);
        grasPositionCompute.SetTexture(0, ShaderIDCache.NoiseMapHeightId, noiseMapHeight);
        
        grasPositionCompute.SetInt(ShaderIDCache.MaxTerrainHeightId, terrainHeight * 2);
        grasPositionCompute.SetFloat(ShaderIDCache.HalfBoundsHeightId, terrainHeight / 2f);

        materials = new List<Material>(numberOfQuads) { grassMaterial };

        for (int i = 1; i < numberOfQuads; i++)
        {
            materials.Add(new Material(grassMaterial));
        }
    }
    
    private void OnDisable()
    {
        PositionsBuffer.Release();
        PositionsBuffer = null;
        ArgsBuffer.Release();
        ArgsBuffer = null;
    }

    private void OnEnable()
    {
        ArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        
        inspectorUpdater.InitializeWithCaches();
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            transform.hasChanged = false;
            inspectorUpdater.UpdateBounds();
        }
        
        foreach (var material in materials)
        {
            Graphics.DrawMeshInstancedIndirect(inspectorUpdater.CachedQuad, 0, material,
                inspectorUpdater.CachedBounds, ArgsBuffer);
        }
    }

    private void OnValidate()
    {
        if(!EditorApplication.isPlaying || ArgsBuffer == null || inspectorUpdater == null) return;

        inspectorUpdater.CachedDensity = density;
        inspectorUpdater.CachedPositionalNoiseStrength = positionalNoiseStrength;
        
        inspectorUpdater.CachedNumberOfQuads = numberOfQuads;
        inspectorUpdater.CachedQuadHeight = quadHeight;
        inspectorUpdater.CachedQuadWidth = quadWidth;
        
        if (grassMaterial.color != grasColor)
        {
            foreach (var material in materials)
            {
                material.color = grasColor;
            }
            
            return;
        }
        
        if (grassMaterial.GetColor(ShaderIDCache.TipColorId) != tipColor)
        {
            foreach (var material in materials)
            {
                material.SetColor(ShaderIDCache.TipColorId, tipColor);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if(!EditorApplication.isPlaying) return;

        var bounds = inspectorUpdater.CachedBounds;
        
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
