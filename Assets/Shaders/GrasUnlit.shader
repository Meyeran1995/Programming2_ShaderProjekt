Shader "Unlit/GrasUnlit"
{
    Properties
    {
        _MainTex ("Main texture", 2D) = "white" {}
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
            
            StructuredBuffer<float4> _Positions;
            float _Rotation;
            
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

            float4 RotateAroundYInDegrees (const float4 vertex, const float degrees)
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

                const float4 buffered_pos = _Positions[instance_id];
                
                v.vertex.y *= buffered_pos.w;
                
                const float4 rotated_pos = RotateAroundYInDegrees(v.vertex, _Rotation);
                const float3 world_pos = TransformObjectToWorld(rotated_pos) + buffered_pos.xyz;

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
