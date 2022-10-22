#pragma instancing_options procedural:setup

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
StructuredBuffer<float4> vectorBuffer;
#endif

void setup()
{
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
	float4 data = float4(1,1,1,1) * unity_InstanceID;

	/*float rotation = data.w * data.w * _Time.y * 0.5f;
	rotate2D(data.xz, rotation);*/

	unity_ObjectToWorld._11_21_31_41 = float4(data.w, 0, 0, 0);
	unity_ObjectToWorld._12_22_32_42 = float4(0, data.w, 0, 0);
	unity_ObjectToWorld._13_23_33_43 = float4(0, 0, data.w, 0);
	unity_ObjectToWorld._14_24_34_44 = float4(data.xyz, 1);
	unity_WorldToObject = unity_ObjectToWorld;
	unity_WorldToObject._14_24_34 *= -1;
	unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
#endif
}

void GetInstancedID(out uint Out)
{
	Out = 0;
#ifndef SHADERGRAPH_PREVIEW
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	Out = unity_InstanceID;
#endif
#endif
}

void InstancedVector_float(out float4 Out)
{
	uint instanceID = 0;
	GetInstancedID(instanceID);
#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
	Out = vectorBuffer[instanceID];
#else
	Out = float4(0.5, 0.5, 0.5, 0.5);
#endif
}