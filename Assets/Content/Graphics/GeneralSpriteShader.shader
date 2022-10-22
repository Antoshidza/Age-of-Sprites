Shader "Universal Render Pipeline/2D/General Sprite Shader"
{
    Properties
    {
        _MainTex("_MainTex", 2D) = "white" {}
        _ExtraST("_ExtraST", Vector) = (0,0,0,0)
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _ExtraST;
    CBUFFER_END
    ENDHLSL

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite On

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue" = "Transparent" "RenderType" = "TransparentCutout"}
            //AlphaTest Greater 0.5

            HLSLPROGRAM
            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            #pragma target 4.5
            #pragma exclude_renderers gles gles3 glcore
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float2 uv			: TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS		: SV_POSITION;
                float2	uv				: TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)
            StructuredBuffer<float4x4> _transformMatrixBuffer;
            StructuredBuffer<float4> _mainTexSTBuffer;
#endif

            void setup()
            {
#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)
                unity_ObjectToWorld = 0.0;
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(0, 0, 0, 1); //setting position
                unity_ObjectToWorld._m00_m11_m22_m33 = float4(1, 1, 1, 1); //setting scale
                //unity_ObjectToWorld = _transformMatrixBuffer[unity_InstanceID];
#endif
            }

            float2 TilingAndOffset(float2 UV, float2 Tiling, float2 Offset)
            {
                return UV * Tiling + Offset;
            }
            Varyings UnlitVertex(Attributes attributes, uint instanceID : SV_InstanceID)
            {
                Varyings varyings = (Varyings)0;

                //extract all CBuffer data here
#if defined(UNITY_INSTANCING_ENABLED) || defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(UNITY_STEREO_INSTANCING_ENABLED)
                float4 mainTexST = _mainTexSTBuffer[instanceID];
#else
                float4 mainTexST = float4(0, 0, 0, 0); //TODO: figure out deault value
#endif

                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_TRANSFER_INSTANCE_ID(attributes, varyings);

                varyings.positionCS = TransformObjectToHClip(attributes.positionOS);
                //varyings.uv = TilingAndOffset(TilingAndOffset(attributes.uv, mainTexST.xy, mainTexST.zw), _ExtraST.xy, _ExtraST.zw);
                varyings.uv = TilingAndOffset(attributes.uv, mainTexST.xy, mainTexST.zw);

                return varyings;
            }

            float4 UnlitFragment(Varyings varyings) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, varyings.uv);
                clip(texColor.w - 0.5);
                return texColor;
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
