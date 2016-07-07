using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class SnowChunkManager : MonoBehaviour 
{


	public Vector3 m_origin = Vector3.zero;
	public int m_snowChunkHeight = 10;
	public int m_snowChunkWidth = 10;
	public int m_snowChunkLength = 10;
	public float m_cellSize = 0.1f;

	public int m_numChunksX = 10;
	public int m_numChunksY = 10;
	public int m_numChunksZ = 10;

	public float m_Gravity = 9.8f;
	public ComputeShader m_compute;
	public Material m_Material;

	struct SnowChunk
	{
		public ComputeBuffer m_buffer;
		public Vector3 m_origin;
		public int m_sizeX;
		public int m_sizeY;
		public int m_sizeZ;
		public Vector3 m_index;
	}

	struct SnowCell
	{
		public float fillGrade;
		public Vector3 worldPosition;
		public Vector3 velocity;
	}

	private Dictionary<Vector3, SnowChunk> m_chunks = new Dictionary<Vector3, SnowChunk>();

	private ComputeBuffer m_quadPoints;
	private ComputeBuffer m_completeBuffer;
	private ComputeBuffer m_completeBuffer2;
	private List<Vector3> m_chunkIndices = new List<Vector3>();

	private uint frameCounter = 0;

	private List<SphereCollider> m_sphereColliders = new List<SphereCollider>();

	void Awake()
	{

		List<SnowCell> allCells = new List<SnowCell>();

		m_compute.SetInt("_Width", m_snowChunkWidth);
		m_compute.SetInt("_Height", m_snowChunkHeight);
		m_compute.SetInt("_FullWidth", m_snowChunkWidth * m_numChunksX);
		m_compute.SetInt("_FullHeight", m_snowChunkHeight * m_numChunksY);
		m_compute.SetFloat("_CellSize", m_cellSize);
		m_compute.SetFloat("_Gravity", 9.8f);

		for (int x = 0; x < m_snowChunkWidth * m_numChunksX; x++ )
		{
			for (int y = 0; y < m_snowChunkHeight * m_numChunksY; y++)
			{
				for (int z = 0; z < m_snowChunkLength * m_numChunksZ; z++)
				{
					SnowCell cell = new SnowCell();
					cell.fillGrade = 0.1f; // Random.Range(0.0f, 0.5f);
					cell.worldPosition = new Vector3(m_origin.x + x * m_cellSize,
														m_origin.y + y * m_cellSize,
														m_origin.z + z * m_cellSize
													);
					allCells.Add(cell);
				}
			}
		}

		for (int i = 0; i < m_numChunksX; i++ )
		{
			for (int j = 0; j < m_numChunksY; j++)
			{
				for (int k = 0; k < m_numChunksZ; k++)
				{
					m_chunkIndices.Add(new Vector3(m_snowChunkWidth * i, m_snowChunkHeight * j, m_snowChunkLength * k));
				}
			}
		}

		m_completeBuffer = new ComputeBuffer(allCells.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SnowCell)));
		m_completeBuffer.SetData(allCells.ToArray());
		m_completeBuffer2 = new ComputeBuffer(allCells.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(SnowCell)));
		m_completeBuffer2.SetData(allCells.ToArray());

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
		m_Material.SetFloat("_CellSize", m_cellSize);
		m_Material.SetPass(0);

		m_compute.SetBuffer(m_compute.FindKernel("Update"), "_Cells", m_completeBuffer);
		m_compute.SetBuffer(m_compute.FindKernel("Update"), "_OutputCells", m_completeBuffer2);
		m_compute.SetBuffer(m_compute.FindKernel("UpdateCells"), "_Cells", m_completeBuffer);
		m_compute.SetBuffer(m_compute.FindKernel("UpdateCells"), "_OutputCells", m_completeBuffer2);
		m_compute.SetBuffer(m_compute.FindKernel("CSMain"), "_Cells", m_completeBuffer);
		m_compute.SetBuffer(m_compute.FindKernel("CSMain"), "_OutputCells", m_completeBuffer2);
		m_compute.SetBuffer(m_compute.FindKernel("DoSphereCollision"), "_Cells", m_completeBuffer);
		m_compute.SetBuffer(m_compute.FindKernel("DoSphereCollision"), "_OutputCells", m_completeBuffer2);


		m_sphereColliders.AddRange(GameObject.FindObjectsOfType<SphereCollider>());

		Debug.Log(m_sphereColliders.Count);
	}


	void OnPostRender()
	{
		m_Material.SetPass(0);
		m_Material.SetBuffer("_Cells", m_completeBuffer);

		Graphics.DrawProcedural(MeshTopology.Triangles, 6, m_completeBuffer.count);
	}

	void Update()
	{

		foreach (Vector3 index in m_chunkIndices)
		{
			calculateMovement(index);
		}

		/*
		foreach (Vector3 index in m_chunkIndices)
		{
			UpdateCells(index);
		}*/

		foreach (SphereCollider coll in m_sphereColliders)
		{
			foreach (Vector3 index in m_chunkIndices)
			{
				sphereCollision(index, coll.transform.position, coll.radius * coll.transform.localScale.y);
			}
		}

		frameCounter++;
		if (frameCounter == 2)
			frameCounter = 0;
	}

	void FixedUpdate()
	{	
		foreach(Vector3 index in m_chunkIndices)
		{
			 calculateGravity(index);
		}
	}

	void calculateGravity(Vector3 _originIndex)
	{

		m_compute.SetFloat("_DeltaTime", Time.fixedDeltaTime);
		m_compute.SetFloat("_Gravity", m_Gravity);
		m_compute.SetVector("_OriginIndex", new Vector4(_originIndex.x, _originIndex.y, _originIndex.z, 0));
		m_compute.SetInt("_FullWidth", m_snowChunkWidth * m_numChunksX);
		m_compute.SetInt("_FullHeight", m_snowChunkHeight * m_numChunksY);
		m_compute.Dispatch(m_compute.FindKernel("CSMain"), m_snowChunkWidth / 8, m_snowChunkHeight / 8, m_snowChunkLength / 8);
	}

	void calculateMovement(Vector3 _originIndex)
	{

		m_compute.SetFloat("_DeltaTime", Time.deltaTime);
		m_compute.SetFloat("_Gravity", m_Gravity);
		m_compute.SetInt("_FullWidth", m_snowChunkWidth * m_numChunksX);
		m_compute.SetInt("_FullHeight", m_snowChunkHeight * m_numChunksY);
		m_compute.SetInt("_FrameCounter", (int)frameCounter);
		m_compute.SetVector("_OriginIndex", new Vector4(_originIndex.x, _originIndex.y, _originIndex.z, 0));

		m_compute.Dispatch(m_compute.FindKernel("Update"), m_snowChunkWidth / 8, m_snowChunkHeight / 8, m_snowChunkLength / 8);

	}

	void sphereCollision(Vector3 _originIndex, Vector3 _sphereCenter, float _sphereRadius)
	{

		m_compute.SetFloat("_DeltaTime", Time.deltaTime);
		m_compute.SetFloat("_Gravity", m_Gravity);
		m_compute.SetInt("_FullWidth", m_snowChunkWidth * m_numChunksX);
		m_compute.SetInt("_FullHeight", m_snowChunkHeight * m_numChunksY);
		m_compute.SetVector("_OriginIndex", new Vector4(_originIndex.x, _originIndex.y, _originIndex.z, 0));
		m_compute.SetVector("_SphereCenter", new Vector4(_sphereCenter.x, _sphereCenter.y, _sphereCenter.z, 0));
		m_compute.SetFloat("_SphereRadius", _sphereRadius);

		m_compute.Dispatch(m_compute.FindKernel("DoSphereCollision"), m_snowChunkWidth / 8, m_snowChunkHeight / 8, m_snowChunkLength / 8);
	}

	void UpdateCells(Vector3 _originIndex)
	{

		m_compute.SetFloat("_DeltaTime", Time.deltaTime);
		m_compute.SetFloat("_Gravity", m_Gravity);
		m_compute.SetInt("_FullWidth", m_snowChunkWidth * m_numChunksX);
		m_compute.SetInt("_FullHeight", m_snowChunkHeight * m_numChunksY);
		m_compute.SetInt("_FrameCounter", (int)frameCounter);
		m_compute.SetVector("_OriginIndex", new Vector4(_originIndex.x, _originIndex.y, _originIndex.z, 0));

		m_compute.Dispatch(m_compute.FindKernel("UpdateCells"), m_snowChunkWidth / 8, m_snowChunkHeight / 8, m_snowChunkLength / 8);
	}


	void OnDestroy()
	{
		foreach (SnowChunk chunk in m_chunks.Values)
			chunk.m_buffer.Dispose();

		m_quadPoints.Dispose();
		m_completeBuffer.Dispose();
		m_completeBuffer2.Dispose();
	}
}
