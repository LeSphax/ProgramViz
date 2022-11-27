using UnityEngine;
using System.Collections;
using ComputeShaderUtility;

public class Nodes : MonoBehaviour
{
    private int instanceCount;
    public Mesh instanceMesh;
    public Material instanceMaterial;
    public int subMeshIndex = 0;
    public float idealDistance = 0.1f;
    public float speed = 0.1f;

    const int UpdateNodesKernel = 0;

    private int cachedInstanceCount = -1;
    private int cachedSubMeshIndex = -1;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer colorBuffer;
    private ComputeBuffer linksBuffer;
    private ComputeBuffer argsBuffer;
    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };

    public ComputeShader nodesCompute;

    private Graph graph;

    void Start()
    {
        graph = GetComponent<Graph>();
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        instanceCount = graph.modules.Count;
        UpdateBuffers();
    }

    void Update()
    {
        // Update starting position buffer
        //if (cachedInstanceCount != instanceCount || cachedSubMeshIndex != subMeshIndex)
        //    UpdateBuffers();

        // Render
        Graphics.DrawMeshInstancedIndirect(instanceMesh, subMeshIndex, instanceMaterial, new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)), argsBuffer);
        Vector4[] test = new Vector4[instanceCount];
        positionBuffer.GetData(test);
        Debug.Log(test[1]);
        nodesCompute.SetFloat("idealDistance", idealDistance);
        nodesCompute.SetFloat("speed", speed);
        ComputeHelper.Dispatch(nodesCompute, instanceCount, 1, 1, UpdateNodesKernel);
    }

    void UpdateBuffers()
    {
        // Ensure submesh index is in range
        if (instanceMesh != null)
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, instanceMesh.subMeshCount - 1);

        // Positions
        if (positionBuffer != null)
            positionBuffer.Release();
        
        var instanceCount = graph.modules.Count;

        positionBuffer = new ComputeBuffer(instanceCount, 16);
        colorBuffer = new ComputeBuffer(instanceCount, 12);
        linksBuffer = new ComputeBuffer(instanceCount * instanceCount, 4);
        Vector4[] positions = new Vector4[instanceCount];
        Vector3[] colors = new Vector3[instanceCount];
        uint[] links = new uint[instanceCount * instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            positions[i] = new Vector4( Random.value, Random.value, Random.value, 0.1f);

            if (graph.modules[i].dependents.Count == 0)
            {
                colors[i] = new Vector3(1,0,0);
            }
            else if (graph.modules[i].dependencies.Count == 0)
            {
                colors[i] = new Vector3(0, 1, 0);
            }
            else
            {
                colors[i] = new Vector3(0, 0, 1);
            }
            int[] dependentIndices = new int[graph.modules[i].dependents.Count];
            for (int j = 0; j < graph.modules[i].dependents.Count; j++)
            {
                dependentIndices[j] = graph.modules[i].dependents[j].index;
            }

            for (int j = 0; j < dependentIndices.Length; j++)
            {
                links[i * instanceCount + dependentIndices[j]] = 1;
            }

            int[] dependencyIndices = new int[graph.modules[i].dependencies.Count];
            for (int j = 0; j < graph.modules[i].dependencies.Count; j++)
            {
                dependencyIndices[j] = graph.modules[i].dependencies[j].index;
            }

            for (int j = 0; j < dependencyIndices.Length; j++)
            {
                links[i * instanceCount + dependencyIndices[j]] = 1;
            }
           


        }
        positionBuffer.SetData(positions);
        colorBuffer.SetData(colors);
        linksBuffer.SetData(links);
        instanceMaterial.SetBuffer("positionBuffer", positionBuffer);
        nodesCompute.SetBuffer(UpdateNodesKernel, "positionBuffer", positionBuffer);
        nodesCompute.SetBuffer(UpdateNodesKernel, "linksBuffer", linksBuffer);
        nodesCompute.SetInt("instanceCount", instanceCount);

        instanceMaterial.SetBuffer("colorBuffer", colorBuffer);
       

        // Indirect args
        if (instanceMesh != null)
        {
            args[0] = (uint)instanceMesh.GetIndexCount(subMeshIndex);
            args[1] = (uint)instanceCount;
            args[2] = (uint)instanceMesh.GetIndexStart(subMeshIndex);
            args[3] = (uint)instanceMesh.GetBaseVertex(subMeshIndex);
        }
        else
        {
            args[0] = args[1] = args[2] = args[3] = 0;
        }
        argsBuffer.SetData(args);

        cachedInstanceCount = instanceCount;
        cachedSubMeshIndex = subMeshIndex;
    }

    void OnDisable()
    {
        if (positionBuffer != null)
            positionBuffer.Release();
        positionBuffer = null;


        if (colorBuffer != null)
            colorBuffer.Release();
        colorBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
    }
}