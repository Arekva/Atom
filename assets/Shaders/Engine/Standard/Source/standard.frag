#version 460

layout(location = 0) out vec4 out_G_albedo_luminance;           // RGB(Albedo) A(Luminance)
layout(location = 1) out vec4 out_G_normal_roughness_metalness; // RG(Normal)B(Roughness + Z Normal sign)A(Metalness)
layout(location = 2) out vec4 out_G_position_translucency;      // RGB(Position)A(Translucency)
// layout(location = 3) out float out_G_depth;                  // R(Depth)

layout(location = 0) in vec4 in_position;
layout(location = 1) in vec2 in_uv;
layout(location = 2) in vec3 in_normal;

void main() {
    vec3 albedo = vec3(1.0);
    vec3 normal = in_normal;
    vec3 position = in_position.xyz;

    float luminance = 0.0;
    float roughness = 0.0;
    float metalness = 0.0;
    float translucency = 0.0;

    float znorm_roughness_pack = uintBitsToFloat(floatBitsToUint(abs(roughness)) | (floatBitsToUint(normal.z) & 0x80000000));

    out_G_albedo_luminance = vec4(albedo, luminance);
    out_G_normal_roughness_metalness = vec4(normal.xy, znorm_roughness_pack, metalness);
    out_G_position_translucency = vec4(position, translucency);
    // out_G_depth = automatically done
}