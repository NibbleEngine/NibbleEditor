
float calcLightAttenuation(Light light, vec4 _fragPos){
    float lfAttenuation = 1.0f;

    //General Configuration
    
    //New light system
    //float lfLightIntensity = sqrt(light.color.w);
    //float lfLightIntensity = log(light.color.w) / log(10.0);
    
    vec3 lightPos = light.position.xyz; 
    float l_distance = distance(lightPos, _fragPos.xyz); //Calculate distance of 
    float lfDistanceSquared = l_distance * l_distance; //Distance to light squared
    
    float lfFalloffType = light.parameters.x;
    float lfFOV = light.parameters.y;
    float lfCutOff = 0.05;

    vec3 lspotDir = normalize(light.direction.xyz);
    //TODO add light range in the direction.w component

    //Calculate distance attenuation
    if (lfFalloffType < 1e-4)
    {
        // Quadratic Distance attenuation
        lfAttenuation *= 1.0 / lfDistanceSquared;
    } else if (lfFalloffType - 1.0 < 1e-4) {
        //Constant
        lfAttenuation *= 1.0;
    }
    else if (lfFalloffType - 2.0 < 1e-4)
    {
        // Linear Distance attenuation
        lfAttenuation *= 1.0 / l_distance;
    }
    else if (lfFalloffType - 3.0 < 1e-4)
    {
        // Linear Distance attenuation
        lfAttenuation *= 1.0 / sqrt(l_distance);
    }

    //Calculate attenuation of spot lights
    vec3 L = normalize(_fragPos.xyz - lightPos);
    vec3 acL = (1 - light.direction.w) * L + light.direction.w * lspotDir;
    //lfAttenuation *= dot(acL, L);
    
    float theta = dot(L, lspotDir);
    float epsilon   = light.parameters.y - light.parameters.z;
    float intensity = clamp((theta - light.parameters.z) / epsilon, 0.0, 1.0);
    lfAttenuation = mix(lfAttenuation, lfAttenuation * intensity, light.direction.w);
    
    return lfAttenuation;
}


vec3 calcLighting(Light light, vec4 fragPos, vec3 N, vec3 cameraPos, vec3 cameraDir,
            vec3 albedoColor, float lfMetallic, float lfRoughness, float lfAo, float lfAoStrength, vec3 lfEmissive) {
    
    vec3 F0 = vec3(0.04); 
    F0 = mix(F0, albedoColor, lfMetallic);
    
    //ao = 1.0;
    //return vec3(lfRoughness, 0.0, 0.0);

    vec3 V = normalize(cameraPos - fragPos.xyz); //Calculate viewer vector based on camera position
    vec3 L = normalize(light.position.xyz - fragPos.xyz);    
    vec3 H = normalize(V + L);
    
    float attenuation = calcLightAttenuation(light, fragPos);
    
    vec3 radiance = light.color.xyz * attenuation * light.color.w;
    
    //KHRONOS WAY
    float VdotH = max(min(dot(V, H), 1.0), 0.0);
    float NdotL = max(min(dot(N, L), 1.0), 0.0);
    float NdotV = max(min(dot(N, V), 1.0), 0.0);
    float NdotH = max(min(dot(N, H), 1.0), 0.0);

    vec3 f_diffuse = vec3(0.0);
    vec3 f_specular = vec3(0.0);
    
    //TODO: Apply AO information only when IBL is considered

    f_diffuse = radiance * NdotL * BRDF_lambertian(F0, vec3(1.0), mix(albedoColor, vec3(0.0), lfMetallic), 1.0, VdotH);
    f_specular = radiance * NdotL * BRDF_specularGGX(F0, vec3(1.0), lfRoughness * lfRoughness, 1.0, VdotH, NdotL, NdotV, NdotH);
    return f_diffuse + f_specular + lfEmissive;
}

vec3 calcLightingPhong(Light light, vec4 fragPos, vec3 N, vec3 cameraPos, vec3 cameraDir,
            vec3 albedoColor, float lfMetallic, float lfRoughness, float lfAo, float lfAoStrength, vec3 lfEmissive) {
    
    vec3 V = normalize(cameraPos - fragPos.xyz); //Calculate viewer vector based on camera position
    vec3 L = normalize(light.position.xyz - fragPos.xyz);    
    vec3 H = normalize(V + L);
    
    //KHRONOS WAY
    float VdotH = max(min(dot(V, H), 1.0), 0.0);
    float NdotL = max(min(dot(N, L), 1.0), 0.0);
    float NdotV = max(min(dot(N, V), 1.0), 0.0);
    float NdotH = max(min(dot(N, H), 1.0), 0.0);


    vec3 specColor = vec3(1.0, 1.0, 1.0);
    //Blinn Phong
    float specAngle = max(dot(H, N), 0.0);
    float specular = pow(specAngle, 16.0);


    //Phong
    //vec3 reflectDir = reflect(-L, N);
    //float specAngle = max(dot(reflectDir, V), 0.0);
    //float specular = pow(specAngle, 4.0);
    
    float attenuation = calcLightAttenuation(light, fragPos);

    float dist = max(length(light.position.xyz - fragPos.xyz), 0.001);
    vec3 radiance = light.color.xyz * light.color.w / (dist * dist);
    
    
    vec3 f_diffuse = vec3(0.0);
    vec3 f_specular = vec3(0.0);
    
    //TODO: Apply AO information only when IBL is considered

    f_diffuse = NdotL * radiance * albedoColor;
    f_specular = specular * specColor;
    return f_diffuse + f_specular;
}