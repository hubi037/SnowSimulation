using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPHManager : MonoBehaviour {

	public ComputeShader m_ComputeShader;
	public Material m_Material;
	public int m_ParticleCount = 1000;
	public Vector3 m_StartPosition = Vector3.one * 3;
	public Vector3 m_GridSize = Vector3.one * 5;
	public Vector3 m_GridCells = Vector3.one * 16;
	public int m_iNumNeighbours = 32;

	private float m_GridCellSize = 0;

	private ComputeBuffer m_quadPoints;

	private ComputeBuffer m_positions;
	private ComputeBuffer m_velocities;
	private ComputeBuffer m_densities;

	private ComputeBuffer m_gridLengths;
	private ComputeBuffer m_gridParticles;

	private ComputeBuffer m_debugOutput;

	// Use this for initialization
	void Awake () 
	{
		m_positions = new ComputeBuffer(m_ParticleCount, sizeof(float) * 3);
		m_velocities = new ComputeBuffer(m_ParticleCount, sizeof(float) * 3);
		m_densities = new ComputeBuffer(m_ParticleCount, sizeof(float));

		m_gridLengths = new ComputeBuffer((int) (m_GridCells.x * m_GridCells.y * m_GridCells.z), sizeof(int));

		//we reserve 64 slots for every grid cell to store particle indices belonging to that cell
		m_gridParticles = new ComputeBuffer((int)(m_GridCells.x * m_GridCells.y * m_GridCells.z) * 32, sizeof(int));


		m_debugOutput = new ComputeBuffer(m_ParticleCount, sizeof(float));
		
		List<Vector3> position = new List<Vector3>();

		for (int i = 0; i < m_ParticleCount; i++)
		{
			position.Add(m_StartPosition + UnityEngine.Random.insideUnitSphere * 0.5f);
		}

		m_positions.SetData(position.ToArray());

		m_ComputeShader.SetVector("_Gravity", new Vector3(0, -9.8f, 0));
		m_ComputeShader.SetVector("_GridOrigin", Vector3.zero);
		m_ComputeShader.SetVector("_GridSize", m_GridSize);
		m_ComputeShader.SetVector("_GridCells", m_GridCells);

		m_quadPoints = new ComputeBuffer(6, sizeof(float) * 3);
		m_quadPoints.SetData(new[]
		{
			new Vector3(-1, 1, 0),
			new Vector3(1, 1, 0),
			new Vector3(1, -1, 0),
			new Vector3(1, -1, 0),
			new Vector3(-1, -1, 0),
			new Vector3(-1, 1, 0),
		});

		m_Material.SetBuffer("quadPoints", m_quadPoints);
		m_Material.SetPass(0);

		AddBuffersToKernel(m_ComputeShader.FindKernel("UpdatePositions"));
		AddBuffersToKernel(m_ComputeShader.FindKernel("InsertParticles"));
		AddBuffersToKernel(m_ComputeShader.FindKernel("ResetGrid"));
		AddBuffersToKernel(m_ComputeShader.FindKernel("ComputeDensity"));
		AddBuffersToKernel(m_ComputeShader.FindKernel("ComputeForces"));


		m_GridCellSize = m_GridSize.x / m_GridCells.x;
		Debug.Log(m_GridCellSize);
		m_ComputeShader.SetFloat("_GridCellSize", m_GridCellSize);
		m_ComputeShader.SetFloat("_KernelRadius", m_GridCellSize/2.0f);
		m_ComputeShader.SetFloat("_ParticleMass", 1f);
		m_ComputeShader.SetInt("_NumNeighbours", m_iNumNeighbours);
	}

	void AddBuffersToKernel(int kernel)
	{
		m_ComputeShader.SetBuffer(kernel, "mPos", m_positions);
		m_ComputeShader.SetBuffer(kernel, "mVel", m_velocities);
		m_ComputeShader.SetBuffer(kernel, "mDensity", m_densities);
		m_ComputeShader.SetBuffer(kernel, "mGridLengths", m_gridLengths);
		m_ComputeShader.SetBuffer(kernel, "mGridParticles", m_gridParticles);
		m_ComputeShader.SetBuffer(kernel, "mDebug", m_debugOutput);
	}
	
	// Update is called once per frame
	void Update () 
	{
		m_ComputeShader.SetFloat("_DeltaTime", Time.deltaTime);

		m_ComputeShader.Dispatch(m_ComputeShader.FindKernel("ResetGrid"), (int)m_GridCells.x / 8, (int)m_GridCells.x / 8, (int)m_GridCells.x / 8);
		m_ComputeShader.Dispatch(m_ComputeShader.FindKernel("InsertParticles"), m_ParticleCount / 100, 1, 1);
		m_ComputeShader.Dispatch(m_ComputeShader.FindKernel("ComputeDensity"), m_ParticleCount / 100, 1, 1);
		m_ComputeShader.Dispatch(m_ComputeShader.FindKernel("ComputeForces"), m_ParticleCount / 100, 1, 1);
		m_ComputeShader.Dispatch(m_ComputeShader.FindKernel("UpdatePositions"), m_ParticleCount / 100, 1, 1);
		
		float[] output = new float[m_ParticleCount];
		m_debugOutput.GetData(output);

		float count = 0;
		for (int i = 0; i < 10; i++)
		{
			//count += output[i];
			Debug.Log(output[i]);
		}
	}


	void OnPostRender()
	{
		m_Material.SetPass(0);
		m_Material.SetBuffer("_Particles", m_positions);

		Graphics.DrawProcedural(MeshTopology.Triangles, 6, m_positions.count);
	}

	void OnDestroy()
	{
		m_positions.Dispose();
		m_velocities.Dispose();
		m_quadPoints.Dispose();
		m_gridLengths.Dispose();
		m_debugOutput.Dispose();
		m_gridParticles.Dispose();
		m_densities.Dispose();
	}
}
