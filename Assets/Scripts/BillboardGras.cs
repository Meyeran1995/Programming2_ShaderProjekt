using UnityEngine;

public class BillboardGras : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Range(0f, Max_Resolution)] private int resolution;

    [Header("References")]
    [SerializeField] private ComputeShader grasPositionCompute;
    [SerializeField] private Material grasMaterial;
    [SerializeField] private MeshFilter cube;
    
    private ComputeBuffer positionsBuffer, argsBuffer;
    private Mesh quad;
    private Bounds bounds;
    private int resolutionSquared;
    
    private static readonly int PositionsId = Shader.PropertyToID("_Positions");

    private const int Max_Resolution = 1000;

    private void Awake()
    {
        quad = QuadCreator.CreateQuad();
        bounds = new Bounds(transform.position, Vector3.one * (2f + 2f / resolution));
        resolutionSquared = resolution * resolution;
        
        //A compute buffer contains arbitrary untyped data. We have to specify the exact size of each element in bytes, via a second argument.
        //We need to store 3D position vectors, which consist of three float numbers, so the element size is three times the size of a float (four bytes).
        positionsBuffer = new ComputeBuffer(Max_Resolution * Max_Resolution, 3 * sizeof(float));
        grasPositionCompute.SetInt("_Resolution", resolution);
        grasPositionCompute.SetBuffer(0, PositionsId, positionsBuffer);
        
        argsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments);
    }

    private void Start()
    {
        // Numthreads(8,8,1) -> 8x8 group size, so we need dimensions equal to resolution divided by group size
        int groups = Mathf.CeilToInt(resolution / 8f);
        
        grasPositionCompute.Dispatch(0, groups, groups, 1);
        grasMaterial.SetBuffer(PositionsId, positionsBuffer);
        
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        // Arguments for drawing mesh.
        // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
        args[0] = (uint)cube.sharedMesh.GetIndexCount(0);
        args[1] = (uint)resolutionSquared;
        args[2] = (uint)cube.sharedMesh.GetIndexStart(0);
        args[3] = (uint)cube.sharedMesh.GetBaseVertex(0);
        argsBuffer.SetData(args);
    }

    private void Update()
    {
        
        Graphics.DrawMeshInstancedIndirect(cube.sharedMesh, 0, grasMaterial, bounds, argsBuffer);
    }

    private void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
    }
}
