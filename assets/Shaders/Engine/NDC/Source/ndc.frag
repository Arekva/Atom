#version 460

layout(location = 0) in vec2 in_uv;
layout(location = 0) out vec4 out_color;

layout(binding = 0) uniform sampler2D image;

void main() {
    out_color = vec4(texture(image, in_uv).rgb, 1.0); // force alpha to 1.0
}