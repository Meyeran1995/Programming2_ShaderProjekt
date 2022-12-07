using UnityEngine;

public class InspectorUpdateListener
{
    public int CachedResolution
    {
        set
        {
            if(resolution == value) return;
            resolution = value;
            UpdateResolution();
        }
    }
    
    public int CachedNumberOfQuads
    {
        set
        {
            if(numberOfQuads == value) return;
            numberOfQuads = value;
            rotationIncrement = 360f / numberOfQuads / 2f;
            gras.UpdateMaterialList();
            UpdateComputeBuffer();
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
            UpdateComputeBuffer();
        }
    }
    
    public float CachedDisplacement
    {
        set
        {
            if(FloatsAreEqual(displacement, value)) return;

            displacement = value;
            UpdateComputeBuffer();
        }
    }
    
    public float CachedNoiseHeightStrength
    {
        set
        {
            if(FloatsAreEqual(noiseHeightStrength, value)) return;

            noiseHeightStrength = value;
            UpdateComputeBuffer();
        }
    }
    
    public Mesh CachedQuad { get; private set; }
    public Bounds CachedBounds { get; private set; }

    private int resolution;
    private int numberOfQuads;
    private Vector2 quadValues;
    private float density;
    private float displacement;
    private float noiseHeightStrength;
    
    private float rotationIncrement;
    private int resolutionSquared;
    private int groups;

    private readonly BillboardGras gras;

    public InspectorUpdateListener(int resolution, int numberOfQuads, float quadWidth, float quadHeight, float density,
        float displacement, float noiseHeightStrength, BillboardGras gras)
    {
        this.gras = gras;
        this.resolution = resolution;
        this.numberOfQuads = numberOfQuads;
        quadValues = new Vector2(quadWidth, quadHeight);
        this.density = density;
        this.displacement = displacement;
        this.noiseHeightStrength = noiseHeightStrength;
        
        rotationIncrement = 360f / numberOfQuads / 2f;
    }

    public void InitializeWithCaches()
    {
        UpdateResolution();
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
        var grasPositionCompute = gras.Compute;
        
        grasPositionCompute.SetInt(ShaderIDCache.ResolutionId, resolution);
        grasPositionCompute.SetFloat(ShaderIDCache.DensityId, density);
        grasPositionCompute.SetBuffer(0, ShaderIDCache.PositionsId, gras.PositionsBuffer);
        
        grasPositionCompute.SetFloat(ShaderIDCache.HeightMapDisplacementStrengthId, displacement);
        grasPositionCompute.SetFloat(ShaderIDCache.HeightNoiseScalingId, noiseHeightStrength);
        
        grasPositionCompute.Dispatch(0, groups, groups, 1);
        
        UpdateBounds();

        float angle = 0f;
        
        foreach (var material in gras.GrasMaterial)
        {
           UpdateMaterial(material, angle);
           angle += rotationIncrement;
        }
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

    private void UpdateBounds()
    {
        float quadResolution = resolution / density + quadValues.x;
        CachedBounds = new Bounds(gras.transform.position, new Vector3(quadResolution, quadValues.y * 2f, quadResolution));
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
