/* Start of fullscreen_vertex.glsl */

layout(location = 0) out vec2 out_uv;

vec4[3] vertices = vec4[3]
(
    /*vec4(-1.0,-3.0, 0.0, 1.0),
    vec4(-1.0, 1.0, 0.0, 1.0),
    vec4( 3.0, 1.0, 0.0, 1.0)*/

    vec4(-1.0,  1.0, 0.0, 1.0),
    vec4(-1.0, -3.0, 0.0, 1.0),
    vec4( 3.0,  1.0, 0.0, 1.0)
);

void fullscreen() {
    out_uv = (vertices[gl_VertexIndex].xy + vec2(1.0)) / 2.0;
    
    gl_Position = vertices[gl_VertexIndex];
}

void fullscreen_invert() {
    out_uv = 1.0 - ((vertices[gl_VertexIndex].xy + vec2(1.0)) / 2.0);
    
    gl_Position = vertices[gl_VertexIndex];
}

/* End of fullscreen_vertex.glsl */