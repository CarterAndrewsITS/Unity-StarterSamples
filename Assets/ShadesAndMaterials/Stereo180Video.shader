Shader "Custom/Stereo180Video"
{
    Properties
    {
        _MainTex("Video Texture", 2D) = "black" {}
        _OcclusionMaxDistance("Max Occlusion Distance", Range(0.1, 50.0)) = 5.0
        _EnvironmentDepthBias("Environment Depth Bias", Float) = 0.0
    }
        SubShader
        {
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_instancing
                #pragma multi_compile _ HARD_OCCLUSION SOFT_OCCLUSION

                #include "UnityCG.cginc"
                #include "Packages/com.meta.xr.sdk.core/Shaders/EnvironmentDepth/BiRP/EnvironmentOcclusionBiRP.cginc"

                sampler2D _MainTex;
                float _OcclusionMaxDistance;
                float _EnvironmentDepthBias;

                // These are set by Meta's EnvironmentDepthManager
                //UNITY_DECLARE_TEX2DARRAY(_EnvironmentDepthTexture);
                //uniform float4x4 _EnvironmentDepthReprojectionMatrices[2];

                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv     : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float4 pos      : SV_POSITION;
                    float2 uv       : TEXCOORD0;
                    float3 worldPos : TEXCOORD1;
                    META_DEPTH_VERTEX_OUTPUT(2)
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                v2f vert(appdata v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_TRANSFER_INSTANCE_ID(v, o);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.uv = v.uv;
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                    META_DEPTH_INITIALIZE_VERTEX_OUTPUT(o, v.vertex);

                    return o;
                }

                fixed4 frag(v2f i) : SV_Target
                {
                    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // --- Stereo UV selection (side-by-side) ---
                float2 uv = i.uv;
                uv.x = uv.x * 0.5 + unity_StereoEyeIndex * 0.5;
                fixed4 color = tex2D(_MainTex, uv);

                // --- Sample the actual real-world depth at this pixel ---
                // Reproject the fragment's world position into depth texture space
                float4 depthSpace = mul(
                    _EnvironmentDepthReprojectionMatrices[unity_StereoEyeIndex],
                    float4(i.worldPos, 1.0));
                float2 depthUV = (depthSpace.xy / depthSpace.w + 1.0f) * 0.5f;

                // Sample the environment depth — this is the distance to the
                // real-world surface in metres at this screen pixel
                float envDepth = SampleEnvironmentDepthLinear(depthUV);

                // --- Distance gate ---
                // Only occlude if the real-world surface is CLOSER than the threshold.
                // If the real world is further away (empty space, far wall beyond
                // threshold), keep showing the video.
                if (envDepth < _OcclusionMaxDistance)
                {
                    // Real world object is within range — occlude by going transparent
                    color.a = 0.0;
                }
                else
                {
                    color.a = 1.0;
                }

                return color;
            }
            ENDCG
        }
        }
}