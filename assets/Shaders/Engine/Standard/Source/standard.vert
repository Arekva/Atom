#version 460

layout(binding = 0) buffer InstanceData {
    mat4 models[];
} _instanceData;

#include <camera.glsl>

#include <deferred_vertex_io.glsl>

void main() {
    mat4 model_matrix = _instanceData.models[gl_InstanceIndex];

    vec4 world_normal = vec4(in_normal, 1.0) * inverse(model_matrix);
    vec4 world_position = vec4(in_position, 1.0) * model_matrix;

    world_position.z *= -1.0;
    

    set_gl_position(model_matrix, in_position);

    out_position = world_position;
    out_uv = in_uv;
    out_normal = world_normal.xyz;
}