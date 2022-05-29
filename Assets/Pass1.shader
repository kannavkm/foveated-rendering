Shader "Custom/Pass1"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_iResolutionX ("_iResolutionX", float) = 500
		_iResolutionY ("_iResolutionY", float) = 500
		_eyeX ("_eyeX", float) = 0.5
		_eyeY ("_eyeY", float) = 0.5
		_scaleRatio ("_scaleRatio", float) = 2.0
		_kernel ("_kernel", float) = 1.0
		_iApplyLogMap1 ("_iApplyLogMap1", int) = 1
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			uniform float _iResolutionX;
			uniform float _iResolutionY;
			uniform float _eyeX;
			uniform float _eyeY;
			uniform float _scaleRatio;
			uniform float _kernel;
			uniform int _iApplyLogMap1;
			float powInvFunc(float lr, float kernel)
			{
				float param = 1.0f / kernel;
				return pow(lr, param);
			}

			// fixed4 heart(v2f i)
			// {
			// 	fixed2 p = (2.0*i.uv-1)/min(1,1);
			// 	
			//     // background color
			//     fixed3 bcol = fixed3(1.0,0.8,0.7-0.07*p.y)*(1.0-0.25*length(p));
			//
			//     // animate
			//     fixed tt = fmod(_Time.y,1.5)/1.5;
			//     fixed ss = pow(tt,.2)*0.5 + 0.5;
			//     ss = 1.0 + ss*0.5*sin(tt*6.2831*3.0 + p.y*0.5)*exp(-tt*4.0);
			//     p *= fixed2(0.5,1.5) + ss*fixed2(0.5,-0.5);
			//
			//     // shape
			// #if 1
			//     p *= 0.8;
			//     p.y = -0.1 - p.y*1.2 + abs(p.x)*(1.0-abs(p.x));
			//     fixed r = length(p);
			// 	fixed d = 0.5;
			// #else
			// 	p.y -= 0.25;
			//     fixed a = atan2(p.y,p.x)/3.141593;
			//     fixed r = length(p);
			//     fixed h = abs(a);
			//     fixed d = (13.0*h - 22.0*h*h + 10.0*h*h*h)/(6.0-5.0*h);
			// #endif
			//     
			// 	// color
			// 	fixed s = 0.75 + 0.75*p.x;
			// 	s *= 1.0-0.4*r;
			// 	s = 0.3 + 0.7*s;
			// 	s *= 0.5+0.5*pow( 1.0-clamp(r/d, 0.0, 1.0 ), 0.1 );
			// 	fixed3 hcol = fixed3(1.0,0.5*r,0.3)*s;
			// 	
			//     fixed3 col = lerp( bcol, hcol, smoothstep( -0.01, 0.01, d-r) );
			//
			//     return  fixed4(col,1.0);
			// }


			fixed2 hash( fixed2 p ) { p=fixed2(dot(p,fixed2(127.1,311.7)),dot(p,fixed2(269.5,183.3))); return frac(sin(p)*18.5453); }
			
			// return distance, and cell id
			fixed2 voronoi( in fixed2 x )
			{
			    fixed2 n = floor( x );
			    fixed2 f = frac( x );
			
				fixed3 m = fixed3( 8.0 , 8.0 , 8.0 );
			    [unroll(100)]
				for( int j=-1; j<=1; j++ )
			    [unroll(100)]
				for( int i=-1; i<=1; i++ )
			    {
			        fixed2  g = fixed2( fixed(i), fixed(j) );
			        fixed2  o = hash( n + g );
			      //fixed2  r = g - f + o;
				    fixed2  r = g - f + (0.5+0.5*sin(_Time.y+6.2831*o));
					fixed d = dot( r, r );
			        if( d<m.x )
			            m = fixed3( d, o );
			    }
			
			    return fixed2( sqrt(m.x), m.y+m.z );
			}
			
			fixed4 vvornoi(v2f i) 
			{
			    fixed2 p = i.uv.xy/max(1,1);
			    
			    // computer voronoi patterm
			    fixed2 c = voronoi( (14.0+6.0*sin(0.2*_Time.y))*p );
			
			    // colorize
			    fixed3 col = 0.5 + 0.5*cos( c.y*6.2831 + fixed3(0.0,1.0,2.0) );	
			    col *= clamp(1.0 - 0.4*c.x*c.x,0.0,1.0);
			    col -= (1.0-smoothstep( 0.08, 0.09, c.x));
				
			    return  fixed4( col, 1.0 );
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 iResolution = float2(_iResolutionX, _iResolutionY);
				float2 cursorPos = float2(_eyeX * 2.0 - 1.0, _eyeY * 2.0 - 1.0);
				float maxr = max(
					max(
						length((float2( 1.0, 1.0) - cursorPos.xy) * iResolution.xy), 
						length((float2( 1.0,-1.0) - cursorPos.xy) * iResolution.xy)
						),
					max(length((float2(-1.0, 1.0) - cursorPos.xy) * iResolution.xy), 
						length((float2(-1.0,-1.0) - cursorPos.xy) * iResolution.xy)
						)
					);
				float maxLr = log(maxr * 0.5);
				float maxTheta = 6.28318530718f;
			
				float2 tc = _scaleRatio * i.uv;
				tc.x = powInvFunc(tc.x, _kernel);
			
				float x = exp(tc.x * maxLr) * cos(tc.y * maxTheta);
				float y = exp(tc.x * maxLr) * sin(tc.y * maxTheta);
			
				float2 pq = float2(x,y) + (cursorPos + 1.0) * 0.5 * iResolution.xy; //[0, iReso.x]
				float2 newCoord = (_iApplyLogMap1 > 0) ? pq / iResolution : i.uv;
			
				v2f logcord;
				logcord.uv = newCoord;
				
				fixed4 col = vvornoi(logcord);
				
				return col;
			}

			ENDCG
		}
		
	}
}
