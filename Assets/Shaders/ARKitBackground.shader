Shader "Unlit/ARKitBackgroundWithBlackFilter"
{
    Properties
    {
        _textureY ("TextureY", 2D) = "white" {}
        _textureCbCr ("TextureCbCr", 2D) = "black" {}
        _HumanStencil ("HumanStencil", 2D) = "black" {}
        _HumanDepth ("HumanDepth", 2D) = "black" {}
        _EnvironmentDepth ("EnvironmentDepth", 2D) = "black" {}
        _FilterOpacity ("Black Filter Opacity", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "ForceNoShadowCasting" = "True"
        }

        Pass
        {
            Cull Off
            ZTest Always
            ZWrite On
            Lighting Off
            LOD 100
            Tags
            {
                "LightMode" = "Always"
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local ARKIT_BACKGROUND_URP
            #pragma multi_compile_local __ ARKIT_HUMAN_SEGMENTATION_ENABLED ARKIT_ENVIRONMENT_DEPTH_ENABLED

#if ARKIT_BACKGROUND_URP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            #define ARKIT_TEXTURE2D_HALF(texture) TEXTURE2D(texture)
            #define ARKIT_SAMPLER_HALF(sampler) SAMPLER(sampler)
            #define ARKIT_TEXTURE2D_FLOAT(texture) TEXTURE2D(texture)
            #define ARKIT_SAMPLER_FLOAT(sampler) SAMPLER(sampler)
            #define ARKIT_SAMPLE_TEXTURE2D(texture,sampler,texcoord) SAMPLE_TEXTURE2D(texture,sampler,texcoord)

#else // Legacy RP

            #include "UnityCG.cginc"

            #define real4 half4
            #define real4x4 half4x4
            #define TransformObjectToHClip UnityObjectToClipPos
            #define FastSRGBToLinear GammaToLinearSpace

            #define ARKIT_TEXTURE2D_HALF(texture) UNITY_DECLARE_TEX2D_HALF(texture)
            #define ARKIT_SAMPLER_HALF(sampler)
            #define ARKIT_TEXTURE2D_FLOAT(texture) UNITY_DECLARE_TEX2D_FLOAT(texture)
            #define ARKIT_SAMPLER_FLOAT(sampler)
            #define ARKIT_SAMPLE_TEXTURE2D(texture,sampler,texcoord) UNITY_SAMPLE_TEX2D(texture,texcoord)

#endif

            struct appdata
            {
                float3 position : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct fragment_output
            {
                real4 color : SV_Target;
                float depth : SV_Depth;
            };

            CBUFFER_START(UnityARFoundationPerFrame)
            float4x4 _UnityDisplayTransform;
            float _UnityCameraForwardScale;
            CBUFFER_END

            CBUFFER_START(ARKitColorTransformations)
            static const real4x4 s_YCbCrToSRGB = real4x4(
                real4(1.0h,  0.0000h,  1.4020h, -0.7010h),
                real4(1.0h, -0.3441h, -0.7141h,  0.5291h),
                real4(1.0h,  1.7720h,  0.0000h, -0.8860h),
                real4(0.0h,  0.0000h,  0.0000h,  1.0000h)
            );
            CBUFFER_END

            CBUFFER_START(ARKitBlackFilter)
            half _FilterOpacity;
            CBUFFER_END

            inline float ConvertDistanceToDepth(float d)
            {
                d = _UnityCameraForwardScale > 0.0 ? _UnityCameraForwardScale * d : d;
                return (d < _ProjectionParams.y) ? 0.0f : ((1.0f / _ZBufferParams.z) * ((1.0f / d) - _ZBufferParams.w));
            }

            ARKIT_TEXTURE2D_HALF(_textureY);
            ARKIT_SAMPLER_HALF(sampler_textureY);
            ARKIT_TEXTURE2D_HALF(_textureCbCr);
            ARKIT_SAMPLER_HALF(sampler_textureCbCr);
#if ARKIT_ENVIRONMENT_DEPTH_ENABLED
            ARKIT_TEXTURE2D_FLOAT(_EnvironmentDepth);
            ARKIT_SAMPLER_FLOAT(sampler_EnvironmentDepth);
#elif ARKIT_HUMAN_SEGMENTATION_ENABLED
            ARKIT_TEXTURE2D_HALF(_HumanStencil);
            ARKIT_SAMPLER_HALF(sampler_HumanStencil);
            ARKIT_TEXTURE2D_FLOAT(_HumanDepth);
            ARKIT_SAMPLER_FLOAT(sampler_HumanDepth);
#endif

            v2f vert(appdata v)
            {
                float4 position = TransformObjectToHClip(v.position);
                float2 texcoord = mul(float4(v.texcoord, 1.0f, 1.0f), _UnityDisplayTransform).xy;
                v2f o;
                o.position = position;
                o.texcoord = texcoord;
                return o;
            }

            fragment_output frag(v2f i)
            {
                real4 ycbcr = real4(
                    ARKIT_SAMPLE_TEXTURE2D(_textureY, sampler_textureY, i.texcoord).r,
                    ARKIT_SAMPLE_TEXTURE2D(_textureCbCr, sampler_textureCbCr, i.texcoord).rg,
                    1.0h);

                real4 videoColor = mul(s_YCbCrToSRGB, ycbcr);

#if !UNITY_COLORSPACE_GAMMA
                videoColor.xyz = FastSRGBToLinear(videoColor.xyz);
#endif

                // 0 = original camera image; 1 = fully black.
                videoColor.rgb *= (1.0h - saturate(_FilterOpacity));

                float depthValue = 0.0f;
#if ARKIT_ENVIRONMENT_DEPTH_ENABLED
                float envDistance = ARKIT_SAMPLE_TEXTURE2D(_EnvironmentDepth, sampler_EnvironmentDepth, i.texcoord).r;
                depthValue = ConvertDistanceToDepth(envDistance);
#elif ARKIT_HUMAN_SEGMENTATION_ENABLED
                if (ARKIT_SAMPLE_TEXTURE2D(_HumanStencil, sampler_HumanStencil, i.texcoord).r > 0.5h)
                {
                    float humanDistance = ARKIT_SAMPLE_TEXTURE2D(_HumanDepth, sampler_HumanDepth, i.texcoord).r;
                    depthValue = ConvertDistanceToDepth(humanDistance);
                }
#endif

                fragment_output o;
                o.color = videoColor;
                o.depth = depthValue;
                return o;
            }

            ENDHLSL
        }
    }
}
