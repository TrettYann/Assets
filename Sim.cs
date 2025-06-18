using UnityEngine;
using UnityEngine.Apple;

public class Sim: MonoBehaviour
{

    struct Agent
    {
        public Vector2 position;
        public float angle;
        public int blockedSteps;
    }

    public ComputeShader agentShader;
    public ComputeShader diffuseShader;
    private RenderTexture renderTexture;
    private RenderTexture tempRT;
    private RenderTexture occupancyTexture;
    //[Range(0, 10000000)] public int agentCount;
    public int width = 256;
    public int height = 256;
    [Range(0f, 10f)] public float speed = 0.2f;
    [Range(-1f, 1f)] public float dampingFactor;
    [Range(-360f, 360f)] public float rotationAngle;
    [Range(1, 50)] public int SensorOffset;
    public int diffusionFrequency = 1;
    public bool isOscillatory;

    [Range(1, 100)] public int population;



    ComputeBuffer computeBuffer;
    int agentKernel;
    int agentCount;
    int diffuseKernel;
    private int frameCount = 0;
    int threadsPerGroup = 256;
    int groups;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agentCount = (width * height*population)/100;
        groups = Mathf.CeilToInt(agentCount / (float)threadsPerGroup);
        renderTexture = createTexture();
        tempRT = createTexture();
        occupancyTexture = createTexture();
        createBuffer();
        InitAgents();
        setupShader();
        Application.targetFrameRate = 144;
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
        //tempRT = renderTexture; // Refresh TempRT
        Graphics.Blit(occupancyTexture, dest);
        frameCount++;
        agentCount = (width * height * population) / 100;
    }

    RenderTexture createTexture()
    {
        RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);
        rt.enableRandomWrite = true;
        rt.filterMode = FilterMode.Point;
        rt.Create();
        return rt;
    }

    void handleShader()
    {
        // Agent Shader
        int threadsPerGroup = 256;
        int groups = Mathf.CeilToInt(agentCount / (float)threadsPerGroup);
        agentShader.SetTexture(agentKernel, "Result", renderTexture);
        agentShader.SetTexture(agentKernel, "Source", tempRT);
        agentShader.SetFloat("deltaTime", Time.deltaTime);
        agentShader.SetFloat("speed", speed);
        agentShader.SetInt("SensorOffset", SensorOffset);
        agentShader.SetFloat("rotationAngle", rotationAngle * Mathf.Deg2Rad);
        agentShader.SetBuffer(agentKernel, "agents", computeBuffer);
        agentShader.SetBool("isOscillatory", isOscillatory);
        Graphics.SetRenderTarget(occupancyTexture);
        GL.Clear(false, true, Color.clear);
        agentShader.SetTexture(agentKernel, "OccupancyMap", occupancyTexture);
        agentShader.Dispatch(agentKernel, groups, 1, 1);

        



        //Diffuse Shader
        if (frameCount % diffusionFrequency == 0)
        {
            diffuseShader.SetTexture(diffuseKernel, "Source", renderTexture);
            diffuseShader.SetTexture(diffuseKernel, "Result", tempRT);
            diffuseShader.SetFloat("damping", dampingFactor);

            diffuseShader.Dispatch(diffuseKernel, width / 8, height / 8, 1);

            // Swap textures
            var swap2 = renderTexture;
            renderTexture = tempRT;
            tempRT = swap2;
        }
    }



    void InitAgents()
    {
        float radius = Mathf.Min(width, height) * 0.8f;
        Agent[] agents = new Agent[agentCount];
        for (int i = 0; i < agentCount; i++)
        {
            agents[i].angle = Random.Range(0f, Mathf.PI * 2f);
            agents[i].position = new Vector2(Random.value, Random.value); //[0,1]
        }
        computeBuffer.SetData(agents);
    }



    void createBuffer()
    {
        computeBuffer = new ComputeBuffer(agentCount, sizeof(float) * 4);
        
    }

    void setupShader()
    {
        // Agent Shader
        agentKernel = agentShader.FindKernel("CSAgent");

        agentShader.SetInt("width", width);
        agentShader.SetInt("height", height);

        agentShader.SetBuffer(agentKernel, "agents", computeBuffer);
        agentShader.SetTexture(agentKernel, "Result", renderTexture);
        agentShader.SetTexture(agentKernel, "Source", tempRT);

        // DIffuse Shader
        diffuseKernel = diffuseShader.FindKernel("CSDiffuse");

        // Diffuse Shader

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