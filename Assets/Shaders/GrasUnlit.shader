Shader "Unlit/GrasUnlit"
{
    Properties
    {
        _MainTex ("Main texture", 2D) = "white" {}
        _HeightMap ("Height Map", 2D) = "black" {}
        _Color ("Color", COLOR) = (1 , 1 , 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Cull Off
        Zwrite On

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half4 _Color;
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            sampler2D _HeightMap;
            
            StructuredBuffer<float3> _Positions;

            float _Rotation;
            float _HalfQuadWidth;
            
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

            float4 RotateAroundYInDegrees (float4 vertex, const float degrees)
            {
                const float angle = degrees * PI / 180.0;

                float sine, cosine;
                sincos(angle, sine, cosine);
                
                const float2x2 rotation_matrix = float2x2(cosine, -sine, sine, cosine);
                
                return float4(mul(rotation_matrix, vertex.xz), vertex.yw).xzyw;
            }
            
            v2f vert (appdata v, const uint instance_id : SV_InstanceID)
            {
                v2f output;
                output.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float4 offset_pos = v.vertex;
                offset_pos.x -= _HalfQuadWidth;

                const float4 rotated_pos = RotateAroundYInDegrees(offset_pos, _Rotation);
                const float3 world_pos = TransformObjectToWorld(rotated_pos) + _Positions[instance_id];

                output.vertex = TransformWorldToHClip(world_pos);
                return output;
            }

            half4 frag (v2f i) : SV_Target
            {
                const half4 object_color = tex2D(_MainTex, i.uv);
                clip(-(0.5 - object_color.a));
                
                return _Color * object_color;
            }
            ENDHLSL
        }
    }
}
