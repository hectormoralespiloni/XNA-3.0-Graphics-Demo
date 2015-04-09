//Globals used in all shaders
uniform float4x4 xWorld;
uniform float4x4 xView;
uniform float4x4 xProjection;
uniform float3 xLightPosition;
uniform float3 xCameraPosition;
uniform float3 xDiffuseColor;
uniform float3 xLightColor;
uniform float3 xAmbientColor;
uniform float3 xSpecularColor;
uniform texture xTexture;

//Used in charcoal rendering
uniform texture noiseTexture;	//noise texture
uniform texture paperTexture;	//paper texture
uniform texture CET;			//pre-computed contrast enhanced texture
uniform float ambient;			//ambient light's component

//Used in Procedural stripe rendering
uniform float3 backColor;
uniform float3 stripeColor;
uniform float Kd;
uniform float Fuzz;
uniform float Width;

//Used in environment mapping
uniform texture textureCube;
uniform float reflectness;

sampler originalTexture = 
sampler_state 
{
    Texture = < xTexture >;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    ADDRESSU = CLAMP;
    ADDRESSV = CLAMP;
};

//-----------------------------------------------------------------------------
//PHONG SHADING
//-----------------------------------------------------------------------------
struct VSPhongIn
{
    float4 position : POSITION0;
	float3 normal	: NORMAL0;
	float2 texCoord	: TEXCOORD0;
};

struct VSPhongOut
{
    float4 position : POSITION0;
    float3 normal	: TEXCOORD0;
    float3 lightVec : TEXCOORD1;
    float3 viewVec	: TEXCOORD2;
    float2 texCoord	: TEXCOORD3;
};

VSPhongOut VSPhong(VSPhongIn input)
{
    VSPhongOut output;

    float4 worldPosition = mul(input.position, xWorld);
    float4 viewPosition = mul(worldPosition, xView);
    
    output.position = mul(viewPosition, xProjection);
	output.normal = mul(input.normal, xWorld);
	output.lightVec = normalize(xLightPosition - worldPosition);
	output.viewVec = normalize(xCameraPosition - worldPosition);
	output.texCoord = input.texCoord;
    
    return output;
}

float4 PSPhong(VSPhongOut input) : COLOR0
{
	//compute diffuse term
    float3 N = normalize(input.normal);
    float3 L = normalize(input.lightVec);
    float3 V = normalize(input.viewVec);
    float Kd = saturate(dot(L, N));

    //compute specular term
    float3 H = normalize(L + V);
    float Ks = pow(saturate(dot(H, N)), 32);
    
    float4 texColor = tex2D(originalTexture, input.texCoord);
    float4 ambient = float4(xAmbientColor,1);
    float4 diffuse = float4(xDiffuseColor*Kd,1);
    float4 specular = float4(xSpecularColor*Ks,1);
    
    return ambient + (texColor * diffuse) + Ks; 
}

//-----------------------------------------------------------------------------
//CHARCOAL RENDERING
//-----------------------------------------------------------------------------
sampler noiseSampler = 
sampler_state
{
    Texture = < noiseTexture >;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    ADDRESSU = CLAMP;
    ADDRESSV = CLAMP;
};

sampler paperSampler = 
sampler_state
{
    Texture = < paperTexture >;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    ADDRESSU = CLAMP;
    ADDRESSV = CLAMP;
};

sampler cetSampler = 
sampler_state
{
    Texture = < CET >;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
    ADDRESSU = CLAMP;
    ADDRESSV = CLAMP;
};

struct VSCharcoalIn
{
    float4 position : POSITION0;
	float3 normal	: NORMAL0;
};

struct VSCharcoalOut
{
	float4 position		: POSITION0;
	float2 paperCoord	: TEXCOORD0;	//paper texture coordinates
	float2 noiseCoord	: TEXCOORD1;	//noise texture coordinates
	float3 N			: TEXCOORD2;	//normal vector
	float3 L			: TEXCOORD3;	//light vector
};

VSCharcoalOut VSCharcoal(VSCharcoalIn input)
{
	VSCharcoalOut output;
	
	//transform vertices
	float4 worldPosition = mul(input.position, xWorld);
    float4 viewPosition = mul(worldPosition, xView);
    float4 finalPos = mul(viewPosition, xProjection);
    output.position = finalPos;
	
	//compute vertex normals
	output.N = normalize(mul(xWorld,input.normal));
	
	//compute light vector
	output.L = xLightPosition - input.position.xyz;
	
	//compute paper texture coordinates to be in [0,1] range
	output.paperCoord = (finalPos.xy / finalPos.w) * 0.5 + 0.5;
	
	//compute noise texture coordinates
	output.noiseCoord = finalPos.xy;

	return output;
}

//-----------------------------------------------------------------------------
//Computes the lambertian intensity
//and applies a contrast operator.
//@param N - normal vector from VS
//@param L - light vector from VS
//@param A - Ambient light intensity
//@return the computed CEO
//-----------------------------------------------------------------------------
float CEO(float3 N, float3 L, float A)
{
	//normalize normal and light vectors
	N = normalize(N);
	L = normalize(L);
	
	//compute lambertian intensity
	float LI = max(0.0, dot(N, L));
	
	//add light ambient component to lambertian intensity
	LI = clamp(LI + A, 0.0, 1.0);
	
	//oversaturate to enhance the closure effect
	LI = clamp(LI * 1.5, 0.0, 1.0);
	
	//apply the contrast enhancement operator
	float contrast = pow(LI, 3.5);
	
	return contrast;
}

float4 PSCharcoal(VSCharcoalOut input) : COLOR0
{
	//get a random color [0,1]
	float4 rand = tex2D(noiseSampler, input.noiseCoord);

	//compute the Contrast Enhancement Operator (CEO)
	float diffuseColor = CEO(input.N, input.L, ambient);

	//compute the Contrast Enhancement Texture (CET) coordinates	
	float2 CETcoord = float2(0.0, diffuseColor);
	
	//scale texture access from being too far apart
	//this prevents the noise texture from showing up
	CETcoord.x = rand.x * 0.5;
	CETcoord.y *= 0.5;
	
	//get the CET color
	float4 CETColor = tex2D(cetSampler, CETcoord);
	
	//blend CET with CEM
	float4 smudgedColor = (diffuseColor + CETColor) * 0.5;

	//get paper texture color
	//invert the color so a simple vector addition overlay the paper texture onto CEM
	float4 bumpVec = 1.0 - tex2D(paperSampler, input.paperCoord);
	
	return (smudgedColor - bumpVec);
}

//-----------------------------------------------------------------------------
//X-RAY
//-----------------------------------------------------------------------------
struct VSXRayIn
{
	float4 position : POSITION0;
	float4 color	: COLOR0;
	float3 normal	: NORMAL0;
};

struct VSXRayOut
{
	float4 position : POSITION0;
	float4 color	: COLOR0;
	float3 normal	: TEXCOORD0;
	float3 I		: TEXCOORD1;
};

VSXRayOut VSXRay(VSXRayIn input)
{
    VSXRayOut output;

    float4 worldPosition = mul(input.position, xWorld);
    float4 viewPosition = mul(worldPosition, xView);
    float4 finalPos = mul(viewPosition, xProjection);
    
    output.position = finalPos;
    output.normal = mul(input.normal,xWorld);
    output.I = viewPosition.xyz;
    output.color = input.color*2;
    
    return output;
}

float4 PSXRay(VSXRayOut input) : COLOR0
{
    float opac = dot(normalize(-input.normal), normalize(-input.I));
    opac = abs(opac);
    opac = 1.0-pow(opac, 0.3f);
    
    float4 color = opac * input.color;
	color.a = opac;
	
	return color;
}

//-----------------------------------------------------------------------------
//PROCEDURAL STRIPE
//-----------------------------------------------------------------------------
struct VSStripeIn
{
	float4 position : POSITION0;
	float3 normal	: NORMAL0;
	float2 texCoord : TEXCOORD0;
};

struct VSStripeOut
{
	float4 position : POSITION0;
	float3 diffuse	: COLOR0;
	float3 specular	: COLOR1;
	float2 texCoord : TEXCOORD0;
};

VSStripeOut VSStripe(VSStripeIn input)
{
    VSStripeOut output;
    
    float4 worldPosition = mul(input.position, xWorld);
    float4 viewPosition = mul(worldPosition, xView);
   
    output.position = mul(viewPosition, xProjection);
    output.texCoord = input.texCoord;
    
    float3 normal = normalize(mul(input.normal,xWorld));
    float3 lightVec = normalize(xLightPosition - viewPosition);
    float3 viewVec = normalize(xCameraPosition - viewPosition);
    float3 halfVec = normalize(viewVec + lightVec);
    
    float spec = clamp(dot(halfVec, normal), 0.0, 1.0);
    spec = pow(spec, 16.0);
    
    float3 diffuseColor = xLightColor * (Kd * dot(lightVec, normal));
    output.diffuse = clamp(xAmbientColor + diffuseColor, 0.0, 1.0);
    output.specular = clamp((xLightColor * xSpecularColor * spec), 0.0, 1.0);
    
    return output;
}

float4 PSStripe(VSStripeOut input) : COLOR0
{
	//scale texture coords and get the fractional part
    float scaled_t = frac(input.texCoord.x * 10);

    float frac1 = clamp(scaled_t / Fuzz, 0.0, 1.0);
    float frac2 = clamp((scaled_t - Width) / Fuzz, 0.0, 1.0);

    frac1 = frac1 * (1.0 - frac2);
    frac1 = frac1 * frac1 * (3.0 - (2.0 * frac1));

    float3 finalColor = backColor *(1.0-frac1) + (stripeColor * frac1);
    finalColor = finalColor * input.diffuse + input.specular;

    return float4(clamp(finalColor, 0.0, 1.0), 1.0);
}

//-----------------------------------------------------------------------------
//ENVIROMENTAL MAPPING
//-----------------------------------------------------------------------------
sampler envTexture = 
sampler_state
{
	Texture = <textureCube>;
};

struct VSEnvMapIn
{
    float4 position : POSITION0;
	float3 normal	: NORMAL0;
	float2 texCoord	: TEXCOORD0;
};

struct VSEnvMapOut
{
    float4 position : POSITION0;
    float3 normal	: TEXCOORD0;
    float3 lightVec : TEXCOORD1;
    float3 viewVec	: TEXCOORD2;
    float2 texCoord	: TEXCOORD3;
};

VSEnvMapOut VSEnvMap(VSEnvMapIn input)
{
    VSPhongOut output;

    float4 worldPosition = mul(input.position, xWorld);
    float4 viewPosition = mul(worldPosition, xView);
    output.position = mul(viewPosition, xProjection);
	output.normal = mul(input.normal, xWorld);
	output.lightVec = normalize(xLightPosition - worldPosition);
	output.viewVec = normalize(xCameraPosition - worldPosition);
	output.texCoord = input.texCoord;
    
    return output;
}

float4 PSEnvMap(VSEnvMapOut input, uniform int type) : COLOR0
{
	//compute diffuse term
    float3 N = normalize(input.normal);
    float3 L = normalize(input.lightVec);
    float3 V = normalize(input.viewVec);
    float Kd = saturate(dot(L, N));

    //compute specular term
    float3 H = normalize(L + V);
    float Ks = pow(saturate(dot(H, N)), 32);

	if(type == 1)
	{
		//get reflection environment
		float3 R = reflect(-V,N);
		float4 reflect = texCUBE(envTexture, R);
	    
		float4 texColor = tex2D(originalTexture, input.texCoord);
		float4 ambient = float4(xAmbientColor,1);
		float4 diffuse = float4(xDiffuseColor*Kd,1);
		float4 specular = float4(xSpecularColor*Ks,1);
		
		return ambient + (texColor * diffuse * (1.0f - reflectness) + (reflect * reflectness)) + specular; 
	}
	else 
	{
		//get transparency (refraction) term
		float3 T = refract(-V,N,.9);
		float4 refract = texCUBE(envTexture, T);
		
		return refract; 
	}
}

//-----------------------------------------------------------------------------
//TOON SHADING
//-----------------------------------------------------------------------------
struct VSToonIn
{
	float4 position	: POSITION0;
	float3 normal	: NORMAL0;
};

struct VSToonOut
{
	float4 position	: POSITION0;
	float3 normal	: TEXCOORD0;
};

VSToonOut VSToon(VSToonIn input)
{
	VSToonOut output;
	
	float4 worldPosition = mul(input.position, xWorld);
    float4 viewPosition = mul(worldPosition, xView);
    
    output.position = mul(viewPosition, xProjection);
	output.normal = mul(input.normal, xWorld);
	
	return output;
}

float4 PSToon(VSToonOut input) : COLOR0
{
	float intensity;
	float4 color;
	float3 n = normalize(input.normal);
	
	intensity = dot(normalize(xLightPosition),n);
	
	if (intensity > 0.95)
		color = float4(1.0,0.5,0.5,1.0);
	else if (intensity > 0.75)
		color = float4(0.8,0.4,0.4,1.0);
	else if (intensity > 0.5)
		color = float4(0.6,0.3,0.3,1.0);
	else if (intensity > 0.25)
		color = float4(0.4,0.2,0.2,1.0);
	else
		color = float4(0.2,0.1,0.1,1.0);

	return color;
}

//-----------------------------------------------------------------------------
//BUMP MAPPING DIMPLES
//-----------------------------------------------------------------------------
struct VSDimplesIn
{
	float4 position	: POSITION0;
	float3 normal	: NORMAL0;
	float2 texCoord	: TEXCOORD0;
};

struct VSDimplesOut 
{
	float4 position	: POSITION0;
	float2 texCoord	: TEXCOORD0;
	float3 lightDir	: TEXCOORD2;
	float3 viewDir	: TEXCOORD3;
};

VSDimplesOut VSDimples(VSDimplesIn input)
{
	VSDimplesOut output;
	
	
	float4 worldPosition = mul(input.position, xWorld);
    float4 viewPosition = mul(worldPosition, xView);
    
    output.position = mul(viewPosition, xProjection);
    output.texCoord = input.texCoord;
    
    float3 viewDir = viewPosition;

    float3 n = normalize(mul(input.normal, xWorld));
    float3 t = normalize(cross(float3(1.141, 2.78, 3.14), n));
    float3 b = cross(n, t);

    float3 v;
    v.x = dot(xLightPosition, t);
    v.y = dot(xLightPosition, b);
    v.z = dot(xLightPosition, n);
    output.lightDir = normalize(v);

    v.x = dot(viewDir, t);
    v.y = dot(viewDir, b);
    v.z = dot(viewDir, n);
    output.viewDir = normalize(v);
    
    return output;
}

//const vec3 color = vec3(0.7, 0.6, 0.18);
//const float Density = 16.0;
//const float Size = 0.25;
//uniform float Density;
//uniform float Size;
//uniform float Scale;
//const float SpecularFactor = 0.5;

float4 PSDimples(VSDimplesOut input) : COLOR0
{
    float3 litColor;
	float3 color = float3(0.7, 0.6, 0.18);
    float2 c = 16.0 * (input.texCoord);
    float2 p = frac(c) - 0.5;
    float d = (p.x * p.x) + (p.y * p.y);
    if (d >= 0.25)
        p = float2(0.0,0.0);

    float3 normDelta = float3(-p.x, -p.y, 1.0);
      
    litColor = color * max(0.0, dot(normDelta, input.lightDir));
      
    float t = 2.0 * dot(input.lightDir, normDelta);
    float3 reflectDir = t * normDelta;
    reflectDir = input.lightDir - reflectDir;
    
//    vec3 reflectDir = LightDir - 2.0 * dot(LightDir, normDelta) * normDelta;
    
    float spec = max(dot(input.viewDir, reflectDir), 0.0);
    spec = spec * spec;
    spec = spec * spec;
    spec *= 0.5;

    litColor = min(litColor + spec, float3(1.0,1.0,1.0));
    return float4(litColor, 1.0);
//    gl_FragColor = vec4(Scale);
}

//-----------------------------------------------------------------------------
//TECHNIQUES SECTION
//1. Phong Lighting
//2. Charcoal rendering
//3. X-Ray rendering
//4. Procedural Stripes rendering
//5. Reflection mapping
//6. Refraction mapping
//7. Toon shading
//8. Bump Dimples
//-----------------------------------------------------------------------------
technique PhongLighting
{
    pass Pass1
    {
        VertexShader = compile vs_2_0 VSPhong();
        PixelShader = compile ps_2_0 PSPhong();
    }
}

technique Charcoal
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VSCharcoal();
		PixelShader = compile ps_2_0 PSCharcoal();
	}
}

technique XRay
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VSXRay();
		PixelShader = compile ps_2_0 PSXRay();
	}
}

technique ProceduralStripe
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VSStripe();
		PixelShader = compile ps_2_0 PSStripe();
	}
}

technique ReflectionMapping
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VSEnvMap();
		PixelShader = compile ps_2_0 PSEnvMap(1);
	}
}

technique RefractionMapping
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VSEnvMap();
		PixelShader = compile ps_2_0 PSEnvMap(2);
	}
}

technique ToonShading
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VSToon();
		PixelShader = compile ps_2_0 PSToon();
	}
}

technique Dimples
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 VSDimples();
		PixelShader = compile ps_2_0 PSDimples();
	}
}
