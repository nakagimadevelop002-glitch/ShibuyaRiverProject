Shader "Hidden/KriptoFX/KWS/Underwater"
{
    Properties
    {
        [HideInInspector]KWS_StencilMaskValue ("KWS_StencilMaskValue", Int) = 32
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
		
        ZWrite Off
        Cull Off

        Stencil
        {
            Ref [KWS_StencilMaskValue]
            ReadMask [KWS_StencilMaskValue]
            Comp Greater
            Pass keep
        }


        Pass
        {
            HLSLPROGRAM
            #pragma vertex vertUnderwater
            #pragma fragment fragUnderwater
            #pragma target 4.6
            
            #pragma multi_compile_fragment _ KWS_USE_VOLUMETRIC_LIGHT
            #pragma multi_compile_fragment _ USE_PHYSICAL_APPROXIMATION_COLOR USE_PHYSICAL_APPROXIMATION_SSR
            #pragma multi_compile_fragment _ KWS_USE_HALF_LINE_TENSION
            #pragma multi_compile_fragment _ KWS_CAMERA_UNDERWATER
			#pragma multi_compile_fragment _ KWS_USE_CAUSTIC
            
            #pragma multi_compile _ KWS_DYNAMIC_WAVES_VISIBLE_ZONES_1 KWS_DYNAMIC_WAVES_VISIBLE_ZONES_2 KWS_DYNAMIC_WAVES_VISIBLE_ZONES_4 KWS_DYNAMIC_WAVES_VISIBLE_ZONES_8
            #pragma multi_compile_fragment _ KWS_DYNAMIC_WAVES_USE_COLOR
            #pragma multi_compile_fragment _ KWS_USE_LOCAL_WATER_ZONES

            #include "../../Common/KWS_WaterHelpers.cginc"
			
            DECLARE_TEXTURE(_SourceRT);
            float4 KWS_Underwater_RTHandleScale;
            float4 _SourceRTHandleScale;
            float4 _SourceRT_TexelSize;


            float MaskToAlpha(float mask)
            {
                return saturate(mask * mask * mask * 20);
            }

            struct vertexInput
            {
                uint vertexID : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct vertexOutput
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };


            vertexOutput vertUnderwater(vertexInput v)
            {
                vertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = GetTriangleVertexPosition(v.vertexID);
                o.uv = GetTriangleUVScaled(v.vertexID);
                return o;
            }

            half4 fragUnderwater(vertexOutput i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 uv = i.uv;
				
                #if KWS_USE_HALF_LINE_TENSION
					float waterHalflineMask = GetWaterHalfLineTensionMask(uv - float2(0, _SourceRT_TexelSize.y * 5));
					float halfLineUvOffset = -waterHalflineMask * 0.25 + waterHalflineMask * waterHalflineMask * 0.25;
					uv.y -= halfLineUvOffset;
                #endif
				
                float sceneZ = GetSceneDepth(uv);
                float waterMask = GetWaterMask(uv, float2(0, 1));
                float2 volumeDepth = GetWaterVolumeDepth(uv, sceneZ, waterMask);
				

                //float3 blueNoise = KWS_BlueNoise3D.SampleLevel(sampler_linear_repeat, (uv * _ScreenParams.xy) / 128.0, 0);
                //return float4(blueNoise, 1);
                //return float4(GetVolumetricLight(uv).xyz, 1);

                bool surfaceMask = (abs(waterMask - 0.75) < 0.1) && (volumeDepth.y > sceneZ); //bool zero VGPR cost
					
				//return float4(surfaceMask.xxx , 1);

            	
                #ifdef USE_AQUARIUM_MODE
					float aquariumMask = GetWaterAquariumBackfaceMask(uv);
					float backfaceDepth = GetWaterBackfaceDepth(uv);
					if (aquariumMask > 0)
					{
						if (abs(waterMask - 0.25) < 0.1 || backfaceDepth < sceneZ) aquariumMask = 0;
						if (aquariumMask > 0.1) surfaceMask = true;
					}
                #endif
				
                float alpha = volumeDepth.x > 0 && waterMask > 0.5 ?   1 : 0;
                #ifdef USE_AQUARIUM_MODE
					alpha = aquariumMask > 0.1 ?   1 : alpha;
                #endif
                #if KWS_USE_HALF_LINE_TENSION
					alpha = saturate(alpha + waterHalflineMask * 10);
                #endif

                if (alpha == 0) discard;


				float localZoneOffsetBlending = 0;
				float localZoneMaxHeightOffset = 0;
            	
                float waterHeight = KWS_WaterPosition.y;
                float transparent = KWS_Transparent;
                float3 turbidityColor = KWS_TurbidityColor;
                float3 waterColor = KWS_WaterColor;

                transparent = clamp(transparent + KWS_UnderwaterTransparentOffset, 1, KWS_MAX_TRANSPARENT * 2);
            	
                float3 camPos = GetCameraAbsolutePosition();
                float noise = InterleavedGradientNoise(i.vertex.xy, KWS_Time) * 2 - 1;
            	float3 worldPos = GetWorldSpacePositionFromDepth(uv, volumeDepth.y); //todo add real water wave offset

                #if defined(KWS_USE_LOCAL_WATER_ZONES)

	                LocalZoneData blendedZone = (LocalZoneData)0;
	                blendedZone.transparent = transparent;
	                blendedZone.turbidityColor.xyz = turbidityColor.xyz;
	                blendedZone.waterColor.xyz = waterColor.xyz;

            		#if defined(KWS_USE_CAUSTIC)
            			EvaluateBlendedZoneDataWithHeight(blendedZone, camPos, normalize(worldPos - camPos), GetWorldToCameraDistance(worldPos), waterHeight + 20, localZoneMaxHeightOffset, localZoneOffsetBlending);
            		#else
            			EvaluateBlendedZoneData(blendedZone, camPos, normalize(worldPos - camPos), GetWorldToCameraDistance(worldPos), waterHeight + 20, noise);
            		#endif
	               
            		//return float4(saturate(blendedZone.transparent), 0, 0, 1);
	                transparent = blendedZone.transparent;
	                turbidityColor.xyz = blendedZone.turbidityColor.xyz;
	                waterColor.xyz = blendedZone.waterColor.xyz;

            		if (transparent < 0.001) discard;

                #endif



            	
               
                half3 normal = GetWaterNormals(i.uv.xy) * surfaceMask;
                float2 refractionUV = uv.xy + normal.xz;
                half3 refraction = GetSceneColor(refractionUV);
            	


            	#if defined(KWS_USE_CAUSTIC)

            		float waterLevel = KWS_WaterPosition.y;
            		float2 flowDirectionNormalized = 0;
            	
					#if defined(KWS_USE_DYNAMIC_WAVES)
            	
						uint zoneIndexOffset = 0;
						uint zoneIndexCount = 0;
            			float isOutDistance = GetWorldToCameraDistance(worldPos) > KW_WaterFarDistance * 0.5;
            			float velocityLength = 0;
            			float2 flowDirection = 0;
            	
						if (GetTileRange(worldPos, zoneIndexOffset, zoneIndexCount) && !isOutDistance)
						{
							for (uint zoneIndex = zoneIndexOffset; zoneIndex < zoneIndexCount; zoneIndex++)
							{
								ZoneData zone = (ZoneData)0;
								if (GetWaterZone(worldPos, zoneIndex, zone))
								{
									float4 dynamicWaves = GetDynamicWavesZoneBicubic(zone.id, zone.uv);
									float zoneFade = GetDynamicWavesBorderFading(zone.uv);
									waterLevel = max(waterLevel, KWS_WaterPosition.y + dynamicWaves.z * zoneFade + dynamicWaves.w);

									velocityLength += length(dynamicWaves.xy);
									flowDirection = (flowDirection + dynamicWaves.xy);
								}	
							}
						}

            			float scaled = velocityLength / (1.0 + velocityLength);
						flowDirectionNormalized = (velocityLength > 0.001) 		? normalize(flowDirection) * scaled 		: float2(0.0, 0.0);
					#endif

            		
					waterLevel = lerp(waterLevel, localZoneMaxHeightOffset + KWS_WaterPosition.y, localZoneOffsetBlending);
            		float waterDepth =  waterLevel - worldPos.y;	
					float caustic = 0;
            		if (surfaceMask < 0.5) caustic = GetCaustic(worldPos.xz, waterDepth, flowDirectionNormalized, KWS_DynamicWavesTimeScale * 1, 1);
            		
					refraction.rgb *= 1.0 + KWS_CausticStrength * caustic;
				
				#endif
            
			
				
                #if defined(USE_PHYSICAL_APPROXIMATION_COLOR) || defined(USE_PHYSICAL_APPROXIMATION_SSR)
					float3 worldViewDir = GetWorldSpaceViewDirNorm(worldPos);
					float distanceToSurface = saturate((worldPos.y - GetCameraAbsolutePosition().y) * 0.3);
					//float3 refractedRay = refract(-worldViewDir, normal, lerp(0.95, KWS_WATER_IOR, distanceToSurface));
					float3 refractedRay = refract(-worldViewDir, normal, KWS_WATER_IOR);

					float refractedMask = 1 - clamp(-refractedRay.y * 100, 0, 1);
					refractedMask *= surfaceMask;
					
					float3 reflection = KWS_TurbidityColor * 0.05;
                #endif

                #ifdef USE_PHYSICAL_APPROXIMATION_SSR
					float3 reflDir = reflect(-worldViewDir, normal);
					float2 refl_uv = GetScreenSpaceReflectionUV(reflDir, uv + normal.xz * 0.5);
					float4 ssrReflection = GetScreenSpaceReflection(refl_uv, worldPos);
					
					reflection = lerp(refraction.xyz, ssrReflection.xyz, ssrReflection.a);
					reflection = lerp(reflection, 0, distanceToSurface);
					reflection = lerp(reflection.xyz, ssrReflection.xyz, ssrReflection.a);

                #endif
				
                #if defined(USE_PHYSICAL_APPROXIMATION_COLOR) || defined(USE_PHYSICAL_APPROXIMATION_SSR)
					refraction = lerp(refraction, reflection, refractedMask);
                #endif
				
				
            	

            	

            	#if defined (KWS_DYNAMIC_WAVES_USE_COLOR)
	               
	                float3 pos = lerp(camPos, worldPos, (noise * 0.5 + 0.5) * 0.5);
	                uint zoneIndexOffset2 = 0;
	                uint zoneIndexCount2 = 0;
	                if (GetTileRange(pos, zoneIndexOffset2, zoneIndexCount2))
	                {
	                    for (uint zoneIndex = zoneIndexOffset2; zoneIndex < zoneIndexCount2; zoneIndex++)
	                    {
	                        ZoneData zone = (ZoneData)0;
	                        if (GetWaterZone(pos, zoneIndex, zone))
	                        {
	                            float4 colorData = GetDynamicWavesZoneColorData(zone.id, zone.uv);
	                            colorData.rgb = lerp(colorData.rgb, colorData.rgb * 0.35, saturate(colorData.a * colorData.a + colorData.a * 2));
	                            colorData.a *= 1 - saturate((KWS_WaterPosition.y - 1 - pos.y) / (DYNAMIC_WAVE_COLOR_MAX_TRANSPARENT * 2));

	                            turbidityColor = lerp(turbidityColor, colorData.rgb, colorData.a);
	                            waterColor = lerp(waterColor, colorData.rgb, saturate(colorData.a * 2));
	                            transparent = lerp(transparent, DYNAMIC_WAVE_COLOR_MAX_TRANSPARENT, colorData.a);

	                        }
	                    }
	                }

                #endif

				

                float3 volLight = GetVolumetricLightWithAbsorbtion(uv, uv, transparent, turbidityColor, waterColor, refraction, volumeDepth, GetExposure(), 0).xyz;
				

                #if KWS_USE_HALF_LINE_TENSION
					volLight = lerp(volLight, refraction * volLight * 2 * waterHalflineMask * waterHalflineMask + volLight * 0.5, waterHalflineMask);
                #endif


                return float4(volLight, alpha);
            }
            ENDHLSL
        }
    }
}