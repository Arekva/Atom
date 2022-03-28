/* Start of light.glsl */



/* light_type enum */
const uint LIGHT_TYPE_UNDEFINED   = 0x00000000U; /* undefined or error  */
const uint LIGHT_TYPE_DIRECTIONAL = 0x00000001U; /* orthographic        */
const uint LIGHT_TYPE_POINT       = 0x00000002U; /* perspective cubemap */
const uint LIGHT_TYPE_SPOT        = 0x00000003U; /* perspective         */

const uint NO_SHADOWS             = 0xFFFFFFFFU;

struct light {
    /* general light settings       */
    uint        type                 ; /* light type enum value         */
    vec3        color                ; /* emit color of the light       */
    float       power                ; /* emmision power (lux)          */
    float       emitter_size         ; /* radius of source as a sphere  */
    vec3        position             ; /* world position                */
    float       near_clip            ; /* near clip                     */
    float       far_clip             ; /* far clip                      */
    bool        cast_shadows         ; /* do shadows need to be sampled */
    
    /* directional lights           */
    float       width                ; /* orthographic width            */
    float       height               ; /* orthographic height           */

    /* point lights                 */
    uint        point_shadowmap      ; /* depth shadow map (cube) ID    */
    
    /* spot lights                  */
    float       fov_x                ; /* horizontal field of view      */
    float       fov_y                ; /* vertical field of view        */

    /* directional and spot lights  */
    uint        directional_shadowmap; /* depth shadow map (2D) ID      */
    vec3        direction            ; /* forward direction             */
};


layout(binding = 4) buffer Lights {
    uint  count   ;
    light lights[];
} _lights;

layout(binding = 5) uniform sampler2D _directionalShadows[32];

layout(binding = 6) uniform samplerCube _pointShadows[32];

/* End of light.glsl */



