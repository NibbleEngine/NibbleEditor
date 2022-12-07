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

//Outputs
out vec4 screenPos;
out vec2 uv;
out vec3 color;

mat4 rotationMatrix(vec3 axis, float angle)
{
    axis = normalize(axis);
    float s = sin(angle);
    float c = cos(angle);
    float oc = 1.0 - c;
    
    return mat4(oc * axis.x * axis.x + c,           oc * axis.x * axis.y - axis.z * s,  oc * axis.z * axis.x + axis.y * s,  0.0,
                oc * axis.x * axis.y + axis.z * s,  oc * axis.y * axis.y + c,           oc * axis.y * axis.z - axis.x * s,  0.0,
                oc * axis.z * axis.x - axis.y * s,  oc * axis.y * axis.z + axis.x * s,  oc * axis.z * axis.z + c,           0.0,
                0.0,                                0.0,                                0.0,                                1.0);
}

void main()
{
    //Load Per Instance data
    mat4 lWorldMat = instanceData[gl_InstanceID].worldMat;
    
    //Calculate UVs
    float image_id = instanceData[gl_InstanceID].uniforms[0].z;
    uv = uvPosition0.xy + image_id * vec2(1.0, 0.0);
    
    //Pass Color
    color = instanceData[gl_InstanceID].color;
    vec2 size;
    size.x = instanceData[gl_InstanceID].uniforms[0].x;
    size.y = instanceData[gl_InstanceID].uniforms[0].y;
    
    //Pass Imposter Data to fragment shader
    mat4 worldMat = mat4(1.0, 0.0,  0.0, 0.0,
                        0.0, 1.0,  0.0, 0.0,
                        0.0, 0.0, 1.0, 0.0,
                        lWorldMat[3].xyz, 1.0);

    mat4 scaleMat = mat4(size.x, 0.0, 0.0, 0.0,
                         0.0, size.y, 0.0, 0.0,
                         0.0, 0.0, 1.0, 0.0,
                         0.0, 0.0, 0.0, 1.0);
    
    vec4 wPos = worldMat * vPosition; //Calculate world Position

    screenPos = mpCommonPerFrame.projMat * mpCommonPerFrame.viewMat * worldMat * mpCommonPerFrame.cameraRotMat * scaleMat * vPosition;
    gl_Position = screenPos;
}


