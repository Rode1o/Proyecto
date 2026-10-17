// Shader de UI para el botón activo del menú: basado en UI/Default,
// añade un destello diagonal que cruza el sprite y un pulso suave de brillo.
Shader "UI/Menu Button Shine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Shine)]
        _ShineColor ("Shine Color", Color) = (1, 0.95, 0.8, 0.55)
        _ShineWidth ("Shine Width", Range(0.01, 0.5)) = 0.12
        _ShineSlant ("Shine Slant", Range(-1, 1)) = 0.25
        _ShineSpeed ("Shine Speed", Float) = 0.9
        _ShineInterval ("Shine Interval", Float) = 3.0

        [Header(Pulse)]
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.07
        _PulseSpeed ("Pulse Speed", Float) = 2.2

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
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

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

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            fixed4 _ShineColor;
            float _ShineWidth;
            float _ShineSlant;
            float _ShineSpeed;
            float _ShineInterval;
            float _PulseAmount;
            float _PulseSpeed;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                // Pulso de brillo.
                color.rgb *= 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                // El destello recorre el sprite de izquierda a derecha y luego espera hasta el siguiente ciclo.
                // Con sprites sliced la UV.x sigue creciendo de izquierda a derecha, así que vale como eje.
                float cycle = max(_ShineInterval, 1.0 + 2.0 * _ShineWidth);
                float position = frac(_Time.y * _ShineSpeed / cycle) * cycle - _ShineWidth;
                float coord = IN.texcoord.x + (IN.texcoord.y - 0.5) * _ShineSlant;
                float shine = saturate(1.0 - abs(coord - position) / _ShineWidth);
                shine *= shine;

                color.rgb *= color.a;
                color.rgb += _ShineColor.rgb * (_ShineColor.a * shine * color.a);

                return color;
            }
        ENDCG
        }
    }
}
