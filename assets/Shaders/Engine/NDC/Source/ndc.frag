#version 460

layout(location = 0) in vec2 inUv;
layout(location = 0) out vec4 outColor;

layout(binding = 0) uniform sampler2D image;

void main() {
    outColor = vec4(texture(image, inUv).rgb, 1.0); // force alpha to 1.0
}