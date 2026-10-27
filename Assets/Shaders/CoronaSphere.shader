Shader "Custom/CenterOpaqueSphere"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _Power ("Fade Power", Range(0.1,10)) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            fixed4 _Color;
            float _Power;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);

                o.worldNormal =
                    UnityObjectToWorldNormal(v.normal);

                o.worldPos =
                    mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir =
                    normalize(_WorldSpaceCameraPos - i.worldPos);

                float facing =
                    saturate(dot(normalize(i.worldNormal), viewDir));

                float alpha =
                    pow(facing, _Power);

                return fixed4(
                    _Color.rgb,
                    _Color.a * alpha
                );
            }

            ENDCG
        }
    }
}