#version 460

layout(location = 0) out vec4 out_color;

layout(set = 1, binding = 3) uniform FragmentSettings {
    vec3 color;
} _fragmentSettings;


void main() {
    out_color = vec4(_fragmentSettings.color, 1.0);
}
