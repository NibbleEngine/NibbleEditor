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
in vec3 instanceColor;
in float isSelected;

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
    return ( vec3( lNormalTexVec4.r, lNormalTexVec4.g, sqrt( max( 1.0 - lNormalTexVec4.r*lNormalTexVec4.r - lNormalTexVec4.g*lNormalTexVec4.g, 0.0 ) ) ) );
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

void clip(float test) { if (test < 0.0) discard; }


vec4 ApplySelectedColor(vec4 color){
	vec4 new_col = color;
	if (isSelected > 0.0)
		new_col *= vec4(0.005, 1.5, 0.005, 1.0);
	return new_col;
}

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
	
	#if defined(_NB_UNLIT)
		isLit = 0.0;
	#endif
    
    vec4 lTexCoordsVec4;
    lTexCoordsVec4 = uv;
    
    //Manually calculate mipmap level
    float mipmaplevel = mip_map_level(uv.xy);
    
    //Load Base albedo color
    #if defined(_NB_DIFFUSE_MAP)
        lColourVec4 = textureLod(mpCustomPerMaterial.gDiffuseMap, lTexCoordsVec4.xy, mipmaplevel);
    #elif defined(_NB_VERTEX_COLOUR)
		lColourVec4 = vec4(vertColor.rgb, 1.0);
	#else
        lColourVec4 = vec4(mpCustomPerMaterial.uDiffuseFactor, 1.0);
    #endif
    
	//Load Metallic and Roughness values
    #if defined(_NB_AO_METALLIC_ROUGHNESS_MAP) || defined(_NB_METALLIC_ROUGHNESS_MAP)
        vec4 lMasks = texture(mpCustomPerMaterial.gMasksMap, lTexCoordsVec4.xy);
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
        lfAo = textureLod(mpCustomPerMaterial.gAoMap, lTexCoordsVec4.xy, mipmaplevel).r;
    #elif defined(_NB_AO_METALLIC_ROUGHNESS_MAP)
		lfAo = lMasks.r;
	#endif
	
	//NORMALS
    #ifdef _NB_NORMAL_MAP
        //TODO: Try to use lods in the normal maps
        vec4 lTexColour = texture(mpCustomPerMaterial.gNormalMap, lTexCoordsVec4.xy);
        #ifdef _NB_TWO_CHANNEL_NORMAL_MAP
            vec3 lNormalTexVec3 = DecodeNormalMap( lTexColour );
        #else
            //vec3 lNormalTexVec3 = DecodeNormalMap( lTexColour );
            vec3 lNormalTexVec3 = 2.0 * lTexColour.rgb - 1.0;    
        #endif
        lNormalVec3 = normalize(TBN * lNormalTexVec3);        
    #else
        lNormalVec3 = mTangentSpaceNormalVec3;         
    #endif

	//Emissive
	vec3 lfEmissive = mpCustomPerMaterial.uEmissiveFactor;
	lfEmissive *= mpCustomPerMaterial.uEmissiveStrength;
	#ifdef _NB_EMISSIVE_MAP
		lfEmissive *= texture(mpCustomPerMaterial.gEmissiveMap, lTexCoordsVec4.xy).rgb;
	#endif
	
	lColourVec4 = mix(vec4(instanceColor, 1.0), lColourVec4, mpCommonPerFrame.diffuseFlag);
    lNormalVec3 = mix(mTangentSpaceNormalVec3, lNormalVec3, mpCommonPerFrame.diffuseFlag);
	
	//WRITE OUTPUT
    #ifdef _D_DEFERRED_RENDERING
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
		outcolors[3].a = 1.0;
		//outcolors[3].a = ??? //EXTRA SLOT :')
	
	#else
		
		//FORWARD LIGHTING
		vec4 finalColor = lColourVec4;

		#ifndef _NB_UNLIT
		//TODO: Remove that lighting code, I don't like that at all.
		//I should find a way to light everything in the light pass
		if (mpCommonPerFrame.use_lighting > 0.0) {
			for(int i = 0; i < mpCommonPerFrame.light_count; ++i) 
		    {
		    	// calculate per-light radiance
		        Light light = mpCommonPerFrame.lights[i]; 

				//Pos.w is the renderable status of the light
				if (light.position.w < 1.0)
		        	continue;
	    		
	    		finalColor.rgb += calcLighting(light, fragPos, lNormalVec3, 
				mpCommonPerFrame.cameraPosition.xyz, mpCommonPerFrame.cameraDirection.xyz,
		            lColourVec4.rgb, lfMetallic, lfRoughness, lfAo, lfAoStrength, lfEmissive);
			} 
		}
		#endif

		//Weighted Blended order independent transparency
		float z = screenPos.z / screenPos.w;
		float weight = max(min(1.0, max3(finalColor.rgb) * finalColor.a), finalColor.a) *
						clamp(0.03 / (1e-5 + pow(z/200, 4.0)), 1e-2, 3e3);
		
		outcolors[0] = vec4(finalColor.rgb * finalColor.a, finalColor.a) * weight;
		outcolors[1] = vec4(finalColor.a);

	#endif
}


void main(){

	pbr_lighting();
}