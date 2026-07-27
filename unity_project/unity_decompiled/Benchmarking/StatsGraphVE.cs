using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Benchmarking;

public class StatsGraphVE : VisualElement
{
	private List<float> _dataset = new List<float>();

	private float _width;

	private Vertex[] _vertices;

	private ushort[] _triangles;

	private int _pointsCounts;

	private bool _isDirty;

	private Color32 k_graphColor = new Color(1f, 1f, 1f, 0.5f);

	public bool isDirty => _isDirty;

	public StatsGraphVE()
	{
		base.generateVisualContent = GenerateGraphMesh;
		AddToClassList("TimingsGraph");
	}

	private void GenerateGraphMesh(MeshGenerationContext mgc)
	{
		if (_width != base.contentRect.width)
		{
			GenerateVertices();
		}
		MeshWriteData meshWriteData = mgc.Allocate(_vertices.Length, _triangles.Length);
		meshWriteData.SetAllVertices(_vertices);
		meshWriteData.SetAllIndices(_triangles);
		_isDirty = false;
	}

	private void GenerateVertices()
	{
		_width = base.contentRect.width;
		_pointsCounts = Mathf.CeilToInt(_width);
		_pointsCounts = Mathf.Max(_pointsCounts, 2);
		if (_vertices != null)
		{
			Array.Resize(ref _vertices, _pointsCounts * 2);
			Array.Resize(ref _triangles, (_pointsCounts - 1) * 6);
		}
		else
		{
			_vertices = new Vertex[_pointsCounts * 2];
			_triangles = new ushort[(_pointsCounts - 1) * 6];
		}
		float num = _width * 1f / (float)(_pointsCounts - 1);
		for (int i = 0; i < _pointsCounts; i++)
		{
			float x = (float)i * num;
			int num2 = i * 2;
			int num3 = i * 6;
			_vertices[num2] = new Vertex
			{
				position = new Vector3(x, 0.5f * base.contentRect.height, Vertex.nearZ),
				tint = k_graphColor
			};
			_vertices[num2 + 1] = new Vertex
			{
				position = new Vector3(x, base.contentRect.height, Vertex.nearZ),
				tint = k_graphColor
			};
			if (i < _pointsCounts - 1)
			{
				_triangles[num3] = (ushort)num2;
				_triangles[num3 + 1] = (ushort)(num2 + 2);
				_triangles[num3 + 2] = (ushort)(num2 + 1);
				_triangles[num3 + 3] = (ushort)(num2 + 1);
				_triangles[num3 + 4] = (ushort)(num2 + 2);
				_triangles[num3 + 5] = (ushort)(num2 + 3);
			}
		}
		SetData();
	}

	public void SetData(List<float> data = null, bool refresh = false)
	{
		if (data != null)
		{
			_dataset.Clear();
			_dataset.AddRange(data);
		}
		if (_dataset == null || _dataset.Count < 2)
		{
			return;
		}
		for (int i = 0; i < _pointsCounts; i++)
		{
			float num = (float)i * 1f / (float)(_pointsCounts - 1);
			int num2 = Mathf.FloorToInt(Mathf.Max(0f, num - 0.5f / (float)_pointsCounts) * (float)_dataset.Count);
			int num3 = Mathf.CeilToInt(Mathf.Min(1f, num - 0.5f / (float)_pointsCounts) * (float)_dataset.Count);
			float num4 = 0f;
			for (int j = num2; j < num3; j++)
			{
				num4 = Mathf.Max(num4, _dataset[j]);
			}
			_vertices[i * 2].position.y = base.contentRect.height * (1f - num4);
		}
		if (refresh)
		{
			MarkDirtyRepaint();
			_isDirty = true;
		}
	}
}
