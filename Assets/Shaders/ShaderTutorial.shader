Shader "Unlit/ShaderTutorial" // Name a path of shader
{
    Properties // All the stuff defined per material
    {
        _Color ( "Color", Color ) = (1,1,1,0)
        // _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader // Thing that contains the vertex shader and fragment shader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM // We start writing CG code from here on
            #pragma vertex vert // pragma assigns variable name of vertex and fragment shader
            #pragma fragment frag

            #include "UnityCG.cginc" // includes unity specific macros/functions
            #include "Lighting.cginc" // includes unity specific macros/functions
            #include "AutoLight.cginc" // includes unity specific macros/functions

            struct appdata // Mesh data; vertex position, vertex normal, UVs, tangents, vertex colors; can rename to VertexInput
            {
                float4 vertex : POSITION;
                // float4 colors: COLOR;
                float3 normal : NORMAL; // direction of each vertex; aka vertex normals 
                // float4 tangent : TANGENT;
                float2 uv0 : TEXCOORD0;
                // float2 uv1 : TEXCOORD1;
            };

            struct v2f // Vertex Output
            {
                float4 vertex : SV_POSITION; // This is the clip space position, not the same as v.vertex below
                float2 uv0 : TEXCOORD0; // texcoord is an "interpolator"
                float3 normal : TEXCOORD1; 
            };

            // sampler2D _MainTex;
            // float4 _MainTex_ST;

            float4 _Color;

            v2f vert (appdata v) // Vertex Output, takes in mesh data
            {
                v2f o;
                o.uv0 = v.uv0;
                o.normal = v.normal;
                o.vertex = UnityObjectToClipPos(v.vertex); // v.vertex = local space vertex from mesh data
                return o;
            }

            fixed4 frag(v2f i) : SV_Target // Fragment shader outputs a color; takes in vertex output
            {
                //Lighting
                // Direct Light
                float2 uv = i.uv0;
                float3 lightColor = _LightColor0.rgb;
                float3 lightDir = _WorldSpaceLightPos0.xyz;
                float lightFalloff = max(0, dot( lightDir, i.normal)); // dot product between light direction and normal);
                float3 directDiffuseLight = lightColor * lightFalloff;
                
                // Ambient Light
                float3 ambientLight = float3(0.2, 0.35, 0.4);

                // Composite
                float3 diffuseLight = ambientLight + directDiffuseLight;
                float3 finalSurfaceColor = diffuseLight * _Color.rgb;
                
                return float4(finalSurfaceColor, 0);
            }
            ENDCG // Ends CG code
        }
    }
}
