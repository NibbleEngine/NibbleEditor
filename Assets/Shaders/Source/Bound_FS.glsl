/*  Version and extension are added during preprocessing
 *  Copies incoming vertex color without change.
 *  Applies the transformation matrix to vertex position.
 */

//Deferred Shading outputs
out vec4 fragColor;

void main()
{	
    fragColor = vec4(1.0, 1.0, 0.0, 1.0); 
}