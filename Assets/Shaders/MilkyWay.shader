Shader "Custom/SeamlessMilkyWaySphere"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color (中心の色)", Color) = (1.0, 0.8, 0.5, 1.0)
        [HDR] _EdgeColor ("Edge Color (外側の色)", Color) = (0.05, 0.15, 0.4, 1.0)
        
        // 天の川の傾き（X軸回転）
        _TiltAngle ("Galactic Tilt (傾き)", Range(0.0, 360.0)) = 45.0
        
        _Thickness ("Band Thickness (帯の太さ)", Range(0.01, 1.0)) = 0.25
        _NoiseScale ("Noise Scale (雲の細かさ)", Range(1.0, 50.0)) = 15.0
        _Intensity ("Overall Intensity", Range(0.1, 5.0)) = 2.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha One
        // スフィアの内側から見ても描画されるように両面描画にする
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
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                // UVではなく、3Dのローカル座標をフラグメントに渡す
                float3 localPos : TEXCOORD0;
            };

            float4 _CoreColor;
            float4 _EdgeColor;
            float _TiltAngle;
            float _Thickness;
            float _NoiseScale;
            float _Intensity;

            // --- 3D疑似乱数と3Dノイズ関数 ---
            // 3D空間上でノイズを計算するため、繋ぎ目が絶対に発生しません
            float hash(float3 p)
            {
                float3 p3 = frac(p * 0.1031);
                p3 += dot(p3, p3.zyx + 31.32);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise3D(float3 x)
            {
                float3 p = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash(p + float3(0,0,0));
                float n100 = hash(p + float3(1,0,0));
                float n010 = hash(p + float3(0,1,0));
                float n110 = hash(p + float3(1,1,0));
                float n001 = hash(p + float3(0,0,1));
                float n101 = hash(p + float3(1,0,1));
                float n011 = hash(p + float3(0,1,1));
                float n111 = hash(p + float3(1,1,1));

                float x00 = lerp(n000, n100, f.x);
                float x10 = lerp(n010, n110, f.x);
                float x01 = lerp(n001, n101, f.x);
                float x11 = lerp(n011, n111, f.x);

                float y0 = lerp(x00, x10, f.y);
                float y1 = lerp(x01, x11, f.y);

                return lerp(y0, y1, f.z);
            }

            float fbm3D(float3 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 5; i++) // スケールが大きいのでループ回数を増やしてディテールUP
                {
                    v += a * noise3D(p);
                    p = p * 2.0;
                    a *= 0.5;
                }
                return v;
            }
            // ----------------------------------------

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                // 頂点のローカル座標をそのまま渡す
                o.localPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. スフィアの中心からの方向ベクトルを取得（サイズ220に影響されないように正規化）
                float3 dir = normalize(i.localPos);

                // 2. 角度の計算（X軸周りの回転）
                float rad = radians(_TiltAngle);
                float s, c;
                sincos(rad, s, c);
                
                // 3Dベクトルを回転させる
                float3 rotatedDir = dir;
                rotatedDir.y = dir.y * c - dir.z * s;
                rotatedDir.z = dir.y * s + dir.z * c;

                // 3. 天の川の「帯」の計算（回転後のY軸からの距離）
                float distFromEquator = abs(rotatedDir.y);
                float bandMask = smoothstep(_Thickness, 0.0, distFromEquator);

                // 4. 3Dノイズの適用
                // dirにノイズスケールを掛けて、球体の表面に沿って雲を発生させる
                float cloudNoise = fbm3D(dir * _NoiseScale);
                
                // 帯とノイズを合成
                float density = bandMask * cloudNoise;
                density = pow(density, 1.5);

                // 5. 色の合成
                float3 finalColor = lerp(_EdgeColor.rgb, _CoreColor.rgb, density) * density * _Intensity;

                return fixed4(finalColor, density);
            }
            ENDCG
        }
    }
}