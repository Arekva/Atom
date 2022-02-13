#version 460

struct CameraVP {
    mat4 view;
    mat4 projection;
};


layout(binding = 0) buffer InstanceData {
    mat4 models[];
} _instanceData;

layout(binding = 1) buffer CameraMatrices {
    CameraVP matrices[];
} _cameraMatrices;

layout( push_constant ) uniform Constants {
    uint camera_index;
} _constants;


#include <deferred_vertex_io.glsl>

void main() {
    
    uint camera_index = _constants.camera_index;
    mat4 model = _instanceData.models[gl_InstanceIndex];
    mat4 view = _cameraMatrices.matrices[camera_index].view;
    mat4 projection = _cameraMatrices.matrices[camera_index].projection;
    
    gl_Position = projection * view * model * vec4(in_position, 1.0);
    out_position = gl_Position;
    out_uv = in_uv;
    out_normal = in_normal;
}