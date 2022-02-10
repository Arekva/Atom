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
    vec3 normal = normalize(vec3(gNormal.xy, 0.0)); // todo: retrieve xy components + z
    float roughness = abs(gNormal.z);
    float metalness = gNormal.a;

    // gPosition: RGB(Position)A(Translucency)
    vec3 position = gPosition.xyz;
    float translucency = gPosition.w;

    float depth = gDepth.r;

    // if there is nothing here, just apply background color
    // (0-ed normals cannot exist if there is an object)
    if(normal.r == 0 && normal.g == 0 && normal.b == 0)
    {
        outColor = vec4(24/255.0, 26/255.0, 28/255.0, 1.0);
    }
    else // set albedo for now.
    {
        outColor = vec4(albedo.rgb, 1.0);
    }
}