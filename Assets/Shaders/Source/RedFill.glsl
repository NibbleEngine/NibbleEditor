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
	float radius = 0.05 * sin(angle);
	
	if (length(uv0 - vec2(0.5, 0.5)) < radius){
		fragColour = vec4(0.0, 0.0, 1.0, 0.5);
	} else if (uv0.x > 0.98 || uv0.x < 0.03 || uv0.y > 0.98 || uv0.y < 0.03){
		fragColour = vec4(1.0, 0.0, 0.0, 0.5);
	}
	
	//if (uv0.x > 0.98 || uv0.x < 0.03 || uv0.y > 0.98 || uv0.y < 0.03)
	//	fragColour = vec4(0.0, 0.0, 1.0, 0.2);
}
