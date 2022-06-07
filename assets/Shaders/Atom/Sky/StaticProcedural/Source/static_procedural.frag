#version 460

// G-Buffer write data
layout(location = 0) out vec4 out_G_albedo_luminance;           // RGB(Albedo) A(Luminance)
layout(location = 1) out vec4 out_G_normal_roughness_metalness; // RG(Normal)B(Roughness + Z Normal sign)A(Metalness)
layout(location = 2) out vec4 out_G_position_translucency;      // RGB(Position)A(Translucency)
// /* done auto. */ layout(location = 3) out float out_G_depth; // R(Depth)

layout(location = 0) in vec3 in_normal;



layout(set = 1, binding = 0) uniform ColorSettings {
    vec4 day;
} _colorSettings;



const float POSITIVE_INFINITY = 1.0 / 0.0;


float hash(float n) { return fract(sin(n) * 1e4); }
// This one has non-ideal tiling properties that I'm still tuning
float noise(vec3 x) {
	const vec3 step = vec3(110, 241, 171);

	vec3 i = floor(x);
	vec3 f = fract(x);
 
	// For performance, compute the base input to a 1D hash from the integer part of the argument and the 
	// incremental change to the 1D based on the 3D -> 1D wrapping
    float n = dot(i, step);

	vec3 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(mix( hash(n + dot(step, vec3(0, 0, 0))), hash(n + dot(step, vec3(1, 0, 0))), u.x),
                   mix( hash(n + dot(step, vec3(0, 1, 0))), hash(n + dot(step, vec3(1, 1, 0))), u.x), u.y),
               mix(mix( hash(n + dot(step, vec3(0, 0, 1))), hash(n + dot(step, vec3(1, 0, 1))), u.x),
                   mix( hash(n + dot(step, vec3(0, 1, 1))), hash(n + dot(step, vec3(1, 1, 1))), u.x), u.y), u.z);
}

void main() {

    vec3 view_dir = in_normal;

    const vec4 STAR_COLOR = vec4(1.0, 1.0, 1.0, 50000.0);

    out_G_albedo_luminance = noise(view_dir * 100.0) > 0.95 ? STAR_COLOR : vec4(0.0);//_colorSettings.day;
    out_G_normal_roughness_metalness = vec4(0);
    out_G_position_translucency = vec4(vec3(POSITIVE_INFINITY), 0.0);
}