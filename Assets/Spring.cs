﻿using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

//[ExecuteInEditMode]
public class Spring : MonoBehaviour {
    private int sideLength;
    public int numberOfPoints = 100;
    public Shader m_shader;
    public Texture m_particleTexture;
    public GameObject m_placeholder;
   
    private Material m_material;
    private ComputeBuffer m_pointsBuffer;

    private Point[] m_points;
    private Vector3[] m_allVertices;

    private List<Constraint> m_constraints;

    public GameObject ball;

    public float m_windScale;

    class Point {
        private static float DAMPING = 0.01f;
        private static float TIME_STEPSIZE2 = 0.5f*0.5f;

        public Vector3 position;
        public Vector3 oldPosition;
        //public Vector3 acceleration = new Vector3(0,0,0);
        public Vector3 force = new Vector3(0,0,0);
        public float massInverse = 1.0f / 10.0f;
        //public bool movable = true;

        public Point(Vector3 position) {
            this.position = position;
            this.oldPosition = this.position;
        }

        public void addForce(Vector3 f) {
            //acceleration += f/mass;
            force = f;
	    }

        private Vector3 filter(Vector3 a) {
            return a; //only different from a if the point is constrained on something like an axis. 
        }

        private void preconditionedConjucateGradient() {
            Vector3 deltaVelocity = new Vector3(0, 0, 0); //unconstrained

            Vector4 b = new Vector4();
            Matrix4x4 P = new Matrix4x4();
            Matrix4x4 A = new Matrix4x4();

            float epsilon = 0.001f;
            float epsilonSquared = epsilon * epsilon;

            //Vector4 a  = P * b;

            float deltaZero = Vector3.Dot(Matrix4x4.Transpose(P) * b, b); // i think the transpose is correct
            Vector4 r = b - A * deltaVelocity;
            Vector3 c = Matrix4x4.Inverse(P) * r;
            float deltaNew = Vector3.Dot(r, c);
            while (deltaNew > epsilonSquared * deltaZero) {
                Vector4 q = A * c;
                float alpha = deltaNew / (Vector3.Dot(c, q));
                deltaVelocity += alpha * c;
                r -= alpha * q;
                Vector3 s = Matrix4x4.Inverse(P) * r;
                float deltaOld = deltaNew;
                deltaNew = Vector3.Dot(r, s);
                c = s + (deltaNew / deltaOld) * c;
            }
        }

        public void updatePoint(float timeStep) {
		    Vector3 temp = position;
		    position = position + (position-oldPosition)*(1.0f-DAMPING) + /*acceleration*/(force * massInverse)*timeStep;
		    oldPosition = temp;
            force = new Vector3(0, 0, 0);
	    }
    }

    class Constraint {
	    float rest_distance; // the length between particle p1 and p2 in rest configuration

	    Point p1, p2; // the two particles that are connected through this constraint

	    public Constraint(Point p1, Point p2)
	    {
            this.p1 = p1;
            this.p2 = p2;
		    Vector3 vec = p1.position - p2.position;
		    rest_distance = vec.magnitude;
	    }

	    /* This is one of the important methods, where a single constraint between two particles p1 and p2 is solved
	    the method is called by Cloth.time_step() many times per frame*/
	    public void satisfyConstraint()
	    {
		    Vector3 p1_to_p2 = p2.position - p1.position; // vector from p1 to p2
		    float current_distance = p1_to_p2.magnitude; // current distance between p1 and p2
		    Vector3 correctionVector = p1_to_p2*(1 - rest_distance/current_distance); // The offset vector that could moves p1 into a distance of rest_distance to p2
		    Vector3 correctionVectorHalf = correctionVector*0.5f; // Lets make it half that length, so that we can move BOTH p1 and p2.

            //if (p1.movable)
                p1.position += p1.massInverse*correctionVectorHalf; // correctionVectorHalf is pointing from p1 to p2, so the length should move p1 half the length needed to satisfy the constraint.

            //if (p2.movable)
                p2.position += -p2.massInverse*correctionVectorHalf; // we must move p2 the negative direction of correctionVectorHalf since it points from p2 to p1, and not p1 to p2.	
	    }
    };

    private Point getParticle(int i, int j) {
        int index = i + j * sideLength;
        return m_points[index];
    }
    
    void makeConstraint(Point p1, Point p2) {
        m_constraints.Add(new Constraint(p1,p2));
    }

	void Awake () {                     
        sideLength = (int)Mathf.Sqrt(numberOfPoints);

        m_points = new Point[numberOfPoints];
        m_constraints = new List<Constraint>();
        m_allVertices = new Vector3[numberOfPoints];

        for (int i = 0; i < sideLength; i++) {
            for (int j = 0; j < sideLength; j++) {
                int index = i + j * sideLength;
                Vector3 position = new Vector3(i * 1, 0, j * 1);
                m_points[index] = new Point(position);
                m_allVertices[index] = position;
            }
        }
        for (int i = sideLength-1; i >= 0; i--) {
            for (int j = sideLength-1; j >= 0; j--) {

                //connect immediate neighbours with constraints
                if (i < sideLength-1)
                    makeConstraint(getParticle(i,j), getParticle(i+1,j));

				if (j < sideLength-1)
                    makeConstraint(getParticle(i,j), getParticle(i,j+1));

				if (i < sideLength-1 && j < sideLength-1)
                    makeConstraint(getParticle(i,j), getParticle(i+1,j+1));

				if (i < sideLength-1 && j < sideLength-1)
                    makeConstraint(getParticle(i+1,j), getParticle(i,j+1));


                //connect secondary neighbours with constraints
                if (i < sideLength-2)
                    makeConstraint(getParticle(i,j), getParticle(i+2,j));

				if (j < sideLength-2)
                    makeConstraint(getParticle(i,j), getParticle(i,j+2));

				if (i < sideLength-2 && j < sideLength-2)
                    makeConstraint(getParticle(i,j), getParticle(i+2,j+2));

				if (i < sideLength-2 && j < sideLength-2)
                    makeConstraint(getParticle(i+2,j), getParticle(i,j+2));			
            }
        }

        for (int i = 0; i < sideLength; i+=10) {
            //getParticle(i,0).movable = false;
            getParticle(i,0).massInverse = 0;

            //getParticle(i, sideLength - 1).mass = 5;
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
	void FixedUpdate () {
        float aspect = Camera.main.GetComponent<Camera>().aspect;
        m_material.SetFloat("aspect", aspect);

        //1. satisfy constraints for all particles
        //2. time step all particles 

        Vector3 ballCenter = ball.transform.position;//new Vector3(50, -38, 0);
        float ballRadius = 15;


        Vector3 randomForce = new Vector3(/*0.5f*/0, 0, 0.2f) *m_windScale; /** Time.deltaTime*/

        foreach (Constraint constraint in m_constraints) {
            constraint.satisfyConstraint();
        }                                                                        
             
        for (int i = 0; i < sideLength; i++) {
            for (int j = 0; j < sideLength; j++) {
                int index = i + j * sideLength;
                Point point = m_points[index];

                point.addForce(new Vector3(0, -0.5f, 0)); //gravity
                //point.addForce(randomForce);
                point.updatePoint(Time.deltaTime);

			    Vector3 v = point.position - ballCenter;
			    float l = v.magnitude;
			    if (l < ballRadius) // if the particle is inside the ball
			    {
                    //if (point.movable) {
                        point.position += point.massInverse*v.normalized*(ballRadius-l);
                    //}
			    }

                m_allVertices[index] = point.position;
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
