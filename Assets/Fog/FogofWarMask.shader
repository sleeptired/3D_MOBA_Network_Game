Shader "Custom/FogofWarMask"
{
	//Properties
    //{
    //    _Color ("Main Color", Color) = (1,1,1,1)
    //    _MainTex ("Base (RGB)", 2D) = "white" {}
    //}
    //SubShader
    //{
    //    // 1. URP에서 투명 쉐이더를 인식하기 위한 태그 설정
    //    Tags 
    //    { 
    //        "RenderType" = "Transparent" 
    //        "Queue" = "Transparent" 
    //        "RenderPipeline" = "UniversalPipeline" // URP 필수 태그
    //    }
    //
    //    // 2. 블렌딩 옵션 (예전 코드와 동일: SrcAlpha OneMinusSrcAlpha)
    //    Blend SrcAlpha OneMinusSrcAlpha
    //    ZWrite Off // 투명한 물체는 보통 깊이 기록을 끕니다
    //
    //    Pass
    //    {
    //        Name "Unlit"
    //        
    //        HLSLPROGRAM
    //        #pragma vertex vert
    //        #pragma fragment frag
    //        
    //        // URP 핵심 라이브러리 포함
    //        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    //
    //        struct Attributes
    //        {
    //            float4 positionOS : POSITION;
    //            float2 uv : TEXCOORD0;
    //        };
    //
    //        struct Varyings
    //        {
    //            float4 positionCS : SV_POSITION;
    //            float2 uv : TEXCOORD0;
    //        };
    //
    //        // 변수 선언 (CBUFFER는 URP 배칭을 위해 필수)
    //        CBUFFER_START(UnityPerMaterial)
    //            float4 _Color;
    //            float4 _MainTex_ST;
    //        CBUFFER_END
    //
    //        TEXTURE2D(_MainTex);
    //        SAMPLER(sampler_MainTex);
    //
    //        Varyings vert(Attributes IN)
    //        {
    //            Varyings OUT;
    //            // 정점 위치 변환 (Local -> Clip Space)
    //            OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
    //            OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
    //            return OUT;
    //        }
    //
    //        half4 frag(Varyings IN) : SV_Target
    //        {
    //            // 텍스처 색상 가져오기
    //            half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
    //
    //            half4 finalColor;
    //
    //            // --- [핵심 로직 이식] ---
    //            // 예전 코드: o.Albedo = _Color.rgb * baseColor.b;
    //            // 해석: 텍스처의 Blue 채널을 사용하여 색상을 표시
    //            finalColor.rgb = _Color.rgb * baseColor.b;
    //
    //            // 예전 코드: o.Alpha = _Color.a - baseColor.g;
    //            // 해석: 전체 투명도에서 텍스처의 Green 채널만큼 구멍을 뚫음 (Fog 걷어내기)
    //            finalColor.a = _Color.a - baseColor.g;
    //            // -----------------------
    //
    //            return finalColor;
    //        }
    //        ENDHLSL
    //    }
    //}

    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        // 1. 투명 처리를 위한 태그 및 렌더 파이프라인 지정
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        // 2. 블렌딩 및 깊이 설정 (이전 코드의 Blend SrcAlpha OneMinusSrcAlpha 동일)
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

                // --- [이전 코드 로직 이식] ---
                // 1. Albedo: 메인 컬러의 RGB와 텍스처의 Blue 채널을 곱함
                half3 finalRGB = _Color.rgb * baseColor.b;

                // 2. Alpha: 메인 컬러의 Alpha에서 텍스처의 Green 채널을 뺌
                // (초록색이 강할수록 해당 부분이 투명하게 뚫림)
                half finalAlpha = _Color.a - baseColor.g;
                // -----------------------------

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Transparent/VertexLit"
}
