﻿using System;
using SimpleScene;
using SimpleScene.Demos;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace TestBench2
{
	public class TestBench2 : TestBenchBootstrap
	{
		protected Random rand = new Random();

		protected SSScene laserScene = new SSScene ();
		protected SimpleLaserManager laserManager = null;
		//protected SimpleLaserParameters laserParams = null;
		protected WeakReference activeLaser = new WeakReference (null);

		protected SSObjectMesh droneObj1;
		protected Matrix4 laserSourceTxfm;

		protected SSObjectMesh droneObj2;

		public TestBench2 ()
			: base("TestBench2: Lasers")
		{
		}

		static void Main()
		{
			// The 'using' idiom guarantees proper resource cleanup.
			// We request 30 UpdateFrame events per second, and unlimited
			// RenderFrame events (as fast as the computer can handle).
			using (var game = new TestBench2()) {
				game.Run(30.0);
			}
		}

		protected override void setupScene ()
		{
			base.setupScene ();

			var mesh = SSAssetManager.GetInstance<SSMesh_wfOBJ> ("./drone2/", "Drone2.obj");

			// add drones
			droneObj1 = new SSObjectMesh (mesh);
			droneObj1.Pos = new OpenTK.Vector3(-20f, 0f, -15f);
			droneObj1.Orient(Quaternion.FromAxisAngle(Vector3.UnitY, (float)Math.PI/2f));
			droneObj1.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
			droneObj1.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj1.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj1.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj1.Name = "attacker drone";
			//droneObj1.MainColor = Color4.Green;
			droneObj1.renderState.visible = false;
			scene.AddObject (droneObj1);

			droneObj2 = new SSObjectMesh (mesh);
			droneObj2.Pos = new OpenTK.Vector3(20f, 0f, -15f);
			droneObj2.AmbientMatColor = new Color4(0.1f,0.1f,0.1f,0.1f);
			droneObj2.DiffuseMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj2.SpecularMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj2.EmissionMatColor = new Color4(0.3f,0.3f,0.3f,0.3f);
			droneObj2.Name = "target drone";
			droneObj2.MainColor = Color4.Red;
			scene.AddObject (droneObj2);

			// manages laser objects
			laserManager = new SimpleLaserManager(laserScene);

			// tweak the laser start point (by adding an offset in object-local coordinates)
			laserSourceTxfm = Matrix4.CreateTranslation (0f, 1f, 2.75f);

			#if false
			// TODO use manager instead
			SimpleLaser laser = new SimpleLaser (laserParams);
			//laser.start = new Vector3 (-17f, 1f, -15f);
			//laser.end = new Vector3 (19f, 0f, -15f);
			laser.sourceObject = droneObj1;
			laser.destObject = droneObj2;

			lo = new SimpleLaserObject (laser);
			lo.Name = "laser test";
			lo.cameraScene = scene;
			laserScene.AddObject (lo);
			#endif

			#if false
			// debug start location of the laser
			droneObj1.Scale = new Vector3 (0.01f);
			droneObj1.Pos = laser.start;
			#endif

			#if false
			// debug end location of the laser
			droneObj2.Scale = new Vector3 (0.01f);
			droneObj2.Pos = laser.end;
			#endif

			//scene.renderConfig.renderBoundingSpheresLines = true;
		}

		protected void _createLaser()
		{
			var laserParams = new SimpleLaserParameters ();
			//laserParams.numBeams = rand.Next (1, 6);
			if (laserParams.numBeams == 2) {
				// 2's don't look too great
				laserParams.numBeams = 1;
			}
			laserParams.beamStartPlacementScale = 2f * (float)rand.NextDouble ();
			laserParams.beamDestSpread = (float)Math.Pow ((float)rand.NextDouble (), 3f)
				* laserParams.beamStartPlacementScale;

			laserParams.backgroundColor = Color4Helper.RandomDebugColor ();
			laserParams.overlayColor = Color4.White;
			laserParams.interferenceColor = Color4.White;

			var driftScale = (float)rand.NextDouble() * 0.1f;
			laserParams.driftModulationFunc = (t) => driftScale;

			var newLaser = laserManager.addLaser (laserParams, droneObj1, droneObj2);
			newLaser.sourceTxfm = laserSourceTxfm;
			activeLaser.Target = newLaser;
		}

		protected override void renderScenes (
			float fovy, float aspect, float nearPlane, float farPlane, 
			ref Matrix4 mainSceneView, ref Matrix4 mainSceneProj, 
			ref Matrix4 rotationOnlyView, ref Matrix4 screenProj)
		{
			base.renderScenes (
				fovy, aspect, nearPlane, farPlane, 
				ref mainSceneView, ref mainSceneProj, ref rotationOnlyView, ref screenProj);

			laserScene.renderConfig.invCameraViewMatrix = mainSceneView;
			laserScene.renderConfig.projectionMatrix = mainSceneProj;

			GL.Enable (EnableCap.CullFace);
			GL.CullFace (CullFaceMode.Back);
			GL.Enable(EnableCap.DepthTest);
			GL.Disable(EnableCap.DepthClamp);
			GL.DepthFunc(DepthFunction.Less);
			GL.DepthMask (false);

			laserScene.Render ();
		}

		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			base.OnUpdateFrame (e);
			laserScene.Update ((float)e.Time);
		}

		protected void laserKeyDownHandler(object sender, KeyboardKeyEventArgs e)
		{
			if (!base.Focused) return;

			if (e.Key == Key.Q) {
				if (activeLaser.Target == null) {
					_createLaser ();
				}
			}
		}

		protected void laserKeyUpHandler(object sender, KeyboardKeyEventArgs e)
		{
			if (e.Key == Key.Q) {
				if (activeLaser.Target != null) {
					var laser = activeLaser.Target as SimpleLaser;
					laser.release ();
					activeLaser.Target = null;
				}
			}
		}

		protected override void updateTextDisplay ()
		{
			base.updateTextDisplay ();
			textDisplay.Label += "\n\nPress Q to engage a laser";
		}

		protected override void setupInput ()
		{
			base.setupInput ();
			this.KeyUp += laserKeyUpHandler;
			this.KeyDown += laserKeyDownHandler;
		}

		protected override void setupCamera()
		{
			var camera = new SSCameraThirdPerson (droneObj1);
			camera.Pos = Vector3.Zero;
			camera.followDistance = 80.0f;
			scene.ActiveCamera = camera;
			scene.AddObject (camera);
		}
	}
}

