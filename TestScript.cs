using UnityEngine;
using UnityEngine.Apple;

public class TestScript : MonoBehaviour
{


    struct Agent
    {
        public Vector2 position;
        public float angle;
    }

    public ComputeShader computeShader;
    int agentCount = 1;
    public RenderTexture renderTexture;
    [Range(0f, 6.28f)] public float angle;
    [Range(0, 10)] public int offset;
    ComputeBuffer computeBuffer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        renderTexture = new RenderTexture(256, 256, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        createBuffer();
        InitAgents();
        computeShader.SetBuffer(0, "agents", computeBuffer);
        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.SetTexture(0, "Source", renderTexture);
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);
    }


    // Update is called once per frame
    void Update()
    {

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
    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(256, 256, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }

        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.SetTexture(0, "Source", renderTexture);
        computeShader.SetBuffer(0, "agents", computeBuffer);
        computeShader.SetFloat("angle", angle);
        computeShader.SetInt("offset", offset);
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);

        Graphics.Blit(renderTexture, dest);

    }

}