Shader "Custom/ParticleShader" 
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

			struct SnowCell
			{
				float		fillGrade;
				float		newFillGrade;
				float3		worldPosition;
				float3		velocity;
			};

			StructuredBuffer<SnowCell>  _Cells;
			StructuredBuffer<float3>  quadPoints;
			float _CellSize;

			struct v2f 
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
				float2 fillGrade :TEXCOORD1;
			};
			
			float _Size;

			v2f vert (appdata_base v, uint id : SV_VertexID, uint inst : SV_InstanceID)
			{

				float fillGrade = _Cells[inst].fillGrade;
				float3 worldPosition = _Cells[inst].worldPosition;
				float3 quadPoint = quadPoints[id];

				float size = _Size;

				v2f o;
				o.fillGrade = float2(fillGrade, fillGrade);
				o.normal = v.normal;
				o.pos = mul (UNITY_MATRIX_P, mul (UNITY_MATRIX_V, float4(worldPosition, 1.0f)) + float4(quadPoint * _CellSize/2, 0.0f));

				o.uv = quadPoints[id];
				o.uv += float2(1, 1);
				o.uv *= (0.5);

				return o;
			}

			sampler2D _MainTex;	

			float4 frag (v2f i) : COLOR
			{
				clip(i.fillGrade.x);
				float4 col = float4(1.0 - i.uv.x * 0.35 , 1.0f - i.uv.x * 0.25 , 1.0 , i.fillGrade.x) ;

				col = float4(1,1,1, i.fillGrade.x);

				if((i.uv.x * 2.0 - 1.0) * ( i.uv.x * 2.0-1.0) + ( i.uv.y * 2.0 - 1.0) * ( i.uv.y * 2.0 - 1.0) > 1.0f)
					clip(-1);

				return col;
			}
			ENDCG 
		}
	} 	
}
