/* Start of deferred_fragment_io.glsl */

// G-Buffer write data
layout(location = 0) out vec4 out_G_albedo_luminance;           // RGB(Albedo) A(Luminance)
layout(location = 1) out vec4 out_G_normal_roughness_metalness; // RG(Normal)B(Roughness + Z Normal sign)A(Metalness)
layout(location = 2) out vec4 out_G_position_translucency;      // RGB(Position)A(Translucency)
// /* done auto. */ layout(location = 3) out float out_G_depth; // R(Depth)

// Vertex data
layout(location = 0) in vec4 in_position;
layout(location = 1) in vec2 in_uv;
layout(location = 2) in mat3 in_tbn;

/* End of deferred_fragment_io.glsl */