/*  Version and extension are added during preprocessing
 *  Copies incoming vertex color without change.
 *  Applies the transformation matrix to vertex position.
 */

//Includes
#include "common.glsl"
#include "common_structs.glsl"

in vec4 screenPos;
in vec2 uv;
in vec3 color;

//Deferred Shading outputs
out vec4 outcolors[4];

uniform CustomPerMaterialUniforms mpCustomPerMaterial;

void main()
{
  //Fetch imposter texture color
  vec4 lColourVec4 = texture(mpCustomPerMaterial.gDiffuseMap, uv);
  
  if (lColourVec4.a < 0.001)
    discard;
  
  //Imposter textures are supposed to have only alpha channels (for now)
  //In any other case make sure to use the other channels to color the texture
  

  lColourVec4.rgb = color * lColourVec4.a;

  float isLit = 1.0;
  #ifdef _NB_UNLIT
    isLit = 0.0;
  #endif

  //WRITE OUTPUT
  #if defined(_D_DEFERRED_RENDERING)
		//Save Info to GBuffer
	  //Albedo
		outcolors[0] = lColourVec4;
		//Normals
		outcolors[1].a = isLit;
  
  #elif defined (_D_FORWARD_RENDERING)
    outcolors[0] = lColourVec4;
  #endif
  
  
}