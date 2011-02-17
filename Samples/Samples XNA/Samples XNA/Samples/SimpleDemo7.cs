﻿using System.Text;
using FarseerPhysics.DebugViews;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FarseerPhysics.SamplesFramework
{
    internal class SimpleDemo7 : PhysicsGameScreen, IDemoScreen
    {
        #region IDemoScreen Members

        public string GetTitle()
        {
            return "Ragdoll";
        }

        public string GetDetails()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("This demo shows how to combine physics objects to create a ragdoll.");
            sb.AppendLine(string.Empty);
            sb.AppendLine("GamePad:");
            sb.AppendLine("  - Rotate ragdoll: left and right triggers");
            sb.AppendLine("  - Move ragdoll: right thumbstick");
            sb.AppendLine("  - Move cursor: left thumbstick");
            sb.AppendLine("  - Grab object (beneath cursor): A button");
            sb.AppendLine("  - Drag grabbed object: left thumbstick");
            sb.AppendLine("  - Exit to menu: Back button");
            sb.AppendLine(string.Empty);
            sb.AppendLine("Keyboard:");
            sb.AppendLine("  - Rotate ragdoll: left and right arrows");
            sb.AppendLine("  - Move ragdoll: A,S,D,W");
            sb.AppendLine("  - Exit to menu: Escape");
            sb.AppendLine(string.Empty);
            sb.AppendLine("Mouse / Touchscreen");
            sb.AppendLine("  - Grab object (beneath cursor): Left click");
            sb.AppendLine("  - Drag grabbed object: move mouse / finger");
            return sb.ToString();
        }

        #endregion

        private Ragdoll _ragdoll;
        private Body[] _obstacles = new Body[4];

        public override void LoadContent()
        {
            base.LoadContent();

            World.Gravity = new Vector2(0f, 20f);

            new Border(World, ScreenManager.GraphicsDevice.Viewport);

            _ragdoll = new Ragdoll(World, Vector2.Zero);
            LoadObstacles();

            SetUserAgent(_ragdoll.Body, 1000f, 400f);
        }

        private void LoadObstacles()
        {
            for (int i = 0; i < 4; i++)
            {
                _obstacles[i] = BodyFactory.CreateRectangle(World, 5f, 1.5f, 1f);
                _obstacles[i].IsStatic = true;
            }

            _obstacles[0].Position = new Vector2(-9f, 5f);
            _obstacles[1].Position = new Vector2(-8f, -7f);
            _obstacles[2].Position = new Vector2(9f, 7f);
            _obstacles[3].Position = new Vector2(7f, -5f);
        }
    }
}