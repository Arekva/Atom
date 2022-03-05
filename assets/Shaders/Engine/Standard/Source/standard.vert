#version 460

layout(binding = 0) buffer InstanceData {
    mat4 models[];
} _instanceData;

#include <camera.glsl>

#include <deferred_vertex_io.glsl>

void main() {
    mat4 model_matrix = _instanceData.models[gl_InstanceIndex];

    
    
    //vec4(in_normal, 1.0) * model_matrix;

    vec4 world_position = model_matrix * vec4(in_position, 1.0);

    /*set_gl_position(model_matrix, in_position);*/
    CameraVP cam = get_camera_matrices();

    mat4 model_view_matrix = cam.view * model_matrix;

    vec3 world_normal = normalize(mat3(model_matrix) * in_normal);
 
    gl_Position = cam.projection * model_view_matrix * vec4(in_position, 1.0);

    mat4 normal_matrix = transpose(inverse(model_view_matrix));

    

    out_position = world_position;
    out_uv = in_uv;
    out_normal = world_normal;
}