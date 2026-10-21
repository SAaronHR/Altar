Shader "Altar/AzulejoBlanco"
{
    Properties
    {
        _BaseColor ("Color del azulejo", Color) = (0.94,0.95,0.96,1)
        _GroutColor ("Color de la junta", Color) = (0.56,0.59,0.61,1)
        _TileSize ("Lado del azulejo en metros", Float) = 0.3
        _GroutWidth ("Ancho de junta en metros", Float) = 0.003
        _Smoothness ("Brillo seco", Range(0,1)) = 0.25
        [HDR] _EmissionColor ("Emision", Color) = (0,0,0,1)
        _DarkStart ("Oscuridad desde metros", Float) = 8
        _DarkEnd ("Oscuridad completa metros", Float) = 26
        [HideInInspector] _BaseMap ("Base Map", 2D) = "white" {}
        [HideInInspector] _Cutoff ("Cutoff", Float) = 0.5
        [HideInInspector] _Cull ("Cull", Float) = 2
        [HideInInspector] _BumpMap ("Normal", 2D) = "bump" {}
        [HideInInspector] _BumpScale ("Normal Scale", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GroutColor;
                float _TileSize;
                float _GroutWidth;
                half _Smoothness;
                half4 _EmissionColor;
                float _DarkStart;
                float _DarkEnd;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; };
            struct Varyings {
                float4 positionCS:SV_POSITION;
                float3 positionWS:TEXCOORD0;
                half3 normalWS:TEXCOORD1;
                half fog:TEXCOORD2;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fog = ComputeFogFactor(p.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                half3 normal = normalize(input.normalWS);
                float3 axis = abs(normal);
                float2 planar = axis.y > 0.5 ? input.positionWS.xz :
                    (axis.x > 0.5 ? input.positionWS.zy : input.positionWS.xy);
                float size = max(_TileSize,0.01);
                float2 uv = planar / size;
                float2 edge = min(frac(uv),1-frac(uv));
                float2 aa = max(fwidth(uv),0.0001);
                float2 inside = smoothstep(_GroutWidth/(2*size)-aa*0.5,
                    _GroutWidth/(2*size)+aa*0.5,edge);
                half tile = inside.x*inside.y;
                if (_GroutWidth <= 0) tile = 1;
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = lerp(_GroutColor.rgb,_BaseColor.rgb,tile);
                surface.metallic = 0;
                surface.specular = half3(0.04,0.04,0.04);
                surface.smoothness = lerp(0.05,_Smoothness,tile);
                surface.normalTS = half3(0,0,1);
                surface.occlusion = lerp(0.85,1,tile);
                surface.alpha = 1;
                surface.emission = _EmissionColor.rgb;
                InputData data = (InputData)0;
                data.positionWS = input.positionWS;
                data.normalWS = normal;
                data.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                data.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                data.bakedGI = SampleSH(normal);
                data.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                data.shadowMask = half4(1,1,1,1);
                data.vertexLighting = VertexLighting(input.positionWS,normal);
                half4 color = UniversalFragmentPBR(data,surface);
                color.rgb = MixFog(color.rgb,InitializeInputDataFog(float4(input.positionWS,1),input.fog));
                float darkness = smoothstep(_DarkStart,max(_DarkEnd,_DarkStart+0.1),distance(input.positionWS,_WorldSpaceCameraPos));
                color.rgb *= 1.0 - darkness;
                return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
