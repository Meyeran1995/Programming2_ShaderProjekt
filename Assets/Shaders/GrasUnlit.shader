Shader "Unlit/GrasUnlit"
{
    Properties
    {
        _MainTex ("Main texture", 2D) = "white" {}
        _Color ("Color", COLOR) = (1 , 1 , 1, 1)
        _TipColor ("TipColor", COLOR) = (1 , 1 , 1, 1)
        _MinGrasHeight ("MinGrasHeight", float) = 0.5
        _WindStrength ("WindStrength", float) = 0.5
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

            float _WindStrength;
            
            half4 _Color;
            half4 _TipColor;
            float _MinGrasHeight;
            
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
                float height : PSIZE;
            };

            float4 RotateAroundYInDegrees (const float4 vertex, const float degrees)
            {
                const float angle = degrees * PI / 180.0;

                float sine, cosine;
                sincos(angle, sine, cosine);
                
                const float2x2 rotation_matrix = float2x2(cosine, -sine, sine, cosine);
                
                return float4(mul(rotation_matrix, vertex.xz), vertex.yw).xzyw;
            }

            float InverseLerp(float a, float b, float v)
            {
                return clamp((v - a) / (b - a), 0, 1);
            }
            
            v2f vert (appdata v, const uint instance_id : SV_InstanceID)
            {
                v2f output;
                
                const float4 buffered_pos = _Positions[instance_id];
                const float height = buffered_pos.w;

                v.vertex.y *= height;
                
                const float4 rotated_pos = RotateAroundYInDegrees(v.vertex, _Rotation);

                float4 local_pos = rotated_pos;

                const float wind_power = _WindStrength - (height - (1.0f + _MinGrasHeight));
                const float cos_time = cos(_Time.y * wind_power);

                const float trig_value = cos_time * cos_time;
                
                local_pos.x += v.uv.y * trig_value * height * 0.6f;
                local_pos.z += v.uv.y * trig_value * height * 0.4f;
                
                const float3 world_pos = TransformObjectToWorld(local_pos) + buffered_pos.xyz;

                output.uv = TRANSFORM_TEX(v.uv, _MainTex);
                output.vertex = TransformWorldToHClip(world_pos);
                output.height = height;
                
                return output;
            }

            half4 frag (v2f i) : SV_Target
            {
                const half4 texture_color = tex2D(_MainTex, i.uv);
                clip(-(0.5 - texture_color.a));

                float height_factor = InverseLerp(_MinGrasHeight, 1 + _MinGrasHeight, i.height);
                height_factor = lerp(0.0f, height_factor, i.uv.y);

                return lerp(_Color, _TipColor, height_factor) * texture_color;
            }
            ENDHLSL
        }
    }
}
