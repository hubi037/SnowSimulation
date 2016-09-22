using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


public class SPH : MonoBehaviour
{

	public ComputeShader m_compute;
	public Material m_Material;

	public Vector3 m_snowBallPosition = Vector3.one;
	public Vector3 m_startForce = Vector3.zero;

	public int m_particleCount;

	private Vector3 m_gridDimensions = Vector3.one * 5;
	private Vector3 m_gridCells =  Vector3.one * 32;
	private float m_gridCellSize = 0.1f;

	private ComputeBuffer m_quadPoints;
	private ComputeBuffer m_positions;
	private ComputeBuffer m_velocities;
	private ComputeBuffer m_forces;
	private ComputeBuffer m_velEval;
	private ComputeBuffer m_Grid;
	private ComputeBuffer m_Density;
	private ComputeBuffer m_Output;

	private bool m_bIsRunning = false;
	// Use this for initialization
	void Awake()
	{


		m_positions = new ComputeBuffer(m_particleCount, sizeof(float) * 3);
		m_velocities = new ComputeBuffer(m_particleCount, sizeof(float) * 3);
		m_forces = new ComputeBuffer(m_particleCount, sizeof(float) * 3);
		m_velEval = new ComputeBuffer(m_particleCount, sizeof(float) * 3);
		m_Output = new ComputeBuffer(m_particleCount, sizeof(float));
		m_Density = new ComputeBuffer(m_particleCount, sizeof(float));

		m_Grid = new ComputeBuffer((int)(m_gridCells.x * m_gridCells.y * m_gridCells.z), sizeof(int) * 64);

		m_gridCellSize = m_gridDimensions.x / m_gridCells.x;

		Debug.Log(m_gridCellSize);
		Debug.Log(sizeof(int));
		int kernel = m_compute.FindKernel("UpdateParticles");

		m_compute.SetBuffer(kernel, "mPos", m_positions);
		m_compute.SetBuffer(kernel, "mVel", m_velocities);
		m_compute.SetBuffer(kernel, "mForce", m_forces);
		m_compute.SetBuffer(kernel, "mVelEval", m_velEval);
		m_compute.SetBuffer(kernel, "mGrid", m_Grid);
		m_compute.SetBuffer(kernel, "mOutput", m_Output);
		m_compute.SetBuffer(kernel, "mDensity", m_Density);

		kernel = m_compute.FindKernel("AddParticlesToGrid");

		m_compute.SetBuffer(kernel, "mPos", m_positions);
		m_compute.SetBuffer(kernel, "mVel", m_velocities);
		m_compute.SetBuffer(kernel, "mForce", m_forces);
		m_compute.SetBuffer(kernel, "mVelEval", m_velEval);
		m_compute.SetBuffer(kernel, "mGrid", m_Grid);
		m_compute.SetBuffer(kernel, "mOutput", m_Output);
		m_compute.SetBuffer(kernel, "mDensity", m_Density);

		kernel = m_compute.FindKernel("ResetGrid");
		m_compute.SetBuffer(kernel, "mGrid", m_Grid);

		kernel = m_compute.FindKernel("ComputeDensity");
		m_compute.SetBuffer(kernel, "mPos", m_positions);
		m_compute.SetBuffer(kernel, "mVel", m_velocities);
		m_compute.SetBuffer(kernel, "mForce", m_forces);
		m_compute.SetBuffer(kernel, "mVelEval", m_velEval);
		m_compute.SetBuffer(kernel, "mGrid", m_Grid);
		m_compute.SetBuffer(kernel, "mOutput", m_Output);
		m_compute.SetBuffer(kernel, "mDensity", m_Density);

		kernel = m_compute.FindKernel("ComputeForce");
		m_compute.SetBuffer(kernel, "mPos", m_positions);
		m_compute.SetBuffer(kernel, "mVel", m_velocities);
		m_compute.SetBuffer(kernel, "mForce", m_forces);
		m_compute.SetBuffer(kernel, "mVelEval", m_velEval);
		m_compute.SetBuffer(kernel, "mGrid", m_Grid);
		m_compute.SetBuffer(kernel, "mOutput", m_Output);
		m_compute.SetBuffer(kernel, "mDensity", m_Density);

		kernel = m_compute.FindKernel("ComputeCohesion");
		m_compute.SetBuffer(kernel, "mPos", m_positions);
		m_compute.SetBuffer(kernel, "mVel", m_velocities);
		m_compute.SetBuffer(kernel, "mForce", m_forces);
		m_compute.SetBuffer(kernel, "mVelEval", m_velEval);
		m_compute.SetBuffer(kernel, "mGrid", m_Grid);
		m_compute.SetBuffer(kernel, "mOutput", m_Output);
		m_compute.SetBuffer(kernel, "mDensity", m_Density);

		kernel = m_compute.FindKernel("ComputeCompression");
		m_compute.SetBuffer(kernel, "mPos", m_positions);
		m_compute.SetBuffer(kernel, "mVel", m_velocities);
		m_compute.SetBuffer(kernel, "mForce", m_forces);
		m_compute.SetBuffer(kernel, "mVelEval", m_velEval);
		m_compute.SetBuffer(kernel, "mGrid", m_Grid);
		m_compute.SetBuffer(kernel, "mOutput", m_Output);
		m_compute.SetBuffer(kernel, "mDensity", m_Density);

		kernel = m_compute.FindKernel("AddForce");
		m_compute.SetBuffer(kernel, "mPos", m_positions);
		m_compute.SetBuffer(kernel, "mVel", m_velocities);
		m_compute.SetBuffer(kernel, "mForce", m_forces);
		m_compute.SetBuffer(kernel, "mVelEval", m_velEval);
		m_compute.SetBuffer(kernel, "mGrid", m_Grid);
		m_compute.SetBuffer(kernel, "mOutput", m_Output);
		m_compute.SetBuffer(kernel, "mDensity", m_Density);

		kernel = m_compute.FindKernel("ResetParticles");
		m_compute.SetBuffer(kernel, "mPos", m_positions);
		m_compute.SetBuffer(kernel, "mVel", m_velocities);
		m_compute.SetBuffer(kernel, "mForce", m_forces);
		m_compute.SetBuffer(kernel, "mVelEval", m_velEval);
		m_compute.SetBuffer(kernel, "mGrid", m_Grid);
		m_compute.SetBuffer(kernel, "mOutput", m_Output);
		m_compute.SetBuffer(kernel, "mDensity", m_Density);


		m_compute.SetVector("_Gravity", new Vector3(0, -9.8f, 0));
		m_compute.SetFloat("_ParticleMass", 0.05f);
		m_compute.SetVector("_GridOrigin", Vector3.zero);
		m_compute.SetVector("_GridDimensions", m_gridDimensions);
		m_compute.SetFloat("_GridCellSize", m_gridCellSize);
		m_compute.SetFloat("_GridSize", m_gridCells.x);

		m_compute.SetFloat("_BoundDamp", 50000f);
		m_compute.SetFloat("_BoundStiffen", 100f);
		m_compute.SetFloat("_VelocityLimit", 3);
		m_compute.SetFloat("_VelocityLimit2", 3 * 3);

		m_compute.SetFloat("_AccelLimit", 150);
		m_compute.SetFloat("_AccelLimit2", 150 * 150);
		m_compute.SetFloat("_PScale", 0.05f);
		m_compute.SetFloat("_KernelRadius", 0.2f);

		m_compute.SetFloat("_PSmoothRadius", 0.1f);

		List<Vector3> position = new List<Vector3>();

		for (int i = 0; i < m_particleCount; i++)
		{
			position.Add(m_snowBallPosition + UnityEngine.Random.insideUnitSphere);
		}

		m_positions.SetData(position.ToArray());



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

		m_compute.SetVector("_Force", m_startForce);
		m_compute.Dispatch(m_compute.FindKernel("AddForce"), m_particleCount / 1000, 1, 1);
	}

	// Update is called once per frame
	void Update()
	{
		if (Input.GetKeyDown(KeyCode.P))
		{
			m_bIsRunning = !m_bIsRunning;
		}

		if(Input.GetKeyDown(KeyCode.R))
		{
			m_bIsRunning = false;
			List<Vector3> position = new List<Vector3>();
			for (int i = 0; i < m_particleCount; i++)
			{
				position.Add(m_snowBallPosition + UnityEngine.Random.insideUnitSphere);
			}

			m_positions.SetData(position.ToArray());

			m_compute.Dispatch(m_compute.FindKernel("ResetParticles"), m_particleCount / 1000, 1, 1);
			m_compute.SetVector("_Force", m_startForce);
			m_compute.Dispatch(m_compute.FindKernel("AddForce"), m_particleCount / 1000, 1, 1);
		}

		if(m_bIsRunning)
			StepForward();
	}

	void OnGUI()
	{
		string result = GUI.TextField(new Rect(Vector2.one, new Vector2(100, 20)), m_startForce.x.ToString());

		m_startForce.x = Convert.ToSingle(result);
	}

	void StepForward()
	{
		m_compute.SetFloat("_DeltaTime", Time.deltaTime);

		float[] output = new float[m_particleCount];

		m_compute.Dispatch(m_compute.FindKernel("ResetGrid"), (int)m_gridCells.x, (int)m_gridCells.y, (int)m_gridCells.z);
		m_compute.Dispatch(m_compute.FindKernel("AddParticlesToGrid"), m_particleCount / 1000, 1, 1);
		m_compute.Dispatch(m_compute.FindKernel("ComputeDensity"), m_particleCount / 1000, 1, 1);
		m_compute.Dispatch(m_compute.FindKernel("ComputeCohesion"), m_particleCount / 1000, 1, 1);

		//output data gathered from shader for debug purposes
	
		//Debug.Log(counter);

		m_compute.Dispatch(m_compute.FindKernel("UpdateParticles"), m_particleCount / 1000, 1, 1);

		m_Output.GetData(output);

		/*
		int counter = 0;
		for (int i = 0; i < 100; i++)
		{
			Debug.Log(output[i]);
		}*/

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
		m_forces.Dispose();
		m_quadPoints.Dispose();
		m_velEval.Dispose();
		m_Grid.Dispose();
		m_Density.Dispose();
		m_Output.Dispose();
	}
}
