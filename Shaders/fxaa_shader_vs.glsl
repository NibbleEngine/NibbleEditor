/*  Version and extension are added during preprocessing
 *  Copies incoming vertex color without change.
 *  Applies the transformation matrix to vertex position.
 */

#include "common_structs.glsl"

layout(location = 0) in vec4 vPosition;

//Uniform Blocks
layout (std140, binding=0) uniform _COMMON_PER_FRAME
{
    CommonPerFrameUniforms mpCommonPerFrame;
};

out vec2 uv;
out vec2 v_rgbNW;
out vec2 v_rgbNE;
out vec2 v_rgbSW;
out vec2 v_rgbSE;
out vec2 v_rgbM;



void main()
{
	uv = vPosition.xy * vec2(0.5, 0.5) + vec2(0.5, 0.5);
    
    vec2 fragCoord = uv * mpCommonPerFrame.frameDim;   
    vec2 inverseVP = 1.0 / mpCommonPerFrame.frameDim;
    v_rgbNW = (fragCoord + vec2(-1.0, -1.0)) * inverseVP;
    v_rgbNE = (fragCoord + vec2(1.0, -1.0)) * inverseVP;
    v_rgbSW = (fragCoord + vec2(-1.0, 1.0)) * inverseVP;
    v_rgbSE = (fragCoord + vec2(1.0, 1.0)) * inverseVP;
    v_rgbM = vec2(fragCoord * inverseVP);
    
    gl_Position = vec4(vPosition.xyz, 1.0);

}