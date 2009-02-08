﻿using System.Collections.Generic;
using System.Text;
using FarseerGames.AdvancedSamples.ScreenSystem;
using FarseerGames.FarseerPhysics;
using FarseerGames.FarseerPhysics.Collisions;
using FarseerGames.FarseerPhysics.Dynamics;
using FarseerGames.FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FarseerGames.AdvancedSamples.Demos.Demo8
{
    public class Demo8Screen : GameScreen
    {
        private Geom _leftGeom;
        private List<TextMessage> _messages;
        private Geom _rightGeom;
        private Geom _selectedGeom;

        public override void Initialize()
        {
            PhysicsSimulator = new PhysicsSimulator(new Vector2(0, 100));
            PhysicsSimulatorView = new PhysicsSimulatorView(PhysicsSimulator);
            PhysicsSimulatorView.EnableVerticeView = true;
            PhysicsSimulatorView.EnableEdgeView = true;
            PhysicsSimulatorView.EnableContactView = false;
            PhysicsSimulatorView.EnableAABBView = false;
            PhysicsSimulatorView.EnablePerformancePanelView = false;
            PhysicsSimulatorView.EnableCoordinateAxisView = false;
            PhysicsSimulatorView.EdgeColor = Color.Red;
            PhysicsSimulatorView.EdgeLineThickness = 2;
            PhysicsSimulatorView.VerticeColor = Color.CornflowerBlue;
            PhysicsSimulatorView.VerticeRadius = 5;
            DebugViewEnabled = true;

            _messages = new List<TextMessage>();

            base.Initialize();
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            for (int i = _messages.Count - 1; i >= 0; i--)
            {
                _messages[i].ElapsedTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (_messages[i].ElapsedTime > 5)
                {
                    _messages.Remove(_messages[i]);
                }
            }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public override void Draw(GameTime gameTime)
        {
            ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend);
            for (int i = _messages.Count - 1; i >= 0; i--)
            {
                ScreenManager.SpriteBatch.DrawString(ScreenManager.SpriteFonts.DetailsFont, _messages[i].Text,
                                       new Vector2(10, (_messages.Count - 1 - i) * ScreenManager.SpriteFonts.DetailsFont.LineSpacing), Color.Black);
            }

            ScreenManager.SpriteBatch.DrawString(ScreenManager.SpriteFonts.DetailsFont, "Backspace = Subtract",
                                                 new Vector2(10, ScreenManager.ScreenHeight - 120), Color.Black);
            ScreenManager.SpriteBatch.DrawString(ScreenManager.SpriteFonts.DetailsFont, "Space = Union",
                                                 new Vector2(10, ScreenManager.ScreenHeight - 105), Color.Black);
            ScreenManager.SpriteBatch.DrawString(ScreenManager.SpriteFonts.DetailsFont, "Click to Drag polygons",
                                                 new Vector2(10, ScreenManager.ScreenHeight - 90), Color.Black);
            ScreenManager.SpriteBatch.DrawString(ScreenManager.SpriteFonts.DetailsFont, "Q,W,E = Create Circle",
                                                 new Vector2(10, ScreenManager.ScreenHeight - 75), Color.Black);
            ScreenManager.SpriteBatch.DrawString(ScreenManager.SpriteFonts.DetailsFont, "Create Rectangle",
                                                 new Vector2(10, ScreenManager.ScreenHeight - 60), Color.Black);

            ScreenManager.SpriteBatch.End();

            base.Draw(gameTime);
        }

        public override void HandleInput(InputState input)
        {
            if (FirstRun)
            {
                ScreenManager.AddScreen(new PauseScreen(GetTitle(), GetDetails(), this));
                FirstRun = false;
            }
            if (input.PauseGame)
            {
                ScreenManager.AddScreen(new PauseScreen(GetTitle(), GetDetails(), this));
            }
            else
            {
                HandleMouseInput(input);
            }

            HandKeyboardInput(input);
            base.HandleInput(input);
        }

        private void DoUnion()
        {
            // Get the world coordinates for the left Geometry
            Vertices poly1 = new Vertices(_leftGeom.WorldVertices);

            // Get the world coordinates for the right Geometry
            Vertices poly2 = new Vertices(_rightGeom.WorldVertices);

            // Do the union
            PolyUnionError error;
            Vertices union = Vertices.Union(poly1, poly2, out error);

            // Check for errors.
            switch (error)
            {
                case PolyUnionError.NoIntersections:
                    WriteMessage("ERROR: Polygons do not intersect!");
                    return;
                case PolyUnionError.Poly1InsidePoly2:
                    WriteMessage("Polygon 1 completely inside polygon 2.");
                    return;
                case PolyUnionError.InfiniteLoop:
                    WriteMessage("Infinite Loop detected.");
                    break;
                case PolyUnionError.None:
                    WriteMessage("No errors with union.");
                    break;
            }

            // No errors, set the product of the union.
            SetProduct(union);
        }

        private void DoSubtract()
        {
            // Get the world coordinates for the left Geometry
            Vertices poly1 = new Vertices(_leftGeom.WorldVertices);

            // Get the world coordinates for the right Geometry
            Vertices poly2 = new Vertices(_rightGeom.WorldVertices);

            // Do the subtraction.
            PolyUnionError error;
            Vertices subtract = Vertices.Subtract(poly1, poly2, out error);

            // Check for errors
            switch (error)
            {
                case PolyUnionError.NoIntersections:
                    WriteMessage("ERROR: Polygons do not intersect!");
                    return;

                case PolyUnionError.Poly1InsidePoly2:
                    WriteMessage("Polygon 1 completely inside polygon 2.");
                    return;

                case PolyUnionError.InfiniteLoop:
                    WriteMessage("Infinite Loop detected.");
                    break;

                case PolyUnionError.None:
                    WriteMessage("No errors with subtraction.");
                    break;
            }

            // No errors, set the product of the union.
            SetProduct(subtract);
        }

        private void DoIntersect()
        {
            // Get the world coordinates for the left Geometry
            Vertices poly1 = new Vertices(_leftGeom.WorldVertices);

            // Get the world coordinates for the right Geometry
            Vertices poly2 = new Vertices(_rightGeom.WorldVertices);

            // Do the subtraction.
            PolyUnionError error;
            Vertices intersect = Vertices.Intersect(poly1, poly2, out error);

            // Check for errors
            switch (error)
            {
                case PolyUnionError.NoIntersections:
                    WriteMessage("ERROR: Polygons do not intersect!");
                    return;

                case PolyUnionError.Poly1InsidePoly2:
                    WriteMessage("Polygon 1 completely inside polygon 2.");
                    return;

                case PolyUnionError.InfiniteLoop:
                    WriteMessage("Infinite Loop detected.");
                    break;

                case PolyUnionError.None:
                    WriteMessage("No errors with intersection.");
                    break;
            }

            // No errors, set the product of the union.
            SetProduct(intersect);
        }

        /// <summary>
        /// Removes the two original polygons and creates the new polygon body and geometry.
        /// </summary>
        /// <param name="product">Polygon to set as the product.</param>
        private void SetProduct(Vertices product)
        {
            _rightGeom = null;
            _leftGeom = null;

            PhysicsSimulator.GeomList.Clear();
            PhysicsSimulator.BodyList.Clear();

            Body body = BodyFactory.Instance.CreatePolygonBody(PhysicsSimulator, product, 1);
            body.Position = ScreenManager.ScreenCenter;
            body.IsStatic = true;

            Geom g = GeomFactory.Instance.CreatePolygonGeom(PhysicsSimulator, body, product, ColliderData.DefaultSettings);

            _leftGeom = g;
        }

        private void AddCircle(int radius, int numSides)
        {
            Vertices verts = Vertices.CreateCircle(radius, numSides);
            Body body = BodyFactory.Instance.CreateCircleBody(PhysicsSimulator, radius, 1.0f);
            body.Position = ScreenManager.ScreenCenter;
            body.IsStatic = true;
            Geom geom = GeomFactory.Instance.CreatePolygonGeom(PhysicsSimulator, body, verts, ColliderData.DefaultSettings);

            SetGeom(geom);
        }

        private void AddRectangle(int width, int height)
        {
            Vertices verts = Vertices.CreateRectangle(width, height);
            Body body = BodyFactory.Instance.CreateRectangleBody(PhysicsSimulator, width, height, 1.0f);
            body.Position = ScreenManager.ScreenCenter;
            body.IsStatic = true;
            Geom geom = GeomFactory.Instance.CreatePolygonGeom(PhysicsSimulator, body, verts, ColliderData.DefaultSettings);

            SetGeom(geom);
        }

        private void SetGeom(Geom geom)
        {
            if (_leftGeom == null)
            {
                _leftGeom = geom;
            }
            else if (_rightGeom == null)
            {
                _rightGeom = geom;
            }
        }

        private void WriteMessage(string message)
        {
            _messages.Add(new TextMessage(message));
        }

        private void HandKeyboardInput(InputState input)
        {
            // Add Circles
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Q) && !input.LastKeyboardState.IsKeyDown(Keys.Q))
            {
                if (PhysicsSimulator.GeomList.Count > 1)
                {
                    WriteMessage("Only 2 polygons allowed at a time.");
                }
                else
                {
                    AddCircle(50, 8);
                }
            }

            // Add Circles
            if (input.CurrentKeyboardState.IsKeyDown(Keys.W) && !input.LastKeyboardState.IsKeyDown(Keys.W))
            {
                if (PhysicsSimulator.GeomList.Count > 1)
                {
                    WriteMessage("Only 2 polygons allowed at a time.");
                }
                else
                {
                    AddCircle(50, 16);
                }
            }

            // Add Circles
            if (input.CurrentKeyboardState.IsKeyDown(Keys.E) && !input.LastKeyboardState.IsKeyDown(Keys.E))
            {
                if (PhysicsSimulator.GeomList.Count > 1)
                {
                    WriteMessage("Only 2 polygons allowed at a time.");
                }
                else
                {
                    AddCircle(50, 32);
                }
            }

            // Add Rectangle
            if (input.CurrentKeyboardState.IsKeyDown(Keys.A) && !input.LastKeyboardState.IsKeyDown(Keys.A))
            {
                if (PhysicsSimulator.GeomList.Count > 1)
                {
                    WriteMessage("Only 2 polygons allowed at a time.");
                }
                else
                {
                    AddRectangle(100, 100);
                }
            }

            // Add Rectangle
            if (input.CurrentKeyboardState.IsKeyDown(Keys.S) && !input.LastKeyboardState.IsKeyDown(Keys.S))
            {
                if (PhysicsSimulator.GeomList.Count > 1)
                {
                    WriteMessage("Only 2 polygons allowed at a time.");
                }
                else
                {
                    AddRectangle(100, 50);
                }
            }

            // Add Rectangle
            if (input.CurrentKeyboardState.IsKeyDown(Keys.D) && !input.LastKeyboardState.IsKeyDown(Keys.D))
            {
                if (PhysicsSimulator.GeomList.Count > 1)
                {
                    WriteMessage("Only 2 polygons allowed at a time.");
                }
                else
                {
                    AddRectangle(50, 100);
                }
            }

            // Perform a Union
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Space) && !input.LastKeyboardState.IsKeyDown(Keys.Space))
            {
                if (_leftGeom != null && _rightGeom != null)
                {
                    DoUnion();
                }
            }

            // Perform a Subtraction
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Back) && !input.LastKeyboardState.IsKeyDown(Keys.Back))
            {
                if (_leftGeom != null && _rightGeom != null)
                {
                    DoSubtract();
                }
            }

            // Perform a Subtraction
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Enter) && !input.LastKeyboardState.IsKeyDown(Keys.Enter))
            {
                if (_leftGeom != null && _rightGeom != null)
                {
                    DoIntersect();
                }
            }

            // Simplify
            if (input.CurrentKeyboardState.IsKeyDown(Keys.Tab) && !input.LastKeyboardState.IsKeyDown(Keys.Tab))
            {
                if (_leftGeom != null && _rightGeom == null)
                {
                    Vertices simple = new Vertices(_leftGeom.WorldVertices);
                    simple = Vertices.Simplify(simple);

                    SetProduct(simple);
                }
            }
        }

        private void HandleMouseInput(InputState input)
        {
            if (input.CurrentMouseState.LeftButton == ButtonState.Pressed && input.LastMouseState.LeftButton == ButtonState.Released)
            {
                foreach (Geom g in PhysicsSimulator.GeomList)
                {
                    if (g.Collide(new Vector2(input.CurrentMouseState.X, input.CurrentMouseState.Y)))
                    {
                        _selectedGeom = g;
                        break;
                    }
                }
            }

            if (input.CurrentMouseState.LeftButton == ButtonState.Released && input.LastMouseState.LeftButton == ButtonState.Pressed)
            {
                _selectedGeom = null;
            }

            MouseMove(input.LastMouseState, input.CurrentMouseState);
        }

        private void MouseMove(MouseState oldMouseState, MouseState newMouseState)
        {
            if (_selectedGeom != null)
            {
                _selectedGeom.Body.Position = new Vector2(
                    _selectedGeom.Body.Position.X + (newMouseState.X - oldMouseState.X),
                    _selectedGeom.Body.Position.Y + (newMouseState.Y - oldMouseState.Y));
            }
        }
       
        public static string GetTitle()
        {
            return "Demo8: Polygon subtraction";
        }

        public static string GetDetails()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Shows how you can use the");
            sb.AppendLine("powerful vertices modification");
            sb.AppendLine("methods to subtract and add polygons");
            sb.AppendLine("on the fly.");
            sb.AppendLine(string.Empty);
            sb.AppendLine("Mouse:");
            sb.AppendLine("x");
            sb.AppendLine("x");
            return sb.ToString();
        }
    }
}