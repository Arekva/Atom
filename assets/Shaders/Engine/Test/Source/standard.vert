#version 460

layout(binding = 0) buffer InstanceData {
    mat4 models[];
} _instanceData;

#include <camera.glsl>

#include <deferred_vertex_io.glsl>

void main() {
    set_gl_position(_instanceData.models[gl_InstanceIndex], in_position);

    out_position = gl_Position;
    out_uv = in_uv;
    out_normal = in_normal;
}