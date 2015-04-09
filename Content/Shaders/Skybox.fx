uniform float4x4 xWorld;
uniform float4x4 xView;
uniform float4x4 xProjection;
uniform float3 xCameraPosition;
uniform texture xBaseTexture;

sampler cubeTexture = 
sampler_state
{
    Texture = < xBaseTexture >;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    ADDRESSU = CLAMP;
    ADDRESSV = CLAMP;
};

struct VSSkyboxIn
{
    float4 position	: POSITION;
};

struct VSSkyboxOut 
{
   float4 position	: POSITION;
   float3 viewDir	: TEXCOORD0;
};

VSSkyboxOut VSSkybox(VSSkyboxIn input)
{
	VSSkyboxOut output;

	float4 worldPosition = mul(input.position, xWorld);
    float4 viewPosition = mul(worldPosition, xView);
    output.position = mul(viewPosition, xProjection);
    output.viewDir = worldPosition - xCameraPosition;
    
    return output;
}

float4 PSSkybox(VSSkyboxOut input) : COLOR0
{
	float3 V = normalize(input.viewDir);
    return texCUBE(cubeTexture, V);
}

technique Skybox
{
   pass Pass0
   {
        VertexShader = compile vs_2_0 VSSkybox();
        PixelShader = compile ps_2_0 PSSkybox();
   }
}
