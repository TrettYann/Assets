using UnityEngine;
using UnityEngine.Apple;

public class TestScript : MonoBehaviour
{

    public ComputeShader computeShader;

    public RenderTexture renderTexture;
    [Range(0f, 6.28f)] public float angle;
    [Range(0, 10)] public int offset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        renderTexture = new RenderTexture(256, 256, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();

        computeShader.SetTexture(0, "Result", renderTexture);
        computeShader.SetTexture(0, "Source", renderTexture);
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);
    }


    // Update is called once per frame
    void Update()
    {

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
        computeShader.SetFloat("angle", angle);
        computeShader.SetInt("offset", offset);
        computeShader.Dispatch(0, renderTexture.width / 8, renderTexture.height / 8, 1);

        Graphics.Blit(renderTexture, dest);

    }

}