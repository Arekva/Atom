#version 460

layout(location = 0) out vec2 outUv;

vec4[3] vertices = vec4[3]
(
    vec4(-1.0,-3.0, 0.0, 1.0),
    vec4(-1.0, 1.0, 0.0, 1.0),
    vec4( 3.0, 1.0, 0.0, 1.0)
);

void main() {
    outUv = 1.0 - ((vertices[gl_VertexIndex].xy + vec2(1.0)) / 2.0);
    
    gl_Position = vertices[gl_VertexIndex];
}