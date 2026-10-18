// Relleno circular del retrato de la oveja: degradado radial con la paleta del clima y nubes suaves
// que se desplazan. _Darkness pasa de claro (0) a oscuro (1); lo anima SheepPortrait.
// El círculo sale de la UV (0..1) de la imagen, con borde suavizado: no necesita sprite.
Shader "UI/Portrait Mood Fill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Palette)]
        _LightColor ("Light", Color) = (0.647, 0.624, 0.675, 1)   // A59FAC
        _MidColor ("Mid", Color) = (0.282, 0.212, 0.325, 1)       // 483653
        _DarkColor ("Dark", Color) = (0.188, 0.149, 0.227, 1)     // 30263A
        _Darkness ("Darkness", Range(0, 1)) = 0

        [Header(Clouds)]
        _CloudScale ("Cloud Scale", Float) = 3.5
        _CloudSpeed ("Cloud Speed", Float) = 0.06
        _CloudAmount ("Cloud Amount", Range(0, 0.5)) = 0.12

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
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
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float4 _ClipRect;

            fixed4 _LightColor;
            fixed4 _MidColor;
            fixed4 _DarkColor;
            float _Darkness;
            float _CloudScale;
            float _CloudSpeed;
            float _CloudAmount;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1, 0));
                float c = hash(i + float2(0, 1));
                float d = hash(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 centered = IN.texcoord - 0.5;
                float r = length(centered) * 2.0;

                // Borde del círculo suavizado a un píxel de pantalla.
                float edge = fwidth(r);
                float alpha = 1.0 - smoothstep(1.0 - edge, 1.0, r);

                // Claro: centro Light → borde Mid. Oscuro: centro Mid → borde Dark.
                float t = smoothstep(0.0, 1.0, r);
                float3 inner = lerp(_LightColor.rgb, _MidColor.rgb, _Darkness);
                float3 outer = lerp(_MidColor.rgb, _DarkColor.rgb, _Darkness);
                float3 color = lerp(inner, outer, t);

                // Dos capas de nubes que se cruzan despacio.
                float time = _Time.y * _CloudSpeed;
                float2 p = IN.texcoord * _CloudScale;
                float clouds = valueNoise(p + float2(time, time * 0.3)) * 0.6
                             + valueNoise(p * 2.1 - float2(time * 0.7, 0)) * 0.4;
                color *= 1.0 + (clouds - 0.5) * 2.0 * _CloudAmount;

                half4 result = half4(color, alpha) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                result.rgb *= result.a;
                return result;
            }
        ENDCG
        }
    }
}
