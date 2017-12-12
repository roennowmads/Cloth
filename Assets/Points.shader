Shader "Unlit/Points" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_AlbedoTex("AlbedoTex", 2D) = "white" {}
		_ColorTex("_ColorTex", 2D) = "white" {}
	}
	SubShader {
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		//Tags{"RenderType" = "Opaque"}
		//Blend SrcAlpha OneMinusSrcAlpha
		//Blend One OneMinusSrcAlpha		
		Cull Off
		//ZWrite Off

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#pragma target es3.1

			sampler2D _ColorTex;
			sampler2D _AlbedoTex;

			StructuredBuffer<float3> _Points;

			uniform float aspect;

			struct appdata
			{
				uint id : SV_VertexID;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				fixed3 color : COLOR;
				float2 texCoord : TEXCOORD0;
			};

			static const half2 quadCoords[6] = {
				half2(-0.1, -0.1),
				half2(0.1, 0.1),
				half2(0.1, -0.1),

				half2(-0.1, 0.1),
				half2(0.1, 0.1),
				half2(-0.1, -0.1)
			};

			static const float2 quadTexCoords[6] = {
				float2(0.0, 0.0),
				float2(1.0, 1.0),
				float2(1.0, 0.0),

				float2(0.0, 1.0),
				float2(1.0, 1.0),
				float2(0.0, 0.0)
			};

			static const float inv6 = 1.0 / 6.0;
			static const float inv255 = 1.0 / 255.0;

			// Declare instanced properties inside a cbuffer.
			// Each instanced property is an array of by default 500(D3D)/128(GL) elements. Since D3D and GL imposes a certain limitation
			// of 64KB and 16KB respectively on the size of a cubffer, the default array size thus allows two matrix arrays in one cbuffer.
			// Use maxcount option on #pragma instancing_options directive to specify array size other than default (divided by 4 when used
			// for GL).
			//UNITY_INSTANCING_CBUFFER_START(Props)
			//	UNITY_DEFINE_INSTANCED_PROP(float4, _Pos)	// Make _Color an instanced property (i.e. an array)
			//	UNITY_INSTANCING_CBUFFER_END

			void vert(in appdata v, out v2f o)
			{
				UNITY_INITIALIZE_OUTPUT(v2f, o)

				float quadId = v.id * inv6;
				
				uint quad_vertexID = mad(-6.0, floor(quadId), v.id);  //Useful trick: foo % n == foo & (n - 1), if n is a power of 2

				float4 position = float4(_Points[quadId], 1.0);

				o.vertex = UnityObjectToClipPos(position);

				half size = 10.0;
				half2 quadSize = half2(size, size * aspect);
				half2 deltaSize = quadCoords[quad_vertexID] * quadSize;
				o.vertex.xy += deltaSize;

				o.texCoord = quadTexCoords[quad_vertexID];
			}

			struct fragOutput
			{
				fixed4 color : SV_Target;
			};


			fragOutput frag(v2f i)
			{
				fragOutput o;

				fixed albedo = tex2Dlod(_AlbedoTex, float4(i.texCoord, 0.0, 0.0) /*float2(0.5,0.5)*/).a;
				//fixed albedo = tex2D(_AlbedoTex, i.texCoord /*float2(0.5,0.5)*/).a;
				
				if (albedo < 0.5) 
					discard;

				//o.color = fixed4(/*i.color*/fixed3(0.5,0.1,0.1), albedo*0.0525);

				//good for fireball:
				o.color = fixed4(/*i.color*/albedo,albedo,albedo, albedo/*albedo*//**0.25*/);
				return o;
			}
			ENDCG
		}
	}
}