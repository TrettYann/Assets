using UnityEngine;

public class Decay : MonoBehaviour
{
    public ComputeShader computeShader;

    public RenderTexture renderTexture;

    [Range(-1f, 1f)] public float decayFactor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        renderTexture = new RenderTexture(256, 256, 24);
        renderTexture.enableRandomWrite = true;
        renderTexture.Create();
        computeShader.SetTexture(computeShader.FindKernel("CSMain"), "Result", renderTexture);
        computeShader.Dispatch(computeShader.FindKernel("CSMain"), renderTexture.width / 8, renderTexture.height / 8, 1);
    }


    // Update is called once per frame
    void Update()
    {
        computeShader.SetFloat("decayFactor", decayFactor);
    }

    private void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(256, 256, 24);
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();
        }
        computeShader.SetTexture(computeShader.FindKernel("CSDecay"), "Result", renderTexture);
        computeShader.Dispatch(computeShader.FindKernel("CSDecay"), renderTexture.width / 8, renderTexture.height / 8, 1);

        Graphics.Blit(renderTexture, dest);

    }
}
