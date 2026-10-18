// Cielo barato para WebGL: degradado vertical de tres colores y una capa de nubes con una sola
// muestra de ruido. _Storm mezcla el cielo despejado con el de tormenta; lo anima WeatherController.
Shader "Skybox/Weather Gradient"
{
    Properties
    {
        [Header(Despejado)]
        // La cámara mira hacia abajo: casi todo lo visible es Horizon y Bottom.
        _ClearTop ("Top", Color) = (0.40, 0.62, 0.90, 1)
        _ClearHorizon ("Horizon", Color) = (0.82, 0.90, 0.97, 1)
        _ClearBottom ("Bottom", Color) = (0.58, 0.74, 0.92, 1)

        [Header(Tormenta)]
        _StormTop ("Top", Color) = (0.188, 0.149, 0.227, 1)      // 30263A
        _StormHorizon ("Horizon", Color) = (0.647, 0.624, 0.675, 1) // A59FAC
        _StormBottom ("Bottom", Color) = (0.282, 0.212, 0.325, 1)  // 483653

        _Storm ("Storm", Range(0, 1)) = 0

        [Header(Nubes)]
        _CloudColor ("Cloud Color", Color) = (1, 1, 1, 1)
        _StormCloudColor ("Storm Cloud Color", Color) = (0.26, 0.21, 0.31, 1)
        _CloudScale ("Cloud Scale", Float) = 2.2
        _CloudSpeed ("Cloud Speed", Float) = 0.01
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            half4 _ClearTop, _ClearHorizon, _ClearBottom;
            half4 _StormTop, _StormHorizon, _StormBottom;
            half _Storm;
            half4 _CloudColor, _StormCloudColor;
            float _CloudScale, _CloudSpeed;

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            v2f vert(float4 vertex : POSITION)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(vertex);
                o.dir = vertex.xyz;
                return o;
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
                return lerp(lerp(hash(i), hash(i + float2(1, 0)), f.x),
                            lerp(hash(i + float2(0, 1)), hash(i + float2(1, 1)), f.x), f.y);
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.dir);
                float y = dir.y;

                half3 top = lerp(_ClearTop.rgb, _StormTop.rgb, _Storm);
                half3 horizon = lerp(_ClearHorizon.rgb, _StormHorizon.rgb, _Storm);
                half3 bottom = lerp(_ClearBottom.rgb, _StormBottom.rgb, _Storm);

                half3 color = y > 0
                    ? lerp(horizon, top, saturate(pow(y, 0.6)))
                    : lerp(horizon, bottom, saturate(-y * 3.0));

                // Nubes proyectadas sobre un plano: solo por encima del horizonte.
                // La tormenta las vuelve más densas y oscuras.
                float2 uv = dir.xz / max(y, 0.15) * _CloudScale + _Time.y * _CloudSpeed;
                float n = valueNoise(uv);
                float coverage = lerp(0.62, 0.3, _Storm);
                float cloud = smoothstep(coverage, coverage + 0.25, n) * saturate(y * 4.0);
                half3 cloudColor = lerp(_CloudColor.rgb, _StormCloudColor.rgb, _Storm);
                color = lerp(color, cloudColor, cloud * lerp(0.55, 0.8, _Storm));

                return half4(color, 1);
            }
        ENDCG
        }
    }
    Fallback Off
}
