/*  Version and extension are added during preprocessing
 *  Copies incoming vertex color without change.
 *  Applies the transformation matrix to vertex position.
 */

/* Fills the incoming geometry with red color. Used for testing. */

//Includes
#include "common.glsl"
#include "common_structs.glsl"

//Uniform Blocks
layout (std140, binding=0) uniform _COMMON_PER_FRAME
{
    CommonPerFrameUniforms mpCommonPerFrame;
};

//Diffuse Textures
uniform sampler2D InTex;

out vec4 fragColour; 
in vec2 uv0;

void main()
{

	float angle = 3.14 * fract(mpCommonPerFrame.gfTime);
	float radius = 0.10 * sin(angle);
	
	float width = 44.1;
	float space = 200.0;
	
	vec2 uv = (uv0 - vec2(0.5)) * 1000;
	
	vec2 a1 = mod(uv + space, width);
	vec2 a2 = mod(uv - space, width);

	vec2 a = a1 - a2;
	float x = min(a.x, a.y);
	
	x = clamp(x, 0.0, 1.0);
	vec3 col = vec3(0.8);

	float dist = length(uv);
	col *= 1.0 - length(abs(uv)/ 1000.0);
	fragColour = vec4(col, 1.0 - x);
	
	
}
