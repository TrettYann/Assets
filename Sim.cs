using UnityEngine;
using UnityEngine.Apple;

public class Sim: MonoBehaviour
{

    struct Agent
    {
        public Vector2 position;
        public float angle;
    }

    public ComputeShader computeShader;
    public ComputeShader decayShader;
    public RenderTexture renderTexture;
    [Range(0, 1000)] public int agentCount;
    public int width = 256;
    public int height = 256;
    [Range(0f, 10f)] public float speed = 0.2f;
    [Range(-1f, 1f)] public float decayFactor;

    ComputeBuffer computeBuffer;
    int mainKernel;
    int decayKernel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        createTexture();
        createBuffer();
        InitAgents();
        setupShader();
        Application.targetFrameRate = 140;
    }


    // Update is called once per frame
    void Update()
    {
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        
        if (renderTexture == null)
        {
            createTexture();
        }

        handleShader();
        Graphics.Blit(renderTexture, dest);

    }

    void createTexture()
    {
        renderTexture = new RenderTexture(width, height, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.Create();
    }

    void handleShader()
    {
        // Main Shader
        int threadsPerGroup = 256;
        int groups = Mathf.CeilToInt(agentCount / (float)threadsPerGroup);
        computeShader.SetTexture(mainKernel, "Result", renderTexture);
        computeShader.SetFloat("deltaTime", Time.deltaTime);
        computeShader.SetFloat("speed", speed);
        computeShader.SetBuffer(mainKernel, "agents", computeBuffer);
        computeShader.Dispatch(mainKernel, groups, 1, 1);

        // Decay Shader
        decayShader.SetFloat("decayFactor", decayFactor);
        decayShader.SetTexture(decayKernel, "Result", renderTexture);
        decayShader.Dispatch(mainKernel, width / 8, height / 8, 1);
    }



    void InitAgents()
    {
        Agent[] agents = new Agent[agentCount];
        for (int i = 0; i < agentCount; i++)
        {
            agents[i].position = new Vector2(Random.value, Random.value); // [0,1]
            agents[i].angle = Random.Range(0f, Mathf.PI * 2f);
        }
        computeBuffer.SetData(agents);
    }



    void createBuffer()
    {
        computeBuffer = new ComputeBuffer(agentCount, sizeof(float) * 3);
        
    }

    void setupShader()
    {
        mainKernel = computeShader.FindKernel("CSMain");
        decayKernel = decayShader.FindKernel("CSDecay");
        computeShader.SetBuffer(mainKernel, "agents", computeBuffer);
        computeShader.SetTexture(mainKernel, "Result", renderTexture);
        computeShader.SetInt("width", width);
        computeShader.SetInt("height", height);
        decayShader.SetTexture(decayKernel, "Result", renderTexture);

    }

    void OnDestroy()
    {
        computeBuffer.Release();
    }
}