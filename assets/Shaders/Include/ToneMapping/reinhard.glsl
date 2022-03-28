/* Start of ToneMapping/reinhard.glsl */

vec3 tonemapping_reinhard(vec3 hdr_color, float exposure) {
    const vec3 GAMMA    = vec3(1.0/2.2);

    // map HDR to reinhard
    vec3 raw_mapped = hdr_color / (hdr_color + 1.0);

    vec3 gamma_corrected = pow(raw_mapped, GAMMA);

    return gamma_corrected;
}

vec3 tonemapping_reinhard_exposure(vec3 hdr_color, vec3 exposure) {
    const vec3 GAMMA    = vec3(1.0/2.2);

    // map HDR to reinhard
    vec3 raw_mapped = vec3(1.0) - exp(-hdr_color * exposure);

    vec3 gamma_corrected = pow(raw_mapped, GAMMA);

    return gamma_corrected;
}

/* End of ToneMapping/reinhard.glsl */