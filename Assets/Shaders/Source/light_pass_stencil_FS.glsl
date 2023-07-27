/*  Version and extension are added during preprocessing
 *  Copies incoming vertex color without change.
 *  Applies the transformation matrix to vertex position.
 */
 
//Includes
#include "common.glsl"
#include "common_structs.glsl"
#include "brdf.glsl"
#include "common_lighting.glsl"

in vec4 screenPos;
in vec4 lightPos;
in vec4 lightColor;
in vec4 lightDirection;
in vec4 lightParameters;
out vec4 fragColor;

void main()
{
    fragColor = vec4(1.0, 1.0, 1.0, 1.0); //Indicative color
}

