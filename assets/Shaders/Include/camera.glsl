/* Start of camera.glsl */

const uint MAX_FRAMES_COUNT = 3;
const uint MAX_CAMERA_COUNT = 1024;

// Camera View-Projection matrices
struct CameraVP {
    mat4 view;
    mat4 projection;
};

// All the cameras' view-projection matrices
layout(binding = 1) buffer CameraMatrices {
    CameraVP matrices[MAX_CAMERA_COUNT];
} _cameraMatrices;

// The camera index that is currently rendering that object 
layout( push_constant ) uniform Constants {
    uint value;
    uint frame;
} _currentCameraIndex;

// Standard gl_Position set with projection*view*model*in_Position
void set_gl_position(mat4 model_matrix, vec3 vertex_position) {

    uint camera_index = _currentCameraIndex.value;
    uint current_frame = _currentCameraIndex.frame;

    CameraVP cam = _cameraMatrices.matrices[camera_index * MAX_FRAMES_COUNT + current_frame];
 
    gl_Position = cam.projection * cam.view * model_matrix * vec4(vertex_position, 1.0);
}

/* End of camera.glsl */