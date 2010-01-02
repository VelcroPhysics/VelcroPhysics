/*
* Box2D.XNA port of Box2D:
* Copyright (c) 2009 Brandon Furtwangler, Nathan Furtwangler
*
* Original source Box2D:
* Copyright (c) 2006-2009 Erin Catto http://www.gphysics.com 
* 
* This software is provided 'as-is', without any express or implied 
* warranty.  In no event will the authors be held liable for any damages 
* arising from the use of this software. 
* Permission is granted to anyone to use this software for any purpose, 
* including commercial applications, and to alter it and redistribute it 
* freely, subject to the following restrictions: 
* 1. The origin of this software must not be misrepresented; you must not 
* claim that you wrote the original software. If you use this software 
* in a product, an acknowledgment in the product documentation would be 
* appreciated but is not required. 
* 2. Altered source versions must be plainly marked as such, and must not be 
* misrepresented as being the original software. 
* 3. This notice may not be removed or altered from any source distribution. 
*/

using FarseerPhysics.TestBed.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FarseerPhysics.TestBed.Tests
{
    public class RevoluteTest : Test
    {
        private RevoluteTest()
        {
            Body ground;
            {
                
                ground = _world.CreateBody();

                PolygonShape shape = new PolygonShape(0);
                shape.SetAsEdge(new Vector2(-40.0f, 0.0f), new Vector2(40.0f, 0.0f));
                ground.CreateFixture(shape);
            }

            {
                CircleShape shape = new CircleShape(0.5f, 5);

                Body body = _world.CreateBody();
                body.Position = new Vector2(0.0f, 20.0f);
                body.BodyType = BodyType.Dynamic;

                body.CreateFixture(shape);

                const float w = 100.0f;
                body.AngularVelocity = w;
                body.LinearVelocity = new Vector2(-8.0f * w, 0.0f);

                RevoluteJointDef rjd = new RevoluteJointDef();
                rjd.Initialize(ground, body, new Vector2(0.0f, 12.0f));
                rjd.MotorSpeed = 1.0f * Settings.Pi;
                rjd.MaxMotorTorque = 10000.0f;
                rjd.EnableMotor = false;
                rjd.LowerAngle = -0.25f * Settings.Pi;
                rjd.UpperAngle = 0.5f * Settings.Pi;
                rjd.EnableLimit = true;
                rjd.CollideConnected = true;

                _joint = (RevoluteJoint)_world.CreateJoint(rjd);
            }
        }

        public override void Keyboard(KeyboardState state, KeyboardState oldState)
        {
            if (state.IsKeyDown(Keys.L) && oldState.IsKeyUp(Keys.L))
            {
                _joint.EnableLimit(_joint.IsLimitEnabled());
            }

            if (state.IsKeyDown(Keys.S) && oldState.IsKeyUp(Keys.S))
            {
                _joint.EnableMotor(false);
            }
        }

        public override void Step(Framework.Settings settings)
        {
            base.Step(settings);
            _debugView.DrawString(50, _textLine, "Keys: (l) limits, (a) left, (s) off, (d) right");
            _textLine += 15;
            //float torque1 = _joint1.GetMotorTorque();
            //_debugDraw.DrawString(50, _textLine, "Motor Torque = %4.0f, %4.0f : Motor Force = %4.0f", (float) torque1, (float) torque2, (float) force3);
            //_textLine += 15;
        }

        internal static Test Create()
        {
            return new RevoluteTest();
        }

        private RevoluteJoint _joint;
    }
}