#version 460

layout(binding = 0) buffer InstanceData {
    mat4 models[];
} _instanceData;

layout(binding = 2) uniform sampler2D _height;
layout(binding = 3) uniform Settings {
    float height;
} _settings;

#include <camera.glsl>

// vertex inputs
layout(location = 0) in vec3 in_position;
layout(location = 1) in vec2 in_uv;
layout(location = 2) in vec3 in_normal;
layout(location = 3) in vec4 in_tangent;


mat4 get_model_matrix() {
    return _instanceData.models[gl_InstanceIndex * MAX_FRAMES_COUNT + get_current_frame()];
}

void main() {
    mat4 model_matrix   = get_model_matrix();
    vec3 position       = in_position + in_normal * texture(_height, in_uv).r * _settings.height;

    CameraVP cam = get_camera_matrices();
    gl_Position = cam.projection * cam.view * model_matrix * vec4(position, 1.0);
}