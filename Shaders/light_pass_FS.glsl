/*  Version and extension are added during preprocessing
 *  Copies incoming vertex color without change.
 *  Applies the transformation matrix to vertex position.
 */
 
//Includes
#include "common.glsl"
#include "common_structs.glsl"
#include "common_lighting.glsl"


//Diffuse Textures
uniform sampler2D albedoTex;
uniform sampler2D normalTex;
uniform sampler2D depthTex;
uniform sampler2D parameterTex;

in vec4 screenPos;
in vec4 lightPos;
in vec4 lightColor;
in vec4 lightDirection;
in vec4 lightParameters;
out vec4 fragColor;

uniform CommonPerFrameSamplers mpCommonPerFrameSamplers;

//Uniform Blocks
layout (std140, binding=0) uniform _COMMON_PER_FRAME
{
    CommonPerFrameUniforms mpCommonPerFrame;
};

vec4 worldfromDepth(in vec2 screen, in float depth)
{
	vec4 world;
	
	float n = mpCommonPerFrame.cameraNearPlane;
	float f = mpCommonPerFrame.cameraFarPlane;

	//Convert depth back to (-1:1)
	world.xy = 2.0 * screen - 1.0;
	world.z = 2.0 * depth - 1.0; 
	world.w = 1.0f;

	world = mpCommonPerFrame.projMatInv * world;
	world /= world.w;
	world = mpCommonPerFrame.lookMatInv * world;
	//world /= world.w;

	return world;
}

// Converts post-projection z/w to linear z
float LinearDepth(float perspectiveDepth)
{
	float n = mpCommonPerFrame.cameraNearPlane;
	float f = mpCommonPerFrame.cameraFarPlane;

	float ProjectionA = f / (f - n);
    float ProjectionB = (-f * n) / (f - n);
	
	return ProjectionB / (perspectiveDepth - ProjectionA);
}


void main()
{
	//sample our texture
	vec4 bloomColor = vec4(0.0, 0.0, 0.0, 0.0);
	vec3 clearColor = vec3(0.13, 0.13, 0.13);
	vec2 uv = gl_FragCoord.xy / (1.0 * mpCommonPerFrame.frameDim);
    
	vec4 albedoColor = texture(albedoTex, uv);	
	vec4 fragNormal = texture(normalTex, uv);
	vec4 fragParams = texture(parameterTex, uv);
	vec4 depthColor = texture(depthTex, uv);
	
	//Calculate fragment Position from depth
	vec4 fragPos = worldfromDepth(uv, depthColor.r);

	//Load Frag Info
	float ao = fragParams.x;
	float lfMetallic = fragParams.y;
	float lfRoughness = fragParams.z;
	float lfSubsurface = fragParams.a;
	float isLit = fragNormal.a;
	
	vec4 finalColor = vec4(0.0);
    
    //finalColor = mix(finalColor, albedoColor, lfGlow);
	vec4 ambient = vec4(vec3(0.03) * albedoColor.rgb, 0.0);


	if (isLit > 0.0)
	{
		Light light;
		light.position = lightPos;
		light.direction = lightDirection;
		light.color = lightColor;
		light.parameters = lightParameters;
		
		finalColor.rgb = calcLighting(light, fragPos, fragNormal.xyz, 
				mpCommonPerFrame.cameraPosition.xyz, mpCommonPerFrame.cameraDirection.xyz, albedoColor.rgb, lfMetallic, lfRoughness, ao);
		
		//finalColor.rgb = vec3(0.0, 1.0, 0.0);

		finalColor.a = albedoColor.a;
	} else {
		finalColor = albedoColor;	
	}
	
	fragColor = finalColor;
}
