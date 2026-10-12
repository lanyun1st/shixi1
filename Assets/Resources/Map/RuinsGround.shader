Shader "Portfolio/RuinsGround"
{
    Properties
    {
        [PerRendererData] _MainTex ("Stone variation", 2D) = "white" {}
        _BaseTex ("Continuous stone", 2D) = "white" {}
        _MossTex ("Moss detail", 2D) = "white" {}
        _CrackTex ("Worn detail", 2D) = "white" {}
        _Theme ("Room", Float) = 1
        _Playable ("Playable bounds", Vector) = (80,220,3120,2160)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex, _BaseTex, _MossTex, _CrackTex;
            float _Theme;
            float4 _Playable;
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; };
            v2f vert(appdata v)
            {
                v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;
                float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.world=float2(world.x,720-world.y);return o;
            }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.world;
                float n=noise(p/370+_Theme*17)*.7+noise(p/155+5)*.3;
                // Worn paths emerge gradually instead of switching whole rows of tiles.
                float spine=1600+sin(p.y*.0035)*42;
                float cross=1200+sin(p.x*.003)*30;
                float distance=min(abs(p.x-spine),abs(p.y-cross));
                float road=1-smoothstep(65,300,distance+(n-.5)*100);
                float west=1-smoothstep(700,2450,p.x);
                float edgeDistance=min(min(p.x-_Playable.x,_Playable.z-p.x),min(p.y-_Playable.y,_Playable.w-p.y));
                float dampEdge=1-smoothstep(25,400,edgeDistance);
                float mossTheme=_Theme<1.5?.04:(_Theme<2.5?.28:0);
                float moss=smoothstep(.28,.85,n*.67+west*.23+dampEdge*.18+mossTheme)*(1-road*.94);
                float wear=smoothstep(.38,.86,noise(p/420+31))*(.15+smoothstep(1400,3000,p.x)*.4+(_Theme>2.5?.25:0))*(1-road*.65);
                // All neighbours meet the same opaque base at their edges. Atlas gutters
                // never participate, and no new bitmap assets or texture readback are needed.
                float2 local=saturate((i.uv-.105)/.79);
                float edge=min(min(local.x,1-local.x),min(local.y,1-local.y));
                float detail=smoothstep(0,.07,edge);
                fixed3 base=tex2D(_BaseTex,i.uv).rgb;
                fixed3 col=lerp(base,tex2D(_MainTex,i.uv).rgb,.14*detail);
                col=lerp(col,tex2D(_CrackTex,i.uv).rgb,wear*detail);
                col=lerp(col,tex2D(_MossTex,i.uv).rgb,moss*detail*.87);
                float outerShade=lerp(1,.56,smoothstep(0,130,max(0,-edgeDistance)));
                col*=float3(.76,.79,.79)*outerShade;
                return fixed4(col,1);
            }
            ENDCG
        }
    }
}
