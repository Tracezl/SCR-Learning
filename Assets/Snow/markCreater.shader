Shader "Unlit/markCreater"
{
	Properties
	{
		_MainTex("Texture", 2D) = "gray" {}
		_SnowTrans("Texture", 2D) = "gray" {}
		_MarkAttenuation("Mark Attenuation", float) = 1.0
	}
	SubShader
	{
		Tags{ "RenderType" = "Opaque" }
		LOD 100

		Pass//0:CreateTrack
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _SnowTrans;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				//这张是tempRT，用于往上叠加
				fixed col = tex2D(_SnowTrans, i.uv).r;
				//这张是新绘制的痕迹RT，*2将值提高至0~2
				fixed snowMark = 2 * tex2D(_MainTex, i.uv).r;
				//判断只有高于水平面的地方，才会进行痕迹图的叠加
				if (col >= 0)
					//因为之前*2了，这里将痕迹直接乘到原图上，则白色的地方就被提高2倍至1，黑色为0。其他仍然保持0.5
					col = col * snowMark;

				return saturate(col);
			}
			ENDCG
		}
	}
}
