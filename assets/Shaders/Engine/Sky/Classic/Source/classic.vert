#version 460

layout(binding = 0) buffer InstanceData {
    mat4 models[];
} _instanceData;

// vertex inputs
layout(location = 0) in vec3 in_position;
layout(location = 1) in vec2 in_uv;
layout(location = 2) in vec3 in_normal;
layout(location = 3) in vec4 in_tangent; // ignored

#include <camera.glsl>

void main() {
    mat4 model_matrix = _instanceData.models[gl_InstanceIndex];
    
    //set_gl_position(_instanceData.models[gl_InstanceIndex], in_position);

    CameraVP cam = _cameraMatrices.matrices[get_camera_index()];
 
    gl_Position = cam.projection * cam.view * model_matrix * vec4(in_position, 1.0);
}