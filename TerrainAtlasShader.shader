Shader "Custom/TerrainAtlasShader"
{
    Properties
    {
        _MainTex ("Texture Atlas", 2D) = "white" {}
        _NormalMap ("Normal Map Atlas", 2D) = "bump" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Padding ("Padding", Float) = 2.0
        
        _BlendSpread ("Blend Spread", Range(0,1)) = 0.5
        _BlendStrength ("Blend Strength", Range(0,1)) = 0.5
        
        // Height thresholds for each terrain type
        _HeightThreshold0 ("Height Threshold Deep Water", Range(0,1)) = 0.025
        _HeightThreshold1 ("Height Threshold River", Range(0,1)) = 0.05
        _HeightThreshold2 ("Height Threshold Shallow Water", Range(0,1)) = 0.1
        _HeightThreshold3 ("Height Threshold Sand", Range(0,1)) = 0.3
        _HeightThreshold4 ("Height Threshold Grass", Range(0,1)) = 0.45
        _HeightThreshold5 ("Height Threshold Forest", Range(0,1)) = 0.6
        _HeightThreshold6 ("Height Threshold Rock", Range(0,1)) = 0.75
        // Snow is implicitly everything above _HeightThreshold6
        
        // Tiling and Offset for each terrain type within its atlas region
        _Tiling0 ("Tiling Deep Water", Vector) = (1.0, 1.0, 0, 0)
        _Offset0 ("Offset Deep Water", Vector) = (0.0, 0.0, 0, 0)
        _Tiling1 ("Tiling River", Vector) = (1.0, 1.0, 0, 0)
        _Offset1 ("Offset River", Vector) = (0.0, 0.0, 0, 0)
        _Tiling2 ("Tiling Shallow Water", Vector) = (1.0, 1.0, 0, 0)
        _Offset2 ("Offset Shallow Water", Vector) = (0.0, 0.0, 0, 0)
        _Tiling3 ("Tiling Sand", Vector) = (1.0, 1.0, 0, 0)
        _Offset3 ("Offset Sand", Vector) = (0.0, 0.0, 0, 0)
        _Tiling4 ("Tiling Grass", Vector) = (1.0, 1.0, 0, 0)
        _Offset4 ("Offset Grass", Vector) = (0.0, 0.0, 0, 0)
        _Tiling5 ("Tiling Forest", Vector) = (1.0, 1.0, 0, 0)
        _Offset5 ("Offset Forest", Vector) = (0.0, 0.0, 0, 0)
        _Tiling6 ("Tiling Rock", Vector) = (1.0, 1.0, 0, 0)
        _Offset6 ("Offset Rock", Vector) = (0.0, 0.0, 0, 0)
        _Tiling7 ("Tiling Snow", Vector) = (1.0, 1.0, 0, 0)
        _Offset7 ("Offset Snow", Vector) = (0.0, 0.0, 0, 0)
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _NormalMap_ST;
            half _Glossiness;
            half _Metallic;
            float _Padding;
            
            float _BlendSpread;
            float _BlendStrength;
            
            float _HeightThreshold0;
            float _HeightThreshold1;
            float _HeightThreshold2;
            float _HeightThreshold3;
            float _HeightThreshold4;
            float _HeightThreshold5;
            float _HeightThreshold6;
            
            float4 _Tiling0, _Offset0;
            float4 _Tiling1, _Offset1;
            float4 _Tiling2, _Offset2;
            float4 _Tiling3, _Offset3;
            float4 _Tiling4, _Offset4;
            float4 _Tiling5, _Offset5;
            float4 _Tiling6, _Offset6;
            float4 _Tiling7, _Offset7;
        CBUFFER_END

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_NormalMap);
        SAMPLER(sampler_NormalMap);

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 texcoord : TEXCOORD0;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float3 positionWS : TEXCOORD1;
            float3 normalWS : TEXCOORD2;
            float4 tangentWS : TEXCOORD3;
            float4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        // Biome types
        #define BIOME_DESERT 1
        #define BIOME_SAVANNA 2
        #define BIOME_TROPICAL_RAINFOREST 3
        #define BIOME_GRASSLAND 4
        #define BIOME_WOODLAND 5
        #define BIOME_SEASONAL_FOREST 6
        #define BIOME_TEMPERATE_RAINFOREST 7
        #define BIOME_BOREAL_FOREST 8
        #define BIOME_TUNDRA 9
        #define BIOME_ICE 10

        // Terrain types
        #define TERRAIN_DEEP_WATER 0
        #define TERRAIN_RIVER 1
        #define TERRAIN_SHALLOW_WATER 2
        #define TERRAIN_SAND 3
        #define TERRAIN_GRASS 4
        #define TERRAIN_FOREST 5
        #define TERRAIN_ROCK 6
        #define TERRAIN_SNOW 7

        float2 GetAtlasUV(int biomeType, int terrainType, float2 uv)
        {
            // Calculate the size of each tile in the atlas including padding
            float paddedTextureSize = 1024.0 + _Padding * 2.0;
            float tileSizeX = 1.0 / 8.0;  // 8 columns
            float tileSizeY = 1.0 / 10.0; // 10 rows
            
            // Calculate the padding offsets
            float paddingOffsetX = _Padding / paddedTextureSize;
            float paddingOffsetY = _Padding / paddedTextureSize;

            // Calculate the base position in the atlas
            float2 atlasUV;
            atlasUV.x = terrainType * tileSizeX + paddingOffsetX;
            atlasUV.y = (biomeType - 1) * tileSizeY + paddingOffsetY;

            // Calculate the size of each tile without padding
            float2 tileSize = float2(tileSizeX - 2.0 * paddingOffsetX, tileSizeY - 2.0 * paddingOffsetY);

            // Apply tiling and offset within the specific atlas region
            float4 tilingOffset;
            switch (terrainType)
            {
                case TERRAIN_DEEP_WATER: tilingOffset = float4(_Tiling0.xy, _Offset0.xy); break;
                case TERRAIN_RIVER: tilingOffset = float4(_Tiling1.xy, _Offset1.xy); break;
                case TERRAIN_SHALLOW_WATER: tilingOffset = float4(_Tiling2.xy, _Offset2.xy); break;
                case TERRAIN_SAND: tilingOffset = float4(_Tiling3.xy, _Offset3.xy); break;
                case TERRAIN_GRASS: tilingOffset = float4(_Tiling4.xy, _Offset4.xy); break;
                case TERRAIN_FOREST: tilingOffset = float4(_Tiling5.xy, _Offset5.xy); break;
                case TERRAIN_ROCK: tilingOffset = float4(_Tiling6.xy, _Offset6.xy); break;
                case TERRAIN_SNOW: tilingOffset = float4(_Tiling7.xy, _Offset7.xy); break;
            }

            // Apply tiling and offset within the specific atlas region
            float2 tiledUV = frac(uv * tilingOffset.xy + tilingOffset.zw) * tileSize;

            // Combine the base atlas position with the tiled and offset UV
            return atlasUV + tiledUV;
        }

        float4 SampleTerrainTexture(int biomeType, int terrainType, float2 uv)
        {
            float2 atlasUV = GetAtlasUV(biomeType, terrainType, uv);
            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, atlasUV);
        }

        float3 SampleTerrainNormal(int biomeType, int terrainType, float2 uv)
        {
            float2 atlasUV = GetAtlasUV(biomeType, terrainType, uv);
            return UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, atlasUV));
        }

        float CalculateBlendFactor(float height, float lowerThreshold, float upperThreshold)
        {
            float blendRange = _BlendSpread * _BlendStrength;
            float lowerBlend = smoothstep(lowerThreshold - blendRange, lowerThreshold + blendRange, height);
            float upperBlend = 1 - smoothstep(upperThreshold - blendRange, upperThreshold + blendRange, height);
            return lowerBlend * upperBlend;
        }

        void BlendTerrainLayers(float height, int biomeType, float2 uv, out float4 albedo, out float3 normal)
        {
            float4 layerAlbedo[8];
            float3 layerNormal[8];
            float blendFactor[8];

            // Sample all layers
            for (int i = 0; i < 8; i++)
            {
                layerAlbedo[i] = SampleTerrainTexture(biomeType, i, uv);
                layerNormal[i] = SampleTerrainNormal(biomeType, i, uv);
            }

            // Calculate blend factors
            blendFactor[0] = CalculateBlendFactor(height, 0, _HeightThreshold0);
            blendFactor[1] = CalculateBlendFactor(height, _HeightThreshold0, _HeightThreshold1);
            blendFactor[2] = CalculateBlendFactor(height, _HeightThreshold1, _HeightThreshold2);
            blendFactor[3] = CalculateBlendFactor(height, _HeightThreshold2, _HeightThreshold3);
            blendFactor[4] = CalculateBlendFactor(height, _HeightThreshold3, _HeightThreshold4);
            blendFactor[5] = CalculateBlendFactor(height, _HeightThreshold4, _HeightThreshold5);
            blendFactor[6] = CalculateBlendFactor(height, _HeightThreshold5, _HeightThreshold6);
            blendFactor[7] = CalculateBlendFactor(height, _HeightThreshold6, 1);

            // Normalize blend factors
            float totalBlend = 0;
            for (int j = 0; j < 8; j++)
            {
                totalBlend += blendFactor[j];
            }
            for (int k = 0; k < 8; k++)
            {
                blendFactor[k] /= totalBlend;
            }

            // Blend layers
            albedo = 0;
            normal = 0;
            for (int l = 0; l < 8; l++)
            {
                albedo += layerAlbedo[l] * blendFactor[l];
                normal += layerNormal[l] * blendFactor[l];
            }
            normal = normalize(normal);
        }

        void InitializeSurfaceData(Varyings IN, out SurfaceData surfaceData)
        {
            surfaceData = (SurfaceData)0;

            float4 vertexColor = IN.color;
            
            // Extract biome and height information from vertex color
            int biomeType = floor(vertexColor.r * 10.0) + 1; // +1 to match the 1-10 range
            float height = vertexColor.g;

            float4 albedo;
            float3 normalTS;
            BlendTerrainLayers(height, biomeType, IN.uv, albedo, normalTS);

            surfaceData.albedo = albedo.rgb;
            surfaceData.alpha = albedo.a;
            surfaceData.normalTS = normalTS;
            surfaceData.smoothness = _Glossiness;
            surfaceData.metallic = _Metallic;
            surfaceData.occlusion = 1.0;
            surfaceData.emission = 0;
        }

        void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
        {
            inputData = (InputData)0;
            inputData.positionWS = input.positionWS;
            inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, cross(input.normalWS, input.tangentWS.xyz) * input.tangentWS.w, input.normalWS));
            inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
            inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
            inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.positionCS.z);
            inputData.vertexLighting = half3(0, 0, 0);
            inputData.bakedGI = SampleSH(inputData.normalWS);
            inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma target 4.5

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.uv = input.texcoord;
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                real sign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = float4(normalInput.tangentWS.xyz, sign);
                output.color = input.color;

                return output;
            }

            half4 LitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceData surfaceData;
                InitializeSurfaceData(input, surfaceData);

                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);

                half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            float3 _LightDirection;

            float4 GetShadowPositionHClip(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uv = input.texcoord;
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.uv = input.texcoord;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }

        // DepthNormals Pass
        Pass
        {
            Name "DepthNormals"
            Tags
            {
                "LightMode" = "DepthNormals"
            }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile_fog
            #pragma multi_compile _ DOTS_INSTANCING_ON

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.uv = input.texcoord;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            void DepthNormalsFragment(Varyings input, out half4 outNormalWS : SV_Target)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Convert normal to half precision to save bandwidth
                half3 normalWS = normalize(input.normalWS);
                float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);
                half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);
                outNormalWS = half4(packedNormalWS, 0.0);
            }
            ENDHLSL
        }
    }
}
