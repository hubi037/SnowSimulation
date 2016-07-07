#ifndef SNOW_INCLUDED
#define SNOW_INCLUDED


//helper functions
//gets only recompiled if there are changes in the including file!
float bspline(float input)
{
	input = abs(input);
	float w;
	if (input < 1)
		w = input*input*(input / 2 - 1) + 2 / 3.0;
	else if (input < 2)
		w = input*(input*(-input / 6 + 1) - 2) + 4 / 3.0;
	else return 0;
	if (w < 0)
		return 0;
	return w;

}

float bsplineSlope(float input)
{
	float abs_input = abs(input);
	float w;

	if (abs_input < 1)
		return 1.5*input*abs_input - 2 * input;
	else if (input < 2)
		return -input*abs_input / 2 + 2 * input - 2 * input / abs_input;
	else return 0;
}

float3x3 outerProduct(float3 v, const float3 w)
{
	return float3x3(v.x*w.x, v.y*w.x, v.z*w.x,
		v.x*w.y, v.y*w.y, v.z*w.y,
		v.x*w.z, v.y*w.z, v.z*w.z);
}

float product(float4 input)
{
	return input.x * input.y * input.z * input.w;
}

float3x3 diag_sum(float3x3 input, const float c)
{
	for (int i=0; i<3; i++)
		input[i][i] += c;

	return input;
}
#endif