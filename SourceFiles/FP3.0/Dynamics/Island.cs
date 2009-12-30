﻿/*
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

using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;

namespace FarseerPhysics
{
    /// This is an internal class.
    internal class Island
    {
        public void Reset(int bodyCapacity, int contactCapacity, int jointCapacity, ContactManager contactManager)
        {
            _bodyCapacity = bodyCapacity;
            _contactCapacity = contactCapacity;
            _jointCapacity = jointCapacity;

            _bodyCount = 0;
            _contactCount = 0;
            _jointCount = 0;

            _contactManager = contactManager;

            if (_bodies == null || _bodies.Length < bodyCapacity)
            {
                _bodies = new Body[bodyCapacity];
            }

            if (_contacts == null || _contacts.Length < contactCapacity)
            {
                _contacts = new Contact[contactCapacity * 2];
            }

            if (_joints == null || _joints.Length < jointCapacity)
            {
                _joints = new Joint[jointCapacity * 2];
            }
        }

        public void Clear()
        {
            _bodyCount = 0;
            _contactCount = 0;
            _jointCount = 0;
        }

        public void Solve(ref TimeStep step, Vector2 gravity, bool allowSleep)
        {
            // Integrate velocities and apply damping.
            for (int i = 0; i < _bodyCount; ++i)
            {
                Body b = _bodies[i];

                if (b.GetBodyType() != BodyType.Dynamic)
                {
                    continue;
                }

                // Integrate velocities.
                b._linearVelocity += step.dt * (gravity + b._invMass * b._force);
                b._angularVelocity += step.dt * b._invI * b._torque;

                // Apply damping.
                // ODE: dv/dt + c * v = 0
                // Solution: v(t) = v0 * exp(-c * t)
                // Time step: v(t + dt) = v0 * exp(-c * (t + dt)) = v0 * exp(-c * t) * exp(-c * dt) = v * exp(-c * dt)
                // v2 = exp(-c * dt) * v1
                // Taylor expansion:
                // v2 = (1.0f - c * dt) * v1
                b._linearVelocity *= MathUtils.Clamp(1.0f - step.dt * b._linearDamping, 0.0f, 1.0f);
                b._angularVelocity *= MathUtils.Clamp(1.0f - step.dt * b._angularDamping, 0.0f, 1.0f);
            }

            _contactSolver.Reset(_contacts, _contactCount);

            // Initialize velocity constraints.
            _contactSolver.InitVelocityConstraints(ref step);

            for (int i = 0; i < _jointCount; ++i)
            {
                _joints[i].InitVelocityConstraints(ref step);
            }

            // Solve velocity constraints.
            for (int i = 0; i < step.velocityIterations; ++i)
            {
                for (int j = 0; j < _jointCount; ++j)
                {
                    _joints[j].SolveVelocityConstraints(ref step);
                }

                _contactSolver.SolveVelocityConstraints();
            }

            // Post-solve (store impulses for warm starting).
            for (int j = 0; j < _jointCount; ++j)
            {
                _joints[j].FinalizeVelocityConstraints();
            }

            _contactSolver.FinalizeVelocityConstraints();

            // Integrate positions.
            for (int i = 0; i < _bodyCount; ++i)
            {
                Body b = _bodies[i];

                if (b.GetBodyType() == BodyType.Static)
                {
                    continue;
                }

                // Check for large velocities.
                Vector2 translation = step.dt * b._linearVelocity;
                if (Vector2.Dot(translation, translation) > Settings.MaxTranslationSquared)
                {
                    translation.Normalize();
                    b._linearVelocity = (Settings.MaxTranslation * step.inv_dt) * translation;
                }

                float rotation = step.dt * b._angularVelocity;
                if (rotation * rotation > Settings.MaxRotationSquared)
                {
                    if (rotation < 0.0)
                    {
                        b._angularVelocity = -step.inv_dt * Settings.MaxRotation;
                    }
                    else
                    {
                        b._angularVelocity = step.inv_dt * Settings.MaxRotation;
                    }
                }

                // Store positions for continuous collision.
                b._sweep.c0 = b._sweep.c;
                b._sweep.a0 = b._sweep.a;

                // Integrate
                b._sweep.c += step.dt * b._linearVelocity;
                b._sweep.a += step.dt * b._angularVelocity;

                // Compute new transform
                b.SynchronizeTransform();

                // Note: shapes are synchronized later.
            }

            // Iterate over constraints.
            for (int i = 0; i < step.positionIterations; ++i)
            {
                bool contactsOkay = _contactSolver.SolvePositionConstraints(Settings.ContactBaumgarte);

                bool jointsOkay = true;
                for (int j = 0; j < _jointCount; ++j)
                {
                    bool jointOkay = _joints[j].SolvePositionConstraints(Settings.ContactBaumgarte);
                    jointsOkay = jointsOkay && jointOkay;
                }

                if (contactsOkay && jointsOkay)
                {
                    // Exit early if the position errors are small.
                    break;
                }
            }

            Report(_contactSolver._constraints);

            if (allowSleep)
            {
                float minSleepTime = Settings.MaxFloat;

                const float linTolSqr = Settings.LinearSleepTolerance * Settings.LinearSleepTolerance;
                const float angTolSqr = Settings.AngularSleepTolerance * Settings.AngularSleepTolerance;

                for (int i = 0; i < _bodyCount; ++i)
                {
                    Body b = _bodies[i];
                    if (b.GetBodyType() == BodyType.Static)
                    {
                        continue;
                    }

                    if (b._invMass == 0.0f)
                    {
                        continue;
                    }

                    if ((b._flags & BodyFlags.AutoSleep) == 0)
                    {
                        b._sleepTime = 0.0f;
                        minSleepTime = 0.0f;
                    }

                    if ((b._flags & BodyFlags.AutoSleep) == 0 ||
                        b._angularVelocity * b._angularVelocity > angTolSqr ||
                        Vector2.Dot(b._linearVelocity, b._linearVelocity) > linTolSqr)
                    {
                        b._sleepTime = 0.0f;
                        minSleepTime = 0.0f;
                    }
                    else
                    {
                        b._sleepTime += step.dt;
                        minSleepTime = Math.Min(minSleepTime, b._sleepTime);
                    }
                }

                if (minSleepTime >= Settings.TimeToSleep)
                {
                    for (int i = 0; i < _bodyCount; ++i)
                    {
                        Body b = _bodies[i];
                        b.SetAwake(false);
                    }
                }
            }
        }

        public void SolveTOI(ref TimeStep subStep)
        {
            _contactSolver.Reset(_contacts, _contactCount);

            // No warm starting is needed for TOI events because warm
            // starting impulses were applied in the discrete solver.

            // Warm starting for joints is off for now, but we need to
            // call this function to compute Jacobians.
            for (int i = 0; i < _jointCount; ++i)
            {
                _joints[i].InitVelocityConstraints(ref subStep);
            }

            // Solve velocity constraints.
            for (int i = 0; i < subStep.velocityIterations; ++i)
            {
                _contactSolver.SolveVelocityConstraints();
                for (int j = 0; j < _jointCount; ++j)
                {
                    _joints[j].SolveVelocityConstraints(ref subStep);
                }
            }

            // Don't store the TOI contact forces for warm starting
            // because they can be quite large.

            // Integrate positions.
            for (int i = 0; i < _bodyCount; ++i)
            {
                Body b = _bodies[i];

                if (b.GetBodyType() == BodyType.Static)
                {
                    continue;
                }

                // Check for large velocities.
                Vector2 translation = subStep.dt * b._linearVelocity;
                if (Vector2.Dot(translation, translation) > Settings.MaxTranslationSquared)
                {
                    translation.Normalize();
                    b._linearVelocity = (Settings.MaxTranslation * subStep.inv_dt) * translation;
                }

                float rotation = subStep.dt * b._angularVelocity;
                if (rotation * rotation > Settings.MaxRotationSquared)
                {
                    if (rotation < 0.0)
                    {
                        b._angularVelocity = -subStep.inv_dt * Settings.MaxRotation;
                    }
                    else
                    {
                        b._angularVelocity = subStep.inv_dt * Settings.MaxRotation;
                    }
                }

                // Store positions for continuous collision.
                b._sweep.c0 = b._sweep.c;
                b._sweep.a0 = b._sweep.a;

                // Integrate
                b._sweep.c += subStep.dt * b._linearVelocity;
                b._sweep.a += subStep.dt * b._angularVelocity;

                // Compute new transform
                b.SynchronizeTransform();

                // Note: shapes are synchronized later.
            }


            // Solve position constraints.
            const float k_toiBaumgarte = 0.75f;
            for (int i = 0; i < subStep.positionIterations; ++i)
            {
                bool contactsOkay = _contactSolver.SolvePositionConstraints(k_toiBaumgarte);
                bool jointsOkay = true;
                for (int j = 0; j < _jointCount; ++j)
                {
                    bool jointOkay = _joints[j].SolvePositionConstraints(k_toiBaumgarte);
                    jointsOkay = jointsOkay && jointOkay;
                }

                if (contactsOkay && jointsOkay)
                {
                    break;
                }
            }

            Report(_contactSolver._constraints);
        }

        public void Add(Body body)
        {
            Debug.Assert(_bodyCount < _bodyCapacity);
            body._islandIndex = _bodyCount;
            _bodies[_bodyCount++] = body;
        }

        public void Add(Contact contact)
        {
            _contacts[_contactCount++] = contact;
        }

        public void Add(Joint joint)
        {
            Debug.Assert(_jointCount < _jointCapacity);
            _joints[_jointCount++] = joint;
        }

        private void Report(ContactConstraint[] constraints)
        {
            for (int i = 0; i < _contactCount; ++i)
            {
                Contact c = _contacts[i];

                ContactConstraint cc = constraints[i];

                ContactImpulse impulse = new ContactImpulse();
                for (int j = 0; j < cc.pointCount; ++j)
                {
                    impulse.normalImpulses[j] = cc.points[j].normalImpulse;
                    impulse.tangentImpulses[j] = cc.points[j].tangentImpulse;
                }

                if (_contactManager.PostSolve != null)
                    _contactManager.PostSolve(c, ref impulse);
            }
        }

        private ContactSolver _contactSolver = new ContactSolver();
        private ContactManager _contactManager;

        public Body[] _bodies;
        public Contact[] _contacts;
        public Joint[] _joints;

        public int _bodyCount;
        public int _contactCount;
        public int _jointCount;

        private int _bodyCapacity;
        public int _contactCapacity;
        public int _jointCapacity;
    }
}
