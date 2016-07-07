using UnityEngine;
using System.Collections;
using System.Collections.Generic;


//need this because Unity only has Matrix4x4, is only used on GPU side
public struct Matrix3x3
{
	public float m00;
	public float m01;
	public float m02;
	public float m10;
	public float m11;
	public float m12;
	public float m20;
	public float m21;
	public float m22;
}

public struct SnowParticle
{
	public Vector4 position;
	public Vector4 velocity;
	public Vector4 gridPosition;
	public Matrix3x3 def_plastic;
	public Matrix3x3 def_elastic;
	public Matrix3x3 velocityGradient;

	public float volume;
	public float mass;
	public float density;
	public float lambda;
	public float mu;
	public float xi;
	public float criticalCompressionRatio;
	public float criticalStretchRatio;


}
