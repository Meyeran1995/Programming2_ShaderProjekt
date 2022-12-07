using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BillboardGras : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Color grasColor;
    [Space]
    [SerializeField, Range(0f, Max_resolution)] private int resolution;
    [SerializeField, Min(0f)] private float quadWidth, quadHeight;
    [SerializeField, Min(0f)] private float density;
    [SerializeField, Range(1, 6)] private int numberOfQuads;
    [Space]
    [SerializeField, Min(0f)] private float displacementStrength;
    [Space]
    [SerializeField] private Texture noiseMapHeight;
    [SerializeField] private float noiseHeightStrength;
    
    [Header("References")]
    [SerializeField] private ComputeShader grasPositionCompute;
    [SerializeField] private Material grassMaterial;
    [SerializeField] private Terrain terrain;
    
    public ComputeShader Compute => grasPositionCompute;
    public ComputeBuffer PositionsBuffer { get; private set; }
    public ComputeBuffer ArgsBuffer { get; private set; }

    public Material[] GrasMaterial => materials.ToArray();

    private const int Max_resolution = 1000;

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
    
    private void Awake()
    {
        inspectorUpdater = new InspectorUpdateListener(resolution, numberOfQuads, quadWidth, quadHeight, density,
            displacementStrength, noiseHeightStrength, this);
        
        grasPositionCompute.SetTexture(0, ShaderIDCache.HeightMapId, terrain.terrainData.heightmapTexture);
        grasPositionCompute.SetTexture(0, ShaderIDCache.NoiseMapHeightId, noiseMapHeight);

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
        //A compute buffer contains arbitrary untyped data. We have to specify the exact size of each element in bytes, via a second argument.
        //We need to store 3D position vectors, which consist of three float numbers, so the element size is three times the size of a float (four bytes).
        PositionsBuffer = new ComputeBuffer(Max_resolution * Max_resolution, 4 * sizeof(float));
        ArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        
        inspectorUpdater.InitializeWithCaches();
    }

    private void Update()
    {
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
        inspectorUpdater.CachedResolution = resolution;
        inspectorUpdater.CachedNumberOfQuads = numberOfQuads;
        inspectorUpdater.CachedDisplacement = displacementStrength;

        inspectorUpdater.CachedQuadHeight = quadHeight;
        inspectorUpdater.CachedQuadWidth = quadWidth;

        inspectorUpdater.CachedNoiseHeightStrength = noiseHeightStrength;
        
        if (grassMaterial.color == grasColor) return;
        
        foreach (var material in materials)
        {
            material.color = grasColor;
        }
    }

    private void OnDrawGizmos()
    {
        if(!EditorApplication.isPlaying) return;
        
        Gizmos.DrawWireCube(transform.position, inspectorUpdater.CachedBounds.size);
    }
}
