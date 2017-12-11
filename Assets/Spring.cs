using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[ExecuteInEditMode]
public class Spring : MonoBehaviour {
    private int sideLength;
    public int numberOfPoints = 100;
    public Shader m_shader;
    public Texture m_particleTexture;
    public GameObject m_placeholder;
   
    private Material m_material;
    private ComputeBuffer m_pointsBuffer;

    private Point[] points;
    private Vector3[] m_allVertices;

    class Point {
        private static float DAMPING = 0.01f;
        private static float TIME_STEPSIZE2 = 0.5f*0.5f;

        public Vector3 position;
        public Vector3 oldPosition;
        public Vector3 acceleration;
        public float mass;

        public Point(Vector3 position) {
            this.position = position;
        }

        void updatePoint (Vector3 oldPos) {
		    Vector3 temp = position;
		    position = position + (position-oldPosition)*(1.0f-DAMPING) + acceleration*TIME_STEPSIZE2;
		    oldPosition = temp;
		    acceleration = new Vector3(0, 0, 0); // acceleration is reset since it HAS been translated into a change in position (and implicitely into velocity)
	    }
    }

    //private GameObject spheres;

    /**/

	// Use this for initialization
	void Awake () {                     
        sideLength = (int)Mathf.Sqrt(numberOfPoints);

        points = new Point[numberOfPoints];
        m_allVertices = new Vector3[numberOfPoints];

        for (int i = 0; i < sideLength; i++) {
            for (int j = 0; j < sideLength; j++) {
                int index = i + j * sideLength;
                Vector3 position = new Vector3(i * 10, j * 10, 0);
                points[index] = new Point(position);
                m_allVertices[index] = position;//points[index].position;
            }
        }

        m_material = new Material(m_shader);

        m_pointsBuffer = new ComputeBuffer (numberOfPoints, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Default);
        m_pointsBuffer.SetData(m_allVertices);
        m_material.SetBuffer("_Points", m_pointsBuffer);
        float aspect = Camera.main.GetComponent<Camera>().aspect;
        m_material.SetFloat("aspect", aspect);
        m_material.SetTexture("_AlbedoTex", m_particleTexture);
	}
	
	// Update is called once per frame
	void Update () {
        //1. satisfy constraints for all particles
        //2. time step all particles                            

        for (int i = 0; i < sideLength; i++) {
            for (int j = 0; j < sideLength; j++) {
                int index = i + j * sideLength;
                //do stuff to point here
                points[index].position = points[index].position + new Vector3(0.005f, 0, 0);

                m_allVertices[index] = points[index].position;
            }
        }

        m_pointsBuffer.SetData(m_allVertices);
        
	}

    private void OnRenderObject() {
        m_material.SetPass(0);
        GL.MultMatrix(m_placeholder.transform.localToWorldMatrix);
        Graphics.DrawProcedural(MeshTopology.Triangles, m_pointsBuffer.count * 6);
    }
}
