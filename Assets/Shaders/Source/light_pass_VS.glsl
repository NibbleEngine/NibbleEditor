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
    lightDirection.xyz = instanceData[gl_InstanceID].uniforms[2].xyz; // direction
    lightDirection.w = instanceData[gl_InstanceID].uniforms[2].w; //type
    //Light Color
    lightColor.xyz = instanceData[gl_InstanceID].color; // use the instance color
    lightColor.w = instanceData[gl_InstanceID].uniforms[0].z; // intensity
    //Light Params
    lightParameters.x = instanceData[gl_InstanceID].uniforms[0].w; //falloff
    lightParameters.y = instanceData[gl_InstanceID].uniforms[0].x; //inner fov
    lightParameters.z = instanceData[gl_InstanceID].uniforms[0].y; //outter fov
    lightParameters.w = instanceData[gl_InstanceID].uniforms[1].y; //falloff radius
    
    //Create ScaleMat
    mat4 scaleMat = mat4(vec4(lightParameters.w, 0, 0, 0),
                         vec4(0, lightParameters.w, 0, 0),
                         vec4(0, 0, lightParameters.w, 0),
                         vec4(0, 0, 0, 1));

    vec4 wPos = lWorldMat * scaleMat * vPosition; //Calculate light world position
    screenPos = mpCommonPerFrame.projMat * mpCommonPerFrame.viewMat * mpCommonPerFrame.rotMat * wPos;
    gl_Position = screenPos;
}
