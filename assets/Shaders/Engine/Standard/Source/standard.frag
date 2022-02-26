#version 460

#include <deferred_fragment_io.glsl>

void main() {
    vec3 albedo = vec3(1.0);
    vec3 normal = normalize(in_normal);
    vec3 position = in_position.xyz;
    vec2 uv = in_uv;

    float luminance = 0.0;
    float roughness = 1.0;
    float metalness = 1.0;
    float translucency = 1.0;

    float znorm_roughness_pack = uintBitsToFloat(floatBitsToUint(abs(roughness)) | (floatBitsToUint(normal.z) & 0x80000000));

    out_G_albedo_luminance = vec4(albedo, luminance);
    out_G_normal_roughness_metalness = vec4(normal, /*znorm_roughness_pack*/ metalness);
    out_G_position_translucency = vec4(position, translucency);
    // out_G_depth = automatically done
}