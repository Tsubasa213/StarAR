Shader "Custom/StarCrossGlare_OriginBillboard"

{

    Properties

    {

        [HDR] _Color ("Glare Color", Color) = (1.0, 0.9, 0.6, 1.0)

        _SpikeFactor ("Spike Sharpness (Shape)", Range(0.1, 1.0)) = 0.5

        _Scale ("Size Scale", Range(0.1, 3.0)) = 1.0

        _GlowPower ("Glow Softness", Range(0.1, 5.0)) = 2.0

    }

    SubShader

    {

        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        LOD 100



        ZWrite Off

        Blend SrcAlpha One

        // 念のため両面描画にしておきます

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

                float2 uv : TEXCOORD0;

            };



            struct v2f

            {

                float2 uv : TEXCOORD0;

                float4 pos : SV_POSITION;

            };



            float4 _Color;

            float _SpikeFactor;

            float _Scale;

            float _GlowPower;



            v2f vert (appdata v)

            {

                v2f o;

               

                // 1. オブジェクトの中心のワールド座標を取得

                float3 centerWorld = unity_ObjectToWorld._m03_m13_m23;

               

                // 2. 【重要】向くべき方向（原点 0,0,0）へのベクトルを計算

                float3 forward = normalize(float3(0, 0, 0) - centerWorld);

               

                // ※もし将来的に「カメラ」の方向に向けたくなった場合は、上の行を消して下の行を使います

                // float3 forward = normalize(_WorldSpaceCameraPos - centerWorld);



                // 3. 上方向(Up)と右方向(Right)を計算し、常に原点を向く仮想的な面を作る

                float3 upGuide = float3(0, 1, 0);

                // ターゲットの真上や真下に星がある場合の計算エラー（外積が0になる現象）を防ぐ

                if (abs(forward.y) > 0.999)

                {

                    upGuide = float3(0, 0, 1);

                }

               

                float3 right = normalize(cross(upGuide, forward));

                float3 up = cross(forward, right);

               

                // 4. TransformのScale（大きさ）を取得

                float scaleX = length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x));

                float scaleY = length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y));

               

                // 5. Quadの頂点（v.vertex.xy）を、先ほど計算した原点向きの姿勢に合わせてワールド空間に再配置する

                float3 worldPos = centerWorld

                                  + right * v.vertex.x * scaleX

                                  + up * v.vertex.y * scaleY;

               

                // 6. ワールド空間からクリップ空間に変換（画面描画用）

                o.pos = mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));

               

                o.uv = v.uv;

                return o;

            }



            fixed4 frag (v2f i) : SV_Target

            {

                // フラグメントシェーダーは前回と全く同じです（図形の計算）

                float2 uv = i.uv * 2.0 - 1.0;

                float shape = pow(abs(uv.x), _SpikeFactor) + pow(abs(uv.y), _SpikeFactor);

                float alpha = saturate(1.0 - shape * _Scale);

                alpha = pow(alpha, _GlowPower);

               

                return fixed4(_Color.rgb * alpha, alpha);

            }

            ENDCG

        }

    }

}