/*  Version and extension are added during preprocessing
 *  Copies incoming vertex color without change.
 *  Applies the transformation matrix to vertex position.
 */

//Tone Mapping shader

//Includes
#include "common.glsl"
#include "common_structs.glsl"

//Diffuse Textures
uniform sampler2D inTex;


//Uniform Blocks
layout (std140, binding=0) uniform _COMMON_PER_FRAME
{
    CommonPerFrameUniforms mpCommonPerFrame;
};


out vec4 fragColour; 

void main()
{
    vec2 uv = gl_FragCoord.xy / (1.0 * mpCommonPerFrame.frameDim);
    vec4 color = texture(inTex, uv);
    
    //Revert Gamma Correction
    color.rgb = GammaCorrectInput(color.rgb);

    //Exposure tone mapping
	color.rgb = vec3(1.0) - exp(-color.rgb * mpCommonPerFrame.cameraPosition.w);
    
    //NMS Kodak Tone Mapping
    //color.rgb = TonemapKodak(color.rgb) / TonemapKodak( vec3(1.0,1.0,1.0) );
    
    //Gamma correction
    color.rgb = GammaCorrectOutput(color.rgb);
    fragColour = vec4(color.rgb, color.a);
}
