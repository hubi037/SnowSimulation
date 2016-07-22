using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class SnowChunkManager : MonoBehaviour 
{


	public Vector3 m_origin = Vector3.zero;

	public float m_cellSize = 0.1f;
	public Vector3 m_numCells; 

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
		public bool isColliding;
		public Vector3 worldPosition;
		public Vector3 velocity;
	}

	struct _SphereCollider
	{
		public Vector3 position;
		public Vector3 velocity;
		public float radius;
		public float padding;
	}

	private Dictionary<Vector3, SnowChunk> m_chunks = new Dictionary<Vector3, SnowChunk>();

	private ComputeBuffer m_quadPoints;
	private ComputeBuffer m_completeBuffer;
	private ComputeBuffer m_atomicStorage;
	private ComputeBuffer m_colliders;

	private bool running = false;

	private List<SphereCollider> m_sphereColliders = new List<SphereCollider>();

	void Awake()
	{
		m_sphereColliders.AddRange(GameObject.FindObjectsOfType<SphereCollider>());

		m_compute.SetInt("_FullWidth", (int)m_numCells.x);
		m_compute.SetInt("_FullHeight", (int)m_numCells.y);
		m_compute.SetInt("_FullLength", (int)m_numCells.z);
		m_compute.SetFloat("_CellSize", m_cellSize);
		m_compute.SetFloat("_Gravity", 9.8f);
		m_compute.SetVector("_GridOrigin", m_origin);

		m_completeBuffer = new ComputeBuffer((int)(m_numCells.x * m_numCells.y * m_numCells.z), sizeof(float) * 8);
		m_atomicStorage = new ComputeBuffer(m_completeBuffer.count, sizeof(uint) * 2 + sizeof(float) * 4 * 63 + sizeof(float) * 2);


		List<_SphereCollider> colliders = new List<_SphereCollider>();

		foreach(SphereCollider coll in m_sphereColliders)
		{
			_SphereCollider newCollider = new _SphereCollider();
			newCollider.position = coll.transform.position;
			newCollider.radius = coll.radius;
			newCollider.velocity = Vector3.zero;		
			colliders.Add(newCollider);
		}

		m_colliders = new ComputeBuffer(colliders.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(_SphereCollider)));
		m_colliders.SetData(colliders.ToArray());

		Debug.Log(m_colliders.count);

		Debug.Log(m_completeBuffer.count);
		Debug.Log(sizeof(uint) * 2 + sizeof(float) * 4 * 63 + sizeof(float) * 2);

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

		m_compute.SetInt("_NumColliders", m_colliders.count);

		m_compute.SetBuffer(m_compute.FindKernel("Update"), "_Cells", m_completeBuffer);
		m_compute.SetBuffer(m_compute.FindKernel("Update"), "_AtomicStorage", m_atomicStorage);
		m_compute.SetBuffer(m_compute.FindKernel("UpdateCells"), "_Cells", m_completeBuffer);
		m_compute.SetBuffer(m_compute.FindKernel("UpdateCells"), "_AtomicStorage", m_atomicStorage);
		m_compute.SetBuffer(m_compute.FindKernel("InitialiseGrid"), "_Cells", m_completeBuffer);
		m_compute.SetBuffer(m_compute.FindKernel("InitialiseGrid"), "_AtomicStorage", m_atomicStorage);
		m_compute.SetBuffer(m_compute.FindKernel("UpdateVelocities"), "_Cells", m_completeBuffer);
		m_compute.SetBuffer(m_compute.FindKernel("UpdateVelocities"), "_AtomicStorage", m_atomicStorage);
		m_compute.SetBuffer(m_compute.FindKernel("UpdateVelocities"), "_Colliders", m_colliders);

		m_compute.Dispatch(m_compute.FindKernel("InitialiseGrid"), (int)m_numCells.x / 8, (int)m_numCells.y / 8, (int)m_numCells.z / 8);

		Debug.Log(Vector3.Dot(Vector3.down, new Vector3(-1,-1,0)));
	}


	void OnPostRender()
	{
		m_Material.SetPass(0);
		m_Material.SetBuffer("_Cells", m_completeBuffer);

		Graphics.DrawProcedural(MeshTopology.Triangles, 6, m_completeBuffer.count);
	}

	void Update()
	{
		m_sphereColliders = new List<SphereCollider>();
		m_sphereColliders.AddRange(GameObject.FindObjectsOfType<SphereCollider>());

		List<_SphereCollider> colliders = new List<_SphereCollider>();

		foreach (SphereCollider coll in m_sphereColliders)
		{
			_SphereCollider newCollider = new _SphereCollider();
			newCollider.position = coll.transform.position;
			newCollider.radius = coll.radius;

			Rigidbody collRigid = coll.GetComponent<Rigidbody>();

			if (collRigid)
				newCollider.velocity = collRigid.velocity;
			else
				newCollider.velocity = Vector3.zero;

			colliders.Add(newCollider);
		}

		m_colliders.SetData(colliders.ToArray());

		if (Input.GetKeyDown(KeyCode.P))
			running = !running;

		if (Input.GetKeyDown(KeyCode.Q) && Time.timeScale < 1.0f)
		{
			Time.timeScale += 0.1f;
			Debug.Log("Set Timescale to: " + Time.timeScale);
		}

		if (Input.GetKeyDown(KeyCode.E) && Time.timeScale > 0.0f)
		{
			Time.timeScale -= 0.1f;
			Debug.Log("Set Timescale to: " + Time.timeScale);
		}


		if (Input.GetKeyDown(KeyCode.R))
		{
			m_compute.Dispatch(m_compute.FindKernel("InitialiseGrid"), (int)m_numCells.x / 8, (int)m_numCells.y / 8, (int)m_numCells.z / 8);
			running = false;
		}

		if(running)
		{
			UpdateVelocities();
			calculateMovement();
			UpdateCells();
		}
	}


	void calculateMovement()
	{

		m_compute.SetFloat("_DeltaTime", Time.deltaTime);

		m_compute.Dispatch(m_compute.FindKernel("Update"), (int)m_numCells.x / 8, (int)m_numCells.y / 8, (int)m_numCells.z / 8);

	}

	void UpdateVelocities()
	{
		m_compute.SetFloat("_DeltaTime", Time.deltaTime);

		m_compute.Dispatch(m_compute.FindKernel("UpdateVelocities"), (int)m_numCells.x / 8, (int)m_numCells.y / 8, (int)m_numCells.z / 8);

	}

	void UpdateCells()
	{

		m_compute.SetFloat("_DeltaTime", Time.deltaTime);

		m_compute.Dispatch(m_compute.FindKernel("UpdateCells"), (int)m_numCells.x / 8, (int)m_numCells.y / 8, (int)m_numCells.z / 8);
	}


	void OnDestroy()
	{
		foreach (SnowChunk chunk in m_chunks.Values)
			chunk.m_buffer.Dispose();

		m_quadPoints.Dispose();
		m_completeBuffer.Dispose();
		m_atomicStorage.Dispose();
		m_colliders.Dispose();
	}
}
