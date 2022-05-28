#version 460

layout(location = 0) in vec3 in_position;

layout(set = 0, binding = 2) uniform VertexSettings {
    mat4 model_matrix;
} _vertexSettings;

#include <camera.glsl>



mat4 get_model_matrix() {
    return _vertexSettings.model_matrix;
}

void main() {
    mat4 model_matrix   = get_model_matrix();
    CameraVP cam = get_camera_matrices();

    gl_Position = cam.projection * cam.view * model_matrix * vec4(in_position, 1.0);
}