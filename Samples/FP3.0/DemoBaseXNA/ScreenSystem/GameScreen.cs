using System;
using DemoBaseXNA.DemoShare;
using DemoBaseXNA.DrawingSystem;
using FarseerPhysics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DemoBaseXNA.ScreenSystem
{
    /// <summary>
    /// Enum describes the screen transition state.
    /// </summary>
    public enum ScreenState
    {
        TransitionOn,
        Active,
        TransitionOff,
        Hidden,
    }

    /// <summary>
    /// A screen is a single layer that has update and draw logic, and which
    /// can be combined with other layers to build up a complex menu system.
    /// For instance the main menu, the options menu, the "are you sure you
    /// want to quit" message box, and the main game itself are all implemented
    /// as screens.
    /// </summary>
    public abstract class GameScreen : IDisposable
    {
        private bool _otherScreenHasFocus;
        //Note: This should not really be here. It should be in an engine instead that takes care of physics
        protected bool firstRun = true;
        private Border _border;
        private Body _groundBody;
        private MouseJoint _mouseJoint;
        private Vector2 _mouseWorld;
        private LineRenderHelper _lineRender;

        protected GameScreen()
        {
            ScreenState = ScreenState.TransitionOn;
            TransitionPosition = 1;
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
        }

        public World PhysicsSimulator { get; set; }

        public PhysicsSimulatorView PhysicsSimulatorView { get; set; }

        public bool DebugViewEnabled { get; set; }

        /// <summary>
        /// Normally when one screen is brought up over the top of another,
        /// the first screen will transition off to make room for the new
        /// one. This property indicates whether the screen is only a small
        /// popup, in which case screens underneath it do not need to bother
        /// transitioning off.
        /// </summary>
        public bool IsPopup { get; protected set; }

        /// <summary>
        /// Indicates how long the screen takes to
        /// transition on when it is activated.
        /// </summary>
        public TimeSpan TransitionOnTime { get; protected set; }

        /// <summary>
        /// Indicates how long the screen takes to
        /// transition off when it is deactivated.
        /// </summary>
        public TimeSpan TransitionOffTime { get; protected set; }

        /// <summary>
        /// Gets the current position of the screen transition, ranging
        /// from zero (fully active, no transition) to one (transitioned
        /// fully off to nothing).
        /// </summary>
        public float TransitionPosition { get; protected set; }

        /// <summary>
        /// Gets the current alpha of the screen transition, ranging
        /// from 255 (fully active, no transition) to 0 (transitioned
        /// fully off to nothing).
        /// </summary>
        public byte TransitionAlpha
        {
            get { return (byte)(255 - TransitionPosition * 255); }
        }

        /// <summary>
        /// Gets the current screen transition state.
        /// </summary>
        public ScreenState ScreenState { get; protected set; }

        /// <summary>
        /// There are two possible reasons why a screen might be transitioning
        /// off. It could be temporarily going away to make room for another
        /// screen that is on top of it, or it could be going away for good.
        /// This property indicates whether the screen is exiting for real:
        /// if set, the screen will automatically remove itself as soon as the
        /// transition finishes.
        /// </summary>
        public bool IsExiting { get; protected set; }

        /// <summary>
        /// Checks whether this screen is active and can respond to user input.
        /// </summary>
        public bool IsActive
        {
            get
            {
                return !_otherScreenHasFocus &&
                       (ScreenState == ScreenState.TransitionOn ||
                        ScreenState == ScreenState.Active);
            }
        }

        /// <summary>
        /// Gets the manager that this screen belongs to.
        /// </summary>
        public ScreenManager ScreenManager { get; internal set; }

        #region IDisposable Members

        public virtual void Dispose() { }

        #endregion

        public virtual void Initialize() { }

        /// <summary>
        /// Load graphics content for the screen.
        /// </summary>
        public virtual void LoadContent()
        {
            if (PhysicsSimulator != null)
            {
                PhysicsSimulatorView = new PhysicsSimulatorView(PhysicsSimulator, ScreenManager.Camera);
                PhysicsSimulatorView.LoadContent(ScreenManager.GraphicsDevice, ScreenManager.ContentManager);
                _lineRender = new LineRenderHelper(100, ScreenManager.GraphicsDevice);
                float borderWidth = 2f;

                _border = new Border(50, 40, borderWidth, new Vector2(0, 0));
                _border.Load(ScreenManager.GraphicsDevice, PhysicsSimulator, ScreenManager.QuadRenderEngine);
                _groundBody = PhysicsSimulator.CreateBody();
            }
        }

        /// <summary>
        /// Unload content for the screen.
        /// </summary>
        public virtual void UnloadContent() { }

        /// <summary>
        /// Allows the screen to run logic, such as updating the transition position.
        /// Unlike <see cref="HandleInput"/>, this method is called regardless of whether the screen
        /// is active, hidden, or in the middle of a transition.
        /// </summary>
        public virtual void Update(GameTime gameTime, bool otherScreenHasFocus,
                                   bool coveredByOtherScreen)
        {
            _otherScreenHasFocus = otherScreenHasFocus;

            if (IsExiting)
            {
                // If the screen is going away to die, it should transition off.
                ScreenState = ScreenState.TransitionOff;

                if (!UpdateTransition(gameTime, TransitionOffTime, 1))
                {
                    // When the transition finishes, remove the screen.
                    ScreenManager.RemoveScreen(this);

                    IsExiting = false;
                }
            }
            else if (coveredByOtherScreen)
            {
                // If the screen is covered by another, it should transition off.
                if (UpdateTransition(gameTime, TransitionOffTime, 1))
                {
                    // Still busy transitioning.
                    ScreenState = ScreenState.TransitionOff;
                }
                else
                {
                    // Transition finished!
                    ScreenState = ScreenState.Hidden;
                }
            }
            else
            {
                // Otherwise the screen should transition on and become active.
                if (UpdateTransition(gameTime, TransitionOnTime, -1))
                {
                    // Still busy transitioning.
                    ScreenState = ScreenState.TransitionOn;
                }
                else
                {
                    // Transition finished!
                    ScreenState = ScreenState.Active;
                }
            }

            if (!coveredByOtherScreen && !otherScreenHasFocus)
            {
                if (PhysicsSimulator != null)
                {
                    PhysicsSimulator.Step(1f / 60f, 8, 3);
                    PhysicsSimulator.ClearForces();
                }
            }
        }

        /// <summary>
        /// Helper for updating the screen transition position.
        /// </summary>
        private bool UpdateTransition(GameTime gameTime, TimeSpan time, int direction)
        {
            // How much should we move by?
            float transitionDelta;

            if (time == TimeSpan.Zero)
                transitionDelta = 1;
            else
                transitionDelta = (float)(gameTime.ElapsedGameTime.TotalMilliseconds /
                                          time.TotalMilliseconds);

            // Update the transition position.
            TransitionPosition += transitionDelta * direction;

            // Did we reach the end of the transition?
            if ((TransitionPosition <= 0) || (TransitionPosition >= 1))
            {
                TransitionPosition = MathHelper.Clamp(TransitionPosition, 0, 1);
                return false;
            }
            // Otherwise we are still busy transitioning.
            return true;
        }

        /// <summary>
        /// Allows the screen to handle user input. Unlike Update, this method
        /// is only called when the screen is active, and not when some other
        /// screen has taken the focus.
        /// </summary>
        public virtual void HandleInput(InputState input)
        {
            //Xbox
            if (input.LastGamePadState.Buttons.Y != ButtonState.Pressed && input.CurrentGamePadState.Buttons.Y == ButtonState.Pressed)
            {
                DebugViewEnabled = !DebugViewEnabled;
                //PhysicsSimulator.EnableDiagnostics = DebugViewEnabled;
            }

            //Windows
            if (!input.LastKeyboardState.IsKeyDown(Keys.F1) && input.CurrentKeyboardState.IsKeyDown(Keys.F1))
            {
                DebugViewEnabled = !DebugViewEnabled;
                //PhysicsSimulator.EnableDiagnostics = DebugViewEnabled;
            }

#if !XBOX
            if (PhysicsSimulator != null)
            {
                HandleMouseInput(input);
                PhysicsSimulatorView.HandleInput(input);
            }
            ScreenManager.Camera.HandleInput(input);
#endif
        }

#if !XBOX
        private void HandleMouseInput(InputState input)
        {
            Mouse(input.CurrentMouseState, input.LastMouseState);
        }

        private void Mouse(MouseState state, MouseState oldState)
        {
            Vector3 p = ScreenManager.GraphicsDevice.Viewport.Unproject(new Vector3(state.X, state.Y, 0),
                ScreenManager.Camera.Projection, 
                ScreenManager.Camera.View, Matrix.Identity);

            Vector2 position = new Vector2(p.X, p.Y );
            

            //position = GameInstance.ConvertScreenToWorld(state.X, state.Y);

            if (state.LeftButton == ButtonState.Released && oldState.LeftButton == ButtonState.Pressed)
            {
                MouseUp(position);
            }
            else if (state.LeftButton == ButtonState.Pressed && oldState.LeftButton == ButtonState.Released)
            {
                MouseDown(position);
            }

            MouseMove(position);
        }

        private void MouseDown(Vector2 p)
        {
            _mouseWorld = p;

            if (_mouseJoint != null)
            {
                return;
            }

            // Make a small box.
            AABB aabb;
            Vector2 d = new Vector2(0.001f, 0.001f);
            aabb.LowerBound = p - d;
            aabb.UpperBound = p + d;

            Fixture _fixture = null;

            // Query the world for overlapping shapes.
            PhysicsSimulator.QueryAABB(
                (fixture) =>
                {
                    Body body = fixture.Body;
                    if (body.BodyType == BodyType.Dynamic)
                    {
                        bool inside = fixture.TestPoint(p);
                        if (inside)
                        {
                            _fixture = fixture;

                            // We are done, terminate the query.
                            return false;
                        }
                    }

                    // Continue the query.
                    return true;
                }, ref aabb);

            if (_fixture != null)
            {
                Body body = _fixture.Body;
                _mouseJoint = new MouseJoint(_groundBody, body, p);
                _mouseJoint.MaxForce = 1000.0f * body.Mass;
                PhysicsSimulator.CreateJoint(_mouseJoint);
                body.Awake = true;
            }
        }

        private void MouseUp(Vector2 p)
        {
            if (_mouseJoint != null)
            {
                PhysicsSimulator.DestroyJoint(_mouseJoint);
                _mouseJoint = null;
            }

            //if (_bombSpawning)
            //{
            //    CompleteBombSpawn(p);
            //}
        }

        private void MouseMove(Vector2 p)
        {
            _mouseWorld = p;

            if (_mouseJoint != null)
            {
                _mouseJoint.Target = p;
            }
        }
#endif

        /// <summary>
        /// This is called when the screen should draw itself.
        /// </summary>
        public virtual void Draw(GameTime gameTime)
        {
            if (PhysicsSimulator != null)
            {
                ScreenManager.SpriteBatch.Begin(SpriteBlendMode.AlphaBlend);

                if (_mouseJoint != null)
                {
                    _lineRender.Submit(new Vector3(_mouseJoint.WorldAnchorA, 0), new Vector3(_mouseJoint.WorldAnchorB, 0), Color.Black);
                }

                PhysicsSimulatorView.Draw(ScreenManager.SpriteBatch);

                _lineRender.Render(ScreenManager.GraphicsDevice, ScreenManager.Camera.Projection, ScreenManager.Camera.View);
                _lineRender.Clear();
                _border.Draw();
                ScreenManager.SpriteBatch.End();
            }
        }

        /// <summary>
        /// Tells the screen to go away. Unlike <see cref="ScreenManager"/>.RemoveScreen, which
        /// instantly kills the screen, this method respects the transition timings
        /// and will give the screen a chance to gradually transition off.
        /// </summary>
        public void ExitScreen()
        {
            if (TransitionOffTime == TimeSpan.Zero)
            {
                // If the screen has a zero transition time, remove it immediately.
                ScreenManager.RemoveScreen(this);
            }
            else
            {
                // Otherwise flag that it should transition off and then exit.
                IsExiting = true;
            }
        }
    }
}