Shader "Universal Render Pipeline/2D/ColoredSimpleSpriteShader"
{
    Properties
    {
        _MainTex("_MainTex", 2D) = "white" {}
        _ColorMask("_ColorMask", 2D) = "black" {}
        _ExtraST("_ExtraST", Vector) = (1,1,0,0)
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _ExtraST;
    CBUFFER_END
    ENDHLSL

    SubShader
    {
        Tags {"Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue" = "AlphaTest" "RenderType" = "TransparentCutout"}
            ZTest LEqual    //Default
            // ZTest Less | Greater | GEqual | Equal | NotEqual | Always
            ZWrite On       //Default
            Cull Off

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

            TEXTURE2D(_ColorMask);
            SAMPLER(sampler_ColorMask);

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
            StructuredBuffer<int> _propertyPointers;
            StructuredBuffer<float4> _mainTexSTBuffer;
            StructuredBuffer<int> _sortingIndexBuffer;
            StructuredBuffer<float2> _positionBuffer;
            StructuredBuffer<float2> _pivotBuffer;
            StructuredBuffer<float2> _heightWidthBuffer;
            StructuredBuffer<float4> _colorBuffer;
#endif

            void setup()
            {
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                int propertyIndex = _propertyPointers[unity_InstanceID];
                float2 scale = _heightWidthBuffer[propertyIndex];
                float2 renderPos = _positionBuffer[propertyIndex] - scale * _pivotBuffer[propertyIndex];
                unity_ObjectToWorld = half4x4
                (
                    scale.x, 0, 0, renderPos.x,
                    0, scale.y, 0, renderPos.y,
                    0, 0, 1, 0,
                    0, 0, 0, 1
                );
#endif
            }

            float2 TilingAndOffset(float2 UV, float2 Tiling, float2 Offset)
            {
                return UV * Tiling + Offset;
            }
            Varyings UnlitVertex(Attributes attributes, uint instanceID : SV_InstanceID)
            {
                Varyings varyings = (Varyings)0;

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                int propertyIndex = _propertyPointers[instanceID];
                float4 mainTexST = _mainTexSTBuffer[propertyIndex];
                int sortingIndex = _sortingIndexBuffer[propertyIndex];
#else
                float4 mainTexST = float4(1, 1, 0, 0);
                int sortingIndex = 0;
#endif

                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_TRANSFER_INSTANCE_ID(attributes, varyings);

                varyings.positionCS = TransformObjectToHClip(attributes.positionOS);
                varyings.positionCS.z = 1.0 / (sortingIndex + 1); //+1 to prevent 0 sorting index
                varyings.uv = TilingAndOffset(TilingAndOffset(attributes.uv, mainTexST.xy, mainTexST.zw), _ExtraST.xy, _ExtraST.zw);

                return varyings;
            }

            half4 UnlitFragment(Varyings varyings, uint instanceID : SV_InstanceID) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, varyings.uv);
                half4 maskColor = SAMPLE_TEXTURE2D(_ColorMask, sampler_ColorMask, varyings.uv);
                clip(texColor.w - 0.5);

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
                int propertyIndex = _propertyPointers[instanceID];
                float4 color = _colorBuffer[propertyIndex];
                float4 maskedColor = maskColor * color;
                float mask = ceil(maskColor.x) * maskColor.w;
                return lerp(texColor, maskedColor, mask);
                //return texColor * color;
#else
                return texColor;
#endif
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
