Shader "ProjectRevenant/Sprites/Outline Highlight"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1)
        _OutlineThickness ("Outline Thickness (px)", Float) = 1

        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
        [HideInInspector] _OverlayScale ("Overlay Scale", Vector) = (1,1,1,1)
        [HideInInspector] _UvCenter ("UV Center", Vector) = (0.5,0.5,0,0)
        [HideInInspector] _UvRect ("UV Rect", Vector) = (0,0,1,1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        // ── Universal 2D pass ──────────────────────────────────────────────────
        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex SpriteVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_SKINNED_VERTEX_INPUTS
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineThickness;
                float4 _OverlayScale;
                float4 _UvCenter;
                float4 _UvRect;
            CBUFFER_END

            Varyings SpriteVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                
                // Un-stretch the UVs to counteract the mesh scaling, scaling around the atlas slice center
                output.uv = (input.uv - _UvCenter.xy) * _OverlayScale.xy + _UvCenter.xy;
                
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half GetAlpha(float2 uv)
            {
                // If the modified UV falls outside the sprite's atlas rect, it's transparent padding
                if (uv.x < _UvRect.x || uv.x > _UvRect.z || uv.y < _UvRect.y || uv.y > _UvRect.w)
                    return 0.0h;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            // 8-neighbour pixel-perfect outline.
            // The overlay renders behind the unit's source sprites.
            // We only colour transparent pixels that are adjacent to opaque ones;
            // the source sprite covers the interior automatically.
            half4 OutlineFragment(Varyings input) : SV_Target
            {
                float2 d = _MainTex_TexelSize.xy * _OutlineThickness;

                half centerAlpha = GetAlpha(input.uv);

                half n = 0;
                n = max(n, GetAlpha(input.uv + float2( d.x,  0  )));
                n = max(n, GetAlpha(input.uv + float2(-d.x,  0  )));
                n = max(n, GetAlpha(input.uv + float2( 0,    d.y)));
                n = max(n, GetAlpha(input.uv + float2( 0,   -d.y)));
                n = max(n, GetAlpha(input.uv + float2( d.x,  d.y)));
                n = max(n, GetAlpha(input.uv + float2(-d.x,  d.y)));
                n = max(n, GetAlpha(input.uv + float2( d.x, -d.y)));
                n = max(n, GetAlpha(input.uv + float2(-d.x, -d.y)));

                // Edge: transparent pixel (alpha < 0.1) adjacent to an opaque one (alpha >= 0.1).
                // Using step() instead of ceil() handles anti-aliased edges and atlas bleed
                // where border pixels may have tiny non-zero alpha that ceil() misreads as opaque.
                half isSolid    = step(0.1h, centerAlpha);
                half neighborOk = step(0.1h, n);
                half isEdge     = (1.0h - isSolid) * neighborOk;

                if (isEdge < 0.01h)
                    return half4(0, 0, 0, 0);

                return half4(_OutlineColor.rgb, isEdge * _OutlineColor.a * input.color.a);
            }
            ENDHLSL
        }

        // ── UniversalForward (scene view / fallback) ───────────────────────────
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex SpriteVertex
            #pragma fragment OutlineFragment
            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_SKINNED_VERTEX_INPUTS
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4  color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineThickness;
                float4 _OverlayScale;
                float4 _UvCenter;
                float4 _UvRect;
            CBUFFER_END

            Varyings SpriteVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                output.positionCS = TransformObjectToHClip(input.positionOS);
                
                // Un-stretch the UVs to counteract the mesh scaling, scaling around the atlas slice center
                output.uv = (input.uv - _UvCenter.xy) * _OverlayScale.xy + _UvCenter.xy;
                
                output.color = input.color * _Color * unity_SpriteColor;
                return output;
            }

            half GetAlpha(float2 uv)
            {
                // If the modified UV falls outside the sprite's atlas rect, it's transparent padding
                if (uv.x < _UvRect.x || uv.x > _UvRect.z || uv.y < _UvRect.y || uv.y > _UvRect.w)
                    return 0.0h;
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            half4 OutlineFragment(Varyings input) : SV_Target
            {
                float2 d = _MainTex_TexelSize.xy * _OutlineThickness;

                half centerAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;

                half n = 0;
                n = max(n, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2( d.x,  0  )).a);
                n = max(n, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-d.x,  0  )).a);
                n = max(n, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2( 0,    d.y)).a);
                n = max(n, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2( 0,   -d.y)).a);
                n = max(n, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2( d.x,  d.y)).a);
                n = max(n, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-d.x,  d.y)).a);
                n = max(n, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2( d.x, -d.y)).a);
                n = max(n, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-d.x, -d.y)).a);

                half isEdge = (1.0h - ceil(centerAlpha)) * ceil(n);

                if (isEdge < 0.01h)
                    return half4(0, 0, 0, 0);

                return half4(_OutlineColor.rgb, isEdge * _OutlineColor.a * input.color.a);
            }
            ENDHLSL
        }
    }

    Fallback "Sprites/Default"
}
