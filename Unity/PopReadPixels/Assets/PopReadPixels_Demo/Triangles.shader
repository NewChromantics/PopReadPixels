Shader "PopReadPixels/Triangles"
{
	Properties
	{
		GridSize("GridSize", Range(1,30) ) = 10
		TimeSpeed("TimeSpeed", Range(0.01,10.0) ) = 0.5
		ColourA("ColourA", COLOR ) = (0,0,0,1)
		ColourB("ColourB", COLOR ) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
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
				float4 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float GridSize;
			float TimeSpeed;
			float4 ColourA;
			float4 ColourB;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = UnityObjectToClipPos(v.vertex);
				//o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				//o.uv = v.vertex;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				int GridSizei = (int)GridSize;
				float2 uv = ((i.uv / i.uv.w) + 1.0f) / 2.0f;
				float Step = 1.0f / GridSizei;
				float2 GridUv = fmod( uv, 1.0f / GridSizei ) / Step;

				GridUv = (GridUv - 0.5f) * 2.0f;

				float Time = 1 - fmod( _Time.y * TimeSpeed, 1 );

				float Radius = length( GridUv ) + Time;
				float RingSize = 0.5f;

				float Ring = fmod( Radius, RingSize ) / RingSize;
				return ( Ring < 0.5f ) ? ColourA : ColourB;
			}
			ENDCG
		}
	}
}
