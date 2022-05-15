#version 460

#include <deferred_fragment_io.glsl>

layout(set = 1, binding = 0) uniform sampler2D _albedo;
layout(set = 1, binding = 1) uniform sampler2D _normal;
layout(set = 1, binding = 2) uniform sampler2D _ambientOcclusion;
layout(set = 1, binding = 3) uniform sampler2D _metalness;
layout(set = 1, binding = 4) uniform sampler2D _roughness;



void main() {
    const vec2 offset = vec2(0.0);
    const vec2 tilling = vec2(1.0);

    vec2 transformed_uv = ((in_uv + offset) * tilling);

    vec3 albedo = vec3(texture(_albedo, transformed_uv));

    vec3 tex_normal = normalize((texture(_normal, in_uv).rgb) * 2.0 - 1.0);
    vec3 normal     = in_tbn * tex_normal                                 ;
    vec3 position   = in_position.xyz                                     ;
    vec2 uv         = in_uv                                               ;

    float luminance = 0.0;
    float roughness = texture(_roughness, in_uv).r;
    float metalness = texture(_metalness, in_uv).r;
    float translucency = 1.0;

    float znorm_roughness_pack = uintBitsToFloat(floatBitsToUint(abs(roughness)) | (floatBitsToUint(normal.z) & 0x80000000));

    out_G_albedo_luminance = vec4(albedo, luminance);
    out_G_normal_roughness_metalness = vec4(normal, /*znorm_roughness_pack*/ metalness);
    out_G_position_translucency = vec4(position, roughness);
    // out_G_depth = automatically done
}