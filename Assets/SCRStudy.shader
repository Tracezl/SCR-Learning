Shader "SRPStudy/UnlitTexture"
{
	Properties
	{
		_Color("Color Tint", Color) = (0.5,0.5,0.5)
		_MainTex("MainTex",2D) = "white"{}
		_SpecularPow("SpecularPow",range(5,50)) = 20
	}

		HLSLINCLUDE
	#include "UnityCG.cginc"

	uniform float4 _Color;
	sampler2D _MainTex;
	//这边需要平行光参数用于计算
	half4 _DLightDir;
	fixed4 _DLightColor;

	half4 _CameraPos;
	fixed _SpecularPow;
	struct a2v
	{
		float4 position : POSITION;
		float2 uv : TEXCOORD0;
		//需要模型法线进行计算光照
		float3 normal : NORMAL;
	};

	struct v2f
	{
		float4 position : SV_POSITION;
		float2 uv : TEXCOORD0;
		//法线传入像素管线计算像素光照
		float3 normal : NORMAL;
		float3 worldPos : TEXCOORD1;
	};

	v2f vert(a2v v)
	{

		v2f o;
		UNITY_INITIALIZE_OUTPUT(v2f, o);
		o.uv = v.uv;
		//这边仍然使用的Unity的内置矩阵。目前来看还可以用。如果以后不行了，可以考虑
		//管线自己往里面传矩阵。第一个是MVP矩阵。管线里面传camera.projectMatrix*
		//camera.worldToCameraMatrix之后，再传入一个物体的localToWorldMatrix即可。
		//法线转换可以根据是否统一缩放来进行转换，可以参考目前unitycg.cginc中相关
		//代码，传入M矩阵即可。
		o.position = UnityObjectToClipPos(v.position);
		o.normal = UnityObjectToWorldNormal(v.normal);

		o.worldPos = mul(unity_ObjectToWorld, v.position).xyz;
		return o;
	}
	half4 frag(v2f i) : SV_Target
	{
		half4 fragColor = half4(_Color.rgb,1.0) * tex2D(_MainTex, i.uv);
		//获得光照参数，进行兰伯特光照计算
		half light = saturate(dot(i.normal, _DLightDir));
		half3 viewDir = normalize(_CameraPos - i.worldPos);
		half3 halfDir = normalize(viewDir + _DLightDir.xyz);
		fixed specular = pow(saturate(dot(i.normal, halfDir)), _SpecularPow);
		fragColor.rgb *= (light + specular) * _DLightColor;

		return fragColor;
	}

		ENDHLSL

		SubShader
	{
		Tags{ "Queue" = "Geometry" }
			LOD 100
			Pass
		{
			//注意这里,默认是没写光照类型的,自定义管线要求必须写,渲染脚本中会调用,否则无法渲染
			//这也是为啥新建一个默认unlitshader,无法被渲染的原因
			Tags{ "LightMode" = "BaseLit" }
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDHLSL
		}
	}
}