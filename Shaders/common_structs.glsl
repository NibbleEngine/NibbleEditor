//Light struct
struct Light //locations:6
{
    vec4 position; //w is empty
    vec4 color; //w is intensity
    vec4 direction; //w is empty
    vec4 parameters; //x: falloff, y: fov, z: type, w: empty
};

struct MeshInstance
{
    mat4 uniforms;
    int boneIndicesRemap[64];
    mat4 worldMat;
    mat4 normalMat;
    mat4 worldMatInv;
    vec3 color;
    float isSelected;
};

//Common Per Mesh Struct
struct CommonPerMeshUniforms
{
    //CommonPerMeshUniforms
    vec4 gUserDataVec4;
    vec3 color; //Mesh Default Color
    float skinned;
    MeshInstance instanceData[512]; //Instance world matrices, normal matrices, occlusion and selection status
};

//Custom Per Frame Struct
struct CommonPerFrameUniforms
{
    float diffuseFlag; //Enable Textures //floats align to 16 bytes
    float use_lighting; //Enable lighting
    float gfTime; //Time
    float MSAA_SAMPLES; //MSAA Samples
    vec2 frameDim; //Dimensions of the render frame
    float cameraNearPlane;
    float cameraFarPlane;
    //Rendering Options
    mat4 rotMat;
    mat4 rotMatInv;
    mat4 mvp;
    mat4 lookMatInv;
    mat4 projMatInv;
    vec4 cameraPosition; //w component is the HDR exposure
    int light_count;
    vec3 cameraDirection;
};

struct CommonPerFrameSamplers
{
    sampler2D depthMap; //Scene Depth Map
    sampler2DArray shadowMap; //Dummy - NOT USED
};


struct CustomPerMaterialUniforms  //locations:73
{
    #ifdef _F55_MULTITEXTURE
        sampler2DArray gDiffuseMap;
        sampler2DArray gDiffuse2Map;
        sampler2DArray gMasksMap;
        sampler2DArray gNormalMap;
    #else
        
        #if defined(_NB_DIFFUSE_MAP)
            sampler2D gDiffuseMap;
        #endif
        #ifdef _F16_DIFFUSE2MAP
            sampler2D gDiffuse2Map;
        #endif
        
        #if defined(_NB_AO_METALLIC_ROUGHNESS_MAP) || defined(_NB_METALLIC_ROUGHNESS_MAP)
            sampler2D gMasksMap;
        #endif
        
        #if defined(_NB_AO_MAP)
            sampler2D gAoMap;
        #endif
        
        #if defined(_NB_NORMAL_MAP)
            sampler2D gNormalMap;
        #endif

        #if defined(_NB_EMISSIVE_MAP)
            sampler2D gEmissiveMap;
        #endif

    #endif

    //Uniforms
    vec3 uDiffuseFactor;
    float uMetallicFactor;
    float uRoughnessFactor;
    float uOcclusionStrength;
    vec3 uEmissiveFactor;
    float uEmissiveStrength;
    float testValue;

};

struct CommonPerSceneUniforms
{
    samplerBuffer skinMatsTex;
};