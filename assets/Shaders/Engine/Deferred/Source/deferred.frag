#version 460

layout (input_attachment_index = 0, set = 0, binding = 0) uniform subpassInput gAlbedo;
layout (input_attachment_index = 1, set = 0, binding = 1) uniform subpassInput gNormal;
layout (input_attachment_index = 2, set = 0, binding = 2) uniform subpassInput gPosition;
layout (input_attachment_index = 3, set = 0, binding = 3) uniform subpassInput gDepth;

layout(location = 0) in vec2 inUv;
layout(location = 0) out vec4 outColor;

vec3 map(vec3 value, float oldLow, float oldHigh, float newLow, float newHigh) {
    return newLow + (value - oldLow) * (newHigh - newLow) / (oldHigh - oldLow);
} 

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
    //float roughness = abs(gNormal.z);
    float metalness = gNormal.a;

    // gPosition: RGB(Position)A(Translucency)
    vec3 position = gPosition.xyz;
    float roughness = gPosition.w;

    float depth = gDepth.r;


    const vec3  sun_direction = normalize(vec3(0.5,1.0,1.5));
    const float sun_intensity = 111000.0;
    const vec3  sun_color     = vec3(1.00, 0.97, 0.91);
    const vec3  sun_light     = sun_intensity * sun_color;

    const float sky_intensity = 20000.0;
    const vec3  sky_color     = vec3(0.36, 0.49, 0.72);
    const vec3  sky_light     = sky_intensity * sky_color;

    const float min_intensity = sky_intensity;
    const float max_intensity = sun_intensity;

    const vec3  gamma         = vec3(1.0/2.2);

    
    float sun_angle = max(dot(normal, sun_direction), 0.0);

    // halfway direction vector
    //vec3 halfway_dir = normalize(normalize() + normalize());

    vec3 raw_color = (albedo * (sky_light + sun_angle * sun_light));

    const float target_min_lightness = 0.5;
    const float target_max_lightness = 1.5;

    vec3 normalized_color = raw_color / max_intensity;

    vec3 gamma_corrected_color = pow(normalized_color, gamma);
    outColor = vec4(gamma_corrected_color, 1.0);
}