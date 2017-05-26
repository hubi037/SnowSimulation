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
	public int m_iNumNeighbours = 16;

	private float m_GridCellSize = 0;

	private ComputeBuffer m_quadPoints;

	private ComputeBuffer m_positions;
	private ComputeBuffer m_velocities;
	private ComputeBuffer m_forces;

	private ComputeBuffer m_densities;
	private ComputeBuffer m_pressure;
	private ComputeBuffer m_neighbours;
	private ComputeBuffer m_neighbourCounts;

	private ComputeBuffer m_gridLengths;
	private ComputeBuffer m_gridParticles;

	private ComputeBuffer m_debugOutput;

	private bool m_bPaused = true;

	// Use this for initialization
	void Awake () 
	{

		m_ParticleCount = 32 * 32 * 32;

		m_positions = new ComputeBuffer(m_ParticleCount, sizeof(float) * 3);
		m_velocities = new ComputeBuffer(m_ParticleCount, sizeof(float) * 3);
		m_forces = new ComputeBuffer(m_ParticleCount, sizeof(float) * 3);

		m_densities = new ComputeBuffer(m_ParticleCount, sizeof(float));
		m_pressure = new ComputeBuffer(m_ParticleCount, sizeof(float));
		m_neighbours = new ComputeBuffer(m_ParticleCount * m_iNumNeighbours, sizeof(int));
		m_neighbourCounts = new ComputeBuffer(m_ParticleCount, sizeof(int));

		m_gridLengths = new ComputeBuffer((int) (m_GridCells.x * m_GridCells.y * m_GridCells.z), sizeof(int));

		//we reserve 64 slots for every grid cell to store particle indices belonging to that cell
		m_gridParticles = new ComputeBuffer((int)(m_GridCells.x * m_GridCells.y * m_GridCells.z) * 4, sizeof(int));


		m_debugOutput = new ComputeBuffer(m_ParticleCount, sizeof(float));
		

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
		AddBuffersToKernel(m_ComputeShader.FindKernel("ComputeForce"));
		AddBuffersToKernel(m_ComputeShader.FindKernel("FindNeighbours"));


		m_GridCellSize = m_GridSize.x / m_GridCells.x;
		m_ComputeShader.SetFloat("_GridCellSize", m_GridCellSize);
		m_ComputeShader.SetFloat("_KernelRadius", m_GridCellSize);
		m_ComputeShader.SetFloat("_ParticleMass", 1f);
		m_ComputeShader.SetFloat("_RestDensity", 1000);
		m_ComputeShader.SetFloat("_GasConstant", 2f);
		m_ComputeShader.SetInt("_NumNeighbours", m_iNumNeighbours);
		m_ComputeShader.SetInt("_NumCellParticles", 4);


		float cubeLength = 100f;

		List<Vector3> position = new List<Vector3>();

		for (int x = 0; x < 32; x++)
			for (int y = 0; y < 32; y++)
				for (int z = 0; z < 32; z++ )
				{
					position.Add(m_StartPosition + new Vector3(x,y,z) * m_GridCellSize * 0.5f);
				}


		Debug.Log(m_GridCellSize);
		m_positions.SetData(position.ToArray());

		List<Vector3> empty = new List<Vector3>();
		for (int i = 0; i < m_ParticleCount; i++)
			empty.Add(Vector3.zero);

		m_velocities.SetData(empty.ToArray());
		m_forces.SetData(empty.ToArray());
	}

	void AddBuffersToKernel(int kernel)
	{
		m_ComputeShader.SetBuffer(kernel, "mPos", m_positions);
		m_ComputeShader.SetBuffer(kernel, "mVel", m_velocities);
		m_ComputeShader.SetBuffer(kernel, "mForce", m_forces);
		m_ComputeShader.SetBuffer(kernel, "mDensity", m_densities);
		m_ComputeShader.SetBuffer(kernel, "mPressure", m_pressure);
		m_ComputeShader.SetBuffer(kernel, "mGridLengths", m_gridLengths);
		m_ComputeShader.SetBuffer(kernel, "mGridParticles", m_gridParticles);
		m_ComputeShader.SetBuffer(kernel, "mDebug", m_debugOutput);
		m_ComputeShader.SetBuffer(kernel, "mNeighbours", m_neighbours);
		m_ComputeShader.SetBuffer(kernel, "mNeighbourCounts", m_neighbourCounts);
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (Input.GetKeyDown(KeyCode.P))
			m_bPaused = !m_bPaused;

		if (m_bPaused)
			return;

		m_ComputeShader.SetFloat("_DeltaTime", Time.deltaTime);

		m_ComputeShader.Dispatch(m_ComputeShader.FindKernel("ResetGrid"), (int)m_GridCells.x / 8, (int)m_GridCells.x / 8, (int)m_GridCells.x / 8);
		m_ComputeShader.Dispatch(m_ComputeShader.FindKernel("InsertParticles"), m_ParticleCount / 512, 1, 1);
		m_ComputeShader.Dispatch(m_ComputeShader.FindKernel("FindNeighbours"), m_ParticleCount / 512, 1, 1);
		m_ComputeShader.Dispatch(m_ComputeShader.FindKernel("ComputeDensity"), m_ParticleCount / 512, 1, 1);
		m_ComputeShader.Dispatch(m_ComputeShader.FindKernel("ComputeForce"), m_ParticleCount / 512, 1, 1);
		m_ComputeShader.Dispatch(m_ComputeShader.FindKernel("UpdatePositions"), m_ParticleCount / 512, 1, 1);
		
		float[] output = new float[m_ParticleCount];
		m_debugOutput.GetData(output);

		if(Input.GetKeyDown(KeyCode.L))
		{
			float count = 0;
			for (int i = 0; i < m_ParticleCount; i++)
			{
				//count += output[i];
				Debug.Log(output[i]);
			}
		}

	}


	void OnPostRender()
	{
		m_Material.SetPass(0);
		m_Material.SetBuffer("_Particles", m_positions);
		m_Material.SetBuffer("_Densities", m_densities);
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
		m_neighbourCounts.Dispose();
		m_neighbours.Dispose();
		m_pressure.Dispose();
		m_forces.Dispose();
	}
}
