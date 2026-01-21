Shader "Custom/FogofWarMask"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        // 투명 처리를 위한 태그 및 렌더 파이프라인 지정
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        // 블렌딩 및 깊이 설정 
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // URP 표준 라이브러리 포함
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            // URP의 SRP Batcher 호환성을 위한 상수 버퍼 선언
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
            CBUFFER_END

            // 텍스처 및 샘플러 선언
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // 로컬 좌표를 클립 공간 좌표로 변환
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                // UV 타이핑/오프셋 적용
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 텍스처 컬러 샘플링
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Albedo: 메인 컬러의 RGB와 텍스처의 Blue 채널을 곱함
                half3 finalRGB = _Color.rgb * baseColor.b;

                // Alpha: 메인 컬러의 Alpha에서 텍스처의 Green 채널을 뺌
                half finalAlpha = _Color.a - baseColor.g;

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}
