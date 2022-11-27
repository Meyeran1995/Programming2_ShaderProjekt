using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BillboardGras : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Range(0f, Max_resolution)] private int resolution;
    [SerializeField, Min(0f)] private float quadWidth, quadHeight;
    [SerializeField, Min(0f)] private float density;
    [SerializeField, Range(1, 6)] private int numberOfQuads;

    [Header("References")]
    [SerializeField] private ComputeShader grasPositionCompute;
    [SerializeField] private Material grassMaterial;
    
    [Header("Debug")]
    [SerializeField] private List<Material> materials;

    private ComputeBuffer positionsBuffer, argsBuffer;
    private Mesh quad;
    private Bounds bounds;
    private int resolutionSquared;
    private int groups;
    private float rotationIncrement;

    private static readonly int PositionsId = Shader.PropertyToID("_Positions");
    private static readonly int DensityId = Shader.PropertyToID("_Density");
    private static readonly int ResolutionId = Shader.PropertyToID("_Resolution");
    private static readonly int RotationId = Shader.PropertyToID("_Rotation");

    private const int Max_resolution = 1000;

    private int resolutionCache, numberOfQuadsCache;
    private Vector2 quadCache;
    private float densityCache;

    private void Awake()
    {
        resolutionCache = resolution;
        quadCache = new Vector2(quadWidth, quadHeight);
        densityCache = density;

        numberOfQuadsCache = numberOfQuads;
        rotationIncrement = 360f / numberOfQuads / 2f;
        
        materials = new List<Material>(numberOfQuads) { grassMaterial };

        for (int i = 1; i < numberOfQuads; i++)
        {
            materials.Add(new Material(grassMaterial));
        }
    }
    
    private void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
        argsBuffer.Release();
        argsBuffer = null;
    }

    private void OnEnable()
    {
        //A compute buffer contains arbitrary untyped data. We have to specify the exact size of each element in bytes, via a second argument.
        //We need to store 3D position vectors, which consist of three float numbers, so the element size is three times the size of a float (four bytes).
        positionsBuffer = new ComputeBuffer(Max_resolution * Max_resolution, 3 * sizeof(float));
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
        UpdateResolution();
    }

    private void Update()
    {
        foreach (var material in materials)
        {
            Graphics.DrawMeshInstancedIndirect(quad, 0, material, bounds, argsBuffer);
        }
    }

    private void UpdateResolution()
    {
        resolutionSquared = resolution * resolution;
        // Numthreads(8,8,1) -> 8x8 group size, so we need dimensions equal to resolution divided by group size
        groups = Mathf.CeilToInt(resolution / 8f);
        
        UpdateMesh();
        UpdateComputeBuffer();
    }
    
    private void UpdateComputeBuffer()
    {
        grasPositionCompute.SetInt(ResolutionId, resolution);
        grasPositionCompute.SetFloat(DensityId, density);
        grasPositionCompute.SetBuffer(0, PositionsId, positionsBuffer);

        grasPositionCompute.Dispatch(0, groups, groups, 1);
        
        UpdateBounds();

        float angle = 0f;
        
        foreach (var material in materials)
        {
           UpdateMaterial(material, angle);
           angle += rotationIncrement;
        }
    }

    private void UpdateMesh()
    {
        quad = QuadCreator.CreateQuad(quadWidth, quadHeight);
        uint[] args = new uint[5];
        
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = quad.GetIndexCount(0);
        args[1] = (uint)resolutionSquared;
        args[2] = quad.GetIndexStart(0);
        args[3] = quad.GetBaseVertex(0);
        
        argsBuffer.SetData(args);
    }

    private void UpdateBounds()
    {
        float quadResolution = resolution / density + quadWidth;
        bounds = new Bounds(transform.position, new Vector3(quadResolution, quadHeight * 2f, quadResolution));
    }
    
    private void UpdateMaterial(Material material, float angle)
    {
        material.SetFloat(RotationId, angle);
        material.SetBuffer(PositionsId, positionsBuffer);
    }

    private void OnValidate()
    {
        //TODO: add color parameter to script
        if(!EditorApplication.isPlaying || argsBuffer == null) return;

        if (resolutionCache != resolution)
        {
            resolutionCache = resolution;
            UpdateResolution();
            return;
        }

        if (densityCache > density || densityCache < density)
        {
            densityCache = density;
            UpdateComputeBuffer();
            return;
        }

        if (Math.Abs(quadCache.x - quadWidth) > 0.001f || Math.Abs(quadCache.y - quadHeight) > 0.001f)
        {
            quadCache.x = quadWidth;
            quadCache.y = quadHeight;
            UpdateMesh();
            UpdateBounds();
            return;
        }

        if (numberOfQuadsCache != numberOfQuads)
        {
            numberOfQuadsCache = numberOfQuads;
            rotationIncrement = 360f / numberOfQuads / 2f;
            UpdateMaterialList();
            UpdateComputeBuffer();
        }
    }

    private void UpdateMaterialList()
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

    private void OnDrawGizmos()
    {
        if(!EditorApplication.isPlaying) return;
        
        Gizmos.DrawWireCube(transform.position, bounds.size);
    }
}
