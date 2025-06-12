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
    public ComputeShader diffuseShader;
    public RenderTexture renderTexture;
    public RenderTexture tempRT;
    [Range(0, 1000)] public int agentCount;
    public int width = 256;
    public int height = 256;
    [Range(0f, 10f)] public float speed = 0.2f;
    [Range(-1f, 1f)] public float dampingFactor;
    public int diffusionFrequency = 1;

    ComputeBuffer computeBuffer;
    int mainKernel;
    int diffuseKernel;
    private int frameCount = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        renderTexture = createTexture();
        tempRT = createTexture();
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
        frameCount++;

    }

    RenderTexture createTexture()
    {
        RenderTexture rt = new RenderTexture(width, height, 24);
        rt.enableRandomWrite = true;
        rt.filterMode = FilterMode.Point;
        rt.Create();
        return rt;
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


        if (frameCount % diffusionFrequency == 0)
        {
            diffuseShader.SetTexture(diffuseKernel, "Source", renderTexture);
            diffuseShader.SetTexture(diffuseKernel, "Result", tempRT);
            diffuseShader.SetFloat("damping", dampingFactor);

            diffuseShader.Dispatch(diffuseKernel, width / 8, height / 8, 1);

            // Swap textures
            var swap = renderTexture;
            renderTexture = tempRT;
            tempRT = swap;
        }
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
        diffuseKernel = diffuseShader.FindKernel("CSDiffuse");
        computeShader.SetBuffer(mainKernel, "agents", computeBuffer);
        computeShader.SetTexture(mainKernel, "Result", renderTexture);
        computeShader.SetInt("width", width);
        computeShader.SetInt("height", height);

        diffuseShader.SetInt("width", width);
        diffuseShader.SetInt("height", height);
        

        diffuseShader.SetTexture(diffuseKernel, "Source", renderTexture);
        diffuseShader.SetTexture(diffuseKernel, "Result", tempRT);

    }

    void OnDestroy()
    {
        computeBuffer.Release();
    }
}