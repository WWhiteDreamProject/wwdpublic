#version 140
#define HAS_MOD
#define HAS_DFDX
#define HAS_FLOAT_TEXTURES
#define HAS_SRGB
#define HAS_UNIFORM_BUFFERS
#define VERTEX_SHADER

// -- Utilities Start --

// It's literally just called the Z-Library for alphabetical ordering reasons.
//  - 20kdc

// -- varying/attribute/texture2D --

#ifndef HAS_VARYING_ATTRIBUTE
#define texture2D texture
#ifdef VERTEX_SHADER
#define varying out
#define attribute in
#else
#define varying in
#define attribute in
#define gl_FragColor colourOutput
out highp vec4 colourOutput;
#endif
#endif

#ifndef NO_ARRAY_PRECISION
#define ARRAY_LOWP lowp
#define ARRAY_MEDIUMP mediump
#define ARRAY_HIGHP highp
#else
#define ARRAY_LOWP lowp
#define ARRAY_MEDIUMP mediump
#define ARRAY_HIGHP highp
#endif

// -- shadow depth --

highp vec4 zClydeShadowDepthPack(highp vec2 val) {
#ifdef HAS_FLOAT_TEXTURES
    return vec4(val, 0.0, 1.0);
#else
    highp vec2 valH = floor(val);
    return vec4(valH / 255.0, val - valH);
#endif
}

// Inverts the previous function.
highp vec2 zClydeShadowDepthUnpack(highp vec4 val) {
#ifdef HAS_FLOAT_TEXTURES
    return val.xy;
#else
    return (val.xy * 255.0) + val.zw;
#endif
}

// -- srgb/linear conversion core --

highp vec4 zFromSrgb(highp vec4 sRGB)
{
    highp vec3 higher = pow((sRGB.rgb + 0.055) / 1.055, vec3(2.4));
    highp vec3 lower = sRGB.rgb / 12.92;
    highp vec3 s = max(vec3(0.0), sign(sRGB.rgb - 0.04045));
    return vec4(mix(lower, higher, s), sRGB.a);
}

highp vec4 zToSrgb(highp vec4 sRGB)
{
    highp vec3 higher = (pow(sRGB.rgb, vec3(0.41666666666667)) * 1.055) - 0.055;
    highp vec3 lower = sRGB.rgb * 12.92;
    highp vec3 s = max(vec3(0.0), sign(sRGB.rgb - 0.0031308));
    return vec4(mix(lower, higher, s), sRGB.a);
}

// -- uniforms --

#ifdef HAS_UNIFORM_BUFFERS
layout (std140) uniform projectionViewMatrices
{
    highp mat3 projectionMatrix;
    highp mat3 viewMatrix;
};

layout (std140) uniform uniformConstants
{
    highp vec2 SCREEN_PIXEL_SIZE;
    highp float TIME;
};
#else
uniform highp mat3 projectionMatrix;
uniform highp mat3 viewMatrix;
uniform highp vec2 SCREEN_PIXEL_SIZE;
uniform highp float TIME;
#endif

uniform sampler2D TEXTURE;
uniform highp vec2 TEXTURE_PIXEL_SIZE;

// -- srgb emulation --

#ifdef HAS_SRGB

highp vec4 zTextureSpec(sampler2D tex, highp vec2 uv)
{
    return texture2D(tex, uv);
}

highp vec4 zAdjustResult(highp vec4 col)
{
    return col;
}
#else
uniform lowp vec2 SRGB_EMU_CONFIG;

highp vec4 zTextureSpec(sampler2D tex, highp vec2 uv)
{
    highp vec4 col = texture2D(tex, uv);
    if (SRGB_EMU_CONFIG.x > 0.5)
    {
        return zFromSrgb(col);
    }
    return col;
}

highp vec4 zAdjustResult(highp vec4 col)
{
    if (SRGB_EMU_CONFIG.y > 0.5)
    {
        return zToSrgb(col);
    }
    return col;
}
#endif

highp vec4 zTexture(highp vec2 uv)
{
    return zTextureSpec(TEXTURE, uv);
}

// -- color --

highp float zGrayscale_BT709(highp vec3 col) {
    return dot(col, vec3(0.2126, 0.7152, 0.0722));
}

// Grayscale function for the ITU's Rec BT-601, primarily intended for SDTV, but amazing for a handful of niche use-cases.
highp float zGrayscale_BT601(highp vec3 col) {
    return dot(col, vec3(0.299, 0.587, 0.114));
}

highp float zGrayscale(highp vec3 col) {
    return zGrayscale_BT709(col);
}

// -- noise --

highp vec2 zRandom(highp vec2 uv){
    uv = vec2( dot(uv, vec2(127.1,311.7) ),
               dot(uv, vec2(269.5,183.3) ) );
    return -1.0 + 2.0 * fract(sin(uv) * 43758.5453123);
}

highp float zNoise(highp vec2 uv) {
    highp vec2 uv_index = floor(uv);
    highp vec2 uv_fract = fract(uv);

    highp vec2 blur = smoothstep(0.0, 1.0, uv_fract);

    return mix( mix( dot( zRandom(uv_index + vec2(0.0,0.0) ), uv_fract - vec2(0.0,0.0) ),
                     dot( zRandom(uv_index + vec2(1.0,0.0) ), uv_fract - vec2(1.0,0.0) ), blur.x),
                mix( dot( zRandom(uv_index + vec2(0.0,1.0) ), uv_fract - vec2(0.0,1.0) ),
                     dot( zRandom(uv_index + vec2(1.0,1.0) ), uv_fract - vec2(1.0,1.0) ), blur.x), blur.y) * 0.5 + 0.5;
}

highp float zFBM(highp vec2 uv) {
    const int octaves = 6;
    highp float amplitude = 0.5;
    highp float frequency = 3.0;
    highp float value = 0.0;

    for(int i = 0; i < octaves; i++) {
        value += amplitude * zNoise(frequency * uv);
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    return value;
}


// -- generative --

// Function that creates a circular gradient. Screenspace shader bread n butter.
highp float zCircleGradient(highp vec2 ps, highp vec2 coord, highp float maxi, highp float radius, highp float dist, highp float power) {
    highp float rad = (radius * ps.y) * 0.001;
    highp float aspectratio = ps.x / ps.y;
    highp vec2 totaldistance = ((ps * 0.5) - coord) / (rad * ps);
    totaldistance.x *= aspectratio;
    highp float length = (length(totaldistance) * ps.y) - dist;
    return pow(clamp(length, 0.0, maxi), power);
}

// -- Utilities End --

attribute vec2 aPos;
attribute vec2 tCoord;
attribute vec2 tCoord2;
attribute vec4 modulate;

varying vec2 UV;
varying vec2 UV2;
varying vec2 Pos;
varying vec4 VtxModulate;

uniform mat3 modelMatrix;

// Allows us to do texture atlassing with texture coordinates 0->1
// Input texture coordinates get mapped to this range.
uniform vec4 modifyUV;

uniform sampler2D SCREEN_TEXTURE;
uniform ARRAY_HIGHP float SCANLINE_INTENSITY;
uniform ARRAY_HIGHP float DISTORTION;
uniform ARRAY_HIGHP float TIME_COEFFICIENT;
uniform ARRAY_HIGHP float GLITCH_INTENSITY;
uniform ARRAY_HIGHP float IMPACT_DARKNESS;
uniform ARRAY_HIGHP float DEATH_EFFECT;


ARRAY_HIGHP float rand( ARRAY_HIGHP vec2 co) {
 return fract ( sin ( dot ( co . xy , vec2 ( 12.9898 , 78.233 ) ) ) * 43758.5453 ) ;

}
ARRAY_HIGHP vec3 neonGlow( ARRAY_HIGHP vec3 color,  ARRAY_HIGHP float intensity) {
 highp float brightness = dot ( color , vec3 ( 0.299 , 0.587 , 0.114 ) ) ;
 highp vec3 neon = vec3 ( 0.0 ) ;
 if ( brightness > 0.65 ) {
 neon += vec3 ( 0.0 , 1.0 , 1.0 ) * ( brightness - 0.65 ) * 2.5 ;
 }
 if ( brightness > 0.45 && brightness < 0.75 ) {
 neon += vec3 ( 1.0 , 0.0 , 0.8 ) * ( brightness - 0.45 ) * 1.8 ;
 }
 return color + neon * intensity ;

}
bool isUIElement( ARRAY_HIGHP vec4 color) {
 highp float brightness = dot ( color . rgb , vec3 ( 0.299 , 0.587 , 0.114 ) ) ;
 if ( brightness > 0.9 && color . a > 0.9 ) {
 return true ;
 }
 if ( ( color . r > 0.9 && color . g < 0.3 && color . b < 0.3 ) || ( color . r < 0.3 && color . g > 0.9 && color . b < 0.3 ) || ( color . r < 0.3 && color . g < 0.3 && color . b > 0.9 ) || ( color . r > 0.9 && color . g > 0.9 && color . b > 0.9 ) ) {
 return true ;
 }
 return false ;

}
bool drawChar( ARRAY_HIGHP vec2 pos,  ARRAY_HIGHP vec2 size,  ARRAY_HIGHP int charIndex,  ARRAY_HIGHP vec2 uv) {
 highp vec2 charPos = ( uv - pos ) / size ;
 if ( charPos . x >= 0.0 && charPos . x < 1.0 && charPos . y >= 0.0 && charPos . y < 1.0 ) {
 highp int x = highp int ( charPos . x * 5.0 ) ;
 highp int y = highp int ( charPos . y * 7.0 ) ;
 if ( charIndex == highp int ( 0 ) ) {
 bool pixels [ 35 ] = bool [ 35 ] ( false , true , true , true , false , true , false , false , false , false , true , false , false , false , false , false , true , true , true , false , false , false , false , false , true , false , false , false , false , true , false , true , true , true , false ) ;
 return pixels [ y * highp int ( 5 ) + x ] ;
 }
 else if ( charIndex == highp int ( 1 ) ) {
 bool pixels [ 35 ] = bool [ 35 ] ( true , false , false , false , true , true , false , false , false , true , false , true , false , true , false , false , false , true , false , false , false , false , true , false , false , false , false , true , false , false , false , false , true , false , false ) ;
 return pixels [ y * highp int ( 5 ) + x ] ;
 }
 else if ( charIndex == highp int ( 2 ) ) {
 bool pixels [ 35 ] = bool [ 35 ] ( false , true , true , true , false , true , false , false , false , false , true , false , false , false , false , false , true , true , true , false , false , false , false , false , true , false , false , false , false , true , false , true , true , true , false ) ;
 return pixels [ y * highp int ( 5 ) + x ] ;
 }
 else if ( charIndex == highp int ( 3 ) ) {
 bool pixels [ 35 ] = bool [ 35 ] ( true , true , true , true , true , false , false , true , false , false , false , false , true , false , false , false , false , true , false , false , false , false , true , false , false , false , false , true , false , false , false , false , true , false , false ) ;
 return pixels [ y * highp int ( 5 ) + x ] ;
 }
 else if ( charIndex == highp int ( 4 ) ) {
 bool pixels [ 35 ] = bool [ 35 ] ( true , true , true , true , true , true , false , false , false , false , true , false , false , false , false , true , true , true , true , false , true , false , false , false , false , true , false , false , false , false , true , true , true , true , true ) ;
 return pixels [ y * highp int ( 5 ) + x ] ;
 }
 else if ( charIndex == highp int ( 5 ) ) {
 bool pixels [ 35 ] = bool [ 35 ] ( true , false , false , false , true , true , true , false , true , true , true , false , true , false , true , true , false , false , false , true , true , false , false , false , true , true , false , false , false , true , true , false , false , false , true ) ;
 return pixels [ y * highp int ( 5 ) + x ] ;
 }
 else if ( charIndex == highp int ( 6 ) ) {
 bool pixels [ 35 ] = bool [ 35 ] ( true , true , true , true , false , true , false , false , false , true , true , false , false , false , true , true , true , true , true , false , true , false , true , false , false , true , false , false , true , false , true , false , false , false , true ) ;
 return pixels [ y * highp int ( 5 ) + x ] ;
 }
 else if ( charIndex == highp int ( 7 ) ) {
 bool pixels [ 35 ] = bool [ 35 ] ( true , true , true , true , false , true , false , false , false , true , true , false , false , false , true , true , true , true , true , false , true , false , true , false , false , true , false , false , true , false , true , false , false , false , true ) ;
 return pixels [ y * highp int ( 5 ) + x ] ;
 }
 else if ( charIndex == highp int ( 8 ) ) {
 bool pixels [ 35 ] = bool [ 35 ] ( false , true , true , true , false , true , false , false , false , true , true , false , false , false , true , true , false , false , false , true , true , false , false , false , true , true , false , false , false , true , false , true , true , true , false ) ;
 return pixels [ y * highp int ( 5 ) + x ] ;
 }
 else if ( charIndex == highp int ( 9 ) ) {
 bool pixels [ 35 ] = bool [ 35 ] ( true , true , true , true , false , true , false , false , false , true , true , false , false , false , true , true , true , true , true , false , true , false , true , false , false , true , false , false , true , false , true , false , false , false , true ) ;
 return pixels [ y * highp int ( 5 ) + x ] ;
 }
 else if ( charIndex == highp int ( 10 ) ) {
 bool pixels [ 35 ] = bool [ 35 ] ( true , true , true , true , false , true , false , false , false , true , true , false , false , false , true , true , true , true , true , false , true , false , true , false , false , true , false , false , true , false , true , false , false , false , true ) ;
 return pixels [ y * highp int ( 5 ) + x ] ;
 }
 }
 return false ;

}


void main()
{
    vec3 transformed = projectionMatrix * viewMatrix * modelMatrix * vec3(aPos, 1.0);
    vec2 VERTEX = transformed.xy;

    // [SHADER_CODE]

    // Pixel snapping to avoid sampling issues on nvidia.
    VERTEX += 1.0;
    VERTEX /= SCREEN_PIXEL_SIZE*2.0;
    VERTEX = floor(VERTEX + 0.5);
    VERTEX *= SCREEN_PIXEL_SIZE*2.0;
    VERTEX -= 1.0;

    gl_Position = vec4(VERTEX, 0.0, 1.0);
    Pos = (VERTEX + 1.0) / 2.0;
    UV = mix(modifyUV.xy, modifyUV.zw, tCoord);
    UV2 = tCoord2;
    VtxModulate = zFromSrgb(modulate);
}
