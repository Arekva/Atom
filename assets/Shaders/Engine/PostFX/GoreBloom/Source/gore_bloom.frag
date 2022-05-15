#version 460

#include <tonemapping.glsl>

#include <light.glsl>

layout(set = 0, binding = 0) uniform sampler2D _render;

layout(location = 0) in vec2 in_uv;
layout(location = 0) out vec4 out_color;

vec3 map(vec3 value, float oldLow, float oldHigh, float newLow, float newHigh) {
    return newLow + (value - oldLow) * (newHigh - newLow) / (oldHigh - oldLow);
}

vec3 get_blur() {
    float blur_size = 5.0; 
    float invAspect = 1.0;//_ScreenParams.y / _ScreenParams.x;
    //init color variable
    vec3 col = vec3(0.0);
    //iterate over blur samples
    for(float index = 0; index < 10; index++) {
        //get uv coordinate of sample
        vec2 uv = in_uv.xy + vec2((index/9 - 0.5) * blur_size * invAspect, 0);
        //add color at position to color
        col += texture(_render, uv).rgb;
    }
    //divide the sum of values by the amount of samples
    col = col / 10;
    return col;
}

void main() {
    out_color = vec4(get_blur(), 0.2);
}