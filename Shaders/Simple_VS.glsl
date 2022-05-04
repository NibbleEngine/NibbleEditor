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
    vec4 wPos = vPosition;
    
    //Check F02_SKINNED
    #ifdef _D_SKINNED
        ivec4 index;
        
        index.x = int(blendIndices.x);
        index.y = int(blendIndices.y);
        index.z = int(blendIndices.z);
        index.w = int(blendIndices.w);

        //Remapped indices
        index.x = instanceData[gl_InstanceID].boneIndicesRemap[index.x];
        index.y = instanceData[gl_InstanceID].boneIndicesRemap[index.y];
        index.z = instanceData[gl_InstanceID].boneIndicesRemap[index.z];
        index.w = instanceData[gl_InstanceID].boneIndicesRemap[index.w];

        //Assemble matrices from 
        lWorldMat =  blendWeights.x * boneMatricesTBO[index.x];
        lWorldMat += blendWeights.y * boneMatricesTBO[index.y];
        lWorldMat += blendWeights.z * boneMatricesTBO[index.z];
        lWorldMat += blendWeights.w * boneMatricesTBO[index.w];
    #endif

    wPos = lWorldMat * vPosition; //Calculate world Position
    fragPos = wPos; //Export world position to the fragment shader
    screenPos = mpCommonPerFrame.mvp * mpCommonPerFrame.rotMat * fragPos;
    gl_Position = screenPos;
    
    //Construct TBN matrix
    //Nullify w components
    vec4 lLocalTangentVec4 = tPosition;
    vec4 lLocalNormalVec4 = nPosition;
    vec4 lLocalBitangentVec4 = bPosition;
    
    //mat4 nMat = instanceData[gl_InstanceID].normalMat;
    //Recalculate nMat to test the rotMat here
    //mat4 nMat =  instanceData[gl_InstanceID].normalMat * transpose(mpCommonPerFrame.rotMatInv);
    mat4 nMat =  transpose(inverse(mpCommonPerFrame.rotMat * instanceData[gl_InstanceID].worldMat));
    //mat4 nMat =  transpose(inverse(instanceData[gl_InstanceID].worldMat));
    
    //OLD
    vec3 lWorldTangentVec4 = normalize(vec3(nMat * vec4(lLocalTangentVec4.xyz, 0.0)));
    vec3 lWorldNormalVec4 = normalize(vec3(nMat * vec4(lLocalNormalVec4.xyz, 0.0)));
    vec3 lWorldBitangentVec4 = cross(lWorldNormalVec4, lWorldTangentVec4);
    
    //Re-orthogonalize tangent
    //lWorldTangentVec4.xyz = normalize(lWorldTangentVec4.xyz - dot(lWorldTangentVec4.xyz, lWorldNormalVec4.xyz) * lWorldNormalVec4.xyz);
    
    //vec4 lWorldBitangentVec4 = normalize(vec4( cross(lWorldNormalVec4.xyz, lWorldTangentVec4.xyz), 0.0));
    
    TBN = mat3( lWorldTangentVec4,
                lWorldBitangentVec4,
                lWorldNormalVec4 );

    //Send world normal to fragment shader
    mTangentSpaceNormalVec3 = lWorldNormalVec4.xyz;
}


