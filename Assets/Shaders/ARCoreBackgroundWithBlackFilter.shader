Shader "Unlit/ARCoreBackgroundWithBlackFilter"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _FilterOpacity("Black Filter Opacity", Range(0, 1)) = 0.8
    }

    // ARCore uses an external camera texture on OpenGLES 3.
    SubShader
    {
        Name "ARCore Background With Black Filter (GLES3)"
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "ForceNoShadowCasting" = "True"
        }

        Pass
        {
            Name "AR Camera Background With Black Filter"
            Cull Off
            ZTest Always
            ZWrite On
            Lighting Off
            LOD 100
            Tags
            {
                "LightMode" = "Always"
            }

            GLSLPROGRAM

            #pragma only_renderers gles3
            #pragma multi_compile_local __ ARCORE_IMAGE_STABILIZATION_ENABLED

            #include "UnityCG.glslinc"

#ifdef SHADER_API_GLES3
            #extension GL_OES_EGL_image_external_essl3 : require
#endif

#ifndef ARCORE_IMAGE_STABILIZATION_ENABLED
            #define ARCORE_TEXCOORD_TYPE vec2
#else
            #define ARCORE_TEXCOORD_TYPE vec3
#endif

            uniform mat4 _UnityDisplayTransform;
            uniform float _FilterOpacity;

#ifdef VERTEX
            varying ARCORE_TEXCOORD_TYPE textureCoord;

            void main()
            {
#ifdef SHADER_API_GLES3
                gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;

#ifdef ARCORE_IMAGE_STABILIZATION_ENABLED
                textureCoord = gl_MultiTexCoord0.xyz;
#else
                textureCoord =
                    (vec4(gl_MultiTexCoord0.x, gl_MultiTexCoord0.y, 1.0, 0.0) *
                     _UnityDisplayTransform).xy;
#endif
#endif
            }
#endif

#ifdef FRAGMENT
            varying ARCORE_TEXCOORD_TYPE textureCoord;
            uniform samplerExternalOES _MainTex;

            void main()
            {
#ifdef SHADER_API_GLES3
#ifdef ARCORE_IMAGE_STABILIZATION_ENABLED
                vec2 tc = textureCoord.xy / textureCoord.z;
#else
                vec2 tc = textureCoord;
#endif
                vec3 result = texture(_MainTex, tc).xyz;
                result *= 1.0 - clamp(_FilterOpacity, 0.0, 1.0);
                gl_FragColor = vec4(result, 1.0);
                gl_FragDepth = 1.0;
#endif
            }
#endif
            ENDGLSL
        }
    }

    // Vulkan uses a regular sampler and HLSL syntax.
    SubShader
    {
        Name "ARCore Background With Black Filter (Vulkan)"
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "ForceNoShadowCasting" = "True"
        }

        Pass
        {
            Name "AR Camera Background With Black Filter"
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

            #pragma only_renderers vulkan
            #pragma multi_compile_local __ ARCORE_IMAGE_STABILIZATION_ENABLED
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

#ifndef ARCORE_IMAGE_STABILIZATION_ENABLED
            #define ARCORE_TEXCOORD_TYPE float2
#else
            #define ARCORE_TEXCOORD_TYPE float3
#endif

            float4x4 _UnityDisplayTransform;
            float _FilterOpacity;

            struct VertexInput
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 position : SV_POSITION;
                ARCORE_TEXCOORD_TYPE textureCoord : TEXCOORD0;
            };

            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;
                output.position = UnityObjectToClipPos(input.vertex.xyz);

#ifdef ARCORE_IMAGE_STABILIZATION_ENABLED
                output.textureCoord = input.uv.xyz;
#else
                output.textureCoord =
                    mul(float4(input.uv.x, input.uv.y, 1.0, 0.0),
                        _UnityDisplayTransform).xy;
#endif
                return output;
            }

            sampler2D _MainTex;

            struct FragmentOutput
            {
                float4 color : SV_Target;
                float depth : SV_Depth;
            };

            FragmentOutput frag(VertexOutput input)
            {
#ifdef ARCORE_IMAGE_STABILIZATION_ENABLED
                float2 tc = input.textureCoord.xy / input.textureCoord.z;
#else
                float2 tc = input.textureCoord;
#endif
                float3 result = tex2D(_MainTex, tc).xyz;
                result *= 1.0 - clamp(_FilterOpacity, 0.0, 1.0);

                FragmentOutput output;
                output.color = float4(result, 1.0);
                // Far depth keeps the AR stars in front of the camera image.
                output.depth = 0.0;
                return output;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
