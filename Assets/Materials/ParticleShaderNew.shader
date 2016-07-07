Shader "Custom/ParticleShaderNew" 
{
	Properties 
	{
		_MainTex("Texture", 2D) = "white" {} 		
		_Size("PointSize", Range(0.001, 0.1)) = 0.01
	}

	// ---- Fragment program cards
	SubShader 
	{

		Pass 
		{
			Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
			Lighting Off
			ZWrite Off
			Blend SrcAlpha OneMinusSrcAlpha // Traditional transparency
		
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct SnowParticle
			{
				float4 position;
				float4 velocity;
				float4 gridPosition;
				float3x3 def_plastic;
				float3x3 def_elastic;
				float3x3 velocityGradient;

				float volume;
				float mass;
				float density;
				float lambda;
				float mu;
				float xi; // Plastic hardening parameter

				// singular values restricted to [criticalCompression, criticalStretch]
				float criticalCompressionRatio;
				float criticalStretchRatio;
			};


			StructuredBuffer<SnowParticle>  _Particles;
			StructuredBuffer<float3>  quadPoints;
			float _Size;

			struct v2f 
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			//build quad for every snow particle
			v2f vert (appdata_base v, uint id : SV_VertexID, uint inst : SV_InstanceID)
			{

				float3 worldPosition = _Particles[inst].position;
				float3 quadPoint = quadPoints[id];

				v2f o;
				o.normal = v.normal;
				o.pos = mul (UNITY_MATRIX_P, mul (UNITY_MATRIX_V, float4(worldPosition, 1.0f)) + float4(quadPoint * _Size/2, 0.0f));

				o.uv = quadPoints[id];
				o.uv += float2(1, 1);
				o.uv *= (0.5);

				return o;
			}

			sampler2D _MainTex;	

			float4 frag (v2f i) : COLOR
			{
				float4 col = float4(1,1,1, 1);
				return col;
			}
			ENDCG 
		}
	} 	
}
