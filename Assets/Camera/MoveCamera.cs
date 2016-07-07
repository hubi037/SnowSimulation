using UnityEngine;
using System.Collections;

public class MoveCamera : MonoBehaviour {

	// Use this for initialization
	void Awake () 
    {
	
	}
	
	// Update is called once per frame
	void Update () 
    {
        if(Input.GetKey(KeyCode.W))
        {
            Camera.main.transform.position += Camera.main.transform.forward * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.S))
        {
            Camera.main.transform.position -= Camera.main.transform.forward * Time.deltaTime;
        }

        if(Input.GetKey(KeyCode.A))
        {
            Camera.main.transform.position -= Camera.main.transform.right * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.D))
        {
            Camera.main.transform.position += Camera.main.transform.right * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }
	
	}
}
