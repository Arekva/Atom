/* Start of deferred_vertex_io.glsl */

// vertex inputs
layout(location = 0) in vec3 in_position;
layout(location = 1) in vec2 in_uv;
layout(location = 2) in vec3 in_normal;
layout(location = 3) in vec4 in_tangent; // ignored

// vertex outputs
layout(location = 0) out vec4 out_position;
layout(location = 1) out vec2 out_uv;
layout(location = 2) out vec3 out_normal;

/* End of deferred_vertex_io.glsl */