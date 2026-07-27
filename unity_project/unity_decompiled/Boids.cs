using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class Boids : MonoBehaviour
{
	[BurstCompile]
	private struct BoidMovementJob : IJobFor
	{
		public NativeArray<Boid> _boids;

		[ReadOnly]
		public NativeArray<Boid> _boidsAlt;

		[ReadOnly]
		public float deltaTime;

		[ReadOnly]
		public int playerBoid;

		public void Execute(int index)
		{
			Boid value = _boids[index];
			if (!value.active)
			{
				return;
			}
			float num = value.turningSpeed;
			if (index == playerBoid)
			{
				num *= 1.5f;
			}
			float num2 = 0f;
			value.rateOfFire -= deltaTime;
			float3 @float;
			if (value.targetID == -1 || !_boidsAlt[value.targetID].active)
			{
				value.targetID = -1;
				value.targetAge = 0;
				@float = math.normalize(value.destination - value.position);
			}
			else
			{
				@float = math.normalize(_boidsAlt[value.targetID].position - value.position);
				num2 = math.dot(@float, math.normalize(value.velocity));
				@float *= 2f;
			}
			float3 float2 = 0;
			float3 float3 = 0;
			float3 float4 = 0;
			int num3 = 0;
			int num4 = 0;
			for (int i = 0; i < _boidsAlt.Length; i++)
			{
				if (value.id == _boidsAlt[i].id || !_boidsAlt[i].active)
				{
					continue;
				}
				float3 float5 = _boidsAlt[i].position - value.position;
				float num5 = math.length(float5);
				if (value.team == _boidsAlt[i].team)
				{
					if (num5 < 100f)
					{
						float2 += float5 * 0.1f;
						num4++;
						float4 += _boidsAlt[i].velocity;
					}
				}
				else if (num5 < 350f)
				{
					if ((value.targetID == -1 || (float)value.targetAge >= 4200f) && i != playerBoid)
					{
						value.targetID = i;
						value.targetAge = 0;
					}
					else if (i == value.targetID)
					{
						Boid value2 = _boids[i];
						if (!value2.active)
						{
							value.targetID = -1;
							value.targetAge = 0;
							continue;
						}
						num *= 2f;
						if (num2 > 0.85f && value.rateOfFire <= 0f)
						{
							value2.health -= value.damage;
							value.shooting = true;
							value.rateOfFire = 0.8f;
						}
						if (value2.health <= 0f)
						{
							value2.active = false;
						}
						_boids[i] = value2;
					}
				}
				if (num5 <= 50f && num5 > 0f)
				{
					float3 -= float5 / (num5 * 0.1f);
					num3++;
				}
			}
			if (num4 > 0)
			{
				float2 /= (float)num4;
				float4 /= (float)num4;
			}
			if (num3 > 0)
			{
				float3 /= (float)num3;
			}
			float3 end = math.normalizesafe(float3.zero + float2 * 0.01f + float3 * 0.5f + float4 * 0.1f + @float + value.velocity + 1.1920929E-07f);
			end = RotateTowards(value.velocity, end, math.radians(100f * deltaTime), 0f);
			value.velocity = math.lerp(value.velocity, math.lerp(value.smoothVel, end, 0.5f), 0.5f);
			value.position += value.velocity * value.speed * deltaTime;
			value.smoothVel = math.lerp(value.smoothVel, end, 0.1f);
			value.targetAge++;
			_boids[index] = value;
		}

		private float3 RotateTowards(float3 start, float3 end, float maxAngle, float maxMagnitude)
		{
			float start2 = math.length(start);
			float end2 = math.length(end);
			float num = math.dot(start, end);
			if (num > 0.9999999f)
			{
				return end;
			}
			float x = math.acos(num);
			return math.normalize(math.mul(float3x3.AxisAngle(math.normalize(math.cross(start, end)), math.min(x, maxAngle)), start) * ClampedMove(start2, end2, maxMagnitude));
		}

		private static float ClampedMove(float start, float end, float clampedDelta)
		{
			float num = end - start;
			if (num > 0f)
			{
				return start + math.min(num, clampedDelta);
			}
			return start - math.min(0f - num, clampedDelta);
		}

		private static float3 Slerp(float3 start, float3 end, float percent)
		{
			float valueToClamp = math.dot(start, end);
			valueToClamp = math.clamp(valueToClamp, -1f, 1f);
			float x = math.acos(valueToClamp) * percent;
			float3 @float = math.normalizesafe(end - start * valueToClamp);
			return start * math.cos(x) + @float * math.sin(x);
		}
	}

	[BurstCompile]
	private struct BoidCopyJob : IJobParallelFor
	{
		public NativeArray<ParticleSystem.Particle> _particles;

		[ReadOnly]
		public NativeArray<Boid> _boids;

		[ReadOnly]
		public int playerBoid;

		public void Execute(int index)
		{
			ParticleSystem.Particle value = _particles[index];
			Boid boid = _boids[index];
			if (boid.active)
			{
				value.velocity = math.normalizesafe(boid.position - (float3)value.position);
				value.position = boid.position;
				value.remainingLifetime = math.clamp(value.remainingLifetime + 1f, 0.1f, 10f);
				value.rotation = 0f;
				value.startSize3D = ((index != playerBoid) ? Vector3.one : Vector3.zero);
				value.startSize = math.clamp(value.startSize + 0.1f, 0f, 1f);
			}
			else
			{
				value.position = Vector3.zero;
				value.remainingLifetime = -10f;
				value.rotation += 0.1f;
				value.startSize3D = Vector3.zero;
				value.startSize = 0f;
			}
			_particles[index] = value;
		}
	}

	public struct Boid
	{
		public float3 position;

		public float3 velocity;

		public float3 smoothVel;

		public float3 destination;

		public bool active;

		public bool shooting;

		public float health;

		public float damage;

		public float rateOfFire;

		public float speed;

		public float turningSpeed;

		public int targetID;

		public int targetAge;

		public int team;

		public int id;

		public void Init(int team, int id)
		{
			position = float3.zero;
			velocity = float3.zero;
			smoothVel = float3.zero;
			destination = float3.zero;
			active = false;
			shooting = false;
			health = 100f;
			damage = 22f;
			rateOfFire = 0.5f;
			speed = 10f;
			turningSpeed = 0.5f;
			targetID = -1;
			targetAge = 1;
			this.team = team;
			this.id = id;
		}

		public void ResetBoid()
		{
			Init(team, id);
		}

		public void Spawn(Vector3 position, Vector3 direction)
		{
			this.position = position + UnityEngine.Random.insideUnitSphere;
			velocity = direction * speed;
			active = true;
		}
	}

	[Serializable]
	public class Team
	{
		public Color shipColor;

		public Color laserColor;

		public Transform[] spawnPoints;

		public Ship ship;
	}

	[Serializable]
	public class Ship
	{
		public Mesh mesh;
	}

	private static Boids _instance;

	[Header("System Settings")]
	public int boidsPerTeam = 256;

	private int maxBoids;

	public ParticleSystem system;

	private ParticleSystemRenderer systemRenderer;

	public ParticleSystem explosionSystem;

	public ParticleSystem.EmitParams explosionSystem_ep;

	public ParticleSystem laserSystem;

	public ParticleSystem.EmitParams laserSystem_ep;

	public float spawnRate = 2f;

	public Team[] teams;

	[Header("Boid Settings")]
	public float boidSpeed = 10f;

	public float boidSpeedVariation = 2f;

	public float boidTurnSpeed = 0.5f;

	public float boidTurnSpeedVariation = 0.1f;

	public Transform randomBoid;

	private Boid nullBoid;

	private int boidIndex = -1;

	private int boidSpawnIndex;

	private NativeArray<Boid> _boids;

	[NonSerialized]
	public NativeArray<Boid> _boidsAlt;

	private NativeArray<ParticleSystem.Particle> _particles;

	private JobHandle _boidMovementHandle;

	private JobHandle _boidCopyHandle;

	private float[] DamageList;

	private bool startFrame = true;

	private bool resetNextFrame;

	private float spawnTicker = 1f;

	public static Boids Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = UnityEngine.Object.FindFirstObjectByType<Boids>();
			}
			return _instance;
		}
	}

	private void OnEnable()
	{
		nullBoid = new Boid
		{
			active = false,
			id = -1
		};
		maxBoids = boidsPerTeam * teams.Length;
		system.AllocateMeshIndexAttribute();
		ParticleSystem.MainModule main = system.main;
		main.maxParticles = maxBoids;
		_particles = new NativeArray<ParticleSystem.Particle>(maxBoids, Allocator.Persistent);
		system.GetParticles(_particles);
		_boids = new NativeArray<Boid>(maxBoids, Allocator.Persistent);
		DamageList = new float[_boids.Length];
		systemRenderer = system.GetComponent<ParticleSystemRenderer>();
		Mesh[] array = new Mesh[teams.Length];
		for (int i = 0; i < teams.Length; i++)
		{
			array[i] = teams[i].ship.mesh;
			for (int j = 0; j < boidsPerTeam; j++)
			{
				int num = i * boidsPerTeam + j;
				Boid value = _boids[num];
				value.Init(num % teams.Length, num);
				value.ResetBoid();
				_boids[num] = value;
				ParticleSystem.Particle value2 = _particles[num];
				value2.SetMeshIndex(value.team);
				value2.startLifetime = 6f;
				value2.startColor = teams[value.team].laserColor;
				_particles[num] = value2;
			}
		}
		systemRenderer.SetMeshes(array);
		system.SetParticles(_particles);
		_boidsAlt = new NativeArray<Boid>(maxBoids, Allocator.Persistent);
	}

	private void OnDisable()
	{
		_boidCopyHandle.Complete();
		if (_boids.IsCreated)
		{
			_boids.Dispose();
		}
		if (_particles.IsCreated)
		{
			_particles.Dispose();
		}
		if (_boidsAlt.IsCreated)
		{
			_boidsAlt.Dispose();
		}
	}

	private void Update()
	{
		SpawnTicker();
		if (startFrame)
		{
			return;
		}
		_boidCopyHandle.Complete();
		if (resetNextFrame)
		{
			for (int i = 0; i < _boids.Length; i++)
			{
				Boid value = _boids[i];
				value.ResetBoid();
				_boids[i] = value;
			}
			boidSpawnIndex = 0;
			for (int j = 0; j < DamageList.Length; j++)
			{
				DamageList[j] = 0f;
			}
			resetNextFrame = false;
		}
		SpawnNewShips();
		SpawnExplosions();
		SpawnLasers();
		if ((bool)randomBoid)
		{
			if (boidIndex >= 0 && _boids[boidIndex].active && _boids[boidIndex].team == 0)
			{
				randomBoid.forward = _boids[boidIndex].velocity;
				randomBoid.position = _boids[boidIndex].position;
			}
			else
			{
				boidIndex = UnityEngine.Random.Range(0, _boids.Length);
			}
		}
		system.SetParticles(_particles);
		_boidsAlt.CopyFrom(_boids);
	}

	private void LateUpdate()
	{
		BoidMovementJob boidMovementJob = default(BoidMovementJob);
		boidMovementJob._boids = _boids;
		boidMovementJob._boidsAlt = _boidsAlt;
		boidMovementJob.deltaTime = Time.deltaTime;
		boidMovementJob.playerBoid = boidIndex;
		BoidMovementJob jobData = boidMovementJob;
		_boidMovementHandle = jobData.Schedule(_boids.Length, default(JobHandle));
		BoidCopyJob boidCopyJob = default(BoidCopyJob);
		boidCopyJob._particles = _particles;
		boidCopyJob._boids = _boids;
		boidCopyJob.playerBoid = boidIndex;
		BoidCopyJob jobData2 = boidCopyJob;
		_boidCopyHandle = IJobParallelForExtensions.Schedule(jobData2, _boids.Length, 32, _boidMovementHandle);
		JobHandle.ScheduleBatchedJobs();
		if (startFrame)
		{
			startFrame = false;
		}
	}

	public Boid GetBoid(int id)
	{
		foreach (Boid item in _boidsAlt)
		{
			if (item.id == id)
			{
				return item;
			}
		}
		return nullBoid;
	}

	public Boid GetClosestBoid(Vector3 positionWS)
	{
		return GetClosestBoid(positionWS, -1);
	}

	public Boid GetClosestBoid(Vector3 positionWS, int team)
	{
		int num = -1;
		float num2 = float.PositiveInfinity;
		for (int i = 0; i < _boidsAlt.Length; i++)
		{
			Boid boid = _boidsAlt[i];
			if (boid.active && !(boid.health <= 0f) && team != boid.team)
			{
				float num3 = Vector3.Distance(boid.position, positionWS);
				if (num3 < num2)
				{
					num2 = num3;
					num = i;
				}
			}
		}
		if (num >= 0 && num < _boidsAlt.Length)
		{
			return _boidsAlt[num];
		}
		return nullBoid;
	}

	public void ResetBoids()
	{
		resetNextFrame = true;
	}

	public void DamageBoid(int id, float damage)
	{
		if (DamageList.Length > id && id >= 0)
		{
			DamageList[id] += damage;
		}
	}

	private void SpawnLasers()
	{
		for (int i = 0; i < _boids.Length; i++)
		{
			Boid value = _boids[i];
			if (value.shooting && value.targetID != -1 && _boids[value.targetID].active)
			{
				laserSystem_ep.position = value.position;
				float3 x = _boids[value.targetID].position - value.position;
				float num = math.length(x);
				float3 @float = math.normalize(x);
				laserSystem_ep.velocity = @float * 1200f;
				laserSystem_ep.startLifetime = num / 1200f;
				laserSystem_ep.startColor = teams[value.team].laserColor;
				laserSystem.Emit(laserSystem_ep, 1);
				value.shooting = false;
			}
			_boids[i] = value;
		}
	}

	private void SpawnExplosions()
	{
		for (int i = 0; i < _boids.Length; i++)
		{
			Boid value = _boids[i];
			value.health -= DamageList[value.id];
			DamageList[value.id] = 0f;
			if (value.health <= 0f)
			{
				explosionSystem_ep.position = value.position;
				explosionSystem_ep.velocity = value.velocity * 0.1f;
				explosionSystem.Emit(explosionSystem_ep, 1);
				value.health = 100f;
				value.position = teams[value.team].spawnPoints[UnityEngine.Random.Range(0, teams[value.team].spawnPoints.Length)].position;
			}
			_boids[i] = value;
		}
	}

	private void SpawnNewShips()
	{
		if (!(spawnTicker > 0f))
		{
			while (_boids[boidSpawnIndex].active)
			{
				boidSpawnIndex = (int)Mathf.Repeat(boidSpawnIndex + 1, _boids.Length);
			}
			Boid value = _boids[boidSpawnIndex];
			Transform transform = teams[value.team].spawnPoints[UnityEngine.Random.Range(0, teams[value.team].spawnPoints.Length)];
			if (transform.gameObject.activeInHierarchy)
			{
				value.Spawn(transform.position, transform.forward);
				_boids[boidSpawnIndex] = value;
				boidSpawnIndex = 0;
			}
			ResetTicker();
		}
	}

	private void SpawnTicker()
	{
		if (spawnTicker > 0f)
		{
			spawnTicker -= Time.deltaTime;
		}
		else
		{
			ResetTicker();
		}
	}

	private void ResetTicker()
	{
		spawnTicker = Mathf.Max(0.05f, spawnRate);
	}
}
