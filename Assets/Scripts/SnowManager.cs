using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class SnowManager : MonoBehaviour
{


	public Vector3 m_origin = Vector3.zero;
	public int m_particleAmount = 100000;
	public Vector3 m_cloudSize = Vector3.one;
	public Material m_snowMaterial;
	public ComputeShader m_computeShader;

	private List<SphereCollider> m_sphereColliders = new List<SphereCollider>();

	private SnowParticleCloud m_particleCloud;
	private SnowGrid m_grid;
	private ComputeBuffer m_quadPoints;
	private ComputeBuffer m_colliderBuffer;
	private bool running = false;

	struct _SphereCollider
	{
		public Vector4 position;
		public Vector4 velocity;
		public float radius;
		public float coeffFriction;
	}

	void Awake()
	{

		//get all colliders in the scene and build a buffer
		m_sphereColliders.AddRange(GameObject.FindObjectsOfType<SphereCollider>());
		List<_SphereCollider> colliders = new List<_SphereCollider>();

		foreach(SphereCollider coll in m_sphereColliders)
		{
			_SphereCollider collider = new _SphereCollider();
			collider.position = coll.transform.position;
			collider.velocity = Vector4.zero; //todo: check for rigidbody and set velocity if it has one
			collider.coeffFriction = 0.1f;
			collider.radius = coll.radius;
			colliders.Add(collider);
		}

		m_colliderBuffer = new ComputeBuffer(colliders.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(_SphereCollider)));
		m_colliderBuffer.SetData(colliders.ToArray());

		Debug.Log("Amount of colliders: " + m_colliderBuffer.count);

		//creates and renders our particles
		m_particleCloud = new SnowParticleCloud(m_particleAmount, m_origin, m_cloudSize, m_computeShader, m_snowMaterial);
		//creates our grid and performs the snow simulation on the particles and the grid
		m_grid = new SnowGrid(Vector3.zero, new Vector3(4, 4, 4), new Vector3(64,64,64), m_particleCloud, m_computeShader);

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

		m_snowMaterial.SetBuffer("quadPoints", m_quadPoints);

		m_computeShader.SetBuffer(m_computeShader.FindKernel("UpdateParticlePositions"), "_SphereColliders", m_colliderBuffer);
		m_computeShader.SetFloat("_NumColliders", m_colliderBuffer.count);
	}

	void OnPostRender()
	{
		m_particleCloud.render(m_snowMaterial);
	}

	void Update()
	{

		m_computeShader.SetFloat("_DeltaTime", Time.deltaTime);

		if (Input.GetKeyDown(KeyCode.P))
			running = !running;

		if(running)
		{
			m_grid.resetGrid();
			m_grid.initMass();
			m_grid.explicitGridVelocities();
			//m_grid.equalizeGridVelocities();
			m_grid.updateVelocities();

			m_particleCloud.updateParticlePositions(m_computeShader);
		}
	}

	void OnDestroy()
	{
		m_colliderBuffer.Dispose();
		m_quadPoints.Dispose();
		m_grid.DisposeBuffers();
		m_particleCloud.DisposeBuffers();
	}
}
