using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[ExecuteInEditMode]
public class Spring : MonoBehaviour {

    //private List<Point> m_points;
    private int sideLength;
    public int numberOfPoints = 100;
    public Shader m_shader;
    public Texture m_particleTexture;
    public GameObject m_placeholder;
   
    private Material m_material;
    private ComputeBuffer m_pointsBuffer;

    private Vector3[] points; 

    class Point {
        public Vector3 position;
        public Vector3 oldPosition;

        public Point(Vector3 position) {
            this.position = position;
        }
    }

    //private GameObject spheres;

    /*void updatePoint (Vector3 oldPos) {
		Vector3 temp = pos;
		pos = pos + (pos-old_pos)*(1.0-DAMPING) + acceleration*TIME_STEPSIZE2;
		old_pos = temp;
		acceleration = Vec3(0,0,0); // acceleration is reset since it HAS been translated into a change in position (and implicitely into velocity)
	}*/

	// Use this for initialization
	void Awake () {                     
        sideLength = (int)Mathf.Sqrt(numberOfPoints);

        points = new Vector3[numberOfPoints];

        for (int i = 0; i < sideLength; i++) {
            for (int j = 0; j < sideLength; j++) {
                int index = i + j * sideLength;
                Vector3 point = new Vector3(i * 10, j * 10, 0);
                points[index] = point;
            }
        }

        m_material = new Material(m_shader);

        m_pointsBuffer = new ComputeBuffer (points.Length, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        m_pointsBuffer.SetData(points);
        m_material.SetBuffer("_Points", m_pointsBuffer);
        float aspect = Camera.main.GetComponent<Camera>().aspect;
        m_material.SetFloat("aspect", aspect);
        m_material.SetTexture("_AlbedoTex", m_particleTexture);
	}
	
	// Update is called once per frame
	void Update () {
        for (int i = 0; i < sideLength; i++) {
            for (int j = 0; j < sideLength; j++) {
                int index = i + j * sideLength;
                //do stuff to point here
                points[index] = points[index] + new Vector3(0.005f, 0, 0);                   
            }
       }
       m_pointsBuffer.SetData(points);
        
	}

    private void OnRenderObject() {
        m_material.SetPass(0);
        GL.MultMatrix(m_placeholder.transform.localToWorldMatrix);
        Graphics.DrawProcedural(MeshTopology.Triangles, m_pointsBuffer.count * 6);
    }
}
