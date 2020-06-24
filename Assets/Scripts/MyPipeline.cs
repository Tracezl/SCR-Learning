//引用库文件
using UnityEngine;
using UnityEngine.Rendering;
//自定义个学习用的命名空间
namespace SRPStudy
{
    public class MyPipeline : RenderPipeline
    {
        private MyPipelineAsset myAsset;
        //这个函数会在绘制管线时调用，两个参数，第一个为所有的渲染相关内容(不只有
        //渲染目标，同时还有灯光，反射探针，光照探针等等相关东西),第二个为相机组
        //定义CommandBuffer用来传参
        private CommandBuffer myCommandBuffer;
        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            // 渲染开始后，创建CommandBuffer;
            if (myCommandBuffer == null) myCommandBuffer = new CommandBuffer() { name = "SRP Study CB" };


            //同样定义好最大平行光数量
            const int maxDirectionalLights = 4;
            //将灯光参数改为参数组
            //var _LightDir0 = Shader.PropertyToID("_LightDir0");
            //var _LightColor0 = Shader.PropertyToID("_LightColor0");    
            Vector4[] DLightColors = new Vector4[maxDirectionalLights];
            Vector4[] DLightDirections = new Vector4[maxDirectionalLights];
            //将shader中需要的属性参数映射为ID，加速传参
            var _DLightDir = Shader.PropertyToID("_DLightDir");
            var _DLightColor = Shader.PropertyToID("_DLightColor");

            const int maxPointLights = 4;
            Vector4[] PLightColors = new Vector4[maxPointLights];
            Vector4[] PLightPos = new Vector4[maxPointLights];
            var _PLightPos = Shader.PropertyToID("_PLightPos");
            var _PLightColor = Shader.PropertyToID("_PLightColor");

            var _CameraPos = Shader.PropertyToID("_CameraPos");
            //全部相机逐次渲染
            //同上一节，所有相机开始逐次渲染
            foreach (var camera in cameras)
            {
                //设置渲染相关相机参数,包含相机的各个矩阵和剪裁平面等
                renderContext.SetupCameraProperties(camera);
                //清理myCommandBuffer，设置渲染目标的颜色为灰色。
                myCommandBuffer.ClearRenderTarget(true, true, Color.gray);

                //剪裁
                ScriptableCullingParameters cullParam = new ScriptableCullingParameters();
                camera.TryGetCullingParameters(out cullParam);
                cullParam.isOrthographic = false;
                CullingResults cullResults = renderContext.Cull(ref cullParam);

                //在剪裁结果中获取灯光并进行参数获取
                var lights = cullResults.visibleLights;
                myCommandBuffer.name = "Render Lights";
                int dLightIndex = 0;
                int pLightIndex = 0;
                foreach (var light in lights)
                {
                    //判断灯光类型
                    switch(light.lightType)
                    {
                        case LightType.Directional:
                            //在限定的灯光数量下，获取参数    
                            if (dLightIndex < maxDirectionalLights)
                            {
                                //获取灯光参数,平行光朝向即为灯光Z轴方向。矩阵第一到三列分别为xyz轴项，第四列为位置。
                                Vector4 lightpos = light.localToWorldMatrix.GetColumn(2);
                                DLightColors[dLightIndex] = light.finalColor;
                                DLightDirections[dLightIndex] = -lightpos;
                                DLightDirections[dLightIndex].w = 0;
                                dLightIndex++;
                            }
                            break;
                        case LightType.Point:
                            if (pLightIndex < maxPointLights)
                            {
                                PLightColors[pLightIndex] = light.finalColor;
                                //将点光源的距离设置塞到颜色的A通道
                                PLightColors[pLightIndex].w = light.range;
                                //矩阵第4列为位置
                                PLightPos[pLightIndex] = light.localToWorldMatrix.GetColumn(3);
                                pLightIndex++;
                            }
                            break;
                    }
                }
                //将灯光参数组传入Shader           
                myCommandBuffer.SetGlobalVectorArray(_DLightColor, DLightColors);
                myCommandBuffer.SetGlobalVectorArray(_DLightDir, DLightDirections);
                myCommandBuffer.SetGlobalVectorArray(_PLightColor, PLightColors);
                myCommandBuffer.SetGlobalVectorArray(_PLightPos, PLightPos);
                //传入相机参数。注意是世界空间位置。
                Vector4 cameraPos = camera.transform.position;
                myCommandBuffer.SetGlobalVector(_CameraPos, cameraPos);
                //执行CommandBuffer中的指令
                renderContext.ExecuteCommandBuffer(myCommandBuffer);
                myCommandBuffer.Clear();


                //插入到某个相机的指定渲染阶段
                //camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, myCommandBuffer);
                //camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, myCommandBuffer);
                //同上节，过滤
                FilteringSettings filtSet = new FilteringSettings(RenderQueueRange.opaque, -1);
                //filtSet.renderQueueRange = RenderQueueRange.opaque;
                //filtSet.layerMask = -1;                

                //同上节，设置Renderer Settings
                //注意在构造的时候就需要传入Lightmode参数，对应shader的pass的tag中的LightMode
                SortingSettings sortSet = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
                DrawingSettings drawSet = new DrawingSettings(new ShaderTagId("BaseLit"), sortSet);

                //绘制物体
                renderContext.DrawRenderers(cullResults, ref drawSet, ref filtSet);

                //绘制天空球
                renderContext.DrawSkybox(camera);
                //开始执行渲染内容
                renderContext.Submit();
            }
        }
    }
}