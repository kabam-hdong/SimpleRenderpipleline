Shader "SimpleRP/Lit"
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
			
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment
			
			#include "Lit.hlsl"

			
			ENDHLSL
		}
	}
}
