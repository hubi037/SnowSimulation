using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class SnowParticleCloud
{

	private List<SnowParticle> m_particles;
	private ComputeBuffer m_particleBuffer;
	private ComputeBuffer m_particleDefBuffer;

	private ComputeShader m_computeShader;
	private Material m_material;

	private float POISSONS_RATIO = 0.2f;
	private float E = 1.4e5f;

	// CRITICAL COMPRESSION
	private float MIN_THETA_C = 1.9e-2f;
	private float MAX_THETA_C = 2.5e-2f;

	// CRITICAL STRETCH
	private float MIN_THETA_S = 5e-3f;
	private float MAX_THETA_S = 7.5e-3f;


	struct DeformationMaterial
	{
		public Matrix4x4 svd_v;
		public Matrix4x4 svd_w;
		public Vector4 svd_e;
	};


	// Use this for initialization
	public SnowParticleCloud (int _particleAmount, Vector3 _origin, Vector3 _size, ComputeShader _compute, Material _material) 
	{
		m_particles = new List<SnowParticle>();

		float particle_volume = Constants.PARTICLE_DIAM * Constants.PARTICLE_DIAM * Constants.PARTICLE_DIAM;
		float particle_mass = particle_volume * Constants.DENSITY;

		List<DeformationMaterial> m_deformations = new List<DeformationMaterial>();

		//initialise Particle positions to create a ball of snow
		for(int i = 0; i < _particleAmount; i++)
		{
			Vector3 random = Random.insideUnitSphere;
			Vector3 particlePosition = new Vector3(2 + random.x, 2f + random.y, 2 + random.z); 

			SnowParticle newParticle = new SnowParticle();
			newParticle.position = particlePosition;
			newParticle.mass = particle_mass;
			newParticle.xi = 10;
			newParticle.lambda = (E * POISSONS_RATIO) / ((1 + POISSONS_RATIO) * (1 - 2 * POISSONS_RATIO));
			newParticle.mu = E / (2 * (1 + POISSONS_RATIO));
			newParticle.criticalCompressionRatio = 1f - MAX_THETA_C;
			newParticle.criticalStretchRatio = 1f + MAX_THETA_S;
			newParticle.def_elastic = new Matrix3x3();
			newParticle.def_elastic.m00 = 1.0f;
			newParticle.def_elastic.m11 = 1.0f;
			newParticle.def_elastic.m22 = 1.0f;
			newParticle.def_plastic = newParticle.def_elastic;

			m_particles.Add(newParticle);

			DeformationMaterial newDeformation = new DeformationMaterial();
			newDeformation.svd_w = Matrix4x4.identity;
			newDeformation.svd_v = Matrix4x4.identity;
			newDeformation.svd_e = Vector4.one;
			m_deformations.Add(newDeformation);
		}

		m_particleBuffer = new ComputeBuffer(m_particles.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SnowParticle)));
		m_particleBuffer.SetData(m_particles.ToArray());

		m_particleDefBuffer = new ComputeBuffer(m_particles.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(DeformationMaterial)));
		m_particleDefBuffer.SetData(m_deformations.ToArray());

		m_computeShader = _compute;
		m_material = _material;
		m_computeShader.SetBuffer(_compute.FindKernel("UpdateParticlePositions"), "_Particles", m_particleBuffer);
		m_computeShader.SetBuffer(_compute.FindKernel("UpdateParticlePositions"), "_DefMaterials", m_particleDefBuffer);
		m_computeShader.SetInt("_ParticleCount", m_particleBuffer.count);
		m_material.SetBuffer("_Particles", m_particleBuffer);
	}

	public void render(Material _material)
	{
		m_material.SetPass(0);
		m_material.SetFloat("_Size", Constants.PARTICLE_DIAM);
		//render 6 vertices for every particle
		Graphics.DrawProcedural(MeshTopology.Triangles, 6, m_particleBuffer.count);
	}

	public void updateParticlePositions(ComputeShader _compute)
	{
		int kernel = _compute.FindKernel("UpdateParticlePositions");
		_compute.SetFloat("_DeltaTime", Time.deltaTime);

		_compute.Dispatch(kernel, m_particleBuffer.count / Constants.NUMTHREADS, 1, 1);
	}

	public ComputeBuffer getParticleBuffer() { return m_particleBuffer; }

	public ComputeBuffer getDeformationBuffer() { return m_particleDefBuffer; }

	public void DisposeBuffers()
	{
		m_particleBuffer.Dispose();
		m_particleDefBuffer.Dispose();
	}

	public void setGridBuffer(ComputeBuffer _GridBuffer)
	{
		m_computeShader.SetBuffer(m_computeShader.FindKernel("UpdateParticlePositions"), "_Grid", _GridBuffer);
	}
}
