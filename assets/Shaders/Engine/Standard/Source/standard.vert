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



// vertex inputs
layout(location = 0) in vec3 in_position;
layout(location = 1) in vec2 in_uv;
layout(location = 2) in vec3 in_normal;
layout(location = 3) in vec4 in_tangent; // ignored

layout(location = 0) out vec4 out_position;
layout(location = 1) out vec2 out_uv;
layout(location = 2) out vec3 out_normal;

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