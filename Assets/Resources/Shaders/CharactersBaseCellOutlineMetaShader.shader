// Made with Amplify Shader Editor v1.9.9.10
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Eclipse/Characters/CharactersBaseCellOutlineMetaShader"
{
	Properties
	{
		[Toggle( _SUBSERFON_ON )] _SubserfON( "SubserfON", Float ) = 0
		[Toggle( _USEEMISSIONNOISE_ON )] _UseEmissionNoise( "UseEmissionNoise", Float ) = 0
		[Toggle( _USECUSTOMLIGHTS_ON )] _UseCustomLights( "UseCustomLights", Float ) = 0
		[Toggle( _USEDYNAMICS_ON )] _UseDynamics( "UseDynamics", Float ) = 0
		[Toggle( _USEWIND_ON )] _UseWind( "UseWind", Float ) = 1
		_SpecGloss( "SpecGloss", 2D ) = "black" {}
		_MainTex( "MainTex", 2D ) = "white" {}
		_MainTexTint( "MainTexTint", Color ) = ( 1, 1, 1, 0 )
		_MainTexMul( "MainTexMul", Float ) = 1
		_MainTexAdd( "MainTexAdd", Float ) = 0
		_Normal( "Normal", 2D ) = "bump" {}
		_EmissiveColor( "EmissiveColor", Color ) = ( 0.5019608, 0.5019608, 0.5019608, 1 )
		_EmissionPower( "EmissionPower", Range( 0, 5 ) ) = 0
		_Emission( "Emission", 2D ) = "black" {}
		_SkinMask( "SkinMask", 2D ) = "black" {}
		_RandomSizeValue( "RandomSizeValue", Float ) = 0
		_ShadowOffsetMul( "ShadowOffsetMul", Float ) = 1
		_ShadowTransparencyAdd( "ShadowTransparencyAdd", Float ) = 0.5
		_ShadowsColor( "ShadowsColor", Color ) = ( 0, 0, 0, 0 )
		_SmoothsepMin( "SmoothsepMin", Float ) = 0.1
		_SmoothsepMax( "SmoothsepMax", Float ) = 0.16
		_DepthCompensation( "DepthCompensation", Float ) = 0.02
		_OutlineWidth( "OutlineWidth", Float ) = 0.2
		_OutlineModAdd( "OutlineModAdd", Float ) = -1
		_OutlineModMul( "OutlineModMul", Float ) = -1
		_OutlineAdd( "OutlineAdd", Float ) = 0
		_OutlineMul( "OutlineMul", Float ) = 1
		_Color1( "Color1", Color ) = ( 0, 0, 0, 0 )
		_Color2( "Color2", Color ) = ( 0, 0, 0, 0 )
		_SizeRandomMul( "SizeRandomMul", Float ) = 0
		_SizeRandomAdd( "SizeRandomAdd", Float ) = 0
		_LightDirection( "LightDirection", Vector ) = ( -0.12, 0.5, -1, 0 )
		_Rim1Direction( "Rim1Direction", Vector ) = ( 1, -0.3, -0.5, 0 )
		_Rim1Color( "Rim1Color", Color ) = ( 1, 1, 1, 0 )
		_Rim1StepMin( "Rim1StepMin", Range( 0, 1 ) ) = 0.3
		_Rim1StepMax( "Rim1StepMax", Range( 0, 1 ) ) = 0.4
		_SpecAdd( "SpecAdd", Float ) = 0
		_SkinSubserfColor( "SkinSubserfColor", Color ) = ( 1, 0.3176471, 0.1764706, 0 )
		_SubserfDir( "SubserfDir", Vector ) = ( 0.28, 0.4, -0.21, 0 )
		_SpecColorMul( "SpecColorMul", Color ) = ( 1, 1, 1, 0 )
		_WindMoveTurbulence( "WindMoveTurbulence", Range( 0, 1 ) ) = 0.35
		_AmbientColor( "AmbientColor", Color ) = ( 1, 1, 1, 0 )
		_EnvIntensity( "EnvIntensity", Float ) = 0.5
		_EmissionNoiseTex( "EmissionNoiseTex", 2D ) = "white" {}
		_EmissionNoisePanner( "EmissionNoisePanner", Vector ) = ( 0, 0, 0, 0 )
		_EmissionNoiseMul( "EmissionNoiseMul", Float ) = 1
		_EmissionNoiseAdd( "EmissionNoiseAdd", Float ) = 0
		_PannerJitterSteps( "PannerJitterSteps", Float ) = 3
		_PannerJitterAmmount( "PannerJitterAmmount", Float ) = 0
		_PannerDuration( "PannerDuration", Float ) = 1
		_SkinMulPow( "SkinMulPow", Vector ) = ( 1, 0.32, 0, 0 )
		_Fresnel( "Fresnel (Mul Add)", Vector ) = ( 0.3, 1, 0, 0 )
		_Twist( "Twist", Float ) = 0
		_Offset( "Offset", Vector ) = ( 0, 0, 0, 0 )
		_TwistPivotOffset( "TwistPivotOffset", Vector ) = ( 0, 0, 0, 0 )
		_TwistVector( "TwistVector", Vector ) = ( 0, 1, 0, 0 )
		_Noise1Mul( "Noise1Mul", Vector ) = ( 0.4, 0.4, 0.4, 0 )
		_Noise1Magnitude( "Noise1Magnitude", Vector ) = ( 0.005, 0.001, 0.005, 0 )
		_Noise1SecondaryMul( "Noise1SecondaryMul", Vector ) = ( 5, 5, 5, 0 )
		_Noise1TimeScale( "Noise1TimeScale", Float ) = 1


		//_TessPhongStrength( "Tess Phong Strength", Range( 0, 1 ) ) = 0.5
		//_TessValue( "Tess Max Tessellation", Range( 1, 32 ) ) = 16
		//_TessMin( "Tess Min Distance", Float ) = 10
		//_TessMax( "Tess Max Distance", Float ) = 25
		//_TessEdgeLength ( "Tess Edge length", Range( 2, 50 ) ) = 16
		//_TessMaxDisp( "Tess Max Displacement", Float ) = 25

		[HideInInspector] _QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector] _QueueControl("_QueueControl", Float) = -1

        [HideInInspector][NoScaleOffset] unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset] unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}

		//[HideInInspector][ToggleUI] _ReceiveShadows("Receive Shadows", Float) = 1.0

		[HideInInspector][ToggleUI] _AddPrecomputedVelocity("Add Precomputed Velocity", Float) = 1
		[HideInInspector] _XRMotionVectorsPass("_XRMotionVectorsPass", Float) = 1

		//[HideInInspector] _AlphaClip("__clip", Float) = 0.0
	}

	SubShader
	{
		PackageRequirements
		{
			"com.unity.render-pipelines.universal": "[17.0,18.0]"
		}

		

		

		Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" "UniversalMaterialType"="Unlit" }

	LOD 0

		ZWrite On
		Cull Back
		AlphaToMask Off
		ColorMask RGBA
		Blend One Zero, One Zero
		BlendOp Add, Add

		

		HLSLINCLUDE
		#pragma target 4.5
		#pragma prefer_hlslcc gles
		#pragma only_renderers d3d11 glcore gles gles3 metal vulkan // ensure rendering platforms toggle list is visible

		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

		#define ASE_ADJUST_CLIP_POSITION( x ) x

		#ifndef ASE_TESS_FUNCS
		#define ASE_TESS_FUNCS
		float4 FixedTess( float tessValue )
		{
			return tessValue;
		}

		float CalcDistanceTessFactor (float4 vertex, float minDist, float maxDist, float tess, float4x4 o2w, float3 cameraPos )
		{
			float3 wpos = mul(o2w,vertex).xyz;
			float dist = distance (wpos, cameraPos);
			float f = clamp(1.0 - (dist - minDist) / (maxDist - minDist), 0.01, 1.0) * tess;
			return f;
		}

		float4 CalcTriEdgeTessFactors (float3 triVertexFactors)
		{
			float4 tess;
			tess.x = 0.5 * (triVertexFactors.y + triVertexFactors.z);
			tess.y = 0.5 * (triVertexFactors.x + triVertexFactors.z);
			tess.z = 0.5 * (triVertexFactors.x + triVertexFactors.y);
			tess.w = (triVertexFactors.x + triVertexFactors.y + triVertexFactors.z) / 3.0f;
			return tess;
		}

		float CalcEdgeTessFactor (float3 wpos0, float3 wpos1, float edgeLen, float3 cameraPos, float4 scParams )
		{
			float dist = distance (0.5 * (wpos0+wpos1), cameraPos);
			float len = distance(wpos0, wpos1);
			float f = max(len * scParams.y / (edgeLen * dist), 1.0);
			return f;
		}

		float DistanceFromPlane (float3 pos, float4 plane)
		{
			float d = dot (float4(pos,1.0f), plane);
			return d;
		}

		bool WorldViewFrustumCull (float3 wpos0, float3 wpos1, float3 wpos2, float cullEps, float4 planes[6] )
		{
			float4 planeTest;
			planeTest.x = (( DistanceFromPlane(wpos0, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[0]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[0]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.y = (( DistanceFromPlane(wpos0, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[1]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[1]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.z = (( DistanceFromPlane(wpos0, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[2]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[2]) > -cullEps) ? 1.0f : 0.0f );
			planeTest.w = (( DistanceFromPlane(wpos0, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos1, planes[3]) > -cullEps) ? 1.0f : 0.0f ) +
							(( DistanceFromPlane(wpos2, planes[3]) > -cullEps) ? 1.0f : 0.0f );
			return !all (planeTest);
		}

		float4 DistanceBasedTess( float4 v0, float4 v1, float4 v2, float tess, float minDist, float maxDist, float4x4 o2w, float3 cameraPos )
		{
			float3 f;
			f.x = CalcDistanceTessFactor (v0,minDist,maxDist,tess,o2w,cameraPos);
			f.y = CalcDistanceTessFactor (v1,minDist,maxDist,tess,o2w,cameraPos);
			f.z = CalcDistanceTessFactor (v2,minDist,maxDist,tess,o2w,cameraPos);

			return CalcTriEdgeTessFactors (f);
		}

		float4 EdgeLengthBasedTess( float4 v0, float4 v1, float4 v2, float edgeLength, float4x4 o2w, float3 cameraPos, float4 scParams )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;
			tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
			tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
			tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
			tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			return tess;
		}

		float4 EdgeLengthBasedTessCull( float4 v0, float4 v1, float4 v2, float edgeLength, float maxDisplacement, float4x4 o2w, float3 cameraPos, float4 scParams, float4 planes[6] )
		{
			float3 pos0 = mul(o2w,v0).xyz;
			float3 pos1 = mul(o2w,v1).xyz;
			float3 pos2 = mul(o2w,v2).xyz;
			float4 tess;

			if (WorldViewFrustumCull(pos0, pos1, pos2, maxDisplacement, planes))
			{
				tess = 0.0f;
			}
			else
			{
				tess.x = CalcEdgeTessFactor (pos1, pos2, edgeLength, cameraPos, scParams);
				tess.y = CalcEdgeTessFactor (pos2, pos0, edgeLength, cameraPos, scParams);
				tess.z = CalcEdgeTessFactor (pos0, pos1, edgeLength, cameraPos, scParams);
				tess.w = (tess.x + tess.y + tess.z) / 3.0f;
			}
			return tess;
		}
		#endif //ASE_TESS_FUNCS
		ENDHLSL

		
		Pass
		{
			Name "Outline"
			Tags { "LightMode"="CustomOutlineMode" }

			Cull Front
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA

			Stencil
			{
				Ref 1
				Comp Always
				Pass Replace
			}

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#define _RECEIVE_SHADOWS_OFF
			#define ASE_VERSION 19910
			#define ASE_SRP_VERSION 170300


			#pragma vertex vert
			#pragma fragment frag

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_VERT_TANGENT
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES2
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_VERT_POSITION
			#pragma shader_feature_local _USEDYNAMICS_ON
			#pragma shader_feature_local _USEWIND_ON


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				half4 ase_color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 positionWSAndFogFactor : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				half4 tangentWS : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			half4 _AmbientColor;
			half4 _EmissionNoiseTex_ST;
			half4 _Emission_ST;
			float4 _EmissiveColor;
			half4 _SpecColorMul;
			half4 _SpecGloss_ST;
			half4 _Normal_ST;
			half4 _Rim1Color;
			half4 _MainTex_ST;
			half4 _Color2;
			half4 _Color1;
			half4 _MainTexTint;
			half4 _SkinMask_ST;
			half4 _SkinSubserfColor;
			half4 _ShadowsColor;
			half4 _Offset;
			half3 _SubserfDir;
			half3 _Noise1SecondaryMul;
			half3 _Noise1Magnitude;
			half3 _LightDirection;
			half3 _TwistVector;
			half3 _TwistPivotOffset;
			half3 _Rim1Direction;
			half3 _Noise1Mul;
			half2 _SkinMulPow;
			half2 _EmissionNoisePanner;
			half2 _Fresnel;
			half _EmissionNoiseMul;
			half _ShadowOffsetMul;
			half _ShadowTransparencyAdd;
			half _MainTexAdd;
			half _MainTexMul;
			half _SmoothsepMin;
			half _SmoothsepMax;
			half _EmissionNoiseAdd;
			half _PannerJitterAmmount;
			half _SpecAdd;
			half _PannerDuration;
			half _OutlineModAdd;
			half _OutlineModMul;
			half _RandomSizeValue;
			half _SizeRandomMul;
			half _SizeRandomAdd;
			half _Twist;
			half _Noise1TimeScale;
			half _WindMoveTurbulence;
			half _DepthCompensation;
			half _OutlineAdd;
			half _OutlineMul;
			half _Rim1StepMin;
			half _Rim1StepMax;
			half _EnvIntensity;
			float _EmissionPower;
			half _PannerJitterSteps;
			half _OutlineWidth;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			half4 MetaFadeColor;
			half MetaFadeValue;


			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				half3 temp_output_1767_0 = (input.tangentOS).xyz;
				half3 appendResult1751 = (half3(input.ase_texcoord1.x , input.ase_texcoord2.x , input.ase_texcoord1.y));
				half3 break1756 = appendResult1751;
				half3 normalizeResult1762 = normalize( ( ( temp_output_1767_0 * break1756.x ) + ( ( cross( input.normalOS , temp_output_1767_0 ) * input.tangentOS.w ) * break1756.y ) + ( input.normalOS * break1756.z ) ) );
				half3 SmoothNormal1875 = normalizeResult1762;
				float3 ase_objectScale = float3( length( GetObjectToWorldMatrix()[ 0 ].xyz ), length( GetObjectToWorldMatrix()[ 1 ].xyz ), length( GetObjectToWorldMatrix()[ 2 ].xyz ) );
				half4 transform1977 = mul(GetWorldToObjectMatrix(),half4( ( input.ase_color.b * (_Offset).xyz * input.ase_color.b ) , 0.0 ));
				half3 rotatedValue1987 = RotateAroundAxis( _TwistPivotOffset, ( input.positionOS.xyz + (transform1977).xyz ), _TwistVector, ( input.ase_color.b * _Twist ) );
				half3 vertexToFrag1988 = rotatedValue1987;
				half3 temp_output_1990_0 = (vertexToFrag1988).xyz;
				half mulTime1962 = _TimeParameters.x * _Noise1TimeScale;
				half3 appendResult1957 = (half3(input.positionOS.xyz.z , input.positionOS.xyz.x , input.positionOS.xyz.x));
				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				half3 appendResult1956 = (half3(ase_positionWS.z , ase_positionWS.x , ase_positionWS.x));
				half3 lerpResult1960 = lerp( appendResult1957 , appendResult1956 , _WindMoveTurbulence);
				half3 temp_output_1963_0 = ( mulTime1962 + (lerpResult1960).xyz );
				half4 transform1983 = mul(GetWorldToObjectMatrix(),half4( ( input.ase_color.b * _Noise1Magnitude * ( sin( ( _Noise1Mul * temp_output_1963_0 ) ) + sin( ( temp_output_1963_0 * _Noise1SecondaryMul ) ) ) ) , 0.0 ));
				half3 vertexToFrag1989 = (transform1983).xyz;
				#ifdef _USEWIND_ON
				half3 staticSwitch1992 = ( temp_output_1990_0 + vertexToFrag1989 );
				#else
				half3 staticSwitch1992 = temp_output_1990_0;
				#endif
				#ifdef _USEDYNAMICS_ON
				half3 staticSwitch1994 = ( staticSwitch1992 - input.positionOS.xyz );
				#else
				half3 staticSwitch1994 = float3( 0,0,0 );
				#endif
				half3 DynamicOffset1995 = staticSwitch1994;
				half3 newVertexOffset760 = ( ( ( ( _RandomSizeValue * _SizeRandomMul ) + _SizeRandomAdd ) * input.normalOS ) + input.positionOS.xyz + DynamicOffset1995 );
				half3 worldToObj688 = mul( GetWorldToObjectMatrix(), float4( _WorldSpaceCameraPos, 1 ) ).xyz;
				half3 normalizeResult691 = normalize( ( newVertexOffset760 - worldToObj688 ) );
				half3 vertexToFrag711 = ( ( ( SmoothNormal1875 / ase_objectScale ) * _OutlineWidth * ( ( input.ase_color.g + _OutlineModAdd ) * _OutlineModMul ) ) + ( newVertexOffset760 + ( _DepthCompensation * normalizeResult691 ) ) );
				
				half temp_output_701_0 = saturate( ( ( ( mul( GetObjectToWorldMatrix(), half4( input.positionOS.xyz , 0.0 ) ).xyz.y / ase_objectScale.y ) + _OutlineAdd ) * _OutlineMul ) );
				half3 vertexToFrag714 = ( ( (_Color1).rgb * temp_output_701_0 ) + ( (_Color2).rgb * ( 1.0 - temp_output_701_0 ) ) );
				output.ase_texcoord3.xyz = vertexToFrag714;
				
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord3.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = ( vertexToFrag711 - input.positionOS.xyz );

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				float fogFactor = 0;
				#if defined(ASE_FOG) && !defined(_FOG_FRAGMENT)
					fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
				#endif

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				output.positionWSAndFogFactor = float4( vertexInput.positionWS, fogFactor );
				output.normalWS = normalInput.normalWS;
				output.tangentWS = half4( normalInput.tangentWS, ( input.tangentOS.w > 0.0 ? 1.0 : -1.0 ) * GetOddNegativeScale() );
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				half4 ase_color : COLOR;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_texcoord1 = input.ase_texcoord1;
				output.ase_texcoord2 = input.ase_texcoord2;
				output.ase_color = input.ase_color;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				output.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag ( PackedVaryings input 
						#if defined( ASE_WRITE_DEPTH )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( input );
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX( input );

				#if defined( _SURFACE_TYPE_TRANSPARENT )
					const bool isTransparent = true;
				#else
					const bool isTransparent = false;
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_NEEDS_FRAG_SHADOWCOORDS)
					float4 shadowCoord = TransformWorldToShadowCoord(input.positionWSAndFogFactor.xyz);
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				// @diogo: mikktspace compliant
				float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

				float3 PositionWS = input.positionWSAndFogFactor.xyz;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				half3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				float3 TangentWS = input.tangentWS.xyz * renormFactor;
				float3 BitangentWS = cross( input.normalWS, input.tangentWS.xyz ) * input.tangentWS.w * renormFactor;
				float3 NormalWS = input.normalWS * renormFactor;

				half3 vertexToFrag714 = input.ase_texcoord3.xyz;
				half3 lerpResult2_g30 = lerp( vertexToFrag714 , MetaFadeColor.rgb , MetaFadeValue);
				

				float3 Color = lerpResult2_g30;
				float Alpha = 1;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
				#endif

				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = PositionWS;
				inputData.positionCS = input.positionCS;
				inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;
				inputData.normalWS = NormalWS;
				inputData.viewDirectionWS = ViewDirWS;

				#ifdef ASE_FOG
					inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.positionWSAndFogFactor.w);

					#ifdef TERRAIN_SPLAT_ADDPASS
						Color.rgb = MixFogColor(Color.rgb, half3(0,0,0), inputData.fogCoord);
					#else
						Color.rgb = MixFog(Color.rgb, inputData.fogCoord);
					#endif
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				#if defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( Color, Alpha );
				#else
					return half4( Color, OutputAlpha( Alpha, isTransparent ) );
				#endif
			}
			ENDHLSL
		}

		
		Pass
		{
			
			Name "Forward"
			Tags { "LightMode"="UniversalForward" }

			Cull Back
			ZWrite On
			ZTest LEqual
			Offset 0 , 0
			ColorMask RGBA
			Blend One Zero, One Zero
			BlendOp Add, Add

			Stencil
			{
				Ref 1
				Comp Always
				Pass Replace
			}

			HLSLPROGRAM

			#define _NORMAL_DROPOFF_TS 1
			#define _RECEIVE_SHADOWS_OFF
			#define ASE_VERSION 19910
			#define ASE_SRP_VERSION 170300


			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3

			#pragma multi_compile_fragment _ DEBUG_DISPLAY

			#pragma vertex vert
			#pragma fragment frag

			// Option "Keep Lighting Variants" @david please keep this note for future changes in Unlit  lighting
			//#define UNLIT_REALTIME_LIGHTING 1
			//#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			//#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			//#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			//#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
			//#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
			//#pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
			//#pragma multi_compile _ REFLECTION_PROBE_ROTATION
			//#pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
			//#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			//#pragma multi_compile _ SHADOWS_SHADOWMASK
			//#pragma multi_compile_fragment _ _LIGHT_LAYERS
			//#pragma multi_compile_fragment _ _LIGHT_COOKIES
			//#pragma multi_compile _ _CLUSTER_LIGHT_LOOP

			#define SHADERPASS SHADERPASS_UNLIT

			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
			#if ( UNITY_VERSION >= 60010000 )
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
			#else
			#pragma multi_compile_fog
			#endif
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
			#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceData.hlsl"

			#if defined(LOD_FADE_CROSSFADE)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
            #endif

			#define ASE_NEEDS_VERT_POSITION
			#define ASE_NEEDS_FRAG_WORLD_VIEW_DIR
			#define ASE_NEEDS_VERT_NORMAL
			#define ASE_NEEDS_TEXTURE_COORDINATES0
			#define ASE_NEEDS_FRAG_WORLD_TANGENT
			#define ASE_NEEDS_WORLD_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_NORMAL
			#define ASE_NEEDS_FRAG_WORLD_BITANGENT
			#define ASE_NEEDS_FRAG_TEXTURE_COORDINATES0
			#define ASE_NEEDS_WORLD_POSITION
			#define ASE_NEEDS_FRAG_WORLD_POSITION
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES0
			#define ASE_NEEDS_VERT_TANGENT
			#define ASE_NEEDS_TEXTURE_COORDINATES1
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES1
			#define ASE_NEEDS_TEXTURE_COORDINATES2
			#define ASE_NEEDS_VERT_TEXTURE_COORDINATES2
			#pragma shader_feature_local _USEDYNAMICS_ON
			#pragma shader_feature_local _USEWIND_ON
			#pragma shader_feature_local _USECUSTOMLIGHTS_ON
			#pragma shader_feature_local _USEEMISSIONNOISE_ON
			#pragma shader_feature_local _SUBSERFON_ON


			#if defined(ASE_WRITE_DEPTH_CONSERVATIVE) && (SHADER_TARGET >= 45)
				#define ASE_SV_DEPTH SV_DepthLessEqual
				#define ASE_SV_POSITION_QUALIFIERS linear noperspective centroid
			#else
				#define ASE_SV_DEPTH SV_Depth
				#define ASE_SV_POSITION_QUALIFIERS
			#endif

			struct Attributes
			{
				float4 positionOS : POSITION;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				half4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct PackedVaryings
			{
				ASE_SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
				float4 positionWSAndFogFactor : TEXCOORD0;
				half3 normalWS : TEXCOORD1;
				half4 tangentWS : TEXCOORD2;
				float4 ase_texcoord3 : TEXCOORD3;
				float4 ase_color : COLOR;
				float4 ase_texcoord4 : TEXCOORD4;
				float4 ase_texcoord5 : TEXCOORD5;
				float4 ase_texcoord6 : TEXCOORD6;
				float3 ase_normal : NORMAL;
				float4 ase_texcoord7 : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			CBUFFER_START(UnityPerMaterial)
			half4 _AmbientColor;
			half4 _EmissionNoiseTex_ST;
			half4 _Emission_ST;
			float4 _EmissiveColor;
			half4 _SpecColorMul;
			half4 _SpecGloss_ST;
			half4 _Normal_ST;
			half4 _Rim1Color;
			half4 _MainTex_ST;
			half4 _Color2;
			half4 _Color1;
			half4 _MainTexTint;
			half4 _SkinMask_ST;
			half4 _SkinSubserfColor;
			half4 _ShadowsColor;
			half4 _Offset;
			half3 _SubserfDir;
			half3 _Noise1SecondaryMul;
			half3 _Noise1Magnitude;
			half3 _LightDirection;
			half3 _TwistVector;
			half3 _TwistPivotOffset;
			half3 _Rim1Direction;
			half3 _Noise1Mul;
			half2 _SkinMulPow;
			half2 _EmissionNoisePanner;
			half2 _Fresnel;
			half _EmissionNoiseMul;
			half _ShadowOffsetMul;
			half _ShadowTransparencyAdd;
			half _MainTexAdd;
			half _MainTexMul;
			half _SmoothsepMin;
			half _SmoothsepMax;
			half _EmissionNoiseAdd;
			half _PannerJitterAmmount;
			half _SpecAdd;
			half _PannerDuration;
			half _OutlineModAdd;
			half _OutlineModMul;
			half _RandomSizeValue;
			half _SizeRandomMul;
			half _SizeRandomAdd;
			half _Twist;
			half _Noise1TimeScale;
			half _WindMoveTurbulence;
			half _DepthCompensation;
			half _OutlineAdd;
			half _OutlineMul;
			half _Rim1StepMin;
			half _Rim1StepMax;
			half _EnvIntensity;
			float _EmissionPower;
			half _PannerJitterSteps;
			half _OutlineWidth;
			float _AlphaClip;
			float _Cutoff;
			#ifdef ASE_TESSELLATION
				float _TessPhongStrength;
				float _TessValue;
				float _TessMin;
				float _TessMax;
				float _TessEdgeLength;
				float _TessMaxDisp;
			#endif
			CBUFFER_END

			sampler2D _Normal;
			sampler2D _SpecGloss;
			sampler2D _Emission;
			sampler2D _EmissionNoiseTex;
			sampler2D _MainTex;
			sampler2D _SkinMask;
			half3 _Light0_Pos;
			half2 _Light0_Data;
			half _Light0_InnerFadeWidth;
			half _Light0_Brightness;
			half4 _Light0_Color;
			half3 _Light1_Pos;
			half2 _Light1_Data;
			half _Light1_InnerFadeWidth;
			half _Light1_Brightness;
			half4 _Light1_Color;
			half4 MetaFadeColor;
			half MetaFadeValue;


			float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
			{
				original -= center;
				float C = cos( angle );
				float S = sin( angle );
				float t = 1 - C;
				float m00 = t * u.x * u.x + C;
				float m01 = t * u.x * u.y - S * u.z;
				float m02 = t * u.x * u.z + S * u.y;
				float m10 = t * u.x * u.y + S * u.z;
				float m11 = t * u.y * u.y + C;
				float m12 = t * u.y * u.z - S * u.x;
				float m20 = t * u.x * u.z - S * u.y;
				float m21 = t * u.y * u.z + S * u.x;
				float m22 = t * u.z * u.z + C;
				float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
				return mul( finalMatrix, original ) + center;
			}
			
			half3 SimpleReflectionProbe1_g33( half3 ViewDirWS, half3 NormalWS, half Lod )
			{
				return DecodeHDREnvironment(
				    SAMPLE_TEXTURECUBE_LOD(
				        unity_SpecCube0,
				        samplerunity_SpecCube0,
				        reflect(-normalize(ViewDirWS), normalize(NormalWS)),
				        Lod
				    ),
				    unity_SpecCube0_HDR
				);
			}
			

			PackedVaryings VertexFunction( Attributes input  )
			{
				PackedVaryings output = (PackedVaryings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				half4 transform1977 = mul(GetWorldToObjectMatrix(),half4( ( input.ase_color.b * (_Offset).xyz * input.ase_color.b ) , 0.0 ));
				half3 rotatedValue1987 = RotateAroundAxis( _TwistPivotOffset, ( input.positionOS.xyz + (transform1977).xyz ), _TwistVector, ( input.ase_color.b * _Twist ) );
				half3 vertexToFrag1988 = rotatedValue1987;
				half3 temp_output_1990_0 = (vertexToFrag1988).xyz;
				half mulTime1962 = _TimeParameters.x * _Noise1TimeScale;
				half3 appendResult1957 = (half3(input.positionOS.xyz.z , input.positionOS.xyz.x , input.positionOS.xyz.x));
				float3 ase_positionWS = TransformObjectToWorld( ( input.positionOS ).xyz );
				half3 appendResult1956 = (half3(ase_positionWS.z , ase_positionWS.x , ase_positionWS.x));
				half3 lerpResult1960 = lerp( appendResult1957 , appendResult1956 , _WindMoveTurbulence);
				half3 temp_output_1963_0 = ( mulTime1962 + (lerpResult1960).xyz );
				half4 transform1983 = mul(GetWorldToObjectMatrix(),half4( ( input.ase_color.b * _Noise1Magnitude * ( sin( ( _Noise1Mul * temp_output_1963_0 ) ) + sin( ( temp_output_1963_0 * _Noise1SecondaryMul ) ) ) ) , 0.0 ));
				half3 vertexToFrag1989 = (transform1983).xyz;
				#ifdef _USEWIND_ON
				half3 staticSwitch1992 = ( temp_output_1990_0 + vertexToFrag1989 );
				#else
				half3 staticSwitch1992 = temp_output_1990_0;
				#endif
				#ifdef _USEDYNAMICS_ON
				half3 staticSwitch1994 = ( staticSwitch1992 - input.positionOS.xyz );
				#else
				half3 staticSwitch1994 = float3( 0,0,0 );
				#endif
				half3 DynamicOffset1995 = staticSwitch1994;
				
				half3 ase_normalWS = TransformObjectToWorldNormal( input.normalOS );
				half3 vertexToFrag1650 = ase_normalWS;
				output.ase_texcoord3.xyz = vertexToFrag1650;
				half3 vertexToFrag1741 = ( _WorldSpaceCameraPos - ase_positionWS );
				output.ase_texcoord5.xyz = vertexToFrag1741;
				half3 vertexToFrag1740 = ase_normalWS;
				output.ase_texcoord6.xyz = vertexToFrag1740;
				half vertexToFrag1784 = 1.0;
				output.ase_texcoord3.w = vertexToFrag1784;
				half lerpResult1814 = lerp( ( frac( ( _TimeParameters.x / _PannerDuration ) ) * _PannerDuration ) , ( floor( ( ( frac( ( _TimeParameters.x / _PannerDuration ) ) * _PannerDuration ) * _PannerJitterSteps ) ) / _PannerJitterSteps ) , _PannerJitterAmmount);
				half2 vertexToFrag1776 = ( ( _EmissionNoiseTex_ST.xy * input.ase_texcoord.xy ) + ( _EmissionNoisePanner * lerpResult1814 ) );
				output.ase_texcoord4.zw = vertexToFrag1776;
				float3 ase_viewVectorWS = ( ( unity_OrthoParams.w == 0 ) ? _WorldSpaceCameraPos - ase_positionWS : UNITY_MATRIX_V[ 2 ].xyz );
				float3 ase_viewDirWS = normalize( ase_viewVectorWS );
				half dotResult1371 = dot( _SubserfDir , -ase_viewDirWS );
				half3 temp_output_1767_0 = (input.tangentOS).xyz;
				half3 appendResult1751 = (half3(input.ase_texcoord1.x , input.ase_texcoord2.x , input.ase_texcoord1.y));
				half3 break1756 = appendResult1751;
				half3 normalizeResult1762 = normalize( ( ( temp_output_1767_0 * break1756.x ) + ( ( cross( input.normalOS , temp_output_1767_0 ) * input.tangentOS.w ) * break1756.y ) + ( input.normalOS * break1756.z ) ) );
				half3 SmoothNormal1875 = normalizeResult1762;
				half dotResult1927 = dot( ase_viewDirWS , mul( GetObjectToWorldMatrix(), half4( SmoothNormal1875 , 0.0 ) ).xyz );
				half vertexToFrag1922 = ( saturate( dotResult1371 ) * saturate( ( _Fresnel.x + ( _Fresnel.y * ( 1.0 - dotResult1927 ) ) ) ) );
				output.ase_texcoord5.w = vertexToFrag1922;
				half3 vertexToFrag61_g1 = mul( GetObjectToWorldMatrix(), half4( SmoothNormal1875 , 0.0 ) ).xyz;
				half3 vertexToFrag56_g1 = ase_positionWS;
				half3 temp_output_79_0_g1 = ( _Light0_Pos - vertexToFrag56_g1 );
				half temp_output_82_0_g1 = length( temp_output_79_0_g1 );
				half dotResult94_g1 = dot( vertexToFrag61_g1 , ( temp_output_79_0_g1 / temp_output_82_0_g1 ) );
				half temp_output_68_0_g1 = ( _Light0_Data.y + _Light0_InnerFadeWidth );
				half3 temp_output_76_0_g1 = ( _Light1_Pos - vertexToFrag56_g1 );
				half temp_output_75_0_g1 = length( temp_output_76_0_g1 );
				half dotResult92_g1 = dot( vertexToFrag61_g1 , ( temp_output_76_0_g1 / temp_output_75_0_g1 ) );
				half temp_output_69_0_g1 = ( _Light1_Data.y + _Light1_InnerFadeWidth );
				half3 vertexToFrag1874 = ( ( saturate( dotResult94_g1 ) * ( saturate( ( ( temp_output_82_0_g1 - _Light0_Data.y ) / ( temp_output_68_0_g1 - _Light0_Data.y ) ) ) * saturate( ( 1.0 - ( temp_output_82_0_g1 / _Light0_Data.x ) ) ) ) * _Light0_Brightness * _Light0_Color.rgb ) + ( saturate( dotResult92_g1 ) * ( saturate( ( ( temp_output_75_0_g1 - _Light1_Data.y ) / ( temp_output_69_0_g1 - _Light1_Data.y ) ) ) * saturate( ( 1.0 - ( temp_output_75_0_g1 / _Light1_Data.x ) ) ) ) * _Light1_Brightness * _Light1_Color.rgb ) );
				output.ase_texcoord7.xyz = vertexToFrag1874;
				
				output.ase_color = input.ase_color;
				output.ase_texcoord4.xy = input.ase_texcoord.xy;
				output.ase_normal = input.normalOS;
				
				//setting value to unused interpolator channels and avoid initialization warnings
				output.ase_texcoord6.w = 0;
				output.ase_texcoord7.w = 0;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					float3 defaultVertexValue = input.positionOS.xyz;
				#else
					float3 defaultVertexValue = float3(0, 0, 0);
				#endif

				float3 vertexValue = DynamicOffset1995;

				#ifdef ASE_ABSOLUTE_VERTEX_POS
					input.positionOS.xyz = vertexValue;
				#else
					input.positionOS.xyz += vertexValue;
				#endif

				input.normalOS = input.normalOS;
				input.tangentOS = input.tangentOS;

				#ifdef ASE_CUSTOM_MOTION_VECTOR
					// Declared so the Motion Vector output port surfaces on the master node; only consumed by the motion vector passes.
					float3 aseCustomMotionVector = float3(0, 0, 0);
				#endif

				VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
				VertexNormalInputs normalInput = GetVertexNormalInputs( input.normalOS, input.tangentOS );

				float fogFactor = 0;
				#if defined(ASE_FOG) && !defined(_FOG_FRAGMENT)
					fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
				#endif

				output.positionCS = ASE_ADJUST_CLIP_POSITION( vertexInput.positionCS );
				output.positionWSAndFogFactor = float4( vertexInput.positionWS, fogFactor );
				output.normalWS = normalInput.normalWS;
				output.tangentWS = half4( normalInput.tangentWS, ( input.tangentOS.w > 0.0 ? 1.0 : -1.0 ) * GetOddNegativeScale() );;
				return output;
			}

			#if defined(ASE_TESSELLATION)
			struct VertexControl
			{
				float4 positionOS : INTERNALTESSPOS;
				half3 normalOS : NORMAL;
				half4 tangentOS : TANGENT;
				half4 ase_color : COLOR;
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_texcoord1 : TEXCOORD1;
				float4 ase_texcoord2 : TEXCOORD2;

				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TessellationFactors
			{
				float edge[3] : SV_TessFactor;
				float inside : SV_InsideTessFactor;
			};

			VertexControl vert ( Attributes input )
			{
				VertexControl output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				output.positionOS = input.positionOS;
				output.normalOS = input.normalOS;
				output.tangentOS = input.tangentOS;
				output.ase_color = input.ase_color;
				output.ase_texcoord = input.ase_texcoord;
				output.ase_texcoord1 = input.ase_texcoord1;
				output.ase_texcoord2 = input.ase_texcoord2;
				return output;
			}

			TessellationFactors TessellationFunction (InputPatch<VertexControl,3> input)
			{
				TessellationFactors output;
				float4 tf = 1;
				float tessValue = _TessValue; float tessMin = _TessMin; float tessMax = _TessMax;
				float edgeLength = _TessEdgeLength; float tessMaxDisp = _TessMaxDisp;
				#if defined(ASE_FIXED_TESSELLATION)
				tf = FixedTess( tessValue );
				#elif defined(ASE_DISTANCE_TESSELLATION)
				tf = DistanceBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, tessValue, tessMin, tessMax, GetObjectToWorldMatrix(), _WorldSpaceCameraPos );
				#elif defined(ASE_LENGTH_TESSELLATION)
				tf = EdgeLengthBasedTess(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams );
				#elif defined(ASE_LENGTH_CULL_TESSELLATION)
				tf = EdgeLengthBasedTessCull(input[0].positionOS, input[1].positionOS, input[2].positionOS, edgeLength, tessMaxDisp, GetObjectToWorldMatrix(), _WorldSpaceCameraPos, _ScreenParams, unity_CameraWorldClipPlanes );
				#endif
				output.edge[0] = tf.x; output.edge[1] = tf.y; output.edge[2] = tf.z; output.inside = tf.w;
				return output;
			}

			[domain("tri")]
			[partitioning("fractional_odd")]
			[outputtopology("triangle_cw")]
			[patchconstantfunc("TessellationFunction")]
			[outputcontrolpoints(3)]
			VertexControl HullFunction(InputPatch<VertexControl, 3> patch, uint id : SV_OutputControlPointID)
			{
				return patch[id];
			}

			[domain("tri")]
			PackedVaryings DomainFunction(TessellationFactors factors, OutputPatch<VertexControl, 3> patch, float3 bary : SV_DomainLocation)
			{
				Attributes output = (Attributes) 0;
				output.positionOS = patch[0].positionOS * bary.x + patch[1].positionOS * bary.y + patch[2].positionOS * bary.z;
				output.normalOS = patch[0].normalOS * bary.x + patch[1].normalOS * bary.y + patch[2].normalOS * bary.z;
				output.tangentOS = patch[0].tangentOS * bary.x + patch[1].tangentOS * bary.y + patch[2].tangentOS * bary.z;
				output.ase_color = patch[0].ase_color * bary.x + patch[1].ase_color * bary.y + patch[2].ase_color * bary.z;
				output.ase_texcoord = patch[0].ase_texcoord * bary.x + patch[1].ase_texcoord * bary.y + patch[2].ase_texcoord * bary.z;
				output.ase_texcoord1 = patch[0].ase_texcoord1 * bary.x + patch[1].ase_texcoord1 * bary.y + patch[2].ase_texcoord1 * bary.z;
				output.ase_texcoord2 = patch[0].ase_texcoord2 * bary.x + patch[1].ase_texcoord2 * bary.y + patch[2].ase_texcoord2 * bary.z;
				#if defined(ASE_PHONG_TESSELLATION)
				float3 pp[3];
				for (int i = 0; i < 3; ++i)
					pp[i] = output.positionOS.xyz - patch[i].normalOS * (dot(output.positionOS.xyz, patch[i].normalOS) - dot(patch[i].positionOS.xyz, patch[i].normalOS));
				float phongStrength = _TessPhongStrength;
				output.positionOS.xyz = phongStrength * (pp[0]*bary.x + pp[1]*bary.y + pp[2]*bary.z) + (1.0f-phongStrength) * output.positionOS.xyz;
				#endif
				UNITY_TRANSFER_INSTANCE_ID(patch[0], output);
				return VertexFunction(output);
			}
			#else
			PackedVaryings vert ( Attributes input )
			{
				return VertexFunction( input );
			}
			#endif

			half4 frag ( PackedVaryings input
						#if defined( ASE_WRITE_DEPTH )
						,out float outputDepth : ASE_SV_DEPTH
						#endif
						#ifdef _WRITE_RENDERING_LAYERS
						#if ( UNITY_VERSION >= 60020000 )
						, out uint outRenderingLayers : SV_Target1
						#else
						, out float4 outRenderingLayers : SV_Target1
						#endif
						#endif
						 ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				#if defined( _SURFACE_TYPE_TRANSPARENT )
					const bool isTransparent = true;
				#else
					const bool isTransparent = false;
				#endif

				#if defined(LOD_FADE_CROSSFADE)
					LODFadeCrossFade( input.positionCS );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
					float4 shadowCoord = TransformWorldToShadowCoord( input.positionWSAndFogFactor.xyz );
				#else
					float4 shadowCoord = float4(0, 0, 0, 0);
				#endif

				// @diogo: mikktspace compliant
				float renormFactor = 1.0 / max( FLT_MIN, length( input.normalWS ) );

				float3 PositionWS = input.positionWSAndFogFactor.xyz;
				float3 PositionRWS = GetCameraRelativePositionWS( PositionWS );
				half3 ViewDirWS = GetWorldSpaceNormalizeViewDir( PositionWS );
				float4 ShadowCoord = shadowCoord;
				float4 ScreenPosNorm = float4( GetNormalizedScreenSpaceUV( input.positionCS ), input.positionCS.zw );
				float4 ClipPos = ComputeClipSpacePosition( ScreenPosNorm.xy, input.positionCS.z ) * input.positionCS.w;
				float4 ScreenPos = ComputeScreenPos( ClipPos );
				float3 TangentWS = input.tangentWS.xyz * renormFactor;
				float3 BitangentWS = cross( input.normalWS, input.tangentWS.xyz ) * input.tangentWS.w * renormFactor;
				float3 NormalWS = input.normalWS * renormFactor;

				half3 vertexToFrag1650 = input.ase_texcoord3.xyz;
				half dotResult1151 = dot( ViewDirWS , vertexToFrag1650 );
				half3 normalizeResult1258 = normalize( _Rim1Direction );
				half3 worldToViewDir1643 = mul( UNITY_MATRIX_V, float4( normalizeResult1258, 0.0 ) ).xyz;
				half dotResult1157 = dot( vertexToFrag1650 , worldToViewDir1643 );
				half smoothstepResult1259 = smoothstep( _Rim1StepMin , _Rim1StepMax , ( ( 1.0 - dotResult1151 ) * dotResult1157 ));
				half Rim1Mask1230 = saturate( ( smoothstepResult1259 * input.ase_color.r ) );
				half3 RimLights1097 = ( Rim1Mask1230 * _Rim1Color.rgb * _Rim1Color.a );
				half RimMask1244 = saturate( ( Rim1Mask1230 * _Rim1Color.a ) );
				half2 uv_Normal = input.ase_texcoord4.xy * _Normal_ST.xy + _Normal_ST.zw;
				half3 NormalTex1308 = UnpackNormalScale( tex2D( _Normal, uv_Normal ), 1.0f );
				half3 tanToWorld0 = float3( TangentWS.x, BitangentWS.x, NormalWS.x );
				half3 tanToWorld1 = float3( TangentWS.y, BitangentWS.y, NormalWS.y );
				half3 tanToWorld2 = float3( TangentWS.z, BitangentWS.z, NormalWS.z );
				float3 tanNormal1368 = NormalTex1308;
				half3 worldNormal1368 = normalize( float3( dot( tanToWorld0, tanNormal1368 ), dot( tanToWorld1, tanNormal1368 ), dot( tanToWorld2, tanNormal1368 ) ) );
				half3 WNormal476 = worldNormal1368;
				half3 LightDirection1019 = _LightDirection;
				half3 normalizeResult1425 = normalize( LightDirection1019 );
				half dotResult1449 = dot( reflect( -ViewDirWS , WNormal476 ) , normalizeResult1425 );
				half2 uv_SpecGloss = input.ase_texcoord4.xy * _SpecGloss_ST.xy + _SpecGloss_ST.zw;
				half4 tex2DNode1 = tex2D( _SpecGloss, uv_SpecGloss );
				half temp_output_1433_0 = exp2( ( tex2DNode1.a * 6.0 ) );
				half dotResult1434 = dot( WNormal476 , normalizeResult1425 );
				half3 Spec477 = ( pow( saturate( dotResult1449 ) , temp_output_1433_0 ) * saturate( dotResult1434 ) * ( tex2DNode1.rgb + _SpecAdd ) );
				half3 vertexToFrag1741 = input.ase_texcoord5.xyz;
				half3 ViewDirWS1_g33 = vertexToFrag1741;
				half3 vertexToFrag1740 = input.ase_texcoord6.xyz;
				half3 NormalWS1_g33 = vertexToFrag1740;
				half temp_output_1627_0 =  (4.5 + ( temp_output_1433_0 - 0.0 ) * ( 4.0 - 4.5 ) / ( 6.0 - 0.0 ) );
				half Lod1_g33 = temp_output_1627_0;
				half3 localSimpleReflectionProbe1_g33 = SimpleReflectionProbe1_g33( ViewDirWS1_g33 , NormalWS1_g33 , Lod1_g33 );
				float3 ase_viewVectorOS = mul( ( float3x3 )GetWorldToObjectMatrix(), ( ( unity_OrthoParams.w == 0 ) ? _WorldSpaceCameraPos - PositionWS : UNITY_MATRIX_V[ 2 ].xyz ) );
				float3 ase_viewDirOS = normalize( ase_viewVectorOS );
				half dotResult1611 = dot( ase_viewDirOS , input.ase_normal );
				half3 EnvReflect1625 = ( localSimpleReflectionProbe1_g33 * ( ( pow( saturate( dotResult1611 ) , 1.0 ) * ( 1.0 - Spec477 ) ) + Spec477 ) * ( tex2DNode1.rgb + _SpecAdd ) * _EnvIntensity );
				half2 uv_Emission = input.ase_texcoord4.xy * _Emission_ST.xy + _Emission_ST.zw;
				half vertexToFrag1784 = input.ase_texcoord3.w;
				half3 temp_cast_0 = (vertexToFrag1784).xxx;
				half2 vertexToFrag1776 = input.ase_texcoord4.zw;
				half3 EmissionNoise1944 = ( ( tex2D( _EmissionNoiseTex, vertexToFrag1776 ).rgb * _EmissionNoiseMul ) + _EmissionNoiseAdd );
				#ifdef _USEEMISSIONNOISE_ON
				half3 staticSwitch1782 = EmissionNoise1944;
				#else
				half3 staticSwitch1782 = temp_cast_0;
				#endif
				half2 uv_MainTex = input.ase_texcoord4.xy * _MainTex_ST.xy + _MainTex_ST.zw;
				half3 MainTexMix592 = saturate( ( ( tex2D( _MainTex, uv_MainTex ).rgb + _MainTexAdd ) * _MainTexMul * (_MainTexTint).rgb ) );
				half dotResult635 = dot( WNormal476 , _LightDirection );
				half smoothstepResult658 = smoothstep( _SmoothsepMin , _SmoothsepMax , saturate( ( _ShadowOffsetMul * dotResult635 ) ));
				half ShaderToon644 = saturate( ( smoothstepResult658 + _ShadowTransparencyAdd ) );
				half3 Emission491 = ( ( _EmissiveColor.rgb * ( _EmissionPower * tex2D( _Emission, uv_Emission ).rgb * staticSwitch1782 ) ) + ( MainTexMix592 * ( ShaderToon644 + (( ( 1.0 - ShaderToon644 ) * _ShadowsColor )).rgb ) ) );
				half2 uv_SkinMask = input.ase_texcoord4.xy * _SkinMask_ST.xy + _SkinMask_ST.zw;
				half dotResult1363 = dot( WNormal476 , ViewDirWS );
				half vertexToFrag1922 = input.ase_texcoord5.w;
				#ifdef _SUBSERFON_ON
				half3 staticSwitch1387 = saturate( ( tex2D( _SkinMask, uv_SkinMask ).r * pow( ( _SkinMulPow.x * ( 1.0 - dotResult1363 ) ) , _SkinMulPow.y ) * _SkinSubserfColor.rgb * vertexToFrag1922 ) );
				#else
				half3 staticSwitch1387 = float3( 0,0,0 );
				#endif
				half3 Skin1360 = staticSwitch1387;
				half3 temp_output_1253_0 = ( RimLights1097 + ( ( ( ( 1.0 - saturate( RimMask1244 ) ) * ( ( Spec477 * _SpecColorMul.rgb ) + EnvReflect1625 + Emission491 ) ) + Skin1360 ) * _AmbientColor.rgb ) );
				half3 vertexToFrag1874 = input.ase_texcoord7.xyz;
				#ifdef _USECUSTOMLIGHTS_ON
				half3 staticSwitch1824 = ( temp_output_1253_0 + vertexToFrag1874 );
				#else
				half3 staticSwitch1824 = temp_output_1253_0;
				#endif
				half3 lerpResult2_g31 = lerp( staticSwitch1824 , MetaFadeColor.rgb , MetaFadeValue);
				
				float3 BakedAlbedo = 0;
				float3 BakedEmission = 0;
				float3 Color = lerpResult2_g31;
				float3 Normal = float3(0, 0, 1);
				float Alpha = 1;
				#if defined( _ALPHATEST_ON )
					float AlphaClipThreshold = _Cutoff;
					float AlphaClipThresholdShadow = 0.5;
				#endif


				#if defined( ASE_WRITE_DEPTH )
					input.positionCS.z = input.positionCS.z;
				#endif

				#if defined( _ALPHATEST_ON )
					AlphaDiscard( Alpha, AlphaClipThreshold );
				#endif

				#if defined(MAIN_LIGHT_CALCULATE_SHADOWS) && defined(ASE_CHANGES_WORLD_POS)
					ShadowCoord = TransformWorldToShadowCoord( PositionWS );
				#endif

				InputData inputData = (InputData)0;
				inputData.positionWS = PositionWS;
				inputData.positionCS = input.positionCS;
				inputData.normalizedScreenSpaceUV = ScreenPosNorm.xy;
				inputData.normalWS = NormalWS;
				inputData.viewDirectionWS = ViewDirWS;

				#if defined(_SCREEN_SPACE_OCCLUSION) && !defined(_SURFACE_TYPE_TRANSPARENT) && defined(UNLIT_DEFAULT_SSAO)
					float2 normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
					AmbientOcclusionFactor aoFactor = GetScreenSpaceAmbientOcclusion(normalizedScreenSpaceUV);
					Color.rgb *= aoFactor.directAmbientOcclusion;
				#endif

				#ifdef ASE_FOG
					inputData.fogCoord = InitializeInputDataFog(float4(inputData.positionWS, 1.0), input.positionWSAndFogFactor.w);
				#endif

				#if defined(_DBUFFER) && defined(UNLIT_DEFAULT_DECAL_BLENDING)
					ApplyDecalToBaseColor(input.positionCS, Color);
				#endif

				#ifdef ASE_FOG
					#ifdef TERRAIN_SPLAT_ADDPASS
						Color.rgb = MixFogColor(Color.rgb, half3(0,0,0), inputData.fogCoord);
					#else
						Color.rgb = MixFog(Color.rgb, inputData.fogCoord);
					#endif
				#endif

				#if defined( ASE_WRITE_DEPTH )
					outputDepth = input.positionCS.z;
				#endif

				#ifdef _WRITE_RENDERING_LAYERS
					#if ( UNITY_VERSION >= 60020000 )
					outRenderingLayers = EncodeMeshRenderingLayer();
					#else
					uint renderingLayers = GetMeshRenderingLayer();
					outRenderingLayers = float4( EncodeMeshRenderingLayer( renderingLayers ), 0, 0, 0 );
					#endif
				#endif

				#if defined( ASE_OPAQUE_KEEP_ALPHA )
					return half4( Color, Alpha );
				#else
					return half4( Color, OutputAlpha( Alpha, isTransparent ) );
				#endif
			}
			ENDHLSL
		}

	
	}
	

	

	CustomEditor "UnityEditor.ShaderGraphUnlitGUI"
	FallBack "Hidden/Shader Graph/FallbackError"
	
	Fallback Off
}
/*ASEBEGIN
Version=19910
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1946,"pos":[-2112,-992],"params":["Inherit","False","3364","802.95","","26","1778","1780","1781","1804","1806","1776","1773","1770","1771","1774","1772","1944","1814","1777","1812","1817","1813","1811","1816","1818","1823","1820","1821","1805","1822","1401","EmissionNoise","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1949,"pos":[-736,-3216],"params":["Inherit","False","1988","834.75","","15","1750","1749","1755","1751","1766","1754","1756","1768","1767","1757","1760","1759","1761","1875","1762","SmoothNormals","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":1805,"pos":[-2048,-528],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1822,"pos":[-2048,-432],"params":["Inherit","False","Property","_PannerDuration","PannerDuration","50","0","Create","True","0","0","0","False","0","False","Object","-1","","1","3.333333","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":1750,"pos":[-592,-3008],"params":["Inherit","False","2","3","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":1749,"pos":[-592,-3168],"params":["Inherit","False","1","3","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.NormalVertexDataNode, AmplifyShaderEditor","id":1755,"pos":[-560,-2848],"params":["Inherit","False","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1811,"pos":[-1264,-352],"params":["Inherit","False","Property","_PannerJitterSteps","PannerJitterSteps","48","0","Create","True","0","0","0","False","0","False","Object","-1","","3","3","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":1821,"pos":[-1808,-528],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TangentVertexDataNode, AmplifyShaderEditor","id":1754,"pos":[-592,-2592],"params":["Inherit","False","1","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1751,"pos":[-336,-3072],"params":["Inherit","False","FLOAT3","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.CrossProductOpNode, AmplifyShaderEditor","id":1766,"pos":[-96,-2848],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1812,"pos":[-1040,-432],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.FractNode, AmplifyShaderEditor","id":1820,"pos":[-1616,-528],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":1767,"pos":[-368,-2640],"params":["Inherit","False","True","True","True","False","1","0","FLOAT4","0,0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":1756,"pos":[16,-3072],"params":["Inherit","False","FLOAT3","1","0","FLOAT3","0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1768,"pos":[112,-2848],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.FloorOpNode, AmplifyShaderEditor","id":1817,"pos":[-864,-432],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1823,"pos":[-1424,-528],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1760,"pos":[368,-2544],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1757,"pos":[352,-3088],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1759,"pos":[352,-2848],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":1813,"pos":[-704,-416],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1816,"pos":[-800,-304],"params":["Inherit","False","Property","_PannerJitterAmmount","PannerJitterAmmount","49","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RelayNode, AmplifyShaderEditor","id":1818,"pos":[-1232,-528],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1943,"pos":[400,-112],"params":["Inherit","False","852","490.8","","6","8","1308","1367","1368","476","1295","Normal","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":728,"pos":[-432,-2304],"params":["Inherit","False","1683.615","530.4238","","15","1019","644","653","651","658","1942","652","680","679","636","649","650","635","973","1312","Toon","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1761,"pos":[608,-2864],"params":["Inherit","False","3","3","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.TexCoordVertexDataNode, AmplifyShaderEditor","id":1778,"pos":[-912,-768],"params":["Inherit","False","0","2","0","5","FLOAT2","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.TextureTransformNode, AmplifyShaderEditor","id":1780,"pos":[-944,-944],"params":["Inherit","False","1770","False","1","0","SAMPLER2D","","False","2","FLOAT2","0","FLOAT2","1"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1814,"pos":[-464,-528],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":1777,"pos":[-528,-704],"params":["Inherit","False","Property","_EmissionNoisePanner","EmissionNoisePanner","45","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0","0,0.3","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.NormalizeNode, AmplifyShaderEditor","id":1762,"pos":[768,-2864],"params":["Inherit","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":8,"pos":[656,-64],"params":["Inherit","True","Property","_Normal","Normal","10","0","Create","True","0","0","0","False","0","False","","-1","None","None","True","0","True","bump","Auto","True","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1781,"pos":[-656,-816],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1804,"pos":[-240,-720],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT","0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1312,"pos":[-352,-2224],"params":["Inherit","False","476","WNormal","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1482,"pos":[-1248,1056],"params":["Inherit","False","2497.551","1632.202","Comment","30","1924","1927","1939","1926","1938","1928","1930","1931","1933","1923","1935","1386","1372","1371","1370","1362","1360","1387","1384","1378","1922","1377","1359","1383","1369","1932","1385","1363","1480","1364","Skin","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1875,"pos":[992,-2864],"params":["Inherit","False","SmoothNormal","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1308,"pos":[1008,-64],"params":["Inherit","False","NormalTex","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1806,"pos":[-48,-800],"params":["Inherit","False","2","2","0","FLOAT2","0,0","False","1","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":635,"pos":[-96,-2144],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":650,"pos":[-144,-2240],"params":["Inherit","False","Property","_ShadowOffsetMul","ShadowOffsetMul","16","0","Create","True","0","0","0","True","0","False","Object","-1","","1","3","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1953,"pos":[-3152,4000],"params":["Inherit","False","4371.42","1487.427","Comment","45","1958","1955","1954","1957","1956","1960","1961","1964","1967","1963","1962","1959","1972","1973","1986","1983","1979","1966","1969","1970","1975","1977","1974","1999","2000","1998","1978","1982","1985","1984","1980","1976","1981","1987","1990","1988","1989","1991","1992","1993","1994","1995","1971","1968","1965","Dynamic","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1481,"pos":[1352,2112],"params":["Inherit","False","2806","1682.95","Comment","19","1429","1432","1430","1446","1345","1445","1423","1449","1","1448","1433","1354","1355","1451","477","1940","1435","1434","1444","Spec","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.ObjectToWorldMatrixNode, AmplifyShaderEditor","id":1938,"pos":[-1168,2368],"params":["Inherit","False","0","1","FLOAT4x4","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1926,"pos":[-1168,2496],"params":["Inherit","False","1875","SmoothNormal","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1367,"pos":[544,192],"params":["Inherit","False","1308","NormalTex","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.VertexToFragmentNode, AmplifyShaderEditor","id":1776,"pos":[144,-800],"params":["Inherit","False","False","False","1","0","FLOAT2","0,0","False","1","FLOAT2","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":649,"pos":[80,-2176],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":807,"pos":[-192,464],"params":["Inherit","False","1444.727","510.817","","9","6","518","592","631","593","514","517","515","519","MainTex","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":508,"pos":[1368,-2104],"params":["Inherit","False","1550.753","1266.113","","21","672","668","669","1947","1948","645","670","671","1635","595","491","646","9","11","13","332","1945","1783","1784","1782","10","Emission","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1329,"pos":[-1232,2768],"params":["Inherit","False","2476.143","627.5183","Comment","18","1153","1636","1230","1213","1289","1259","1263","1262","1260","1157","1643","1258","1156","1149","1649","1650","1648","1151","RimLights","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1429,"pos":[1736,2176],"params":["Inherit","False","260","234.7998","V","1","1334","","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor","id":1954,"pos":[-3120,4912],"params":["Inherit","False","0","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor","id":1955,"pos":[-3120,5120],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1939,"pos":[-960,2416],"params":["Inherit","False","2","2","0","FLOAT4x4","1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor","id":1924,"pos":[-1120,2192],"params":["Inherit","False","World","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.WorldNormalVector, AmplifyShaderEditor","id":1368,"pos":[768,192],"params":["Inherit","False","True","1","0","FLOAT3","0,0,1","False","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1773,"pos":[400,-640],"params":["Inherit","False","Property","_EmissionNoiseMul","EmissionNoiseMul","46","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":1770,"pos":[368,-832],"params":["Inherit","True","Property","_EmissionNoiseTex","EmissionNoiseTex","44","0","Create","True","0","0","0","False","0","False","","-1","None","152d47408141cd141832c51342364d59","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":973,"pos":[-368,-2128],"params":["Inherit","False","Property","_LightDirection","LightDirection","31","0","Create","True","0","0","0","False","0","False","Object","-1","","-0.12,0.5,-1","0,0.58,0.6","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":636,"pos":[272,-2176],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":679,"pos":[288,-2096],"params":["Inherit","False","Property","_SmoothsepMin","SmoothsepMin","19","0","Create","True","0","0","0","True","0","False","Object","-1","","0.1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":680,"pos":[304,-2016],"params":["Inherit","False","Property","_SmoothsepMax","SmoothsepMax","20","0","Create","True","0","0","0","True","0","False","Object","-1","","0.16","0.3","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1951,"pos":[1352,3888],"params":["Inherit","False","2800.345","914.9502","","23","1625","1623","1607","1624","1619","1740","1627","1618","1741","1736","1738","1609","1616","1737","1733","1614","1617","1608","1611","1613","1612","2001","2004","Reflection","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1432,"pos":[1784,2608],"params":["Inherit","False","210.1155","145.2681","N","1","1332","","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1430,"pos":[1528,2432],"params":["Inherit","False","468","162.9502","L","2","1336","1425","","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1956,"pos":[-2832,5152],"params":["Inherit","True","FLOAT3","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.DynamicAppendNode, AmplifyShaderEditor","id":1957,"pos":[-2832,4912],"params":["Inherit","True","FLOAT3","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","0","False","3","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1958,"pos":[-2896,5376],"params":["Inherit","False","Property","_WindMoveTurbulence","WindMoveTurbulence","41","0","Create","True","0","0","0","False","0","False","Object","-1","","0.35","0.35","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":1927,"pos":[-768,2240],"params":["Inherit","True","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":476,"pos":[992,192],"params":["Inherit","False","WNormal","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1771,"pos":[704,-816],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1774,"pos":[656,-672],"params":["Inherit","False","Property","_EmissionNoiseAdd","EmissionNoiseAdd","47","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WorldNormalVector, AmplifyShaderEditor","id":1649,"pos":[-1136,3056],"params":["Inherit","False","False","1","0","FLOAT3","0,0,1","False","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor","id":1334,"pos":[1784,2240],"params":["Inherit","False","World","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":1948,"pos":[1960,-1240],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":519,"pos":[192,656],"params":["Inherit","False","Property","_MainTexAdd","MainTexAdd","9","0","Create","True","0","0","0","True","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":515,"pos":[144,736],"params":["Inherit","False","Property","_MainTexTint","MainTexTint","7","0","Create","True","0","0","0","True","0","False","Object","-1","","1,1,1,0","1,1,1,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":6,"pos":[-144,544],"params":["Inherit","True","Property","_MainTex","MainTex","6","0","Create","True","0","0","0","False","0","False","","-1","None","None","True","0","False","white","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":652,"pos":[320,-1936],"params":["Inherit","False","Property","_ShadowTransparencyAdd","ShadowTransparencyAdd","17","0","Create","True","0","0","0","True","0","False","Object","-1","","0.5","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":658,"pos":[496,-2160],"params":["Inherit","False","3","0","FLOAT","0","False","1","FLOAT","0.1","False","2","FLOAT","0.12","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1019,"pos":[-112,-2032],"params":["Inherit","False","LightDirection","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1959,"pos":[-2496,4976],"params":["Inherit","False","Property","_Noise1TimeScale","Noise1TimeScale","60","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.LerpOp, AmplifyShaderEditor","id":1960,"pos":[-2448,5104],"params":["Inherit","False","3","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":1928,"pos":[-544,2224],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1772,"pos":[896,-816],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.VertexToFragmentNode, AmplifyShaderEditor","id":1650,"pos":[-944,3056],"params":["Inherit","False","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":1156,"pos":[-912,3184],"params":["Inherit","False","Property","_Rim1Direction","Rim1Direction","32","0","Create","True","0","0","0","False","0","False","Object","-1","","1,-0.3,-0.5","-3.8,3.5,-1","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1332,"pos":[1816,2656],"params":["Inherit","False","476","WNormal","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor","id":1612,"pos":[1400,4336],"params":["Inherit","False","Object","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.NormalVertexDataNode, AmplifyShaderEditor","id":1613,"pos":[1400,4496],"params":["Inherit","False","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.NegateNode, AmplifyShaderEditor","id":1446,"pos":[2488,2256],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":1947,"pos":[1496,-1176],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":514,"pos":[384,672],"params":["Inherit","False","Property","_MainTexMul","MainTexMul","8","0","Create","True","0","0","0","True","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":518,"pos":[384,576],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":517,"pos":[384,752],"params":["Inherit","False","True","True","True","False","1","0","COLOR","0,0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":651,"pos":[688,-2112],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1336,"pos":[1576,2480],"params":["Inherit","False","1019","LightDirection","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleTimeNode, AmplifyShaderEditor","id":1962,"pos":[-2256,4992],"params":["Inherit","False","1","0","FLOAT","1","False","5","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":1961,"pos":[-2272,5088],"params":["Inherit","False","True","True","True","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor","id":1364,"pos":[-960,1856],"params":["Inherit","False","World","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1480,"pos":[-592,1600],"params":["Inherit","False","476","WNormal","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.NegateNode, AmplifyShaderEditor","id":1370,"pos":[-688,1872],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":1386,"pos":[-992,1696],"params":["Inherit","False","Property","_SubserfDir","SubserfDir","39","0","Create","True","0","0","0","False","0","False","Object","-1","","0.28,0.4,-0.21","-1,1.38,0.43","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1930,"pos":[-368,2160],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":1933,"pos":[-768,2032],"params":["Inherit","False","Property","_Fresnel","Fresnel (Mul Add)","52","0","Create","False","0","0","0","False","0","False","Object","-1","","0.3,1","0.3,1","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1944,"pos":[1024,-816],"params":["Inherit","False","EmissionNoise","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RelayNode, AmplifyShaderEditor","id":1648,"pos":[-720,3056],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ViewDirInputsCoordNode, AmplifyShaderEditor","id":1149,"pos":[-784,2896],"params":["Inherit","False","World","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.NormalizeNode, AmplifyShaderEditor","id":1258,"pos":[-704,3184],"params":["Inherit","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.NormalizeNode, AmplifyShaderEditor","id":1425,"pos":[1816,2480],"params":["Inherit","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1345,"pos":[1720,3264],"params":["Inherit","False","Constant","_GlossMul","GlossMul","57","0","Create","True","0","0","0","False","0","False","Object","-1","","6","6","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":1,"pos":[1560,3392],"params":["Inherit","True","Property","_SpecGloss","SpecGloss","5","0","Create","True","0","0","0","False","0","False","","-1","None","None","True","0","False","black","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":1611,"pos":[1640,4384],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ReflectOpNode, AmplifyShaderEditor","id":1445,"pos":[2744,2368],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1783,"pos":[1384,-1464],"params":["Inherit","False","Constant","_Float1","Float 1","76","0","Create","True","0","0","0","False","0","False","Object","-1","","1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":670,"pos":[1544,-1144],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":669,"pos":[1480,-1048],"params":["Inherit","False","Property","_ShadowsColor","ShadowsColor","18","0","Create","True","0","0","0","True","0","False","Object","-1","","0,0,0,0","0.7725491,0.7725491,0.7725491,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":593,"pos":[624,608],"params":["Inherit","False","3","3","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","2","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":653,"pos":[832,-2112],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":1965,"pos":[-2128,4848],"params":["Inherit","False","Property","_Noise1Mul","Noise1Mul","57","0","Create","True","0","0","0","False","0","False","Object","-1","","0.4,0.4,0.4","0.03,0,0.03","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.Vector4Node, AmplifyShaderEditor","id":1966,"pos":[-2352,4496],"params":["Inherit","False","Property","_Offset","Offset","54","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0,0,0","0,0,0,0","0","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1963,"pos":[-2016,5024],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":1964,"pos":[-2112,5200],"params":["Inherit","False","Property","_Noise1SecondaryMul","Noise1SecondaryMul","59","0","Create","True","0","0","0","False","0","False","Object","-1","","5,5,5","3,0.5,3","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":1363,"pos":[-352,1648],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":1371,"pos":[-496,1840],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1931,"pos":[-208,2064],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":1151,"pos":[-576,2928],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TransformDirectionNode, AmplifyShaderEditor","id":1643,"pos":[-544,3184],"params":["Inherit","False","World","View","False","Fast","False","1","0","FLOAT3","0,0,0","False","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1423,"pos":[2056,3232],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1354,"pos":[2280,3504],"params":["Inherit","False","Property","_SpecAdd","SpecAdd","37","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1608,"pos":[1848,4576],"params":["Inherit","False","477","Spec","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1617,"pos":[1816,4448],"params":["Inherit","False","Constant","_ReflectFrresnelPow","ReflectFrresnelPow","70","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1614,"pos":[1800,4336],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor","id":1733,"pos":[2552,4160],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.WorldSpaceCameraPos, AmplifyShaderEditor","id":1737,"pos":[2520,4000],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":1449,"pos":[2984,2496],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.VertexToFragmentNode, AmplifyShaderEditor","id":1784,"pos":[1528,-1464],"params":["Inherit","False","False","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1945,"pos":[1528,-1384],"params":["Inherit","False","1944","EmissionNoise","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":668,"pos":[1720,-1144],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","COLOR","0,0,0,0","False","1","COLOR","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":631,"pos":[816,608],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":644,"pos":[1008,-2112],"params":["Inherit","False","ShaderToon","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1331,"pos":[32,3456],"params":["Inherit","False","1202.463","463.8848","","7","1244","1097","1652","1167","1653","1233","1166","RimLightsColor","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1968,"pos":[-1856,4928],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor","id":1970,"pos":[-2128,4320],"params":["Inherit","False","0","5","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":1969,"pos":[-2160,4496],"params":["Inherit","False","True","True","True","False","1","0","FLOAT4","0,0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1967,"pos":[-1872,5104],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":1385,"pos":[-192,1664],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector2Node, AmplifyShaderEditor","id":1932,"pos":[-256,1440],"params":["Inherit","False","Property","_SkinMulPow","SkinMulPow","51","0","Create","True","0","0","0","False","0","False","Object","-1","","1,0.32","1,0.32","0","3","FLOAT2","0","FLOAT","1","FLOAT","2"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1372,"pos":[-336,1856],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1923,"pos":[-64,1984],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":1157,"pos":[-272,3056],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":1153,"pos":[-288,2928],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Exp2OpNode, AmplifyShaderEditor","id":1433,"pos":[2248,3232],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1355,"pos":[2440,3440],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.PowerNode, AmplifyShaderEditor","id":1616,"pos":[2088,4352],"params":["Inherit","False","False","2","0","FLOAT","0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":1609,"pos":[2120,4512],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":1738,"pos":[2776,4112],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.WorldNormalVector, AmplifyShaderEditor","id":1736,"pos":[2744,4288],"params":["Inherit","False","False","1","0","FLOAT3","0,0,1","False","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.DotProductOpNode, AmplifyShaderEditor","id":1434,"pos":[2440,2640],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1448,"pos":[3144,2496],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":10,"pos":[1432,-1816],"params":["Float","False","Property","_EmissionPower","EmissionPower","12","0","Create","True","0","0","0","True","0","False","Object","-1","","0","1.15","0","5","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":13,"pos":[1432,-1736],"params":["Inherit","True","Property","_Emission","Emission","13","0","Create","True","0","0","0","False","0","False","","-1","None","None","True","0","False","black","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":645,"pos":[1784,-1320],"params":["Inherit","False","644","ShaderToon","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":672,"pos":[1880,-1144],"params":["Inherit","False","True","True","True","False","1","0","COLOR","0,0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1782,"pos":[1736,-1464],"params":["Inherit","False","Property","_UseEmissionNoise","UseEmissionNoise","1","0","Create","True","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT3","0,0,0","False","0","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","4","FLOAT3","0,0,0","False","5","FLOAT3","0,0,0","False","6","FLOAT3","0,0,0","False","7","FLOAT3","0,0,0","False","8","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":592,"pos":[1008,608],"params":["Inherit","False","MainTexMix","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SinOpNode, AmplifyShaderEditor","id":1971,"pos":[-1712,4928],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1975,"pos":[-1840,4368],"params":["Inherit","False","3","3","0","FLOAT","1","False","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SinOpNode, AmplifyShaderEditor","id":1972,"pos":[-1712,5040],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1369,"pos":[-32,1568],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1935,"pos":[112,1872],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1260,"pos":[-48,2944],"params":["Inherit","True","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1262,"pos":[-32,3184],"params":["Inherit","False","Property","_Rim1StepMin","Rim1StepMin","34","0","Create","True","0","0","0","False","0","False","Object","-1","","0.3","0.385","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1263,"pos":[-32,3264],"params":["Inherit","False","Property","_Rim1StepMax","Rim1StepMax","35","0","Create","True","0","0","0","False","0","False","Object","-1","","0.4","0.432","0","1","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1653,"pos":[528,3568],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.PowerNode, AmplifyShaderEditor","id":1444,"pos":[3384,2512],"params":["Inherit","False","False","2","0","FLOAT","0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.VertexToFragmentNode, AmplifyShaderEditor","id":1741,"pos":[2952,4128],"params":["Inherit","False","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1618,"pos":[2280,4432],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.TFHCRemapNode, AmplifyShaderEditor","id":1627,"pos":[1944,4080],"params":["Inherit","False","5","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","6","False","3","FLOAT","4.5","False","4","FLOAT","4","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.VertexToFragmentNode, AmplifyShaderEditor","id":1740,"pos":[2952,4240],"params":["Inherit","False","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RelayNode, AmplifyShaderEditor","id":1940,"pos":[3448,3456],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1435,"pos":[2648,2624],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":332,"pos":[1992,-1816],"params":["Inherit","False","3","3","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":9,"pos":[1928,-2024],"params":["Float","False","Property","_EmissiveColor","EmissiveColor","11","0","Create","True","0","0","0","True","0","False","Object","-1","","0.5019608,0.5019608,0.5019608,1","0.5908214,0.1167675,0.6037736,1","False","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":595,"pos":[2072,-1624],"params":["Inherit","False","592","MainTexMix","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":671,"pos":[2104,-1336],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor","id":1978,"pos":[-1520,4048],"params":["Inherit","False","0","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":1974,"pos":[-1840,4768],"params":["Inherit","False","Property","_Noise1Magnitude","Noise1Magnitude","58","0","Create","True","0","0","0","False","0","False","Object","-1","","0.005,0.001,0.005","0.07,0,0.07","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.WorldToObjectTransfNode, AmplifyShaderEditor","id":1977,"pos":[-1632,4432],"params":["Inherit","False","1","0","FLOAT4","0,0,0,1","False","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1973,"pos":[-1520,4880],"params":["Inherit","True","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.PowerNode, AmplifyShaderEditor","id":1383,"pos":[192,1504],"params":["Inherit","False","False","2","0","FLOAT","0","False","1","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":1377,"pos":[144,1648],"params":["Inherit","False","Property","_SkinSubserfColor","SkinSubserfColor","38","0","Create","True","0","0","0","False","0","False","Object","-1","","1,0.3176471,0.1764706,0","1,0.3176471,0.1764706,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.VertexToFragmentNode, AmplifyShaderEditor","id":1922,"pos":[272,1872],"params":["Inherit","False","False","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SamplerNode, AmplifyShaderEditor","id":1359,"pos":[96,1216],"params":["Inherit","True","Property","_SkinMask","SkinMask","14","0","Create","True","0","0","0","False","0","False","","-1","None","None","True","0","False","black","Auto","False","Object","-1","Auto","Texture2D","False","8","0","SAMPLER2D","","False","1","FLOAT2","0,0","False","2","FLOAT","0","False","3","FLOAT2","0,0","False","4","FLOAT2","0,0","False","5","FLOAT","1","False","6","FLOAT","0","False","7","SAMPLERSTATE","","False","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SmoothstepOpNode, AmplifyShaderEditor","id":1259,"pos":[320,2944],"params":["Inherit","True","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT","1","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor","id":1636,"pos":[384,3184],"params":["Inherit","False","0","5","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1652,"pos":[752,3568],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1619,"pos":[2472,4544],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1624,"pos":[2392,4688],"params":["Inherit","False","Property","_EnvIntensity","EnvIntensity","43","0","Create","True","0","0","0","False","0","False","Object","-1","","0.5","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1451,"pos":[3656,2528],"params":["Inherit","False","3","3","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":2004,"pos":[3240,4376],"params":["Inherit","False","SimpleReflectionProbeFunction","-1","","33","6e3fb4cd50c1541de89d09546bbb0559","0","3","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","4","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":11,"pos":[2200,-1912],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1635,"pos":[2280,-1624],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":1952,"pos":[1344,1008],"params":["Inherit","False","2500","1022.95","","28","1602","1113","1251","1626","492","1350","1256","1034","1252","1876","1878","1250","1361","1797","1879","1604","1918","1421","1601","1102","1874","1253","1802","1824","1402","1631","744","1996","MainPass","0.495283,1,0.7221011,1","0","0"]}
{"type":"AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor","id":1976,"pos":[-1216,4144],"params":["Inherit","False","0","5","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1980,"pos":[-1184,4336],"params":["Inherit","False","Property","_Twist","Twist","53","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":2000,"pos":[-1328,4320],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":1999,"pos":[-1376,4528],"params":["Inherit","False","True","True","True","False","1","0","FLOAT4","0,0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1979,"pos":[-1280,4720],"params":["Inherit","False","3","3","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1378,"pos":[464,1536],"params":["Inherit","False","4","4","0","FLOAT","0","False","1","FLOAT","0","False","2","FLOAT3","0,0,0","False","3","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1289,"pos":[640,2944],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1244,"pos":[944,3568],"params":["Inherit","False","RimMask","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":477,"pos":[3864,2512],"params":["Inherit","False","Spec","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1623,"pos":[3648,4504],"params":["Inherit","False","4","4","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":646,"pos":[2456,-1912],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":1981,"pos":[-992,4144],"params":["Inherit","False","Property","_TwistVector","TwistVector","56","0","Create","True","0","0","0","False","0","False","Object","-1","","0,1,0","0,1,0","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1984,"pos":[-960,4304],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.Vector3Node, AmplifyShaderEditor","id":1985,"pos":[-1024,4416],"params":["Inherit","False","Property","_TwistPivotOffset","TwistPivotOffset","55","0","Create","True","0","0","0","False","0","False","Object","-1","","0,0,0","0,1,0","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1982,"pos":[-928,4576],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.WorldToObjectTransfNode, AmplifyShaderEditor","id":1983,"pos":[-1040,4720],"params":["Inherit","False","1","0","FLOAT4","0,0,0,1","False","5","FLOAT4","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1384,"pos":[640,1536],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":1602,"pos":[1392,1264],"params":["Inherit","False","Property","_SpecColorMul","SpecColorMul","40","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1,1,0","1,1,1,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1113,"pos":[1424,1184],"params":["Inherit","False","477","Spec","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1251,"pos":[1424,1072],"params":["Inherit","False","1244","RimMask","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1213,"pos":[832,2944],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1625,"pos":[3864,4480],"params":["Inherit","False","EnvReflect","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":491,"pos":[2664,-1912],"params":["Inherit","False","Emission","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RotateAboutAxisNode, AmplifyShaderEditor","id":1987,"pos":[-720,4336],"params":["Inherit","False","False","4","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":1986,"pos":[-800,4720],"params":["Inherit","False","True","True","True","False","1","0","FLOAT4","0,0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1626,"pos":[1616,1376],"params":["Inherit","False","1625","EnvReflect","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":492,"pos":[1616,1456],"params":["Inherit","False","491","Emission","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1350,"pos":[1648,1264],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":1256,"pos":[1632,1072],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1387,"pos":[800,1504],"params":["Inherit","False","Property","_SubserfON","SubserfON","0","0","Create","True","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT3","0,0,0","False","0","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","4","FLOAT3","0,0,0","False","5","FLOAT3","0,0,0","False","6","FLOAT3","0,0,0","False","7","FLOAT3","0,0,0","False","8","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1230,"pos":[1008,2944],"params":["Inherit","False","Rim1Mask","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.VertexToFragmentNode, AmplifyShaderEditor","id":1989,"pos":[-128,4528],"params":["Inherit","False","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.VertexToFragmentNode, AmplifyShaderEditor","id":1988,"pos":[-384,4336],"params":["Inherit","False","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1360,"pos":[1056,1504],"params":["Inherit","False","Skin","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1034,"pos":[1840,1344],"params":["Inherit","False","3","3","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":1252,"pos":[1808,1072],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1876,"pos":[1616,1904],"params":["Inherit","False","1875","SmoothNormal","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ObjectToWorldMatrixNode, AmplifyShaderEditor","id":1878,"pos":[1648,1824],"params":["Inherit","False","0","1","FLOAT4x4","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":1166,"pos":[96,3664],"params":["Inherit","False","Property","_Rim1Color","Rim1Color","33","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1,1,0","0.7725491,0.8313726,0.8431373,1","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1233,"pos":[128,3568],"params":["Inherit","False","1230","Rim1Mask","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1991,"pos":[112,4464],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":1990,"pos":[-112,4288],"params":["Inherit","False","True","True","True","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1250,"pos":[2016,1328],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1361,"pos":[2000,1456],"params":["Inherit","False","1360","Skin","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.WorldPosInputsNode, AmplifyShaderEditor","id":1797,"pos":[1936,1696],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1879,"pos":[1872,1872],"params":["Inherit","False","2","2","0","FLOAT4x4","1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1167,"pos":[528,3680],"params":["Inherit","False","3","3","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.WireNode, AmplifyShaderEditor","id":1998,"pos":[-112,4096],"params":["Inherit","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1992,"pos":[256,4288],"params":["Inherit","False","Property","_UseWind","UseWind","4","0","Create","True","0","0","0","False","0","False","","0","1","1","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT3","0,0,0","False","0","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","4","FLOAT3","0,0,0","False","5","FLOAT3","0,0,0","False","6","FLOAT3","0,0,0","False","7","FLOAT3","0,0,0","False","8","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1604,"pos":[2224,1344],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":1421,"pos":[2176,1536],"params":["Inherit","False","Property","_AmbientColor","AmbientColor","42","0","Create","True","0","0","0","False","0","False","Object","-1","","1,1,1,0","1,1,1,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":1918,"pos":[2192,1744],"params":["Inherit","False","2CustomPiontLightsShaderFunction","-1","","1","0e4a84a21c15e497497ce26ef71832b3","0","2","109","FLOAT3","0,0,0","False","110","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1097,"pos":[736,3680],"params":["Inherit","False","RimLights","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":1993,"pos":[512,4144],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":1601,"pos":[2416,1344],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1102,"pos":[2384,1248],"params":["Inherit","False","1097","RimLights","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.VertexToFragmentNode, AmplifyShaderEditor","id":1874,"pos":[2576,1744],"params":["Inherit","False","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1994,"pos":[688,4128],"params":["Inherit","False","Property","_UseDynamics","UseDynamics","3","0","Create","True","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT3","0,0,0","False","0","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","4","FLOAT3","0,0,0","False","5","FLOAT3","0,0,0","False","6","FLOAT3","0,0,0","False","7","FLOAT3","0,0,0","False","8","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1253,"pos":[2656,1328],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1802,"pos":[2816,1616],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":686,"pos":[1360,-720],"params":["Inherit","False","3167.468","1652.447","","54","743","724","1632","1472","1742","1744","687","688","690","691","695","692","698","762","972","1417","969","1416","971","696","702","1950","713","965","699","714","707","708","710","701","725","721","967","720","719","722","966","706","705","703","704","711","697","694","752","751","750","749","748","747","746","745","975","974","OutlinePass","0.6367924,1,0.6769876,1","0","0"]}
{"type":"AmplifyShaderEditor.CommentaryNode, AmplifyShaderEditor","id":936,"pos":[96,-1712],"params":["Inherit","False","1159.905","659.4207","","11","758","769","768","764","763","767","757","755","759","760","1997","VertexOffset","1,1,1,1","0","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1995,"pos":[976,4128],"params":["Inherit","False","DynamicOffset","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.StaticSwitch, AmplifyShaderEditor","id":1824,"pos":[2960,1328],"params":["Inherit","False","Property","_UseCustomLights","UseCustomLights","2","0","Create","True","0","0","0","False","0","False","","0","0","0","True","","Toggle","2","Key0","Key1","Create","True","True","All","9","1","FLOAT3","0,0,0","False","0","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","3","FLOAT3","0,0,0","False","4","FLOAT3","0,0,0","False","5","FLOAT3","0,0,0","False","6","FLOAT3","0,0,0","False","7","FLOAT3","0,0,0","False","8","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.StickyNoteNode, AmplifyShaderEditor","id":1942,"pos":[496,-2256],"params":["Inherit","False","150","100","New Note","","1,0.7157989,0,1","Was a test\nCan be optimized","0","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":769,"pos":[144,-1664],"params":["Inherit","False","Property","_RandomSizeValue","RandomSizeValue","15","0","Create","True","0","0","0","True","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":758,"pos":[176,-1568],"params":["Inherit","False","Property","_SizeRandomMul","SizeRandomMul","29","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.ObjectToWorldMatrixNode, AmplifyShaderEditor","id":704,"pos":[1392,-672],"params":["Inherit","False","0","1","FLOAT4x4","0"]}
{"type":"AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor","id":703,"pos":[1392,-592],"params":["Inherit","False","0","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":764,"pos":[432,-1584],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":768,"pos":[368,-1488],"params":["Inherit","False","Property","_SizeRandomAdd","SizeRandomAdd","30","0","Create","True","0","0","0","False","0","False","Object","-1","","0","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":705,"pos":[1568,-640],"params":["Inherit","False","2","2","0","FLOAT4x4","0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":767,"pos":[624,-1504],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NormalVertexDataNode, AmplifyShaderEditor","id":763,"pos":[368,-1392],"params":["Inherit","False","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.BreakToComponentsNode, AmplifyShaderEditor","id":706,"pos":[1728,-640],"params":["Inherit","False","FLOAT3","1","0","FLOAT3","0,0,0","False","16","FLOAT","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT","5","FLOAT","6","FLOAT","7","FLOAT","8","FLOAT","9","FLOAT","10","FLOAT","11","FLOAT","12","FLOAT","13","FLOAT","14","FLOAT","15"]}
{"type":"AmplifyShaderEditor.WorldSpaceCameraPos, AmplifyShaderEditor","id":687,"pos":[2064,736],"params":["Inherit","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.ObjectScaleNode, AmplifyShaderEditor","id":724,"pos":[1552,48],"params":["Inherit","False","False","0","4","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3"]}
{"type":"AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor","id":755,"pos":[368,-1248],"params":["Inherit","False","0","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":757,"pos":[752,-1424],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":965,"pos":[2016,-176],"params":["Inherit","False","Property","_OutlineAdd","OutlineAdd","25","0","Create","True","0","0","0","False","0","False","Object","-1","","0","-0.98","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":713,"pos":[1872,-544],"params":["Inherit","False","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.TransformPositionNode, AmplifyShaderEditor","id":688,"pos":[2288,736],"params":["Inherit","False","World","Object","False","Fast","True","1","0","FLOAT3","0,0,0","False","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1997,"pos":[704,-1168],"params":["Inherit","False","1995","DynamicOffset","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":759,"pos":[928,-1344],"params":["Inherit","False","3","3","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":966,"pos":[2224,-224],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":722,"pos":[2176,-112],"params":["Inherit","False","Property","_OutlineMul","OutlineMul","26","0","Create","True","0","0","0","False","0","False","Object","-1","","1","2.26","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.VertexColorNode, AmplifyShaderEditor","id":969,"pos":[2096,144],"params":["Inherit","False","0","5","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1417,"pos":[2096,336],"params":["Inherit","False","Property","_OutlineModAdd","OutlineModAdd","23","0","Create","True","0","0","0","False","0","False","Object","-1","","-1","0","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":690,"pos":[2560,704],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":760,"pos":[1040,-1344],"params":["Inherit","False","newVertexOffset","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":719,"pos":[2368,-624],"params":["Inherit","False","Property","_Color1","Color1","27","0","Create","True","0","0","0","True","0","False","Object","-1","","0,0,0,0","0,0,0,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.ColorNode, AmplifyShaderEditor","id":720,"pos":[2368,-416],"params":["Inherit","False","Property","_Color2","Color2","28","0","Create","True","0","0","0","True","0","False","Object","-1","","0,0,0,0","0.4622642,0,0.4255767,0","True","True","0","6","COLOR","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4","FLOAT3","5"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":967,"pos":[2368,-144],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1950,"pos":[2144,16],"params":["Inherit","False","1875","SmoothNormal","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":1416,"pos":[2384,224],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":972,"pos":[2352,352],"params":["Inherit","False","Property","_OutlineModMul","OutlineModMul","24","0","Create","True","0","0","0","False","0","False","Object","-1","","-1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":692,"pos":[2672,608],"params":["Inherit","False","Property","_DepthCompensation","DepthCompensation","21","0","Create","True","0","0","0","True","0","False","Object","-1","","0.02","0.02","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.NormalizeNode, AmplifyShaderEditor","id":691,"pos":[2736,720],"params":["Inherit","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleDivideOpNode, AmplifyShaderEditor","id":694,"pos":[2432,32],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":721,"pos":[2592,-400],"params":["Inherit","False","True","True","True","False","1","0","COLOR","0,0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ComponentMaskNode, AmplifyShaderEditor","id":725,"pos":[2624,-608],"params":["Inherit","False","True","True","True","False","1","0","COLOR","0,0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SaturateNode, AmplifyShaderEditor","id":701,"pos":[2544,-208],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.OneMinusNode, AmplifyShaderEditor","id":702,"pos":[2784,-208],"params":["Inherit","False","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":696,"pos":[2544,144],"params":["Inherit","False","Property","_OutlineWidth","OutlineWidth","22","0","Create","True","0","0","0","True","0","False","Object","-1","","0.2","0.003","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":971,"pos":[2560,240],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":762,"pos":[2288,528],"params":["Inherit","False","760","newVertexOffset","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":695,"pos":[2912,656],"params":["Inherit","False","2","2","0","FLOAT","0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":697,"pos":[2816,96],"params":["Inherit","False","3","3","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":710,"pos":[2944,-480],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleMultiplyOpNode, AmplifyShaderEditor","id":708,"pos":[2976,-352],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":698,"pos":[3104,528],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":707,"pos":[3216,-320],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.SimpleAddOpNode, AmplifyShaderEditor","id":699,"pos":[3120,224],"params":["Inherit","False","2","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.VertexToFragmentNode, AmplifyShaderEditor","id":711,"pos":[3296,144],"params":["Inherit","False","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.VertexToFragmentNode, AmplifyShaderEditor","id":714,"pos":[3584,-96],"params":["Inherit","False","False","False","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.PosVertexDataNode, AmplifyShaderEditor","id":1742,"pos":[3328,272],"params":["Inherit","False","0","0","5","FLOAT3","0","FLOAT","1","FLOAT","2","FLOAT","3","FLOAT","4"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1362,"pos":[-1024,1584],"params":["Inherit","False","1019","LightDirection","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RangedFloatNode, AmplifyShaderEditor","id":1295,"pos":[448,-16],"params":["Inherit","False","Property","_NormalScale","NormalScale","36","0","Create","True","0","0","0","False","0","False","Object","-1","","1","1","0","0","0","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1401,"pos":[-256,-528],"params":["Inherit","False","Test","-1","True","1","0","FLOAT","0","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.SimpleSubtractOpNode, AmplifyShaderEditor","id":1744,"pos":[3568,144],"params":["Inherit","False","2","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.RegisterLocalVarNode, AmplifyShaderEditor","id":1472,"pos":[3984,-192],"params":["Inherit","False","OutlineColor","-1","True","1","0","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1402,"pos":[3232,1104],"params":["Inherit","False","1401","Test","1","0","OBJECT","","False","1","FLOAT","0"]}
{"type":"AmplifyShaderEditor.GetLocalVarNode, AmplifyShaderEditor","id":1996,"pos":[3360,1488],"params":["Inherit","False","1995","DynamicOffset","1","0","OBJECT","","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":1632,"pos":[3920,-96],"params":["Inherit","False","MetaFadeShaderFunction","-1","","30","838cc0d09f465448c85147043bcd00b9","0","1","3","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.FunctionNode, AmplifyShaderEditor","id":1631,"pos":[3280,1328],"params":["Inherit","False","MetaFadeShaderFunction","-1","","31","838cc0d09f465448c85147043bcd00b9","0","1","3","FLOAT3","0,0,0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.ReflectionProbeNode, AmplifyShaderEditor","id":1607,"pos":[3248,4152],"params":["Inherit","True","3","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.CustomExpressionNode, AmplifyShaderEditor","id":2001,"pos":[3312,4032],"params":["Inherit","False","return DecodeHDREnvironment(\n    SAMPLE_TEXTURECUBE_LOD(\n        unity_SpecCube0,\n        samplerunity_SpecCube0,\n        reflect(-normalize(ViewDirWS), normalize(NormalWS)),\n        Lod\n    ),\n    unity_SpecCube0_HDR\n);","3","Create","3","True","ViewDirWS","FLOAT3","0,0,0","In","","Inherit","False","True","NormalWS","FLOAT3","0,0,0","In","","Inherit","False","True","Lod","FLOAT","0","In","","Inherit","False","Custom Reflection Probe","True","False","0","","False","3","0","FLOAT3","0,0,0","False","1","FLOAT3","0,0,0","False","2","FLOAT","0","False","1","FLOAT3","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":1941,"pos":[5968,1720],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","21","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","GBuffer","0","12","GBuffer","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","True","1","False","","255","False","","255","False","","7","False","","3","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=UniversalGBuffer","False","True","12","d3d11","gles","metal","vulkan","xboxone","xboxseries","playstation","ps4","ps5","switch","switch2","webgpu","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":974,"pos":[2112,452],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","21","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","MotionVectors","0","10","MotionVectors","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","False","False","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=MotionVectors","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":975,"pos":[2112,452],"params":["Float","False","False","-1","3","UnityEditor.ShaderGraphUnlitGUI","0","21","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","XRMotionVectors","0","11","XRMotionVectors","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","True","1","False","","255","False","","1","False","","7","False","","3","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","False","False","False","False","True","1","LightMode=XRMotionVectors","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":745,"pos":[3360,-144],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","21","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","ShadowCaster","0","2","ShadowCaster","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","True","False","False","False","False","0","False","","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=ShadowCaster","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":746,"pos":[3360,-144],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","21","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","DepthOnly","0","3","DepthOnly","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","True","True","False","False","False","0","False","","False","False","False","False","False","False","False","False","False","True","1","False","","False","False","False","True","1","LightMode=DepthOnly","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":747,"pos":[3360,-144],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","21","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","Meta","0","4","Meta","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=Meta","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":748,"pos":[3360,-144],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","21","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","Universal2D","0","5","Universal2D","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=Universal2D","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":749,"pos":[3360,-144],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","21","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","SceneSelectionPass","0","6","SceneSelectionPass","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","2","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=SceneSelectionPass","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":750,"pos":[3360,-144],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","21","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","ScenePickingPass","0","7","ScenePickingPass","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","LightMode=Picking","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":751,"pos":[3360,-144],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","21","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","DepthNormals","0","8","DepthNormals","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=DepthNormals","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":752,"pos":[3360,-144],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","21","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","DepthNormalsOnly","0","9","DepthNormalsOnly","0","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","False","True","1","False","","True","3","False","","False","False","True","1","LightMode=DepthNormalsOnly","False","True","11","d3d11","metal","vulkan","xboxone","xboxseries","playstation","ps4","ps5","switch","switch2","webgpu","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":743,"pos":[4288,-64],"params":["Float","False","False","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","21","New Amplify Shader","2992e84f91cbeb14eab234972e07ea9d","True","ExtraPrePass","0","0","Outline","6","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","14","all","0","True","False","False","False","False","False","False","False","False","False","False","False","False","True","True","1","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","True","True","True","1","False","","255","False","","255","False","","7","False","","3","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=CustomOutlineMode","False","False","0","","0","0","Standard","0","False","0"]}
{"type":"AmplifyShaderEditor.TemplateMultiPassMasterNode, AmplifyShaderEditor","id":744,"pos":[3600,1328],"params":["Half","False","True","-1","2","UnityEditor.ShaderGraphUnlitGUI","0","21","Eclipse/Characters/CharactersBaseCellOutlineMetaShader","2992e84f91cbeb14eab234972e07ea9d","True","Forward","0","1","Forward","12","False","True","1","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","False","True","False","0","False","","255","False","","255","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","False","False","False","True","4","RenderPipeline=UniversalPipeline","RenderType=Opaque=RenderType","Queue=Geometry=Queue=0","UniversalMaterialType=Unlit","True","5","True","6","d3d11","glcore","gles","gles3","metal","vulkan","0","False","True","0","1","False","","0","False","","1","1","False","","0","False","","True","1","False","","1","False","","False","False","False","False","False","False","False","False","False","False","False","True","0","False","","False","True","True","True","True","True","0","False","","False","False","False","False","False","False","True","True","True","1","False","","255","False","","255","False","","7","False","","3","False","","0","False","","0","False","","0","False","","0","False","","0","False","","0","False","","False","True","1","False","","True","3","False","","True","True","0","False","","0","False","","False","True","1","LightMode=UniversalForward","False","False","0","","0","0","Standard","33","Surface","0","0","  Keep Alpha","0","0","  Blend","0","0","Two Sided","1","0","Alpha Clipping","0","639047798981297040","  Use Shadow Threshold","0","0","Fragment Normal Space","0","0","Forward Only","0","639047799019360680","Cast Shadows","0","638999187113204410","Receive Shadows","0","638719392710949480","Receive SSAO","0","639147153902995150","Default Decal Blending","0","639147153897528980","Motion Vectors","0","638863467973922390","  Additional Motion Vectors","1","0","  Alembic Motion Vectors","0","0","  XR Motion Vectors","0","0","GPU Instancing","0","639131490470418080","LOD CrossFade","0","638719392733838990","Built-in Fog","0","639195496783010650","Meta Pass","0","0","Extra Pre Pass","1","638732242673142130","Tessellation","0","0","  Phong","0","0","  Strength","0.5,False,","0","  Type","0","0","  Tess","16,False,","0","  Min","10,False,","0","  Max","25,False,","0","  Edge Length","16,False,","0","  Max Displacement","25,False,","0","Write Depth","0","0","  Conservative","0","0","Vertex Position","1","639114216284034270","0","13","True","True","False","False","False","False","False","False","False","False","False","False","False","False","","False","0"]}
{"wire":[1821,0,1805,0]}
{"wire":[1821,1,1822,0]}
{"wire":[1751,0,1749,1]}
{"wire":[1751,1,1750,1]}
{"wire":[1751,2,1749,2]}
{"wire":[1766,0,1755,0]}
{"wire":[1766,1,1767,0]}
{"wire":[1812,0,1818,0]}
{"wire":[1812,1,1811,0]}
{"wire":[1820,0,1821,0]}
{"wire":[1767,0,1754,0]}
{"wire":[1756,0,1751,0]}
{"wire":[1768,0,1766,0]}
{"wire":[1768,1,1754,4]}
{"wire":[1817,0,1812,0]}
{"wire":[1823,0,1820,0]}
{"wire":[1823,1,1822,0]}
{"wire":[1760,0,1755,0]}
{"wire":[1760,1,1756,2]}
{"wire":[1757,0,1767,0]}
{"wire":[1757,1,1756,0]}
{"wire":[1759,0,1768,0]}
{"wire":[1759,1,1756,1]}
{"wire":[1813,0,1817,0]}
{"wire":[1813,1,1811,0]}
{"wire":[1818,0,1823,0]}
{"wire":[1761,0,1757,0]}
{"wire":[1761,1,1759,0]}
{"wire":[1761,2,1760,0]}
{"wire":[1814,0,1818,0]}
{"wire":[1814,1,1813,0]}
{"wire":[1814,2,1816,0]}
{"wire":[1762,0,1761,0]}
{"wire":[1781,0,1780,0]}
{"wire":[1781,1,1778,0]}
{"wire":[1804,0,1777,0]}
{"wire":[1804,1,1814,0]}
{"wire":[1875,0,1762,0]}
{"wire":[1308,0,8,0]}
{"wire":[1806,0,1781,0]}
{"wire":[1806,1,1804,0]}
{"wire":[635,0,1312,0]}
{"wire":[635,1,973,0]}
{"wire":[1776,0,1806,0]}
{"wire":[649,0,650,0]}
{"wire":[649,1,635,0]}
{"wire":[1939,0,1938,0]}
{"wire":[1939,1,1926,0]}
{"wire":[1368,0,1367,0]}
{"wire":[1770,1,1776,0]}
{"wire":[636,0,649,0]}
{"wire":[1956,0,1955,3]}
{"wire":[1956,1,1955,1]}
{"wire":[1956,2,1955,1]}
{"wire":[1957,0,1954,3]}
{"wire":[1957,1,1954,1]}
{"wire":[1957,2,1954,1]}
{"wire":[1927,0,1924,0]}
{"wire":[1927,1,1939,0]}
{"wire":[476,0,1368,0]}
{"wire":[1771,0,1770,5]}
{"wire":[1771,1,1773,0]}
{"wire":[1948,0,645,0]}
{"wire":[658,0,636,0]}
{"wire":[658,1,679,0]}
{"wire":[658,2,680,0]}
{"wire":[1019,0,973,0]}
{"wire":[1960,0,1957,0]}
{"wire":[1960,1,1956,0]}
{"wire":[1960,2,1958,0]}
{"wire":[1928,0,1927,0]}
{"wire":[1772,0,1771,0]}
{"wire":[1772,1,1774,0]}
{"wire":[1650,0,1649,0]}
{"wire":[1446,0,1334,0]}
{"wire":[1947,0,1948,0]}
{"wire":[518,0,6,5]}
{"wire":[518,1,519,0]}
{"wire":[517,0,515,0]}
{"wire":[651,0,658,0]}
{"wire":[651,1,652,0]}
{"wire":[1962,0,1959,0]}
{"wire":[1961,0,1960,0]}
{"wire":[1370,0,1364,0]}
{"wire":[1930,0,1933,2]}
{"wire":[1930,1,1928,0]}
{"wire":[1944,0,1772,0]}
{"wire":[1648,0,1650,0]}
{"wire":[1258,0,1156,0]}
{"wire":[1425,0,1336,0]}
{"wire":[1611,0,1612,0]}
{"wire":[1611,1,1613,0]}
{"wire":[1445,0,1446,0]}
{"wire":[1445,1,1332,0]}
{"wire":[670,0,1947,0]}
{"wire":[593,0,518,0]}
{"wire":[593,1,514,0]}
{"wire":[593,2,517,0]}
{"wire":[653,0,651,0]}
{"wire":[1963,0,1962,0]}
{"wire":[1963,1,1961,0]}
{"wire":[1363,0,1480,0]}
{"wire":[1363,1,1364,0]}
{"wire":[1371,0,1386,0]}
{"wire":[1371,1,1370,0]}
{"wire":[1931,0,1933,1]}
{"wire":[1931,1,1930,0]}
{"wire":[1151,0,1149,0]}
{"wire":[1151,1,1648,0]}
{"wire":[1643,0,1258,0]}
{"wire":[1423,0,1,4]}
{"wire":[1423,1,1345,0]}
{"wire":[1614,0,1611,0]}
{"wire":[1449,0,1445,0]}
{"wire":[1449,1,1425,0]}
{"wire":[1784,0,1783,0]}
{"wire":[668,0,670,0]}
{"wire":[668,1,669,0]}
{"wire":[631,0,593,0]}
{"wire":[644,0,653,0]}
{"wire":[1968,0,1965,0]}
{"wire":[1968,1,1963,0]}
{"wire":[1969,0,1966,0]}
{"wire":[1967,0,1963,0]}
{"wire":[1967,1,1964,0]}
{"wire":[1385,0,1363,0]}
{"wire":[1372,0,1371,0]}
{"wire":[1923,0,1931,0]}
{"wire":[1157,0,1648,0]}
{"wire":[1157,1,1643,0]}
{"wire":[1153,0,1151,0]}
{"wire":[1433,0,1423,0]}
{"wire":[1355,0,1,5]}
{"wire":[1355,1,1354,0]}
{"wire":[1616,0,1614,0]}
{"wire":[1616,1,1617,0]}
{"wire":[1609,0,1608,0]}
{"wire":[1738,0,1737,0]}
{"wire":[1738,1,1733,0]}
{"wire":[1434,0,1332,0]}
{"wire":[1434,1,1425,0]}
{"wire":[1448,0,1449,0]}
{"wire":[672,0,668,0]}
{"wire":[1782,1,1784,0]}
{"wire":[1782,0,1945,0]}
{"wire":[592,0,631,0]}
{"wire":[1971,0,1968,0]}
{"wire":[1975,0,1970,3]}
{"wire":[1975,1,1969,0]}
{"wire":[1975,2,1970,3]}
{"wire":[1972,0,1967,0]}
{"wire":[1369,0,1932,1]}
{"wire":[1369,1,1385,0]}
{"wire":[1935,0,1372,0]}
{"wire":[1935,1,1923,0]}
{"wire":[1260,0,1153,0]}
{"wire":[1260,1,1157,0]}
{"wire":[1653,0,1233,0]}
{"wire":[1653,1,1166,4]}
{"wire":[1444,0,1448,0]}
{"wire":[1444,1,1433,0]}
{"wire":[1741,0,1738,0]}
{"wire":[1618,0,1616,0]}
{"wire":[1618,1,1609,0]}
{"wire":[1627,0,1433,0]}
{"wire":[1627,2,1345,0]}
{"wire":[1740,0,1736,0]}
{"wire":[1940,0,1355,0]}
{"wire":[1435,0,1434,0]}
{"wire":[332,0,10,0]}
{"wire":[332,1,13,5]}
{"wire":[332,2,1782,0]}
{"wire":[671,0,645,0]}
{"wire":[671,1,672,0]}
{"wire":[1977,0,1975,0]}
{"wire":[1973,0,1971,0]}
{"wire":[1973,1,1972,0]}
{"wire":[1383,0,1369,0]}
{"wire":[1383,1,1932,2]}
{"wire":[1922,0,1935,0]}
{"wire":[1259,0,1260,0]}
{"wire":[1259,1,1262,0]}
{"wire":[1259,2,1263,0]}
{"wire":[1652,0,1653,0]}
{"wire":[1619,0,1618,0]}
{"wire":[1619,1,1608,0]}
{"wire":[1451,0,1444,0]}
{"wire":[1451,1,1435,0]}
{"wire":[1451,2,1940,0]}
{"wire":[2004,2,1741,0]}
{"wire":[2004,3,1740,0]}
{"wire":[2004,4,1627,0]}
{"wire":[11,0,9,5]}
{"wire":[11,1,332,0]}
{"wire":[1635,0,595,0]}
{"wire":[1635,1,671,0]}
{"wire":[2000,0,1978,0]}
{"wire":[1999,0,1977,0]}
{"wire":[1979,0,1970,3]}
{"wire":[1979,1,1974,0]}
{"wire":[1979,2,1973,0]}
{"wire":[1378,0,1359,1]}
{"wire":[1378,1,1383,0]}
{"wire":[1378,2,1377,5]}
{"wire":[1378,3,1922,0]}
{"wire":[1289,0,1259,0]}
{"wire":[1289,1,1636,1]}
{"wire":[1244,0,1652,0]}
{"wire":[477,0,1451,0]}
{"wire":[1623,0,2004,0]}
{"wire":[1623,1,1619,0]}
{"wire":[1623,2,1940,0]}
{"wire":[1623,3,1624,0]}
{"wire":[646,0,11,0]}
{"wire":[646,1,1635,0]}
{"wire":[1984,0,1976,3]}
{"wire":[1984,1,1980,0]}
{"wire":[1982,0,2000,0]}
{"wire":[1982,1,1999,0]}
{"wire":[1983,0,1979,0]}
{"wire":[1384,0,1378,0]}
{"wire":[1213,0,1289,0]}
{"wire":[1625,0,1623,0]}
{"wire":[491,0,646,0]}
{"wire":[1987,0,1981,0]}
{"wire":[1987,1,1984,0]}
{"wire":[1987,2,1985,0]}
{"wire":[1987,3,1982,0]}
{"wire":[1986,0,1983,0]}
{"wire":[1350,0,1113,0]}
{"wire":[1350,1,1602,5]}
{"wire":[1256,0,1251,0]}
{"wire":[1387,0,1384,0]}
{"wire":[1230,0,1213,0]}
{"wire":[1989,0,1986,0]}
{"wire":[1988,0,1987,0]}
{"wire":[1360,0,1387,0]}
{"wire":[1034,0,1350,0]}
{"wire":[1034,1,1626,0]}
{"wire":[1034,2,492,0]}
{"wire":[1252,0,1256,0]}
{"wire":[1991,0,1990,0]}
{"wire":[1991,1,1989,0]}
{"wire":[1990,0,1988,0]}
{"wire":[1250,0,1252,0]}
{"wire":[1250,1,1034,0]}
{"wire":[1879,0,1878,0]}
{"wire":[1879,1,1876,0]}
{"wire":[1167,0,1233,0]}
{"wire":[1167,1,1166,5]}
{"wire":[1167,2,1166,4]}
{"wire":[1998,0,1978,0]}
{"wire":[1992,1,1990,0]}
{"wire":[1992,0,1991,0]}
{"wire":[1604,0,1250,0]}
{"wire":[1604,1,1361,0]}
{"wire":[1918,109,1797,0]}
{"wire":[1918,110,1879,0]}
{"wire":[1097,0,1167,0]}
{"wire":[1993,0,1992,0]}
{"wire":[1993,1,1998,0]}
{"wire":[1601,0,1604,0]}
{"wire":[1601,1,1421,5]}
{"wire":[1874,0,1918,0]}
{"wire":[1994,0,1993,0]}
{"wire":[1253,0,1102,0]}
{"wire":[1253,1,1601,0]}
{"wire":[1802,0,1253,0]}
{"wire":[1802,1,1874,0]}
{"wire":[1995,0,1994,0]}
{"wire":[1824,1,1253,0]}
{"wire":[1824,0,1802,0]}
{"wire":[764,0,769,0]}
{"wire":[764,1,758,0]}
{"wire":[705,0,704,0]}
{"wire":[705,1,703,0]}
{"wire":[767,0,764,0]}
{"wire":[767,1,768,0]}
{"wire":[706,0,705,0]}
{"wire":[757,0,767,0]}
{"wire":[757,1,763,0]}
{"wire":[713,0,706,1]}
{"wire":[713,1,724,2]}
{"wire":[688,0,687,0]}
{"wire":[759,0,757,0]}
{"wire":[759,1,755,0]}
{"wire":[759,2,1997,0]}
{"wire":[966,0,713,0]}
{"wire":[966,1,965,0]}
{"wire":[690,0,762,0]}
{"wire":[690,1,688,0]}
{"wire":[760,0,759,0]}
{"wire":[967,0,966,0]}
{"wire":[967,1,722,0]}
{"wire":[1416,0,969,2]}
{"wire":[1416,1,1417,0]}
{"wire":[691,0,690,0]}
{"wire":[694,0,1950,0]}
{"wire":[694,1,724,0]}
{"wire":[721,0,720,0]}
{"wire":[725,0,719,0]}
{"wire":[701,0,967,0]}
{"wire":[702,0,701,0]}
{"wire":[971,0,1416,0]}
{"wire":[971,1,972,0]}
{"wire":[695,0,692,0]}
{"wire":[695,1,691,0]}
{"wire":[697,0,694,0]}
{"wire":[697,1,696,0]}
{"wire":[697,2,971,0]}
{"wire":[710,0,725,0]}
{"wire":[710,1,701,0]}
{"wire":[708,0,721,0]}
{"wire":[708,1,702,0]}
{"wire":[698,0,762,0]}
{"wire":[698,1,695,0]}
{"wire":[707,0,710,0]}
{"wire":[707,1,708,0]}
{"wire":[699,0,697,0]}
{"wire":[699,1,698,0]}
{"wire":[711,0,699,0]}
{"wire":[714,0,707,0]}
{"wire":[1401,0,1814,0]}
{"wire":[1744,0,711,0]}
{"wire":[1744,1,1742,0]}
{"wire":[1472,0,714,0]}
{"wire":[1632,3,714,0]}
{"wire":[1631,3,1824,0]}
{"wire":[1607,0,1741,0]}
{"wire":[1607,1,1740,0]}
{"wire":[1607,2,1627,0]}
{"wire":[2001,0,1741,0]}
{"wire":[2001,1,1740,0]}
{"wire":[2001,2,1627,0]}
{"wire":[743,0,1632,0]}
{"wire":[743,3,1744,0]}
{"wire":[744,2,1631,0]}
{"wire":[744,5,1996,0]}
ASEEND*/
//CHKSM=DA9E5E0A4AB21D58D513737D0BE5602FEE7F740B