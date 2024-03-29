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

#if defined(_D_FIXED_SCALE)
    uniform float scale;
#endif

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
out vec2 uv0;

void main()
{
    //Pass uv to fragment shader
    uv0 = uvPosition0.xy;
    vertColor = bPosition;

    //Load Per Instance data
#if defined(_D_NO_TRANSFORM)
    mat4 lWorldMat =  mat4(1.0, 0.0,  0.0, 0.0,
                            0.0, 1.0,  0.0, 0.0,
                            0.0, 0.0,  1.0, 0.0,
                            0.0, 0.0,  0.0, 1.0);   
#else
    mat4 lWorldMat = instanceData[gl_InstanceID].worldMat;
#endif

#if defined(_D_FIXED_SCALE)
    mat4 scaleMat =  mat4(scale, 0.0,  0.0, 0.0,
                            0.0, scale,  0.0, 0.0,
                            0.0, 0.0,  scale, 0.0,
                            0.0, 0.0,  0.0, 1.0);    
#endif

    #if defined(_D_FIXED_SCALE)
        vec4 wPos = lWorldMat * scaleMat * vPosition;  
    #else
        vec4 wPos = lWorldMat * vPosition;  
    #endif
    
    fragPos = wPos; //Export world position to the fragment shader
    screenPos = mpCommonPerFrame.projMat * mpCommonPerFrame.viewMat * fragPos;
    gl_Position = screenPos;
}


