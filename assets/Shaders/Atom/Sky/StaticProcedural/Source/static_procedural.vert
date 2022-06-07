#version 460

#include <camera.glsl>

// vertex inputs
layout(location = 0) in vec3 in_position;
layout(location = 1) in vec2 in_uv;
layout(location = 2) in vec3 in_normal;
layout(location = 3) in vec4 in_tangent;

// vertex outputs
layout(location = 0) out vec3 out_normal;

mat4 get_model_matrix() {
    return mat4(1.0);
}

void main() {
    mat4 model_matrix   = get_model_matrix();
    vec3 position       = in_position;

    CameraVP cam = get_camera_matrices();

    out_normal = normalize(position);
    gl_Position = cam.projection * cam.view * model_matrix * vec4(position, 1.0);
}