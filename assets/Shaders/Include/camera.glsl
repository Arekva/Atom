/* Start of camera.glsl */

// Camera View-Projection matrices
struct CameraVP {
    mat4 view;
    mat4 projection;
};

// All the cameras' view-projection matrices
layout(binding = 1) buffer CameraMatrices {
    CameraVP matrices[];
} _cameraMatrices;

// The camera index that is currently rendering that object 
layout( push_constant ) uniform Constants {
    uint value;
} _currentCameraIndex;

// Standard gl_Position set with projection*view*model*in_Position
void set_gl_position(mat4 model_matrix, vec3 vertex_position) {
    uint camera_index = _currentCameraIndex.value;
    CameraVP vp = _cameraMatrices.matrices[camera_index];
    gl_Position = vp.projection * vp.view * model_matrix * vec4(vertex_position, 1.0);
}

/* End of camera.glsl */