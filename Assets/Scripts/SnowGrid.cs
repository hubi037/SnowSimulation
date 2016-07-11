using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SnowGrid
{

	struct GridCell
	{
		public Vector3 velocity;
		public Vector3 velocityChange;
		public Vector3 force;
		public float mass;
		public float active;
		public float density;
	}

	private ComputeBuffer m_gridBuffer;
	private ComputeShader m_computeShader;
	private SnowParticleCloud m_particleCloud;

	private Vector3 m_origin;
	private Vector3 m_dimensions;
	private Vector3 m_cells;
	private float m_cellSize;
	private float m_cellVolume;

	// Use this for initialization
	public SnowGrid (Vector3 _origin, Vector3 _dimensions, Vector3 _cells, SnowParticleCloud _particles, ComputeShader _computeShader) 
	{
		m_particleCloud = _particles;
		m_origin = _origin;
		m_dimensions = _dimensions;
		m_cellSize = _dimensions.x / _cells.x;
		m_cells = _cells;

		m_cellVolume = m_cellSize * m_cellSize * m_cellSize;

		m_gridBuffer = new ComputeBuffer((int)(m_cells.x * m_cells.y * m_cells.z), System.Runtime.InteropServices.Marshal.SizeOf(typeof(GridCell)));

		List<GridCell> gridCells = new List<GridCell>();
		for (int i = 0; i < m_gridBuffer.count; i++ )
		{
			GridCell newCell = new GridCell();
			newCell.mass = 0;

			gridCells.Add(newCell);
		}

		m_gridBuffer.SetData(gridCells.ToArray());

		Debug.Log("Amount of GridCells: " + m_gridBuffer.count);

		m_computeShader = _computeShader;


		//we have to set the buffers for each kernel separately....
		//should find a better way to do this
		m_computeShader.SetBuffer(_computeShader.FindKernel("CalculateVolumes"), "_Particles", m_particleCloud.getParticleBuffer());
		m_computeShader.SetBuffer(_computeShader.FindKernel("CalculateVolumes"), "_Grid", m_gridBuffer);

		m_computeShader.SetBuffer(_computeShader.FindKernel("ExplicitGridVelocities"), "_Particles", m_particleCloud.getParticleBuffer());
		m_computeShader.SetBuffer(_computeShader.FindKernel("ExplicitGridVelocities"), "_Grid", m_gridBuffer);
		m_computeShader.SetBuffer(_computeShader.FindKernel("ExplicitGridVelocities"), "_DefMaterials", m_particleCloud.getDeformationBuffer());

		m_computeShader.SetBuffer(_computeShader.FindKernel("UpdateVelocities"), "_Particles", m_particleCloud.getParticleBuffer());
		m_computeShader.SetBuffer(_computeShader.FindKernel("UpdateVelocities"), "_Grid", m_gridBuffer);

		m_computeShader.SetBuffer(_computeShader.FindKernel("CollisionGrid"), "_Particles", m_particleCloud.getParticleBuffer());
		m_computeShader.SetBuffer(_computeShader.FindKernel("CollisionGrid"), "_Grid", m_gridBuffer);

		m_computeShader.SetBuffer(_computeShader.FindKernel("ResetGrid"), "_Particles", m_particleCloud.getParticleBuffer());
		m_computeShader.SetBuffer(_computeShader.FindKernel("ResetGrid"), "_Grid", m_gridBuffer);

		m_computeShader.SetBuffer(_computeShader.FindKernel("EqualizeGridVelocities"), "_Particles", m_particleCloud.getParticleBuffer());
		m_computeShader.SetBuffer(_computeShader.FindKernel("EqualizeGridVelocities"), "_Grid", m_gridBuffer);

		m_computeShader.SetBuffer(_computeShader.FindKernel("InitialiseGridMassAndVelocities"), "_Particles", m_particleCloud.getParticleBuffer());
		m_computeShader.SetBuffer(_computeShader.FindKernel("InitialiseGridMassAndVelocities"), "_Grid", m_gridBuffer);

		m_particleCloud.setGridBuffer(m_gridBuffer);

		//these need only be set once
		m_computeShader.SetVector("_GridOrigin", new Vector4(m_origin.x, m_origin.y, m_origin.z, 0));
		m_computeShader.SetFloat("_GridCellSize", m_cellSize);
		m_computeShader.SetFloat("_CellVolume", m_cellVolume);
		m_computeShader.SetFloat("_MaxCellDensity", 10f);
		m_computeShader.SetVector("_GridDimensions", new Vector4(m_dimensions.x, m_dimensions.y, m_dimensions.z, 0));
		m_computeShader.SetVector("_GridCells", new Vector4(m_cells.x, m_cells.y, m_cells.z, 0));
		m_computeShader.SetVector("_Gravity", new Vector4(0, -9.8f, 0, 0));
		
		//calculate volumes needs only be done once, depends on initMass
		initMass();
		calculateVolumes();
	}
	 
	public void initMass()
	{
		int kernel = checkKernel("InitialiseGridMassAndVelocities");

		m_computeShader.Dispatch(kernel, m_particleCloud.getParticleBuffer().count / Constants.NUMTHREADS, 1, 1);
	}

	public void calculateVolumes()
	{
		int kernel = checkKernel("CalculateVolumes");

		m_computeShader.Dispatch(kernel, m_particleCloud.getParticleBuffer().count / Constants.NUMTHREADS, 1, 1);
	}

	public void explicitGridVelocities()
	{
		int kernel = checkKernel("ExplicitGridVelocities");

		m_computeShader.Dispatch(kernel, (int)m_cells.x / 8, (int)m_cells.y / 8, (int)m_cells.z / 8);
	}

	public void equalizeGridVelocities()
	{
		int kernel = checkKernel("EqualizeGridVelocities");

		m_computeShader.Dispatch(kernel, (int)m_cells.x / 8, (int)m_cells.y / 8, (int)m_cells.z / 8);
	}

	public void updateVelocities()
	{
		int kernel = checkKernel("UpdateVelocities");

		m_computeShader.Dispatch(kernel, m_particleCloud.getParticleBuffer().count / Constants.NUMTHREADS, 1, 1);

	}

	public void gridCollisions()
	{
		int kernel = checkKernel("CollisionGrid");

		m_computeShader.Dispatch(kernel, (int)m_cells.x / 8, (int)m_cells.y / 8, (int)m_cells.z / 8);
	}

	public void resetGrid()
	{
		int kernel = checkKernel("ResetGrid");

		m_computeShader.Dispatch(kernel, (int)m_cells.x / 8, (int)m_cells.y / 8, (int)m_cells.z / 8);
	}

	int checkKernel(string _kernel)
	{
		int kernel = m_computeShader.FindKernel(_kernel);
		if (kernel < 0)
			Debug.LogError("Kernel not found!");

		return kernel;
	}

	public void DisposeBuffers()
	{
		m_gridBuffer.Dispose();
	}
}
