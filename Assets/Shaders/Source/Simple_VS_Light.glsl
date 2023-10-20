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

uniform CustomPerMaterialUniforms mpCustomPerMaterial;
uniform CommonPerSceneUniforms mpCommonPerScene;

//Uniform Blocks
layout (std140, binding=0) uniform _COMMON_PER_FRAME
{
    CommonPerFrameUniforms mpCommonPerFrame;
};

layout (std430, binding=1) buffer _COMMON_PER_MESH
{
    MeshInstance instanceData[512];
};

layout (std430, binding=2) buffer _COMMON_PER_MESHGROUP
{
    mat4 boneMatricesTBO[512];
};

//Outputs
out vec4 fragPos; 
out vec4 screenPos;
out vec4 vertColor;
out float isSelected;
out vec3 instanceColor;
out vec3 mTangentSpaceNormalVec3;
out vec4 uv;
out mat3 TBN;
flat out int instanceId;

void main()
{
    //Pass uv to fragment shader
    uv = uvPosition0;
    vertColor = bPosition;

    //Load Per Instance data
    instanceId = gl_InstanceID;
    instanceColor = instanceData[gl_InstanceID].color;
    isSelected = instanceData[gl_InstanceID].isSelected;
    
    mat4 lWorldMat = instanceData[gl_InstanceID].worldMat;
    
    fragPos = vec4(vPosition.xyz + lWorldMat[3].xyz, 1.0); 
    screenPos = mpCommonPerFrame.projMat * mpCommonPerFrame.viewMat * vPosition;
    gl_Position = screenPos;
}


