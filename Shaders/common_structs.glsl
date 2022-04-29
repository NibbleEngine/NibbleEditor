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

//Custom Per Frame Struct
struct CustomPerMaterialUniforms  //locations:73
{
    #ifdef _F55_MULTITEXTURE
        sampler2DArray gDiffuseMap;
        sampler2DArray gDiffuse2Map;
        sampler2DArray gMasksMap;
        sampler2DArray gNormalMap;
    #else
        sampler2D gDiffuseMap;
        sampler2D gDiffuse2Map;
        sampler2D gMasksMap;
        sampler2D gNormalMap;
    #endif

    vec4 uniforms[10];
    
    //Uniform Index Convention
    //0: MaterialColour
    //1: MaterialParameters
    //2: MaterialSFX
    //3: MaterailSFXColours
    //4: UVScrollStep
    //5: DissolveData
    //6: CustomParameters

};

struct CommonPerSceneUniforms
{
    samplerBuffer skinMatsTex;
};