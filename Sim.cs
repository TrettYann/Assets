using UnityEngine;
using UnityEngine.Apple;
using System.Collections.Generic;


public class Sim: MonoBehaviour
{

    struct Agent
    {
        public Vector2 position;
        public float angle;
        public int blockedSteps;
        public int shrinkParticle;
        public int devideParticle;
    }

    public ComputeShader agentShader;
    public ComputeShader diffuseShader;
    private RenderTexture renderTexture;
    private RenderTexture tempRT;
    private RenderTexture occupancyTexture;
    //[Range(0, 10000000)] public int agentCount;
    [Range(1, 100)] public int population;
    public int width = 256;
    public int height = 256;
    [Range(0f, 10f)] public float speed = 0.2f;
    [Range(-1f, 1f)] public float dampingFactor;
    [Range(0f, 360f)] public float rotationAngle;
    [Range(0f, 360f)] public float sensoryAngle;
    [Range(1, 50)] public int SensorOffset;
    //[Range(1, 8)] public int nutrientPoints;
    public int diffusionFrequency = 1;
    public int filterFrequency = 3;
    public bool renderDiffusion;
    public bool isOscillatory;
    public bool repellant;
    public bool drawNutrientPoints;

    



    ComputeBuffer computeBuffer;
    int agentKernel;
    int agentCount;
    int diffuseKernel;
    private int frameCount = 0;
    int threadsPerGroup = 256;
    int groups;
    Vector2 cursor;
    RenderTexture rm;

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
        if (drawNutrientPoints) {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mousePos = Input.mousePosition;
                Vector2 coord = new Vector2(Mathf.Round(mousePos.x), Mathf.Round(mousePos.y));
            }
        }
        if (repellant)
        {
                Vector3 mousePos = Input.mousePosition;
                Vector2 coord = new Vector2(Mathf.Round(mousePos.x), Mathf.Round(mousePos.y));
                cursor = coord;
        }
        Debug.Log($"{agentCount}");
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        frameCount++;
        if (renderTexture == null)
        {
            createTexture();
        }
        //agentCount = (width * height * population) / 100;
        handleShader();
        //tempRT = renderTexture; // Refresh TempRT
        if (renderDiffusion)
        {
            rm = renderTexture;
        }
        else
        {
            rm = occupancyTexture;
        }
        Graphics.Blit(rm, dest);
        
        
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
        agentShader.SetInt("cursorX", (int)cursor.x);
        agentShader.SetInt("cursorY", (int)cursor.y);
        agentShader.SetFloat("rotationAngle", rotationAngle * Mathf.Deg2Rad);
        agentShader.SetFloat("sensoryAngle", sensoryAngle * Mathf.Deg2Rad);
        agentShader.SetBuffer(agentKernel, "agents", computeBuffer);
        agentShader.SetBool("isOscillatory", isOscillatory);
        agentShader.SetBool("repellant", repellant);
        agentShader.SetBool("drawNutrientPoints", drawNutrientPoints);
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
        if (frameCount % filterFrequency == 0)
        {
            FilterAgents();
        }
    }

    void FilterAgents()
    {
        Agent[] agents = new Agent[computeBuffer.count];
        computeBuffer.GetData(agents);
        List<Agent> shrinkList = new List<Agent>(agents);
        shrinkList.RemoveAll(agent => agent.shrinkParticle == 1);
        if (shrinkList.Count != computeBuffer.count)
        {
            agentCount = shrinkList.Count;
            computeBuffer.Release();
            computeBuffer = new ComputeBuffer(agentCount, sizeof(float) * 3 + sizeof(int) * 3);
            computeBuffer.SetData(shrinkList.ToArray());

            agents = new Agent[computeBuffer.count];
            computeBuffer.GetData(agents);

        }
        List<Agent> devideList = new List<Agent>(agents);
        devideList.RemoveAll(agent => agent.devideParticle == 0);
        if (devideList.Count > 0)
        {
            Debug.Log($"Added{devideList.Count}");

            List<Agent> newAgents = new List<Agent>();
            foreach (var agent in devideList)
            {
                Agent copy = agent;
                newAgents.Add(copy);
            }
            newAgents.AddRange(shrinkList); 
            newAgents.AddRange(devideList);

            agentCount = newAgents.Count;
            computeBuffer.Release();
            computeBuffer = new ComputeBuffer(agentCount, sizeof(float) * 3 + sizeof(int) * 3);
            computeBuffer.SetData(newAgents.ToArray());
            
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
            agents[i].shrinkParticle = 0;
        }
        computeBuffer.SetData(agents);
    }



    void createBuffer()
    {
        computeBuffer = new ComputeBuffer(agentCount, sizeof(float) * 3 + sizeof(int) * 3);
        
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