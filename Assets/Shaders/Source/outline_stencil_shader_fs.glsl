/*  Version and extension are added during preprocessing
 *  Copies incoming vertex color without change.
 *  Applies the transformation matrix to vertex position.
 */

//Tone Mapping shader

//Includes
#include "common.glsl"
#include "common_structs.glsl"

//Diffuse Textures
uniform sampler2D renderTexture;
uniform sampler2D stencilTexture;

//Uniform Blocks
layout (std140, binding=0) uniform _COMMON_PER_FRAME
{
    CommonPerFrameUniforms mpCommonPerFrame;
};


out vec4 fragColour; 

uniform vec3 outline_color;

void main()
{
    fragColour = vec4(outline_color, 0.8);
}
