#version 460



layout(location = 0) in vec3 in_position;

layout(location = 0) flat out uint out_instance_index;



layout(set = 0, binding = 0) buffer InstanceData {
    mat4 models[];
} _instanceData;

#include <camera.glsl>


mat4 get_model_matrix() {
    return _instanceData.models[gl_InstanceIndex * MAX_FRAMES_COUNT + get_current_frame()];
}

void main() {
    mat4 model_matrix   = get_model_matrix();
    CameraVP cam = get_camera_matrices();

    out_instance_index = gl_InstanceIndex;

    gl_Position = cam.projection * cam.view * model_matrix * vec4(in_position, 1.0);
}