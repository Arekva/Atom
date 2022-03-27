#version 460

layout(binding = 0) buffer InstanceData {
    mat4 models[];
} _instanceData;

layout(binding = 2) uniform sampler2D _height;
/*layout(binding = 3) uniform Settings {
    
} _settings;*/

#include <camera.glsl>

#include <deferred_vertex_io.glsl>



mat4 get_model_matrix() {
    return _instanceData.models[gl_InstanceIndex * MAX_FRAMES_COUNT + get_current_frame()];
}

void main() {
    const float HEIGHT = 0.05;

    mat4 model_matrix   = get_model_matrix();
    vec3 position       = in_position + in_normal * texture(_height, in_uv).r * HEIGHT;
    
    mat3 model_rot_matrix = mat3(model_matrix);

    vec3 /*T*/ tangent  = normalize(model_rot_matrix * in_tangent.xyz);
    vec3 /*N*/ normal   = normalize(model_rot_matrix * in_normal);
    vec3 /*B*/ binormal = normalize(model_rot_matrix * (cross(in_normal, in_tangent.xyz) * in_tangent.w));

    vec4 world_position = model_matrix * vec4(position, 1.0);
    vec2 world_uv       = in_uv                             ;
    mat3 world_normal   = mat3(tangent, binormal, normal)   ;//normalize(mat3(model_matrix) * in_normal);
    
    out_position = world_position;
    out_uv       = world_uv      ;
    out_normal   = world_normal  ;

    CameraVP cam = get_camera_matrices();
    mat4 model_view_matrix = cam.view * model_matrix;

    gl_Position = cam.projection * model_view_matrix * vec4(position, 1.0);
}