#ifndef KWS_WATER_FRAG_PASS
#define KWS_WATER_FRAG_PASS

half4 fragWater(v2fWater i) : SV_Target
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
	
	float2 screenUV = i.screenPos.xy / i.screenPos.w;
	float3 viewDir = GetWorldSpaceViewDirNorm(i.worldPosRefracted);
	float surfaceDepthZ = i.screenPos.z / i.screenPos.w;
	float surfaceDepthZEye = LinearEyeDepthUniversal(surfaceDepthZ);
	float sceneZ = GetSceneDepth(screenUV);
	float sceneZEye = LinearEyeDepthUniversal(sceneZ);
	float distanceToCamera = GetWorldToCameraDistance(i.worldPos);
	
	float3 sceneWorldPos =  GetWorldSpacePositionFromDepth(screenUV, sceneZ);
	float waterDepth = i.worldPosRefracted.y - sceneWorldPos.y;
	waterDepth = lerp(clamp(abs(surfaceDepthZEye - sceneZEye) * 0.1, 0, 10), waterDepth, 1-KWS_Pow10(1-viewDir.y));

	
	//return float4(saturate(waterDepth.xxx * 0.25), 1);
	
	half surfaceMask = max(i.surfaceMask.x >= 0.999, i.surfaceMask.x <= 0.0001);
	half exposure = GetExposure();
	half alpha = GetSurfaceToSceneFading(sceneZEye, surfaceDepthZEye, 2.0);
	
	float2 flowDirection = 0;
	float foamMask = 0;
	bool isDynamicWavesZone = false;
	
	float shorelineFade = 1;
	float riverMask = 0;

	float3 turbidityColor = KWS_TurbidityColor;
	float3 waterColor = KWS_WaterColor;
	float transparent = KWS_Transparent;
	float subsurfaceScatteringAdditionalMask = 0;
	float waterfallMask = 0;
	
	float4 colorData = float4(0, 0, 0, 0);

	float3 dynamicWavesNormal = float3(0, 1, 0);
	float borderFade = 0;
	float dynamicWavesNormalMask = 0;
	float flowSpeedMultiplier = 1;
	float velocityLength = 0;
	float dynamicWavesHeight = 0;


	
	//return float4(i.surfaceMask.x, 0, 0, 1);

	#if defined(KWS_USE_LOCAL_WATER_ZONES)
		
		
		UNITY_LOOP
		for (uint zoneIdx = 0; zoneIdx < KWS_WaterLocalZonesCount; zoneIdx++)
		{
			LocalZoneData zone = KWS_ZoneData_LocalZone[zoneIdx];
				
			// if (zone.cutoutMode > 0)
			// {
			// 	float3 distanceToBox = abs(i.worldPosRefracted.xyz - zone.center.xyz) / zone.halfSize.xyz;
			// 	float distanceToBorder = KWS_MAX(saturate(distanceToBox));
			// 	float cutoutFadeFactor = saturate(smoothstep(0, zone.cutoutFadeFactor * 0.5, 1 - distanceToBorder));
			// 	cutoutFadeFactor = 1 - KWS_Pow2(1 - cutoutFadeFactor);
			// 	alpha = lerp(1 - cutoutFadeFactor, cutoutFadeFactor, zone.cutoutMode == 2);
			// 	//return float4(distanceToBorder, 0, 0, 1);
			// }

			if (zone.overrideHeight > 0.5 && zone.clipWaterBelowZone)
			{
				float2 distanceToBox = abs(mul(i.worldPos.xz - zone.center.xz, zone.rotationMatrix)) / zone.halfSize.xz;
			
				float distanceToBorder = max(distanceToBox.x, distanceToBox.y);
				float zoneMinHeight = zone.center.y - zone.halfSize.y;

				if (distanceToBorder < 1.1 && i.worldPosRefracted.y < zoneMinHeight) discard;
			}
			
	        if (zone.overrideColorSettings > 0.5)
			{

				float tEntry=0;
			
				float3 sceneWorldPos = GetWorldSpacePositionFromDepth(screenUV, sceneZ);
				float3 surfaceOffset = float3(0, max(0, zone.center.y + zone.halfSize.y - i.worldPosRefracted.y) * 0.5f, 0);
					
	        	float density = 0;
	        	if (zone.useSphereBlending > 0.5)
	        	{
	        		density = KWS_SDF_SphereDensity(i.worldPosRefracted,  normalize(sceneWorldPos - i.worldPosRefracted), zone.center.xyz, zone.halfSize.x,  length(i.worldPosRefracted - sceneWorldPos), tEntry);
	        	}
	        	else
	        	{
	        		float2 boxSDF = KWS_SDF_IntersectionBox(i.worldPosRefracted, normalize(sceneWorldPos - i.worldPosRefracted), zone.rotationMatrix, zone.center, zone.halfSize);
	        		density = boxSDF.x < boxSDF.y && boxSDF.y > 0 && boxSDF.x <  length(i.worldPosRefracted - sceneWorldPos);
	        		tEntry = boxSDF.x;
	        	}
	        	
				if (density > 0)
				{
					density = saturate(density * 2);
					density = lerp(0, density, saturate(transparent / max(1, tEntry)));
		
					transparent = lerp(transparent, zone.transparent, density);
					turbidityColor = lerp(turbidityColor, zone.turbidityColor.xyz, density);
					waterColor = lerp(waterColor, zone.waterColor.xyz, density);
				}
			}
		}
		
			
		
	#endif

	if (alpha < 0.001) return 0;
	//	return float4(1, 0, 0, 1);
	float4 advectedUV = 0;
	int useAdvectedUV = 0;
	
	#if defined(KWS_USE_DYNAMIC_WAVES)
		
		
		uint zoneIndexOffset = 0;
		uint zoneIndexCount = 0;
		isDynamicWavesZone = GetTileRange(i.worldPos, zoneIndexOffset, zoneIndexCount);

		if (isDynamicWavesZone)
		{
			float maxIterrations = 0;
			for (uint zoneIndex = zoneIndexOffset; zoneIndex < zoneIndexCount; zoneIndex++)
			{
				
				ZoneData zone = (ZoneData)0;
				if (GetWaterZone(i.worldPos, zoneIndex, zone))
				{
					//return float4( frac(zoneIndex * 0.121345), 0, 0, 1);
					//return float4(zone.uv, 0, 1);

					float4 dynamicWaves = GetDynamicWavesZone(zone.id, zone.uv);

					float4 dynamicWavesAdditionalData = GetDynamicWavesZoneAdditionalDataBicubic(zone.id, zone.uv); //(wetmap, shoreline mask, foam mask, wetDepth)
					float3 zoneNormal = GetDynamicWavesZoneNormalsBicubic(zone.id, zone.uv).xyz;
					float zoneFade = GetDynamicWavesBorderFading(zone.uv);
					float shorelineMask = dynamicWavesAdditionalData.y;

					useAdvectedUV += zone.useAdvectedUV;
					if (useAdvectedUV > 0)
					{
						float4 zoneAdvectedUV = GetDynamicWavesZoneAdvectionUV(zone.id, zone.uv);
						zoneAdvectedUV *= float4(zone.halfSize.x / zone.halfSize.z, 1, zone.halfSize.x / zone.halfSize.z, 1);
						advectedUV = max(advectedUV, zoneAdvectedUV);
					}
					
						
					zoneNormal = lerp(float3(0, 1, 0), zoneNormal, zoneFade);
					flowSpeedMultiplier *= zone.flowSpeedMultiplier;
					float waterfallThreshold = GetDynamicWavesWaterfallTreshold(zoneNormal) * zoneFade;
					dynamicWaves.xy = lerp(dynamicWaves.xy, dynamicWaves.xy * 0.2, waterfallThreshold);
					
					flowDirection = (flowDirection + dynamicWaves.xy);
					shorelineFade *= shorelineMask;
					dynamicWavesNormal = KWS_BlendNormals(dynamicWavesNormal, zoneNormal);
					velocityLength += length(dynamicWaves.xy);
					dynamicWavesHeight += dynamicWaves.z;
					
					//zoneFade *= lerp(saturate(dynamicWaves.z * dynamicWaves.z * 10), 1, dynamicWavesAdditionalData.y);
					borderFade = saturate(borderFade + zoneFade);
					
					foamMask = max(foamMask, dynamicWavesAdditionalData.z * zoneFade);
					waterfallThreshold *= exp(-dynamicWaves.z * 0.35);
					float foamWaveThreshold = 1-saturate(lerp(0.5, saturate(waterfallThreshold * 5), shorelineMask));
					//foamMask *= lerp(0, 1, foamWaveThreshold * foamMask * foamMask);
					//transparent *= lerp(1, 0.25, waterfallThreshold);
					
					//subsurfaceScatteringAdditionalMask += saturate(waterfallThreshold * 0.5);
					waterfallMask = saturate(waterfallMask + waterfallThreshold);
					
					#ifdef KWS_DYNAMIC_WAVES_USE_COLOR
						float4 zoneColorData = GetDynamicWavesZoneColorData(zone.id, zone.uv);
						zoneColorData.rgb = lerp(zoneColorData.rgb, zoneColorData.rgb * 0.35, saturate(zoneColorData.a * zoneColorData.a + zoneColorData.a * 2));
						
						turbidityColor = lerp(turbidityColor, zoneColorData.rgb, zoneColorData.a);
						waterColor = lerp(waterColor, zoneColorData.rgb, saturate(zoneColorData.a * 2));
						transparent = lerp(transparent, DYNAMIC_WAVE_COLOR_MAX_TRANSPARENT, zoneColorData.a);

						colorData = max(colorData, zoneColorData);
					#endif

					
					
				}
			}

			velocityLength *= KWS_Pow2(borderFade);
			dynamicWavesNormalMask = saturate(dynamicWavesHeight * 0.5 * velocityLength) * KWS_Pow3(borderFade);
			flowDirection *= KWS_Pow2(borderFade);
		}
		
	#endif

	
	#if defined(KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE)
		float2 movableZoneUV = 0;
		if (GetWaterZoneMovable(i.worldPos, movableZoneUV))
		{
			isDynamicWavesZone = true;
			float4 dynamicWaves = GetDynamicWavesZoneMovable(movableZoneUV);
			float4 dynamicWavesAdditionalData = GetDynamicWavesZoneAdditionalDataMovable(movableZoneUV); //(wetmap, shoreline mask, foam mask, wetDepth)
			float3 zoneNormal = GetDynamicWavesZoneNormalsMovable(movableZoneUV).xyz;

			useAdvectedUV += KWS_MovableZoneUseAdvectedUV;
			if (useAdvectedUV > 0)
			{
				float4 zoneAdvectedUV = GetDynamicWavesZoneAdvectionMovable(movableZoneUV);
				float aspectRatio = KWS_DynamicWavesZoneSizeMovable.x / KWS_DynamicWavesZoneSizeMovable.z;
				zoneAdvectedUV *= float4(aspectRatio, 1, aspectRatio, 1);
				advectedUV = max(advectedUV, zoneAdvectedUV);
			
			}
			
			float zoneFade = GetDynamicWavesBorderFading(movableZoneUV);
			
			flowSpeedMultiplier *= KWS_MovableZoneFlowSpeedMultiplier;
			zoneNormal = lerp(float3(0, 1, 0), zoneNormal, zoneFade);
			//zoneFade *= lerp(saturate(dynamicWaves.z * dynamicWaves.z * 10), 1, dynamicWavesAdditionalData.y);
			flowDirection = (flowDirection + dynamicWaves.xy);
			foamMask = max(foamMask, dynamicWavesAdditionalData.z * zoneFade);
			dynamicWavesNormal = KWS_BlendNormals(dynamicWavesNormal, zoneNormal);
			borderFade = saturate(borderFade + zoneFade);
			dynamicWavesHeight += dynamicWaves.z;
			//dynamicWavesNormalMask = max(dynamicWavesNormalMask, saturate(dynamicWaves.z * 0.5 * length(dynamicWaves.xy)) * KWS_Pow3(borderFade));
			
			velocityLength *= KWS_Pow2(borderFade);
			dynamicWavesNormalMask = saturate(dynamicWavesHeight * 0.5 * velocityLength) * KWS_Pow3(borderFade);
			flowDirection *= KWS_Pow2(borderFade);
		}
	#endif


	// float3 dynamicWavesMapUV = GetDynamicWavesMapUV(i.worldPosRefracted, distanceToCamera);
	// if (!IsOutsideUvBorders(dynamicWavesMapUV))
	// {
	// 	float4 dynamicWaves = GetDynamicWavesMapBicubic(dynamicWavesMapUV);
	// 	float4 dynamicWavesAdditionalData = GetDynamicWavesAdditionalMapBicubic(dynamicWavesMapUV);
	// 	dynamicWavesNormal = GetDynamicWavesNormalMapBicubic(dynamicWavesMapUV);
	// 	
	// 	waterfallMask = GetDynamicWavesWaterfallTreshold(dynamicWavesNormal);
	// 	dynamicWaves.xy = lerp(dynamicWaves.xy, dynamicWaves.xy * 0.2, waterfallMask);
	// 	waterfallMask *= exp(-dynamicWaves.z * 0.35);
	// 	
	// 	foamMask = dynamicWavesAdditionalData.z;
	// 	flowDirection = dynamicWaves.xy;
	// 	shorelineFade *= dynamicWavesAdditionalData.y;
	// 	velocityLength = length(dynamicWaves.xy);
	// 	dynamicWavesHeight = dynamicWaves.z;
	// }
	
	
	float2 flowDirectionNormalized = NormalizeDynamicWavesVelocity(flowDirection);
	
	float noiseMask = GetNoiseMask(i.worldPos.xz, 1, 1);
	/////////////////////////////////////////////////////////////  NORMAL  ////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	float3 wavesNormalFoam = GetFftWavesNormalFoam(i.worldPos, i.windAttenuation);
	float3 tangentNormal = float3(wavesNormalFoam.x, 1, wavesNormalFoam.z);
	tangentNormal = lerp(float3(0, 1, 0), tangentNormal, any(fwidth(i.worldPos.xz))); //fix horizontal flickering
	
	float oceanFoam = wavesNormalFoam.y * noiseMask;
	
	#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE)
		if (isDynamicWavesZone && borderFade > 0.0)
		{
			riverMask = saturate(i.worldPosRefracted.y - KWS_WaterPosition.y - 4) * (1 - shorelineFade) * borderFade;
			float windAttenuationWithSmallZ = lerp(1, saturate(velocityLength) * (1 - shorelineFade), riverMask);

		
			float3 dynamicWavesFlowNormal = GetFftWavesNormalFoamWithFlowmap(i.worldPos, flowDirection * borderFade * 1, 4.0 * flowSpeedMultiplier, KWS_ScaledTime * KWS_DynamicWavesTimeScale);
			dynamicWavesFlowNormal.xz *= lerp(0.1, 1.0 * RIVER_FLOW_NORMAL_MULTIPLIER, windAttenuationWithSmallZ);
			dynamicWavesFlowNormal = lerp(dynamicWavesNormal, dynamicWavesFlowNormal, riverMask);
			
			tangentNormal = lerp(tangentNormal, dynamicWavesFlowNormal, saturate(dynamicWavesNormalMask * 0.7));
			tangentNormal = lerp(tangentNormal, dynamicWavesFlowNormal, riverMask);
		}
		
	#endif
	
	if (KWS_UseOceanFoam)
	{
		//return float4(wavesNormalFoam.yyy, 1);
		foamMask = max(foamMask, oceanFoam * surfaceMask * shorelineFade * KWS_Pow3(i.windAttenuation));
	}


	tangentNormal = lerp(float3(0, 1, 0), tangentNormal, surfaceMask * alpha);
	float3 worldNormal = KWS_BlendNormals(tangentNormal, i.worldNormal);
	

	//worldNormal = KWS_GetDerivativeNormal(i.worldPosRefracted, _ProjectionParams.x);
	/////////////////////////////////////////////////////////////  end normal  ////////////////////////////////////////////////////////////////////////////////////////////////////////
	//return float4(foamMask, 0, 0, 1);
	

	/////////////////////////////////////////////////////////////////////  REFRACTION  ///////////////////////////////////////////////////////////////////
	
	half3 refraction;
	
	float2 refractionUV;
	//todo surfaceMask > 0.5
	#ifdef KWS_USE_REFRACTION_IOR
		float3 refractionPos = float3(i.worldPos.x, i.worldPosRefracted.y, i.worldPos.z);
		float riverRefractionMultiplier = lerp(1, 3, saturate(dynamicWavesHeight));
		refractionUV = GetRefractedUV_IOR(viewDir, worldNormal, refractionPos, sceneZEye, surfaceDepthZEye, transparent, clamp(waterDepth * riverRefractionMultiplier, 0.1, KWS_MAX_TRANSPARENT));
	#else
		refractionUV = GetRefractedUV_Simple(screenUV, worldNormal);
	#endif
	refractionUV = lerp(screenUV, refractionUV, surfaceMask);
	refractionUV += waterfallMask * clamp(flowDirectionNormalized * 0.5, -0.25, 0.25) * lerp(1.0, RIVER_FLOW_FRESNEL_MULTIPLIER * 0.5, riverMask);
	

	float refractedSceneZ = GetSceneDepth(refractionUV);
	float refractedSceneZEye = LinearEyeDepthUniversal(refractedSceneZ);
	FixRefractionSurfaceLeaking(surfaceDepthZEye, sceneZ, sceneZEye, screenUV, refractedSceneZ, refractedSceneZEye, refractionUV);
	
	//todo surfaceMask > 0.5
	#ifdef KWS_USE_REFRACTION_DISPERSION
		refraction = GetSceneColorWithDispersion(refractionUV, KWS_RefractionDispersionStrength);
	#else
		refraction = GetSceneColor(refractionUV);
	#endif

	//refraction *= float3(0.85, 0.87, 0.9);
	//return float4(refraction, 1);
	/////////////////////////////////////////////////////////////  end refraction  ////////////////////////////////////////////////////////////////////////////////////////////////////////




	
	/////////////////////////////////////////////////////////////////////  CAUSTIC  ///////////////////////////////////////////

	float2 causticUV = GetWorldSpacePositionFromDepth(refractionUV, refractedSceneZ).xz;
	float causticNoiseMask = GetNoiseMask(causticUV, 2.5, 7);
	float caustic = GetCaustic(causticUV, waterDepth, flowDirectionNormalized, KWS_DynamicWavesTimeScale * flowSpeedMultiplier, saturate(noiseMask * causticNoiseMask + 0.1 + velocityLength * 0.25));

	
	float depthFadingDistance = saturate(waterDepth * 0.1);
	caustic *= lerp(saturate(1-saturate(distanceToCamera * 0.01) + 0.25), 1, saturate(depthFadingDistance));
	
	refraction.rgb *= 1.0 + KWS_CausticStrength * caustic; // caustic ~ 0.0–1.0

	/////////////////////////////////////////////////////////////  end caustic  ///////////////////////////////////////////////



	
	/////////////////////////////////////////////////////////////////////  UNDERWATER  ///////////////////////////////////////////////////////////////////
	
	#if defined(KWS_USE_LOCAL_WATER_ZONES)
		//transparent = GetWaterLocalZonesTransparent(screenUV);
		
		//if (transparent < 0.99f) return 0;
	#endif

	float2 volumeDepth = GetWaterVolumeDepth(screenUV, surfaceDepthZ, refractedSceneZ, 0);
	half4 volumeLight = GetVolumetricLightWithAbsorbtion(screenUV, lerp(refractionUV, screenUV, waterfallMask), transparent, turbidityColor, waterColor, refraction, volumeDepth, exposure, 0);
	if (surfaceMask < 0.5) return float4(volumeLight.xyz, 1);

	float depthAngleFix = (surfaceMask < 0.5 || KWS_MeshType == KWS_MESH_TYPE_CUSTOM_MESH) ?                          0.25 : saturate(GetWorldSpaceViewDirNorm(i.worldPos - float3(0, KWS_WindSpeed * 0.5, 0)).y);
	float fade = GetWaterRawFade(i.worldPos, surfaceDepthZEye, refractedSceneZEye, surfaceMask, depthAngleFix);
	half3 underwaterColor = volumeLight.xyz;
	
			
	float4 foamTex = 0;
	
	if(KWS_UseOceanFoam)
	{
		foamTex += KW_FluidsFoamTex.Sample(sampler_trilinear_repeat, i.worldPos.xz * 0.05 * KWS_FoamTextureScaleMultiplier).xyzw * (1-borderFade);
		//foamTex += Texture2DSampleBicubic(KW_FluidsFoamTex, sampler_trilinear_repeat, i.worldPos.xz * 0.015, KW_FluidsFoamTex_TexelSize);
	}

	if (isDynamicWavesZone || KWS_UseOceanFoam || KWS_UseIntersectionFoam)
	{
		float4 foamChannelMask = saturate(1.0 - abs(KWS_FoamTextureType - float4(0, 1, 2, 3)));
		float advectedFoam = 0;
		float flowmapFoam = 0;
		
		if (useAdvectedUV > 0)
		{
			float4 advectedFoam0 = KW_FluidsFoamTex.Sample(sampler_trilinear_repeat, advectedUV.xy * 20 * KWS_FoamTextureScaleMultiplier);
			float4 advectedFoam1 = KW_FluidsFoamTex.Sample(sampler_trilinear_repeat,  advectedUV.zw * 20 * KWS_FoamTextureScaleMultiplier);

			float selectedFoamChannel0 = dot(advectedFoam0, foamChannelMask);
			float selectedFoamChannel1 = dot(advectedFoam1, foamChannelMask);

			float weight0 = sin(((KWS_Time / 4.0) % 1) * 3.1415);
			float weight1 = sin((((KWS_Time + 2.0) /4.0) % 1) * 3.1415);

			weight0 = pow(weight0, 0.5);
			weight1 = pow(weight1, 0.5);

			advectedFoam = max(selectedFoamChannel0 * weight0, selectedFoamChannel1 * weight1) * borderFade;
			advectedFoam = saturate(saturate(pow(advectedFoam, KWS_FoamTextureContrast) - (1 - sqrt(foamMask))));
		
		}
		else
		{
			//flowDirection = clamp(flowDirection * 0.25, -1, 1);
		
			
			foamTex += Texture2DSampleFlowmapJump(KW_FluidsFoamTex, sampler_linear_repeat, i.worldPos.xz * 0.05, flowDirectionNormalized * 0.5, KWS_ScaledTime * 1.0 * KWS_DynamicWavesTimeScale * flowSpeedMultiplier, KWS_FoamTextureScaleMultiplier).xyzw * borderFade;
			float selectedFoamChannel = dot(foamTex, foamChannelMask);
			flowmapFoam = saturate(saturate(pow(selectedFoamChannel, KWS_FoamTextureContrast) - (1 - sqrt(foamMask))));
		}
	
		float3 surfaceColor = GetVolumetricSurfaceLight(screenUV);
		float3 foamColor = 0;
		
		foamColor += advectedFoam;
		foamColor += flowmapFoam;
		foamColor += foamMask * 0.4 * KWS_TurbidityColor;
		foamColor = saturate(foamColor) * KWS_Pow3(alpha);
		
		foamColor *= saturate(surfaceColor);
		foamColor *= lerp(1, colorData.rgb, saturate(colorData.a * 1.35));
		
		underwaterColor = lerp(underwaterColor + foamColor, foamColor, colorData.a);
	}
	//return float4(volumeLight.xyz, 1);
	
	float3 subsurfaceScatteringColor = ComputeSSS(screenUV, GetWaterSSS(screenUV), underwaterColor, volumeLight.a, transparent);
	underwaterColor += subsurfaceScatteringColor * 10;


	/////////////////////////////////////////////////////////////  end underwater  ////////////////////////////////////////////////////////////////////////////////////////////////////////


	/////////////////////////////////////////////////////////////  REFLECTION  ////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	float3 reflDir = reflect(-viewDir, worldNormal);
	reflDir.y *= sign(dot(reflDir, float3(0, 1, 0)));

	float3 reflection = 0;

	#if defined(KWS_SSR_REFLECTION) || defined(KWS_USE_PLANAR_REFLECTION)
		float2 refl_uv = GetScreenSpaceReflectionUV(reflDir, screenUV + tangentNormal.xz * 0.5);
	#endif

	
	#ifdef KWS_USE_PLANAR_REFLECTION
		reflection = GetPlanarReflectionWithClipOffset(refl_uv) * exposure;
	#else

		#ifdef KWS_USE_REFLECTION_PROBES
			reflection = KWS_GetReflectionProbeEnv(screenUV, surfaceDepthZEye, i.worldPosRefracted, reflDir, KWS_SkyLodRelativeToWind, exposure);
		#else
			reflection = KWS_GetSkyColor(reflDir, KWS_SkyLodRelativeToWind, exposure);
		#endif

	#endif

	#ifdef KWS_SSR_REFLECTION
		float4 ssrReflection = GetScreenSpaceReflectionWithStretchingMask(refl_uv, i.worldPosRefracted);

		float inverseReflectionFix = 1 - saturate(dot(reflDir, float3(0, 1, 0)));
		inverseReflectionFix = lerp(1, KWS_Pow5(inverseReflectionFix), saturate(KWS_WindSpeed * 0.25));
		ssrReflection.a = lerp(0, ssrReflection.a, inverseReflectionFix);
	
		reflection = lerp(reflection, ssrReflection.rgb, ssrReflection.a);
	#endif
	
	
	reflection *= surfaceMask * (1 - waterfallMask);
	//reflection = ApplyShorelineWavesReflectionFix(reflDir, reflection, underwaterColor);
	
	/////////////////////////////////////////////////////////////  end reflection  ////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	
	half waterFresnel = ComputeWaterFresnel(worldNormal, viewDir);
	waterFresnel *= lerp(1.0, RIVER_FLOW_FRESNEL_MULTIPLIER, riverMask);
	waterFresnel *= surfaceMask * (1 - waterfallMask);
	
	half3 finalColor = lerp(underwaterColor, reflection, waterFresnel);
	
	#ifdef KWS_REFLECT_SUN
		float3 sunReflection = ComputeSunlight(worldNormal, viewDir, GetMainLightDir(), GetMainLightColor(exposure), volumeLight.a, surfaceDepthZEye, _ProjectionParams.z, transparent);
		float sunFadeFactor = saturate(abs(sceneZEye - surfaceDepthZEye));
		sunFadeFactor = KWS_Pow5(sunFadeFactor) * saturate(1 - waterfallMask * 2);
		finalColor += sunReflection * (1 - foamMask) * sunFadeFactor;
	#endif
	
	half3 fogColor;
	half3 fogOpacity;
	
	GetInternalFogVariables(i.pos, viewDir, surfaceDepthZ, surfaceDepthZEye, i.worldPosRefracted, fogColor, fogOpacity);
	fogOpacity = saturate(fogOpacity * 1.5);
	finalColor = ComputeInternalFog(finalColor, fogColor, fogOpacity);
	finalColor = ComputeThirdPartyFog(finalColor, i.worldPos, screenUV, i.screenPos.z);


	finalColor += srpBatcherFix;

	
	//return float4(alpha, 0, 0, 1);

	return float4(finalColor, alpha);
}


struct FragmentOutput
{
	half4 pass1 : SV_Target0;
	half2 pass2 : SV_Target1;
};

FragmentOutput  fragDepth(v2fDepth i, float facing : VFACE)
{
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
	FragmentOutput o = (FragmentOutput)0;

	float facingColor = 0.75 - facing * 0.25;
	float2 screenUV = i.screenPos.xy / i.screenPos.w;
	float z = i.screenPos.z / i.screenPos.w;
	float sceneDepth = GetSceneDepth(screenUV);
	half surfaceMask = max(i.surfaceMask.x >= 0.999, i.surfaceMask.x <= 0.0001);
	
	#ifdef KWS_PRE_PASS_BACK_FACE
		float mask = i.surfaceMask.x < 0.9999 ?                   0.1 : 1;
	#else
		float mask = facing > 0 ?                   0.25 : 0.75;
		if (surfaceMask < 0.5)
		{
			mask = facing > 0.0 ?                   0.1 : 1;
		}
	#endif


	float2 flowDirection = 0;
	bool isDynamicWavesZone = false;
	
	float3 dynamicWavesNormal = float3(0, 1, 0);
	float dynamicWavesNormalMask = 0;
	float borderFade = 0;
	float flowSpeedMultiplier = 1;
	float velocityLength = 0;
	float shorelineFade = 1;
	float riverMask = 0;
	
	#if defined(KWS_USE_DYNAMIC_WAVES)
		
		float dynamicWavesHeight = 0;
		uint zoneIndexOffset = 0;
		uint zoneIndexCount = 0;
		isDynamicWavesZone = GetTileRange(i.worldPos, zoneIndexOffset, zoneIndexCount);
		
		if (isDynamicWavesZone)
		{
			for (uint zoneIndex = zoneIndexOffset; zoneIndex < zoneIndexCount; zoneIndex++)
			{
				ZoneData zone = (ZoneData)0;
				if (GetWaterZone(i.worldPos, zoneIndex, zone))
				{
					float4 dynamicWaves = GetDynamicWavesZone(zone.id, zone.uv);
					float4 dynamicWavesAdditionalData = GetDynamicWavesZoneAdditionalData(zone.id, zone.uv); //(wetmap, shoreline mask, foam mask, wetDepth)
					float3 zoneNormal = GetDynamicWavesZoneNormals(zone.id, zone.uv).xyz;
					float zoneFade = GetDynamicWavesBorderFading(zone.uv);
					float shorelineMask = dynamicWavesAdditionalData.y;
					
					flowSpeedMultiplier *= zone.flowSpeedMultiplier;
					flowDirection = (flowDirection + dynamicWaves.xy);
					dynamicWavesNormal = KWS_BlendNormals(dynamicWavesNormal, zoneNormal);
					dynamicWavesHeight += dynamicWaves.z;
					velocityLength += length(dynamicWaves.xy);

					shorelineFade *= shorelineMask;
					zoneFade *= lerp(saturate(dynamicWaves.z * dynamicWaves.z * 10), 1, dynamicWavesAdditionalData.y);
					borderFade = saturate(borderFade + zoneFade);
				}
			}

			dynamicWavesNormalMask = saturate(dynamicWavesHeight * 0.5 * velocityLength) * KWS_Pow3(borderFade);
		}

	#endif

	#if defined(KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE)
		float2 movableZoneUV = 0;
		if (GetWaterZoneMovable(i.worldPos, movableZoneUV))
		{
			isDynamicWavesZone = true;
			float4 dynamicWaves = GetDynamicWavesZoneMovable(movableZoneUV);
			float4 dynamicWavesAdditionalData = GetDynamicWavesZoneAdditionalDataMovable(movableZoneUV); //(wetmap, shoreline mask, foam mask, wetDepth)
			float3 zoneNormal = GetDynamicWavesZoneNormalsMovable(movableZoneUV).xyz;
			
			flowSpeedMultiplier *= KWS_MovableZoneFlowSpeedMultiplier;
			float zoneFade = GetDynamicWavesBorderFading(movableZoneUV);
			zoneFade *= lerp(saturate(dynamicWaves.z * dynamicWaves.z * 10), 1, dynamicWavesAdditionalData.y);
			flowDirection = (flowDirection + dynamicWaves.xy);
			dynamicWavesNormal = KWS_BlendNormals(dynamicWavesNormal, zoneNormal);
			borderFade = saturate(borderFade + zoneFade);
			velocityLength += length(dynamicWaves.xy);

			dynamicWavesNormalMask = max(dynamicWavesNormalMask, saturate(dynamicWaves.z * 0.5 * length(dynamicWaves.xy)) * KWS_Pow3(borderFade));
		}
	#endif

	//float2 flowDirectionNormalized = NormalizeDynamicWavesVelocity(flowDirection);

	/////////////////////////////////////////////////////////////  NORMAL  ////////////////////////////////////////////////////////////////////////////////////////////////////////
	
	float3 wavesNormalFoam = GetFftWavesNormalFoam(i.worldPos, i.windAttenuation);
	float3 tangentNormal = float3(wavesNormalFoam.x, 1, wavesNormalFoam.z);
	float3 tangentNormalScatter = GetFftWavesNormalLod(i.worldPos, KWS_WATER_SSR_NORMAL_LOD);
	

	#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE)
		if (isDynamicWavesZone && borderFade > 0.0)
		{
			riverMask = saturate(i.worldPosRefracted.y - KWS_WaterPosition.y - 4) * (1 - shorelineFade) * borderFade;
			float windAttenuationWithSmallZ = lerp(1, saturate(velocityLength) * (1 - shorelineFade), riverMask);

			
			float3 dynamicWavesFlowNormal = GetFftWavesNormalFoamWithFlowmap(i.worldPos, flowDirection * borderFade * 1, 4.0 * flowSpeedMultiplier, KWS_ScaledTime * KWS_DynamicWavesTimeScale);
			dynamicWavesFlowNormal.xz *= lerp(0.1, 1.0 * RIVER_FLOW_NORMAL_MULTIPLIER, windAttenuationWithSmallZ);
			dynamicWavesFlowNormal = lerp(dynamicWavesNormal, dynamicWavesFlowNormal, riverMask);
				
			tangentNormal = lerp(tangentNormal, dynamicWavesFlowNormal, saturate(dynamicWavesNormalMask * 0.7));
			tangentNormal = lerp(tangentNormal, dynamicWavesFlowNormal, riverMask);

		}
		
		
	#endif

	//half surfaceMask = max(i.surfaceMask.x >= 0.999, i.surfaceMask.x <= 0.0001);
	tangentNormal = lerp(float3(0, 1, 0), tangentNormal, surfaceMask);
	float3 worldNormal = KWS_BlendNormals(tangentNormal, i.worldNormal);

	#if defined(KWS_USE_DYNAMIC_WAVES) || defined(KWS_DYNAMIC_WAVES_USE_MOVABLE_ZONE)
		if (isDynamicWavesZone)
		{
			float underwaterRefractionFix = 1 - saturate(1.25 * saturate(1 - dot(dynamicWavesNormal, float3(0, 1, 0))));
			worldNormal *= underwaterRefractionFix;
		}
		
	#endif


	float transparent = KWS_Transparent;

	#if defined(KWS_USE_LOCAL_WATER_ZONES)
		
		UNITY_LOOP
		for (uint zoneIdx = 0; zoneIdx < KWS_WaterLocalZonesCount; zoneIdx++)
		{
			LocalZoneData zone = KWS_ZoneData_LocalZone[zoneIdx];
			
			float2 distanceToBox = abs(mul(i.worldPos.xz - zone.center.xz, zone.rotationMatrix)) / zone.halfSize.xz;
			float distanceToBorder = max(distanceToBox.x, distanceToBox.y);
			float zoneMinHeight = zone.center.y - zone.halfSize.y;
	
			if (distanceToBorder < 1.1 && i.worldPosRefracted.y < zoneMinHeight && GetCameraAbsolutePosition().y > i.worldPos.y) discard;
		}
			
	#endif

	float3 viewDir = GetWorldSpaceViewDirNorm(i.worldPosRefracted);
	float3 lightDir = GetMainLightDir();
	float distanceToCamera = GetWorldToCameraDistance(i.worldPos);
	
	float dotVal = dot(lightDir, float3(0, 1, 0));
	float sunAngleAttenuation = smoothstep(-0.1, 1.0, dotVal);

	float3 refractedVector = normalize(refract(-viewDir, tangentNormalScatter, 0.66));
	float scattering = pow(saturate(dot(refractedVector, lightDir)), 16);
	float sss = saturate(scattering * sunAngleAttenuation * 5000);
	
	
	half tensionMask = 0;
	#ifdef KWS_USE_HALF_LINE_TENSION
		tensionMask = abs(i.localHeightAndTensionMask.y) * KWS_InstancingWaterScale.y * lerp(40, 10, KWS_UnderwaterHalfLineTensionScale);
		if (tensionMask >= 0.99) tensionMask = ((1.2 - tensionMask) * 5);
		if (i.surfaceMask.x > 0.9999 || facing < 0 || z <= sceneDepth) tensionMask = 0;
		tensionMask *= 1 - saturate(distanceToCamera * 0.1);
	#endif

	o.pass1 = half4(transparent / 100.0, mask, sss, tensionMask);
	o.pass2 = worldNormal.xz;

	return o;
}
#endif