using UnityEngine.Rendering;
using UnityEngine;


public class SnowMark : MonoBehaviour
{

    public GameObject markObj;//表示要绘制的痕迹
    public Material markCreater;//生成高度图的材质
    public Material TerrainMat;//地面材质

    private CommandBuffer snowMarkCB;
    private RenderTexture snowMarkRT;
    private RenderTexture tempRT;

    private Camera cam;

    void Start()
    {
        //脚本直接挂接到相机上
        cam = GetComponent<Camera>();
        //这里用mat赋值，未用CB
        TerrainMat.SetTexture("_ParallaxMap", tempRT);
        TerrainMat.SetTexture("_MainTex", snowMarkRT);
        markCreater.SetTexture("_SnowTrans", snowMarkRT);

        if (markObj != null & markCreater != null)
        {
            //获取痕迹渲染目标
            Renderer meshRender = markObj.GetComponent<Renderer>();
            Material meshMat = meshRender.sharedMaterial;

            //创建CB和CB各种常规操作
            snowMarkCB = new CommandBuffer
            {
                name = "SnowCommandPass"
            };
            //这里因为我本身还设置了其他功能，所以用了RFloat格式，一般来说R8即可
            snowMarkRT = new RenderTexture(256, 256, 0, RenderTextureFormat.RFloat);
            //创建一个临时RT，用于每帧的高度图叠加
            tempRT = new RenderTexture(256, 256, 0, RenderTextureFormat.RFloat);

            snowMarkCB.Clear();
            snowMarkCB.SetRenderTarget(snowMarkRT);
            snowMarkCB.ClearRenderTarget(true, true, Color.gray);
            //绘制痕迹
            snowMarkCB.DrawRenderer(meshRender, meshMat);
            cam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, snowMarkCB);
        }
    }
    //释放CB及RT
    void OnDestroy()
    {
        if (markObj != null & markCreater != null)
        {
            if (snowMarkCB != null)
            {
                cam.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, snowMarkCB);
                snowMarkCB.Release();
                snowMarkCB = null;
                snowMarkRT.Release();
                snowMarkRT = null;
                tempRT.Release();
                tempRT = null;
            }
        }
    }
    void Update()
    {
        //每帧的叠加操作 
        Graphics.Blit(snowMarkRT, tempRT, markCreater);
    }
}