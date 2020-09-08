Shader "Unlit/UnlitOccluded"

{
    Properties{
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _OccludedColor("Occluded Color", Color) = (1,1,1,1)
    }

        SubShader{

            Pass
            {
                Tags { "Queue" = "Geometry+1" }
                ZTest Greater
                ZWrite Off

                CGPROGRAM
                #pragma vertex vert          
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest

                half4 _OccludedColor;

                float4 vert(float4 pos : POSITION) : SV_POSITION
                {
                    float4 viewPos = UnityObjectToClipPos(pos);
                    return viewPos;
                }

                    half4 frag(float4 pos : SV_POSITION) : COLOR
                {
                    return _OccludedColor;
                }

                ENDCG
            }

            Pass
            {

            Tags { "RenderType" = "Opaque" "Queue" = "Geometry+1"}

            LOD 200
            ZWrite On
            ZTest LEqual

            CGPROGRAM

                    // use "vert" function as the vertex shader
                    #pragma vertex vert

                    // use "frag" function as the pixel (fragment) shader
                    #pragma fragment frag

                    // vertex shader inputs
                    struct appdata
                    {
                        float4 vertex : POSITION; // vertex position
                        float2 uv : TEXCOORD0; // texture coordinate
                    };

                // vertex shader outputs ("vertex to fragment")
                struct v2f
                {
                    float2 uv : TEXCOORD0; // texture coordinate
                    float4 vertex : SV_POSITION; // clip space position
                };

                // vertex shader
                v2f vert(appdata v)
                {
                    v2f o;
                    // transform position to clip space

                    // (multiply with model*view*projection matrix)
                    o.vertex = UnityObjectToClipPos(v.vertex);

                    // just pass the texture coordinate
                    o.uv = v.uv;

                    return o;
                }

                // texture we will sample
                sampler2D _MainTex;

                // pixel shader; returns low precision ("fixed4" type)
                // color ("SV_Target" semantic)
                fixed4 frag(v2f i) : SV_Target
                {
                    // sample texture and return it
                    fixed4 col = tex2D(_MainTex, i.uv);
                    return col;
                }
                ENDCG

            }

        }

            FallBack "Diffuse"

}