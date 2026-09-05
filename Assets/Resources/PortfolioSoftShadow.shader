Shader "Portfolio/UI Soft Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 localPosition : TEXCOORD0;
                float2 halfSize : TEXCOORD1;
                float4 shadowData : TEXCOORD2;
                float2 shapeData : TEXCOORD3;
                float4 worldPosition : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float4 _ClipRect;

            float RoundedBoxDistance(float2 localPoint, float2 halfSize, float radius)
            {
                radius = clamp(radius, 0.0, min(halfSize.x, halfSize.y));
                float2 q = abs(localPoint) - halfSize + radius;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
            }

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.color = input.color * _Color;
                output.localPosition = input.texcoord1;
                output.halfSize = input.texcoord2;
                output.shadowData = float4(input.normal.xy, input.tangent.xy);
                output.shapeData = float2(input.normal.z, 0.0);
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 offset = input.shadowData.xy;
                float blurRadius = max(input.shadowData.z, 0.001);
                float spread = max(input.shadowData.w, 0.0);
                float cornerRadius = input.shapeData.x;

                float originalDistance = RoundedBoxDistance(
                    input.localPosition, input.halfSize, cornerRadius);
                float shadowDistance = RoundedBoxDistance(
                    input.localPosition - offset,
                    input.halfSize + spread,
                    cornerRadius + spread);

                float softShadow = 1.0 - smoothstep(-blurRadius, blurRadius, shadowDistance);
                float outsideWindow = smoothstep(-0.75, 0.75, originalDistance);

                fixed4 color = input.color;
                color.a *= softShadow * outsideWindow;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
