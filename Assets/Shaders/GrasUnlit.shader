Shader "Unlit/GrasUnlit"
{
    Properties
    {
        _MainTex ("Main texture", 2D) = "white" {}
        _Color ("Color", COLOR) = (1 , 1 , 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        Zwrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma target 4.5

            #include "UnityCG.cginc"

            fixed4 _Color;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            StructuredBuffer<float3> _Positions;
            
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
            
            v2f vert (appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vertex = UnityObjectToClipPos(_Positions[instanceID]);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 objectColor = tex2D(_MainTex, i.uv);

                return _Color * objectColor;
            }
            ENDCG
        }
    }
}
