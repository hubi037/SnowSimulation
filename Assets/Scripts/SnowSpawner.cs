using UnityEngine;
using System.Collections;

public class SnowSpawner : MonoBehaviour {

	public float massPerCell = 0.3f;

	[HideInInspector]
	public float radius;


	// Use this for initialization
	void Start () 
	{
	}
	
	// Update is called once per frame
	void Update () 
	{
		radius = transform.localScale.y;
	}
}
