﻿using System.Text;
using FarseerPhysics.DemoBaseXNA.DemoShare;
using FarseerPhysics.DemoBaseXNA.ScreenSystem;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;

namespace SimpleSamplesXNA.Demo3
{
    internal class Demo3Screen : GameScreen
    {
        private Fixture[] _obstacles = new Fixture[5];

        public override void Initialize()
        {
            World = new World(new Vector2(0, -20));

            base.Initialize();
        }

        public override void LoadContent()
        {
            new Agent(World, new Vector2(5, 10));

            LoadObstacles();

            base.LoadContent();
        }

        private void LoadObstacles()
        {
            for (int i = 0; i < 5; i++)
            {
                _obstacles[i] = FixtureFactory.CreateRectangle(World, 8, 1.5f, 1);
                _obstacles[i].Body.IsStatic = true;

                if (i == 0)
                {
                    _obstacles[i].Restitution = .2f;
                    _obstacles[i].Friction = .2f;
                }
            }

            _obstacles[0].Body.Position = new Vector2(-5, -15);
            _obstacles[1].Body.Position = new Vector2(15, -10);
            _obstacles[2].Body.Position = new Vector2(10, 5);
            _obstacles[3].Body.Position = new Vector2(-10, 15);
            _obstacles[4].Body.Position = new Vector2(-17, 0);
        }

        public string GetTitle()
        {
            return "Demo3: Multiple geometries and static bodies";
        }

        private string GetDetails()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("This demo shows a single body with multiple geometry");
            sb.AppendLine("objects attached.  The yellow circles are offset");
            sb.AppendLine("from the bodies center. The body itself is created");
            sb.AppendLine("using 'CreateRectangleBody' so that it's moment of");
            sb.AppendLine("inertia is that of a rectangle.");
            sb.AppendLine(string.Empty);
            sb.AppendLine("This demo also shows the use of static bodies.");
            sb.AppendLine(string.Empty);
            sb.AppendLine("GamePad:");
            sb.AppendLine("  -Rotate: left and right triggers");
            sb.AppendLine("  -Move: left thumbstick");
            sb.AppendLine(string.Empty);
            sb.AppendLine("Keyboard:");
            sb.AppendLine("  -Rotate: left and right arrows");
            sb.AppendLine("  -Move: A,S,D,W");
            sb.AppendLine(string.Empty);
            sb.AppendLine("Mouse");
            sb.AppendLine("  -Hold down left button and drag");
            return sb.ToString();
        }
    }
}