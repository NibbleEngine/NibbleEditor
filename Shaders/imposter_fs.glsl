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
out vec4 Out_Color;

uniform CustomPerMaterialUniforms mpCustomPerMaterial;

void main()
{
  //Fetch Texture
  vec4 lColourVec4 = texture(mpCustomPerMaterial.gDiffuseMap, uv);
  if (lColourVec4.a < 0.001)
    discard;
  Out_Color = vec4(color.xyz * (vec3(1.0, 1.0, 1.0) - lColourVec4.xyz) , lColourVec4.a);
  //Out_Color = vec4(1.0, 0.0, 0.0, 1.0);
}