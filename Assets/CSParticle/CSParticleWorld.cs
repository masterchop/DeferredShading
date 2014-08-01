﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public struct CSVertexData
{
	public Vector3 position;
	public Vector3 normal;
}

public abstract class ICSParticleWorld
{
	public abstract void OnEnable();
	public abstract void OnDisable();
	public abstract void Start();
	public abstract void Update();
	public abstract void PreGBuffer();
	public abstract void PostGBuffer();
}

public class CSParticleWorldImplCS : ICSParticleWorld
{
	public int kernelProcessColliders;
	public int kernelProcessGBufferCollision;
	public int kernelIntegrate;
	public ComputeBuffer cbSphereColliders;
	public ComputeBuffer cbCapsuleColliders;
	public ComputeBuffer cbBoxColliders;
	public ComputeBuffer cbCubeVertices;

	public override void OnEnable()
	{
	}

	public override void OnDisable()
	{
		cbSphereColliders.Release();
		cbCapsuleColliders.Release();
		cbBoxColliders.Release();
		cbCubeVertices.Release();
	}

	public override void Start()
	{
		CSParticleWorld world = CSParticleWorld.instance;
		kernelProcessColliders = world.csParticle.FindKernel("ProcessColliders");
		kernelProcessGBufferCollision = world.csParticle.FindKernel("ProcessGBufferCollision");
		kernelIntegrate = world.csParticle.FindKernel("Integrate");

		cbCubeVertices = new ComputeBuffer(36, 24);
		{
			const float s = 0.05f;
			const float p = 1.0f;
			const float n = -1.0f;
			const float z = 0.0f;
			Vector3[] positions = new Vector3[24] {
				new Vector3(-s,-s, s), new Vector3( s,-s, s), new Vector3( s, s, s), new Vector3(-s, s, s),
				new Vector3(-s, s,-s), new Vector3( s, s,-s), new Vector3( s, s, s), new Vector3(-s, s, s),
				new Vector3(-s,-s,-s), new Vector3( s,-s,-s), new Vector3( s,-s, s), new Vector3(-s,-s, s),
				new Vector3(-s,-s, s), new Vector3(-s,-s,-s), new Vector3(-s, s,-s), new Vector3(-s, s, s),
				new Vector3( s,-s, s), new Vector3( s,-s,-s), new Vector3( s, s,-s), new Vector3( s, s, s),
				new Vector3(-s,-s,-s), new Vector3( s,-s,-s), new Vector3( s, s,-s), new Vector3(-s, s,-s),
			};
			Vector3[] normals = new Vector3[24] {
				new Vector3(z, z, p), new Vector3(z, z, p), new Vector3(z, z, p), new Vector3(z, z, p),
				new Vector3(z, p, z), new Vector3(z, p, z), new Vector3(z, p, z), new Vector3(z, p, z),
				new Vector3(z, n, z), new Vector3(z, n, z), new Vector3(z, n, z), new Vector3(z, n, z),
				new Vector3(n, z, z), new Vector3(n, z, z), new Vector3(n, z, z), new Vector3(n, z, z),
				new Vector3(p, z, z), new Vector3(p, z, z), new Vector3(p, z, z), new Vector3(p, z, z),
				new Vector3(z, z, n), new Vector3(z, z, n), new Vector3(z, z, n), new Vector3(z, z, n),
			};
			int[] indices = new int[36] {
				0,1,3, 3,1,2,
				5,4,6, 6,4,7,
				8,9,11, 11,9,10,
				13,12,14, 14,12,15,
				16,17,19, 19,17,18,
				21,20,22, 22,20,23,
			};
			CSVertexData[] vertices = new CSVertexData[36];
			for (int i = 0; i < vertices.Length; ++i)
			{
				vertices[i].position = positions[indices[i]];
				vertices[i].normal = normals[indices[i]];
			}
			cbCubeVertices.SetData(vertices);
		}

		// doesn't work on WebPlayer
		//Debug.Log("Marshal.SizeOf(typeof(CSSphereCollider))" + Marshal.SizeOf(typeof(CSSphereCollider)));
		//Debug.Log("Marshal.SizeOf(typeof(CSCapsuleCollider))" + Marshal.SizeOf(typeof(CSCapsuleCollider)));
		//Debug.Log("Marshal.SizeOf(typeof(CSBoxCollider))" + Marshal.SizeOf(typeof(CSBoxCollider)));
		cbSphereColliders = new ComputeBuffer(CSParticleWorld.MAX_SPHERE_COLLIDERS, 44);
		cbCapsuleColliders = new ComputeBuffer(CSParticleWorld.MAX_CAPSULE_COLLIDERS, 56);
		cbBoxColliders = new ComputeBuffer(CSParticleWorld.MAX_BOX_COLLIDERS, 136);

		world.csParticle.SetBuffer(kernelProcessColliders, "sphere_colliders", cbSphereColliders);
		world.csParticle.SetBuffer(kernelProcessColliders, "capsule_colliders", cbCapsuleColliders);
		world.csParticle.SetBuffer(kernelProcessColliders, "box_colliders", cbBoxColliders);
	}

	public override void Update()
	{
		cbSphereColliders.SetData(CSParticleCollider.csSphereColliders.ToArray());
		cbCapsuleColliders.SetData(CSParticleCollider.csCapsuleColliders.ToArray());
		cbBoxColliders.SetData(CSParticleCollider.csBoxColliders.ToArray());
	}

	public override void PreGBuffer()
	{
	}

	public override void PostGBuffer()
	{
	}
}

public class CSParticleWorldImplPS : ICSParticleWorld
{
	RenderTexture rtSphereColliders;
	RenderTexture rtCapsuleColliders;
	RenderTexture rtBoxColliders;
	RenderTexture rtCubeVertices;

	public override void OnEnable()
	{
	}

	public override void OnDisable()
	{
		rtSphereColliders.Release();
		rtCapsuleColliders.Release();
		rtBoxColliders.Release();
		rtCubeVertices.Release();
	}

	public override void Start()
	{
	}

	public override void Update()
	{
	}

	public override void PreGBuffer()
	{
	}

	public override void PostGBuffer()
	{
	}
}




public class CSParticleWorld : MonoBehaviour
{
	public enum Implementation
	{
		ComputeShader,
		PixelShader,
		CPU,
	}
	public const int MAX_SPHERE_COLLIDERS = 256;
	public const int MAX_CAPSULE_COLLIDERS = 256;
	public const int MAX_BOX_COLLIDERS = 256;

	public static CSParticleWorld instance;

	public Implementation implMode;
	public GameObject cam;
	public ComputeShader csParticle;
	public Material matCopyGBuffer;

	public List<CSParticleCollider> prevColliders = new List<CSParticleCollider>();
	public Vector2 rt_size;
	public Matrix4x4 viewproj;

	public RenderTexture[] rtGBufferCopy;
	public RenderBuffer[] rbGBufferCopy;
	public RenderTexture rtNormalBufferCopy { get { return rtGBufferCopy[0]; } }
	public RenderTexture rtPositionBufferCopy { get { return rtGBufferCopy[1]; } }

	public ICSParticleWorld impl;


	void OnEnable()
	{
		instance = this;
		switch (implMode)
		{
			case Implementation.ComputeShader: impl = new CSParticleWorldImplCS(); break;
			case Implementation.PixelShader: impl = new CSParticleWorldImplPS(); break;
		}
		impl.OnEnable();
	}

	void OnDisable()
	{
		impl.OnDisable();
		impl = null;
		if (instance == this)
		{
			instance = null;
		}
	}

	void Start()
	{
		DSRenderer dscam = cam.GetComponent<DSRenderer>();
		dscam.AddCallbackPreGBuffer(() => { DepthPrePass(); }, 800);
		dscam.AddCallbackPostGBuffer(() => { GBufferPass(); }, 1000);
		dscam.AddCallbackTransparent(() => { TransparentPass(); }, 1000);

		impl.Start();
	}

	void Update()
	{
		CSParticleSet.HandleParticleCollisionAll();

		CSParticleCollider.UpdateCSColliders();
		impl.Update();

		prevColliders.Clear();
		prevColliders.AddRange(CSParticleCollider.instances);

		Camera c = cam.GetComponent<Camera>();
		DSRenderer dscam = cam.GetComponent<DSRenderer>();
		Matrix4x4 view = c.worldToCameraMatrix;
		Matrix4x4 proj = c.projectionMatrix;
		proj[2, 0] = proj[2, 0] * 0.5f + proj[3, 0] * 0.5f;
		proj[2, 1] = proj[2, 1] * 0.5f + proj[3, 1] * 0.5f;
		proj[2, 2] = proj[2, 2] * 0.5f + proj[3, 2] * 0.5f;
		proj[2, 3] = proj[2, 3] * 0.5f + proj[3, 3] * 0.5f;
		viewproj = proj * view;
		rt_size = new Vector2(dscam.rtNormalBuffer.width, dscam.rtNormalBuffer.height);


		CSParticleSet.UpdateAll();
	}

	void DepthPrePass()
	{
		CSParticleSet.DepthPrePassAll();
	}

	void GBufferPass()
	{
		Camera c = cam.GetComponent<Camera>();
		DSRenderer dscam = cam.GetComponent<DSRenderer>();
		bool needs_gbuffer_copy = false;
		for (int i = 0; i < CSParticleSet.instances.Count; ++i)
		{
			if (CSParticleSet.instances[i].processGBufferCollision)
			{
				needs_gbuffer_copy = true;
				break;
			}
		}
		if (needs_gbuffer_copy)
		{
			if (rtGBufferCopy == null || rbGBufferCopy == null)
			{
				rtGBufferCopy = new RenderTexture[2];
				rbGBufferCopy = new RenderBuffer[2];
				for (int i = 0; i < rtGBufferCopy.Length; ++i)
				{
					rtGBufferCopy[i] = DSRenderer.CreateRenderTexture((int)c.pixelWidth, (int)c.pixelHeight, 0, RenderTextureFormat.ARGBHalf);
					rbGBufferCopy[i] = rtGBufferCopy[i].colorBuffer;
				}
			}
			Graphics.SetRenderTarget(rbGBufferCopy, rtGBufferCopy[0].depthBuffer);
			matCopyGBuffer.SetTexture("_NormalBuffer", dscam.rtNormalBuffer);
			matCopyGBuffer.SetTexture("_PositionBuffer", dscam.rtPositionBuffer);
			matCopyGBuffer.SetPass(0);
			DSRenderer.DrawFullscreenQuad();
			dscam.SetRenderTargetsGBuffer();
		}

		CSParticleSet.GBufferPassAll();
	}

	void TransparentPass()
	{
		CSParticleSet.TransparentPassAll();
	}
}