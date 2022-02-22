#version 460

layout (input_attachment_index = 0, set = 0, binding = 0) uniform subpassInput gAlbedo;
layout (input_attachment_index = 1, set = 0, binding = 1) uniform subpassInput gNormal;
layout (input_attachment_index = 2, set = 0, binding = 2) uniform subpassInput gPosition;
layout (input_attachment_index = 3, set = 0, binding = 3) uniform subpassInput gDepth;

layout(location = 0) in vec2 inUv;
layout(location = 0) out vec4 outColor;

void main()
{
    vec4 gAlbedo = subpassLoad(gAlbedo);
    vec4 gNormal = subpassLoad(gNormal);
    vec4 gPosition = subpassLoad(gPosition);
    vec4 gDepth = subpassLoad(gDepth);

    // gAlbedo: RGB(Albedo)A(Luminance)
    vec3 albedo = gAlbedo.rgb;
    float luminance = gAlbedo.a;

    // gNormal: RG(Normal.xy)B(abs(Roughness) + sign(Normal.z))A(Metalness)
    vec3 normal = gNormal.xyz;
    //normalize(vec3(gNormal.xy, 0.0)); // todo: retrieve xy components + z
    float roughness = abs(gNormal.z);
    float metalness = gNormal.a;

    // gPosition: RGB(Position)A(Translucency)
    vec3 position = gPosition.xyz;
    float translucency = gPosition.w;

    float depth = gDepth.r;

    
    vec3 sun_dir = normalize(vec3(1.0,1.0,1.0));
    float angle = dot(sun_dir, normal);

    float faint_angle = dot(-sun_dir, normal);

    vec3 full_albedo = albedo * max(angle, 0.0);

    vec3 faint_light_color = vec3(255/255.0, 149/255.0, 0/255.0);
    vec3 faint_albedo = albedo * faint_light_color * max(faint_angle, 0.0) * 0.5;
    

    outColor = vec4(full_albedo + faint_albedo, 1.0);

    //outColor = vec4(vec3(depth), 1.0);
}