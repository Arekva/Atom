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
layout(push_constant) uniform Constants {
    uint value;
    uint frame;
} _currentCameraIndex;

uint get_current_frame() {
    return _currentCameraIndex.frame;
}

uint get_camera_index() {
    uint camera_index = _currentCameraIndex.value;
    uint current_frame = get_current_frame();
    
    return camera_index * MAX_FRAMES_COUNT + current_frame;
}

CameraVP get_camera_matrices(uint index) {
    return _cameraMatrices.matrices[index];
}

CameraVP get_camera_matrices() {
    return _cameraMatrices.matrices[get_camera_index()];
}

// Standard gl_Position set with projection*view*model*in_Position
void set_gl_position(mat4 model_matrix, vec3 vertex_position) {
    CameraVP cam = get_camera_matrices();

    mat4 model_view_matrix = cam.view * model_matrix;
 
    gl_Position = cam.projection * model_view_matrix * vec4(vertex_position, 1.0);
}

/* End of camera.glsl */