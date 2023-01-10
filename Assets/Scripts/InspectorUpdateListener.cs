using UnityEngine;

public class InspectorUpdateListener
{
    public int CachedNumberOfQuads
    {
        set
        {
            if(numberOfQuads == value) return;
            numberOfQuads = value;
            rotationIncrement = 360f / numberOfQuads / 2f;
            gras.UpdateMaterialList();
            UpdateMaterials();
        }
    }
    
    public float CachedQuadWidth
    {
        set
        {
            if(FloatsAreEqual(quadValues.x, value)) return;
            
            quadValues.x = value;
            UpdateMesh();
            UpdateBounds();
        }
    }
    
    public float CachedQuadHeight
    {
        set
        {
            if(FloatsAreEqual(quadValues.y, value)) return;
            
            quadValues.y = value;
            UpdateMesh();
            UpdateBounds();
        }
    }
    
    public float CachedDensity
    {
        set
        {
            if(FloatsAreEqual(density, value)) return;

            density = value;
            UpdateResolution();
        }
    }

    public Mesh CachedQuad { get; private set; }
    public Bounds CachedBounds { get; private set; }

    private readonly int terrainResolutionX;
    private readonly int terrainResolutionY;

    private int resolution;
    private int numberOfQuads;
    private Vector2 quadValues;
    private float density;
    
    private float rotationIncrement;
    private int resolutionSquared;
    private int groups;

    private readonly BillboardGras gras;

    public InspectorUpdateListener(int terrainResolutionX, int terrainResolutionY, int numberOfQuads, float quadWidth, float quadHeight, 
        float density, BillboardGras gras)
    {
        this.gras = gras;
        this.terrainResolutionX = terrainResolutionX;
        this.terrainResolutionY = terrainResolutionY;
        this.numberOfQuads = numberOfQuads;
        quadValues = new Vector2(quadWidth, quadHeight);
        this.density = density;
        
        rotationIncrement = 360f / numberOfQuads / 2f;
    }

    public void InitializeWithCaches()
    {
        UpdateResolution();
        UpdateBounds();
    }

    public void UpdateBounds()
    {
        float sizeX = terrainResolutionX + quadValues.x;
        float sizeY = terrainResolutionY + quadValues.y;
        
        Vector3 position = gras.transform.position;
        Vector3 center = new Vector3(position.x, position.y + sizeY / 2f, position.z);

        CachedBounds = new Bounds(center,
            new Vector3(sizeX, sizeY, sizeX));
    }
    
    private void UpdateResolution()
    {
        resolution = Mathf.CeilToInt(terrainResolutionX * density);
        resolutionSquared = resolution * resolution;
        // Numthreads(8,8,1) -> 8x8 group size, so we need dimensions equal to resolution divided by group size
        groups = Mathf.CeilToInt(resolution / 8f);
        
        UpdateMesh();
        UpdateComputeBuffer();
    }
    
    private void UpdateComputeBuffer()
    {
        var grasPositionCompute = gras.Compute;
        
        grasPositionCompute.SetInt(ShaderIDCache.ResolutionId, resolution);
        grasPositionCompute.SetFloat(ShaderIDCache.DensityId, density);
        
        gras.DispatchBuffer(resolutionSquared, groups);
        UpdateMaterials();
    }

    private void UpdateMesh()
    {
        CachedQuad = QuadCreator.CreateQuad(quadValues.x, quadValues.y);
        uint[] args = new uint[5];
        
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = CachedQuad.GetIndexCount(0);
        args[1] = (uint)resolutionSquared;
        args[2] = CachedQuad.GetIndexStart(0);
        args[3] = CachedQuad.GetBaseVertex(0);
        
        gras.ArgsBuffer.SetData(args);
    }

    private void UpdateMaterials()
    {
        float angle = 0f;
        
        foreach (var material in gras.GrasMaterials)
        {
            UpdateMaterial(material, angle);
            angle += rotationIncrement;
        }
    }
    
    private void UpdateMaterial(Material material, float angle)
    {
        material.SetFloat(ShaderIDCache.RotationId, angle);
        material.SetBuffer(ShaderIDCache.PositionsId, gras.PositionsBuffer);
    }

    private static bool FloatsAreEqual(float value1, float value2)
    {
        return Mathf.Abs(value1 - value2) < 0.001f;
    }
}
