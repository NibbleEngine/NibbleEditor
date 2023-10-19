/*  Version and extension are added during preprocessing
 *  Copies incoming vertex color without change.
 *  Applies the transformation matrix to vertex position.
 */

//Tone Mapping shader

//Includes
#include "common.glsl"
#include "common_structs.glsl"

//Diffuse Textures
uniform sampler2D inTex1;
uniform sampler2D inTex2;
uniform float mix_factor;

//Uniform Blocks
layout (std140, binding=0) uniform _COMMON_PER_FRAME
{
    CommonPerFrameUniforms mpCommonPerFrame;
};


out vec4 fragColour; 

void main()
{
    vec2 uv = gl_FragCoord.xy / (1.0 * mpCommonPerFrame.frameDim);
    
    vec4 col1 = texture(inTex1, uv);
    vec4 col2 = texture(inTex2, uv);

    
    fragColour.rgb = mix(col1.rgb, col2.rgb, mix_factor);
    fragColour.a = col1.a;
}
