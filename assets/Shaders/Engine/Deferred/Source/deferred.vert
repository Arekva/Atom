// Same as NDC

#version 460

layout(location = 0) out vec2 outUv;

vec4[3] vertices = vec4[3]
(
    vec4(-1.0,-3.0, 0.0, 1.0),
    vec4(-1.0, 1.0, 0.0, 1.0),
    vec4( 3.0, 1.0, 0.0, 1.0)
);

void main() {
    outUv = (vertices[gl_VertexIndex].xy + vec2(1.0)) / 2.0;
    outUv.y = 1.0 - outUv.y;
    
    gl_Position = vertices[gl_VertexIndex];
}