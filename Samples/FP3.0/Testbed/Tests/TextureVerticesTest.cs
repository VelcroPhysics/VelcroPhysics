﻿using System;
using FarseerPhysics.Common.Decomposition;
using FarseerPhysics.TestBed.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace FarseerPhysics.TestBed.Tests
{
    public class TextureVerticesTest : Test
    {
        private Body _polygonBody;
        private Texture2D _polygonTexture;
        private Vector2[] _vertices;

        private TextureVerticesTest()
        {
            {
                Body ground = World.CreateBody();

                Vertices edge = PolygonTools.CreateEdge(new Vector2(-40.0f, 0.0f), new Vector2(40.0f, 0.0f));
                PolygonShape shape = new PolygonShape(edge, 0);
                ground.CreateFixture(shape);
            }
        }

        public override void Initialize()
        {
            //load texture that will represent the physics body
            _polygonTexture = GameInstance.Content.Load<Texture2D>("Texture");

            //Create an array to hold the data from the texture
            uint[] data = new uint[_polygonTexture.Width * _polygonTexture.Height];

            //Transfer the texture data to the array
            _polygonTexture.GetData(data);

            List<Vertices> verts = PolygonTools.CreatePolygon(data, _polygonTexture.Width, _polygonTexture.Height, 1.5f, 20, false, true);
            Vector2 scale = new Vector2(0.07f, 0.07f);
            verts[0].Scale(ref scale);

            _vertices = verts[0].ToArray();

            list = BayazitDecomposer.ConvexPartition(verts[0]);

            colors = new Color[list.Count];
            Random random = new Random((int)DateTime.Now.Ticks);

            for (int i = 0; i < list.Count; i++)
            {
                colors[i] = new Color((byte)random.Next(100, 255), (byte)random.Next(100, 255), (byte)random.Next(100, 255));
            }

            _polygonBody = World.CreateBody();
            _polygonBody.BodyType = BodyType.Dynamic;
            _polygonBody.Position = new Vector2(0, 0);

            foreach (Vertices vert in list)
            {
                if (!vert.IsConvex())
                    throw new Exception("eh..");

                PolygonShape shape = new PolygonShape(vert, 1);
                _polygonBody.CreateFixture(shape);
            }

            base.Initialize();
        }

        private List<Vertices> list;
        private Color[] colors;

        public override void Update(Framework.Settings settings)
        {
            for (int i = 0; i < _vertices.Length; i++)
            {
                DebugView.DrawCircle(_vertices[i], 0.07f, Color.White);
            }

            for (int i = 0; i < list.Count; i++)
            {
                Vertices v = list[i];
                Vector2[] vector2s = v.ToArray();

                DebugView.DrawSolidPolygon(ref vector2s, v.Count, colors[i]);
            }


            base.Update(settings);
        }

        public static Test Create()
        {
            return new TextureVerticesTest();
        }
    }
}