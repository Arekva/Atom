#version 460

layout(binding = 0) buffer InstanceData {
    mat4 models[];
} _instanceData;

#include <camera.glsl>

#include <deferred_vertex_io.glsl>

mat4 get_model_matrix() {
    return _instanceData.models[gl_InstanceIndex * MAX_FRAMES_COUNT + get_current_frame()];
}

void main() {
    mat4 model_matrix   = get_model_matrix();
    vec3 position       = in_position;

    vec4 world_position = model_matrix * vec4(position, 1.0);
    vec2 world_uv       = in_uv                             ;
    vec3 world_normal   = mat3(model_matrix) * in_normal    ;
    
    out_position = world_position;
    out_uv       = world_uv      ;
    out_normal   = world_normal  ;

    CameraVP cam = get_camera_matrices();
    mat4 model_view_matrix = cam.view * model_matrix;

    gl_Position = cam.projection * model_view_matrix * vec4(position, 1.0);
}