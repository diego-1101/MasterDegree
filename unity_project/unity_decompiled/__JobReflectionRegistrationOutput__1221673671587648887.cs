using System;
using Unity.Jobs;
using UnityEngine;

[Unity.Jobs.DOTSCompilerGenerated]
internal class __JobReflectionRegistrationOutput__1221673671587648887
{
	public static void CreateJobReflectionData()
	{
		try
		{
			IJobForExtensions.EarlyJobInit<Boids.BoidMovementJob>();
			IJobParallelForExtensions.EarlyJobInit<Boids.BoidCopyJob>();
		}
		catch (Exception ex)
		{
			EarlyInitHelpers.JobReflectionDataCreationFailed(ex);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
	public static void EarlyInit()
	{
		CreateJobReflectionData();
	}
}
