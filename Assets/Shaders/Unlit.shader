Shader "SimpleRP/Unlit"
{
    Properties 
	{
		_Color("Color",color) = (1,1,1,1)
	}
	
	SubShader {
		
		Pass 
		{
			HLSLPROGRAM
			
			#pragma multi_compile_instancing
			#pragma instancing_options assumeuniformscaling
			
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
			
			#include "Unlit.hlsl"

			
			ENDHLSL
		}
	}
}
