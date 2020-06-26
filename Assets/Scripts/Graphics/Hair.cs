using UnityEngine;

public class Hair : MonoBehaviour
{
	public Material m_Material = null;

	public float MaxSpringLength = 1.0f;
	public float SpringDampening = 10.0f;
	public float MicroFrictionStrength = 1.0f;
	public float FrictionStrength = 0.01f;
	public float CollisionStrength = 10.0f;

	public Transform[] m_ControlPoints;

	public Transform m_Head;
	public Transform m_Plane;

	Mesh m_Mesh = null;
	GameObject[] m_Instances = null;

	const int CurlsCount = 16;
	const int StrandsCount = 16;

	public class Simulation
	{
		public class PointInformation
		{
			public Vector3 m_Position = Vector3.zero;
			//public Vector3 m_Acceleration = Vector3.zero;
		}

		public Simulation()
		{
			m_Curls = new PointInformation[CurlsCount * StrandsCount];
			for (int Index = 0, Count = CurlsCount * StrandsCount; Index < Count; ++Index)
			{
				m_Curls[Index] = new PointInformation();
			}
		}

		public PointInformation[] m_Curls = null;
	}

	Simulation m_Simulation = new Simulation();

	// Start is called before the first frame update
	void Start()
	{
		m_Mesh = new Mesh();

		Vector3[] Vertices = new Vector3[]
		{
			new Vector3(0f, 1f, 0f),
			new Vector3(-1f, 0f, 0f),
			new Vector3(0f, 0f, -1f),
			new Vector3(1f, 0f, 0f),
			new Vector3(0f, 0f, 1f),
			new Vector3(0f, -1f, 0f)
		};

		int[] Indices = new int[]
		{
			0, 2, 1,
			0, 3, 2,
			0, 4, 3,
			0, 1, 4,
			5, 1, 2,
			5, 2, 3,
			5, 3, 4,
			5, 4, 1
		};

		m_Mesh.vertices = Vertices;
		m_Mesh.triangles = Indices;
		m_Mesh.RecalculateNormals();

		m_Instances = new GameObject[StrandsCount * CurlsCount];

		Debug.Assert(m_ControlPoints.Length >= 2);

		for (int StrandIndex = 0; StrandIndex < StrandsCount; ++StrandIndex)
		{
			float StrandAlpha = (float)StrandIndex / (float)(StrandsCount - 1);

			Vector3 RootPosition = Vector3.Lerp(m_ControlPoints[0].localPosition, m_ControlPoints[1].localPosition, Mathf.PingPong(StrandAlpha, 0.5f) * 2f);
			RootPosition += new Vector3(StrandAlpha < 0.5f ? -0.1f : 0.1f, 0f, 0f);
			int StrandOffset = StrandIndex * CurlsCount;

			for (int CurlIndex = 0; CurlIndex < CurlsCount; ++CurlIndex)
			{
				Vector3 LocalPosition = RootPosition + new Vector3(0f, -(float)CurlIndex * 2f, 0f);

				int CurlOffset = StrandOffset + CurlIndex;

				m_Instances[CurlOffset] = new GameObject();
				m_Instances[CurlOffset].transform.SetParent(gameObject.transform);
				m_Instances[CurlOffset].transform.localPosition = LocalPosition;
				m_Simulation.m_Curls[CurlOffset].m_Position = m_Instances[CurlOffset].transform.position;

				MeshRenderer Renderer = m_Instances[CurlOffset].AddComponent<MeshRenderer>();
				Renderer.material = m_Material;

				MeshFilter MeshFilterObject = m_Instances[CurlOffset].AddComponent<MeshFilter>();
				MeshFilterObject.mesh = m_Mesh;
			}
		}
	}

	Vector3 GetFriction(Vector3 CurrentForce)
	{
		float CurrentForceStrength = CurrentForce.magnitude * FrictionStrength;

		return (-CurrentForce.normalized + new Vector3(Random.Range(-MicroFrictionStrength, MicroFrictionStrength), 0.0f, 0.0f)) * CurrentForceStrength;
	}

	Vector3 GetCollision(Vector3 CurrentPosition)
	{
		Vector3 PositionToHead = (CurrentPosition - m_Head.position);
		Vector3 CollisionForce = Vector3.zero;
		if (PositionToHead != Vector3.zero)
		{
			CollisionForce += PositionToHead.magnitude < 5.0f ? PositionToHead.normalized * CollisionStrength : Vector3.zero;
		}

		Plane FacePlane = new Plane(m_Plane.up, m_Plane.position);
		if (!FacePlane.GetSide(CurrentPosition))
		{
			CollisionForce -= m_Plane.up * FacePlane.GetDistanceToPoint(CurrentPosition) * CollisionStrength;
		}

		return CollisionForce;
	}

	// Update is called once per frame
	void Update()
	{
		for (int StrandIndex = 0; StrandIndex < StrandsCount; ++StrandIndex)
		{
			int StrandOffset = StrandIndex * CurlsCount;

			m_Simulation.m_Curls[StrandOffset].m_Position = m_Instances[StrandOffset].transform.position;
			for (int CurlIndex = 1; CurlIndex < CurlsCount; ++CurlIndex)
			{
				int CurlOffset = StrandOffset + CurlIndex;

				Vector3 Position = m_Simulation.m_Curls[CurlOffset].m_Position;
				Vector3 Acceleration = Vector3.zero;
				Acceleration += Physics.gravity;

				Vector3 SpringForce = m_Simulation.m_Curls[CurlOffset - 1].m_Position - m_Simulation.m_Curls[CurlOffset].m_Position;
				if (SpringForce != Vector3.zero)
				{
					SpringForce = SpringForce.normalized * Mathf.Max(SpringForce.magnitude - MaxSpringLength, 0.0f) * SpringDampening;
				}
				Acceleration += SpringForce;
				Acceleration += GetFriction(Acceleration);
				Acceleration += GetCollision(Position);
				Position += Acceleration * Time.deltaTime;
				m_Simulation.m_Curls[CurlOffset].m_Position = Position;
				m_Instances[CurlOffset].transform.position = m_Simulation.m_Curls[CurlOffset].m_Position;
			}
		}
	}
}
