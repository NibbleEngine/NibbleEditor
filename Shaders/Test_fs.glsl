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

in vec4 uv;
in vec4 screenPos;
in vec4 vertColor;
out vec4 fragColour; 


vec2 Mandelbrot(vec2 pix){

	return vec2(0.0, 0.0);
}


void main()
{
	//vec2 c = uv.xy;
	vec2 c = uv.xy;
	c = c.yx;
	c = 2.0 * c - 1.0;
	c.x *= 2.0;

	//Try to zoom
	//c *= cos(0.2 * mpCommonPerFrame.gfTime);
	c *= abs(cos(0.05 * mpCommonPerFrame.gfTime * (mpCommonPerFrame.gfTime + 1000.0) / (mpCommonPerFrame.gfTime)));
	//c *= log2mpCommonPerFrame.gfTime;
	c += vec2(-0.05, 0.6805);

	vec2 z = vec2(0.0, 0.0);
	float it = 0.0;
	for (int i=0;i<1024;i++){
		z = vec2(z.x*z.x - z.y*z.y, 2*z.x*z.y) + c;
		if (dot(z,z) > 512) break;
		it += 1.0;
	}

	//IG INSANE SHIT
	float sl = it - log2(log2(dot(z, z))) + 4.0;
	vec3 col = 0.5 + 0.5*cos( 3.0 + sl*0.15 + vec3(0.0,0.6,1.0));

	fragColour = vec4(50.0 * col, 1.0);
	
	//if (uv0.x > 0.98 || uv0.x < 0.03 || uv0.y > 0.98 || uv0.y < 0.03)
	//	fragColour = vec4(0.0, 0.0, 1.0, 0.2);
}
