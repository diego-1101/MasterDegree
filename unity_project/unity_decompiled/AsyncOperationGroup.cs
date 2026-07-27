using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public readonly struct AsyncOperationGroup
{
	public readonly List<AsyncOperation> operations;

	public float progress
	{
		get
		{
			if (operations.Count != 0)
			{
				return operations.Average((AsyncOperation o) => o.progress);
			}
			return 0f;
		}
	}

	public bool isDone => operations.All((AsyncOperation o) => o.isDone);

	public AsyncOperationGroup(int initialCapacity)
	{
		operations = new List<AsyncOperation>(initialCapacity);
	}
}
