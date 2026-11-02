// BLUR DELL'INTERFACCIA — due passate separabili (orizzontale, poi verticale).
//
// È un blit semplice, non un effetto della pipeline URP: funziona su una RenderTexture
// qualsiasi, quindi anche sul gioco renderizzato da una seconda camera a bassa risoluzione.
// Nove campioni per passata, pesi gaussiani: abbastanza per un fondo "vetro smerigliato"
// e abbastanza economico da poter girare mentre il gioco è in pausa.
Shader "Falezia/UiBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Direction ("Direzione (pixel)", Vector) = (1, 0, 0, 0)
        _Radius ("Raggio", Float) = 1.0
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float4 _Direction;
            float _Radius;

            fixed4 frag (v2f_img i) : SV_Target
            {
                float2 step = _Direction.xy * _MainTex_TexelSize.xy * _Radius;

                // pesi gaussiani a 9 campioni
                float4 c = tex2D(_MainTex, i.uv) * 0.2270270270;
                c += tex2D(_MainTex, i.uv + step * 1.0) * 0.1945945946;
                c += tex2D(_MainTex, i.uv - step * 1.0) * 0.1945945946;
                c += tex2D(_MainTex, i.uv + step * 2.0) * 0.1216216216;
                c += tex2D(_MainTex, i.uv - step * 2.0) * 0.1216216216;
                c += tex2D(_MainTex, i.uv + step * 3.0) * 0.0540540541;
                c += tex2D(_MainTex, i.uv - step * 3.0) * 0.0540540541;
                c += tex2D(_MainTex, i.uv + step * 4.0) * 0.0162162162;
                c += tex2D(_MainTex, i.uv - step * 4.0) * 0.0162162162;
                return c;
            }
            ENDCG
        }
    }
    Fallback Off
}
