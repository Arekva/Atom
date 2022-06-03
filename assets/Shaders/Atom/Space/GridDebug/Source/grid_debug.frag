#version 460



layout(location = 0) flat in uint in_instance_index;

layout(location = 0) out vec4 out_color;



layout(set = 1, binding = 0) buffer InstanceData {
    vec4 colors[];
} _instanceData;


void main() {
    out_color = _instanceData.colors[in_instance_index];
}
