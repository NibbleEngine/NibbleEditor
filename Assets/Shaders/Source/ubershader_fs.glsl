/*  Version and extension are added during preprocessing
 *  Copies incoming vertex color without change.
 *  Applies the transformation matrix to vertex position.
 */


//Includes
#include "common.glsl"
#include "brdf.glsl"
#include "common_structs.glsl"
#include "common_lighting.glsl"


uniform CustomPerMaterialUniforms mpCustomPerMaterial;
uniform CommonPerFrameSamplers mpCommonPerFrameSamplers;

#ifdef _D_FORWARD_RENDERING
	uniform Light light[32];
#endif

//Uniform Blocks
layout (std140, binding=0) uniform _COMMON_PER_FRAME
{
    CommonPerFrameUniforms mpCommonPerFrame;
};

layout (std430, binding=1) buffer _COMMON_PER_MESH
{
    MeshInstance instanceData[512];
};

in vec4 fragPos;
in vec4 screenPos;
in vec4 vertColor;
in vec3 mTangentSpaceNormalVec3;
in vec4 uv;
in mat3 TBN;
flat in int instanceId;
flat in uint entityID;
in vec3 instanceColor;
uniform float bilinear_weight;

//Deferred Shading outputs
out vec4 outcolors[4];


#ifdef _F62_DETAIL_ALPHACUTOUT
	const float kfAlphaThreshold = 0.1;
	const float kfAlphaThresholdMax = 0.5;
#elif defined (_F11_ALPHACUTOUT)
	//kfAlphaThreshold = 0.45; OLD
	//kfAlphaThresholdMax = 0.8;
	const float kfAlphaThreshold = 0.5;
	const float kfAlphaThresholdMax = 0.9;
#else
	const float kfAlphaThreshold = 0.0001;
#endif


//New Decoding function - RGTC
vec3 DecodeNormalMap(vec4 lNormalTexVec4 )
{
    lNormalTexVec4 = 2.0 * lNormalTexVec4 - 1.0;
    return ( vec3( lNormalTexVec4.r, lNormalTexVec4.g, sqrt( max( 1.0 - lNormalTexVec4.r * lNormalTexVec4.r - lNormalTexVec4.g * lNormalTexVec4.g, 0.0 ) ) ) );
}

float mip_map_level(in vec2 texture_coordinate)
{
    // The OpenGL Graphics System: A Specification 4.2
    //  - chapter 3.9.11, equation 3.21

    vec2  dx_vtc        = dFdx(texture_coordinate);
    vec2  dy_vtc        = dFdy(texture_coordinate);
    float delta_max_sqr = max(dot(dx_vtc, dx_vtc), dot(dy_vtc, dy_vtc));

    //return max(0.0, 0.5 * log2(delta_max_sqr) - 1.0); // == log2(sqrt(delta_max_sqr));
    return 0.5 * log2(delta_max_sqr); // == log2(sqrt(delta_max_sqr));
}

vec4 texture2D_bilinearBACKUP(in sampler2D t, in vec2 uv)
{
	//Calculate correct mipmaplevel
	float lod = textureQueryLod(t, uv).x; //Get correct sampler lod level
	ivec2 texSize = ivec2(2048, 2048); //Fetch texture size
	vec2 texelSize = 1.0 / texSize; //Calculate texel size
	
	vec2 f = fract( uv * texSize );
    uv += ( .5 - f ) * texelSize;    // move uv to texel centre
    vec4 tl = textureLod(t, uv, lod);
    vec4 tr = textureLod(t, uv + vec2(texelSize.x, 0.0), lod);
    vec4 bl = textureLod(t, uv + vec2(0.0, texelSize.y), lod);
    vec4 br = textureLod(t, uv + vec2(texelSize.x, texelSize.y), lod);
    vec4 tA = mix( tl, tr, f.x );
    vec4 tB = mix( bl, br, f.x );
    return mix( tA, tB, f.y );
}

vec4 texture2D_bilinear(in sampler2D t, in vec2 uv)
{
	//float lod = textureQueryLod(t, uv).x; //Get correct sampler lod level
	return texture(t, uv);
}

void clip(float test) { if (test < 0.0) discard; }

float calcShadow(vec4 _fragPos, Light light){
	// get vector between fragment position and light position
	vec3 fragToLight = (_fragPos - light.position).xyz;
	
	// use the light to fragment vector to sample from the depth map 
	float closestDepth = texture(mpCommonPerFrameSamplers.shadowMap, fragToLight).r; 
	
	// it is currently in linear range between [0,1]. Re-transform back to original value 
	closestDepth *= mpCommonPerFrame.cameraFarPlane; 
	
	// now get current linear depth as the length between the fragment and light position 
	float currentDepth = length(fragToLight);

	// now test for shadows
	float bias = 0.05;
	float shadow = currentDepth - bias > closestDepth ? 1.0 : 0.0;
	
	return shadow;
}

void pbr_lighting(){

	//Final Light/Normal vector calculations
	vec4 lColourVec4 = vec4(1.0);
	vec3 lNormalVec3 = mTangentSpaceNormalVec3;
	vec4 world = fragPos;
	float isLit = 1.0;
    
    float lfRoughness = 1.0;
	float lfMetallic = 0.0;
	float lfSubsurface = 0.0; //Not used atm
	float lfAo = 0.0;
	float lfAoStrength = mpCustomPerMaterial.uOcclusionStrength;
	vec3 lfEmissive = vec3(0.0);

	#if defined(_NB_UNLIT)
		isLit = 0.0;
	#endif
    
    vec4 lTexCoordsVec4;
    lTexCoordsVec4 = uv;
    
    //Manually calculate mipmap level
	//float mipmaplevel = textureQueryLod(mpCustomPerMaterial.gNormalMap, uv.xy).x;
    //float mipmaplevel = mip_map_level(uv.xy);
    
    //Load Base albedo color
    #if defined(_NB_DIFFUSE_MAP)
		lColourVec4 = texture2D_bilinear(mpCustomPerMaterial.gDiffuseMap, lTexCoordsVec4.xy);
    #elif defined(_NB_VERTEX_COLOUR)
		lColourVec4 = vec4(vertColor.rgb, 1.0);
	#else
		lColourVec4 = vec4(mpCustomPerMaterial.uDiffuseFactor, 1.0);	
	#endif

	#if defined(_NB_UNLIT)
		lColourVec4 = -log(max(1.0 - lColourVec4, vec4(0.0001))) / mpCommonPerFrame.cameraPosition.w;
	#endif

	//Alpha CutOut
	if (lColourVec4.a < 1e-4) discard;
	
	//Load Metallic and Roughness values
    #if defined(_NB_AO_METALLIC_ROUGHNESS_MAP) || defined(_NB_METALLIC_ROUGHNESS_MAP)
		vec4 lMasks = texture2D_bilinear(mpCustomPerMaterial.gMasksMap, lTexCoordsVec4.xy);
        lfRoughness = lMasks.g;
        lfMetallic = lMasks.b;
		//Apply material factors
    	lfRoughness *= mpCustomPerMaterial.uRoughnessFactor;
    	lfMetallic *= mpCustomPerMaterial.uMetallicFactor;
    #else
		lfRoughness = mpCustomPerMaterial.uRoughnessFactor;
		lfMetallic = mpCustomPerMaterial.uMetallicFactor;
	#endif
	
	#if defined(_NB_AO_MAP)
		lfAo = texture2D_bilinear(mpCustomPerMaterial.gAoMap, lTexCoordsVec4.xy).r;
	#elif defined(_NB_AO_METALLIC_ROUGHNESS_MAP)
		lfAo = lMasks.r;
	#endif
	
	//NORMALS
    #ifdef _NB_NORMAL_MAP
        vec4 lTexColour = texture2D_bilinear(mpCustomPerMaterial.gNormalMap, lTexCoordsVec4.xy);
		//Custom filtering works great, but it needs the textureSizes passed as uniforms
		//vec4 lTexColour = texture2D_bilinear(mpCustomPerMaterial.gNormalMap, lTexCoordsVec4.xy, mipmaplevel);
		#ifdef _NB_TWO_CHANNEL_NORMAL_MAP
            vec3 lNormalTexVec3 = DecodeNormalMap( lTexColour );
        #else
            vec3 lNormalTexVec3 = 2.0 * lTexColour.rgb - 1.0;    
        #endif
		
		lNormalVec3 = normalize(TBN * lNormalTexVec3);
    #else
        lNormalVec3 = mTangentSpaceNormalVec3;         
    #endif

	//Emissive
	#if defined(_NB_EMISSIVE)
		lfEmissive = mpCustomPerMaterial.uEmissiveFactor;
		lfEmissive *= mpCustomPerMaterial.uEmissiveStrength;
		
		#if defined(_NB_EMISSIVE_MAP)
			lfEmissive *= texture2D_bilinear(mpCustomPerMaterial.gEmissiveMap, lTexCoordsVec4.xy).rgb;
		#endif
	#endif
	
	lColourVec4 = mix(vec4(instanceColor, 1.0), lColourVec4, mpCommonPerFrame.diffuseFlag);
    lNormalVec3 = mix(mTangentSpaceNormalVec3, lNormalVec3, mpCommonPerFrame.diffuseFlag);
	
	//WRITE OUTPUT
    #if defined(_D_DEFERRED_RENDERING)
		//Save Info to GBuffer
	    //Albedo
		outcolors[0] = lColourVec4;
		//Normals
		outcolors[1].rgb = lNormalVec3;
		outcolors[1].a = isLit; //TODO: Use the alpha channel of that attachment to upload any extra material flags
        
        //Export Frag Params
		outcolors[2].x = lfAo;
		outcolors[2].y = lfAoStrength;
		outcolors[2].z = lfMetallic;
		outcolors[2].a = lfRoughness;
		
		//Export Emissive 
		outcolors[3].rgb = lfEmissive;
		outcolors[3].a = float(entityID); //The ID of the entity is here for picking
	#elif defined (_D_FORWARD_RENDERING)
	 
		vec4 finalColor = lColourVec4;
		
		#ifndef _NB_UNLIT
		//TODO: Remove that lighting code, I don't like that at all.
		//I should find a way to light everything in the light pass
		if (mpCommonPerFrame.use_lighting > 0.0) {
			
			Light light;
			light.position = vec4(1.0, 1.0, 1.0, 0.0);
			light.direction = vec4(0.0, 0.0, 0.0, 0.0);
			light.color = vec4(0.8, 0.8, 0.8, 50.0);
			light.parameters = vec4(0.5, 360.0, 0.0, 0.0);
			
			//finalColor.rgb += calcLightingPhong(light, fragPos, lNormalVec3, 
			//mpCommonPerFrame.cameraPosition.xyz, mpCommonPerFrame.cameraDirection.xyz,
			//	lColourVec4.rgb, lfMetallic, lfRoughness, lfAo, lfAoStrength, lfEmissive);
		}
		#endif
		
		//Weighted Blended order independent transparency
		float z = screenPos.z / screenPos.w;
		float weight = max(min(1.0, max3(finalColor.rgb) * finalColor.a), finalColor.a) *
						clamp(0.03 / (1e-5 + pow(z/200, 4.0)), 1e-2, 3e3);
		
		outcolors[0] = vec4(0.5 * (1.0 + finalColor.rgb), 1.0);
		outcolors[1] = vec4(finalColor.a);
	
	#endif
}


void main(){

	pbr_lighting();
}