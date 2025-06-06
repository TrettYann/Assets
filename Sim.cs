using UnityEngine;
using UnityEngine.Apple;

public class Sim: MonoBehaviour
{

    struct Agent
    {
        public Vector2 position;
        public Vector2 velocity;
    }

    public ComputeShader computeShader;
    public RenderTexture renderTexture;
    [Range(0, 1000)] public int agentCount;
    public int width = 256;
    public int height = 256;
    [Range(0f, 10f)] public float speed = 0.2f;

    ComputeBuffer computeBuffer;
    int kernelHandle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        createTexture();
        createBuffer();
        InitAgents();
        setupShader();

    }


    // Update is called once per frame
    void Update()
    {
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        int threadsPerGroup = 256;
        int groups = Mathf.CeilToInt(agentCount / (float)threadsPerGroup);
        if (renderTexture == null)
        {
            createTexture();
        }

        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetInt("width", width);
        computeShader.SetInt("height", height);
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetFloat("speed", speed);
        computeShader.SetBuffer(0, "agents", computeBuffer);
        computeShader.Dispatch(0, groups, 1, 1);
        Graphics.Blit(renderTexture, dest);

    }

    void createTexture()
    {
        renderTexture = new RenderTexture(width, height, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.Create();
    }



    void InitAgents()
    {
        Agent[] agents = new Agent[agentCount];
        for (int i = 0; i < agentCount; i++)
        {
            agents[i].position = new Vector2(Random.value, Random.value); // [0,1]
            agents[i].velocity = Random.insideUnitCircle * 0.01f;
        }
        computeBuffer.SetData(agents);
    }



    void createBuffer()
    {
        computeBuffer = new ComputeBuffer(agentCount, sizeof(float) * 4);
        
    }

    void setupShader()
    {
        kernelHandle = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(0, "agents", computeBuffer);
        computeShader.SetTexture(0, "Result", renderTexture);

    }

    void OnDestroy()
    {
        computeBuffer.Release();
    }
}