/*  Version and extension are added during preprocessing
 *  Copies incoming vertex color without change.
 *  Applies the transformation matrix to vertex position.
 */
 
//Imports
#include "common.glsl"
#include "common_structs.glsl"

//Mesh Attributes
layout(location=0) in vec4 vPosition;
layout(location=1) in vec4 uvPosition0;
layout(location=2) in vec4 nPosition; //normals
layout(location=3) in vec4 tPosition; //tangents
layout(location=4) in vec4 bPosition; //bitangents/ vertex color
layout(location=5) in vec4 blendIndices;
layout(location=6) in vec4 blendWeights;

//Uniform Blocks

layout (std140, binding=0) uniform _COMMON_PER_FRAME
{
    CommonPerFrameUniforms mpCommonPerFrame;
};

layout (std430, binding=1) buffer _COMMON_PER_MESH
{
    MeshInstance instanceData[512];
};

//Outputs
out vec4 screenPos;
out vec4 lightPos;
out vec4 lightColor;
out vec4 lightDirection;
out vec4 lightParameters;

/*
** Returns matrix4x4 from texture cache.
*/
// mat4 get_volume_worldMatrix(int offset)
// {
//     return (mat4(texelFetch(lightsTex, offset),
//                  texelFetch(lightsTex, offset + 1),
//                  texelFetch(lightsTex, offset + 2),
//                  texelFetch(lightsTex, offset + 3)));
// }

// vec4 get_vec4(int offset)
// {
//     return texelFetch(lightsTex, offset);
// }

void main()
{
    //Extract Light Information
    mat4 lWorldMat = instanceData[gl_InstanceID].worldMat;
    //Light Position
    lightPos = lWorldMat[3].xyzw;
    //Light Direction
    //TODO Calculate direction of light based on the transform
    //lightDirection = get_vec4(instanceDataOffset + 4);
    lightDirection = vec4(1.0, 0.0, 0.0, 0.0);
    //Light Color
    lightColor.xyz = instanceData[gl_InstanceID].color;
    lightColor.w = instanceData[gl_InstanceID].uniforms[0].y; //intensity
    //Light Params
    vec4 parameters; //x: falloff, y: fov, z: type, w: empty
    lightParameters.x = instanceData[gl_InstanceID].uniforms[0].w; //falloff
    lightParameters.y = instanceData[gl_InstanceID].uniforms[0].x; //fov
    lightParameters.z = instanceData[gl_InstanceID].uniforms[1].x; //type
    
    vec4 wPos = lWorldMat * vPosition; //Calculate world Position
    screenPos = mpCommonPerFrame.mvp * mpCommonPerFrame.rotMat * wPos;
    gl_Position = screenPos;
}
