﻿/*
  Box2DX Copyright (c) 2008 Ihar Kalasouski http://code.google.com/p/box2dx
  Box2D original C++ version Copyright (c) 2006-2007 Erin Catto http://www.gphysics.com

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.
*/

using Box2DX.Common;
using Math = Box2DX.Common.Math;

namespace Box2DX.Collision
{
    /// <summary>
    /// A convex polygon. It is assumed that the interior of the polygon is to
    /// the left of each edge.
    /// </summary>
    public class PolygonShape : Shape
    {
        // Local position of the polygon centroid.
        public Vec2 Centroid;
        public Vec2[] Vertices = new Vec2[Settings.MaxPolygonVertices];
        public Vec2[] Normals = new Vec2[Settings.MaxPolygonVertices];
        public int VertexCount;

        /// <summary>
        /// Get the support point in the given world direction.
        /// Use the supplied transform.
        /// </summary>
        public int GetSupport(XForm xf, Vec2 d)
        {
            int bestIndex = 0;
            float bestValue = Vec2.Dot(Vertices[0], d);
            for (int i = 1; i < VertexCount; ++i)
            {
                float value = Vec2.Dot(Vertices[i], d);
                if (value > bestValue)
                {
                    bestIndex = i;
                    bestValue = value;
                }
            }

            return bestIndex;
        }

        public Vec2 GetSupportVertex(Vec2 d)
        {
            int bestIndex = 0;
            float bestValue = Vec2.Dot(Vertices[0], d);
            for (int i = 1; i < VertexCount; ++i)
            {
                float value = Vec2.Dot(Vertices[i], d);
                if (value > bestValue)
                {
                    bestIndex = i;
                    bestValue = value;
                }
            }

            return Vertices[bestIndex];
        }

        /// <summary>
        /// Get the vertex count.
        /// </summary>
        /// <returns></returns>
        public int GetVertexCount()
        {
            return VertexCount;
        }

        /// <summary>
        /// Get a vertex by index.
        /// </summary>
        public Vec2 GetVertex(int index)
        {
            Box2DXDebug.Assert(0 <= index && index < VertexCount);
            return Vertices[index];
        }

        public PolygonShape()
        {
            Type = ShapeType.PolygonShape;
            Radius = Settings.PolygonRadius;
        }

        public void SetAsBox(float hx, float hy)
        {
            VertexCount = 4;
            Vertices[0].Set(-hx, -hy);
            Vertices[1].Set(hx, -hy);
            Vertices[2].Set(hx, hy);
            Vertices[3].Set(-hx, hy);
            Normals[0].Set(0.0f, -1.0f);
            Normals[1].Set(1.0f, 0.0f);
            Normals[2].Set(0.0f, 1.0f);
            Normals[3].Set(-1.0f, 0.0f);
            Centroid.SetZero();
        }

        public void SetAsBox(float hx, float hy, Vec2 center, float angle)
        {
            VertexCount = 4;
            Vertices[0].Set(-hx, -hy);
            Vertices[1].Set(hx, -hy);
            Vertices[2].Set(hx, hy);
            Vertices[3].Set(-hx, hy);
            Normals[0].Set(0.0f, -1.0f);
            Normals[1].Set(1.0f, 0.0f);
            Normals[2].Set(0.0f, 1.0f);
            Normals[3].Set(-1.0f, 0.0f);
            Centroid = center;

            XForm xf = new XForm();
            xf.Position = center;
            xf.R.Set(angle);

            // Transform vertices and normals.
            for (int i = 0; i < VertexCount; ++i)
            {
                Vertices[i] = Math.Mul(xf, Vertices[i]);
                Normals[i] = Math.Mul(xf.R, Normals[i]);
            }
        }

        public void SetAsEdge(Vec2 v1, Vec2 v2)
        {
            VertexCount = 2;
            Vertices[0] = v1;
            Vertices[1] = v2;
            Centroid = 0.5f * (v1 + v2);
            Normals[0] = Vec2.Cross(v2 - v1, 1.0f);
            Normals[0].Normalize();
            Normals[1] = -Normals[0];
        }

        public void Set(Vec2[] vertices, int count)
        {
            Box2DXDebug.Assert(3 <= count && count <= Settings.MaxPolygonVertices);
            VertexCount = count;

            // Copy vertices.
            for (int i = 0; i < VertexCount; ++i)
            {
                Vertices[i] = vertices[i];
            }

            // Compute normals. Ensure the edges have non-zero length.
            for (int i = 0; i < VertexCount; ++i)
            {
                int i1 = i;
                int i2 = i + 1 < VertexCount ? i + 1 : 0;
                Vec2 edge = Vertices[i2] - Vertices[i1];
                Box2DXDebug.Assert(edge.LengthSquared() > Settings.FLT_EPSILON * Settings.FLT_EPSILON);
                Normals[i] = Vec2.Cross(edge, 1.0f);
                Normals[i].Normalize();
            }

#if DEBUG
            // Ensure the polygon is convex and the interior
            // is to the left of each edge.
            for (int i = 0; i < VertexCount; ++i)
            {
                int i1 = i;
                int i2 = i + 1 < VertexCount ? i + 1 : 0;
                Vec2 edge = Vertices[i2] - Vertices[i1];

                for (int j = 0; j < VertexCount; ++j)
                {
                    // Don't check vertices on the current edge.
                    if (j == i1 || j == i2)
                    {
                        continue;
                    }

                    Vec2 r = Vertices[j] - Vertices[i1];

                    // Your polygon is non-convex (it has an indentation) or
                    // has colinear edges.
                    float s = Vec2.Cross(edge, r);
                    Box2DXDebug.Assert(s > 0.0f);
                }
            }
#endif

            // Compute the polygon centroid.
            Centroid = ComputeCentroid(Vertices, VertexCount);
        }

        public override bool TestPoint(XForm xf, Vec2 p)
        {
            Vec2 pLocal = Math.MulT(xf.R, p - xf.Position);

            for (int i = 0; i < VertexCount; ++i)
            {
                float dot = Vec2.Dot(Normals[i], pLocal - Vertices[i]);
                if (dot > 0.0f)
                {
                    return false;
                }
            }

            return true;
        }

        public override SegmentCollide TestSegment(XForm xf, out float lambda, out Vec2 normal, Segment segment, float maxLambda)
        {
            lambda = 0f;
            normal = Vec2.Zero;

            float lower = 0.0f, upper = maxLambda;

            Vec2 p1 = Math.MulT(xf.R, segment.P1 - xf.Position);
            Vec2 p2 = Math.MulT(xf.R, segment.P2 - xf.Position);
            Vec2 d = p2 - p1;
            int index = -1;

            for (int i = 0; i < VertexCount; ++i)
            {
                // p = p1 + a * d
                // dot(normal, p - v) = 0
                // dot(normal, p1 - v) + a * dot(normal, d) = 0
                float numerator = Vec2.Dot(Normals[i], Vertices[i] - p1);
                float denominator = Vec2.Dot(Normals[i], d);

                if (denominator == 0.0f)
                {
                    if (numerator < 0.0f)
                    {
                        return SegmentCollide.MissCollide;
                    }
                }
                else
                {
                    // Note: we want this predicate without division:
                    // lower < numerator / denominator, where denominator < 0
                    // Since denominator < 0, we have to flip the inequality:
                    // lower < numerator / denominator <==> denominator * lower > numerator.
                    if (denominator < 0.0f && numerator < lower * denominator)
                    {
                        // Increase lower.
                        // The segment enters this half-space.
                        lower = numerator / denominator;
                        index = i;
                    }
                    else if (denominator > 0.0f && numerator < upper * denominator)
                    {
                        // Decrease upper.
                        // The segment exits this half-space.
                        upper = numerator / denominator;
                    }
                }

                if (upper < lower)
                {
                    return SegmentCollide.MissCollide;
                }
            }

            Box2DXDebug.Assert(0.0f <= lower && lower <= maxLambda);

            if (index >= 0)
            {
                lambda = lower;
                normal = Math.Mul(xf.R, Normals[index]);
                return SegmentCollide.HitCollide;
            }

            lambda = 0f;
            return SegmentCollide.StartInsideCollide;
        }

        public override void ComputeAABB(out AABB aabb, XForm xf)
        {
            Vec2 lower = Math.Mul(xf, Vertices[0]);
            Vec2 upper = lower;

            for (int i = 1; i < VertexCount; ++i)
            {
                Vec2 v = Math.Mul(xf, Vertices[i]);
                lower = Math.Min(lower, v);
                upper = Math.Max(upper, v);
            }

            Vec2 r = new Vec2(Radius, Radius);
            aabb.LowerBound = lower - r;
            aabb.UpperBound = upper + r;
        }

        public override void ComputeMass(out MassData massData, float density)
        {
            // Polygon mass, centroid, and inertia.
            // Let rho be the polygon density in mass per unit area.
            // Then:
            // mass = rho * int(dA)
            // centroid.x = (1/mass) * rho * int(x * dA)
            // centroid.y = (1/mass) * rho * int(y * dA)
            // I = rho * int((x*x + y*y) * dA)
            //
            // We can compute these integrals by summing all the integrals
            // for each triangle of the polygon. To evaluate the integral
            // for a single triangle, we make a change of variables to
            // the (u,v) coordinates of the triangle:
            // x = x0 + e1x * u + e2x * v
            // y = y0 + e1y * u + e2y * v
            // where 0 <= u && 0 <= v && u + v <= 1.
            //
            // We integrate u from [0,1-v] and then v from [0,1].
            // We also need to use the Jacobian of the transformation:
            // D = cross(e1, e2)
            //
            // Simplification: triangle centroid = (1/3) * (p1 + p2 + p3)
            //
            // The rest of the derivation is handled by computer algebra.

            Box2DXDebug.Assert(VertexCount >= 3);

            Vec2 center = new Vec2();
            center.Set(0.0f, 0.0f);
            float area = 0.0f;
            float I = 0.0f;

            // pRef is the reference point for forming triangles.
            // It's location doesn't change the result (except for rounding error).
            Vec2 pRef = new Vec2(0.0f, 0.0f);

#if O
			// This code would put the reference point inside the polygon.
			for (int i = 0; i < _vertexCount; ++i)
			{
				pRef += _vertices[i];
			}
			pRef *= 1.0f / count;
#endif

            float k_inv3 = 1.0f / 3.0f;

            for (int i = 0; i < VertexCount; ++i)
            {
                // Triangle vertices.
                Vec2 p1 = pRef;
                Vec2 p2 = Vertices[i];
                Vec2 p3 = i + 1 < VertexCount ? Vertices[i + 1] : Vertices[0];

                Vec2 e1 = p2 - p1;
                Vec2 e2 = p3 - p1;

                float D = Vec2.Cross(e1, e2);

                float triangleArea = 0.5f * D;
                area += triangleArea;

                // Area weighted centroid
                center += triangleArea * k_inv3 * (p1 + p2 + p3);

                float px = p1.X, py = p1.Y;
                float ex1 = e1.X, ey1 = e1.Y;
                float ex2 = e2.X, ey2 = e2.Y;

                float intx2 = k_inv3 * (0.25f * (ex1 * ex1 + ex2 * ex1 + ex2 * ex2) + (px * ex1 + px * ex2)) + 0.5f * px * px;
                float inty2 = k_inv3 * (0.25f * (ey1 * ey1 + ey2 * ey1 + ey2 * ey2) + (py * ey1 + py * ey2)) + 0.5f * py * py;

                I += D * (intx2 + inty2);
            }

            // Total mass
            massData.Mass = density * area;

            // Center of mass
            Box2DXDebug.Assert(area > Settings.FLT_EPSILON);
            center *= 1.0f / area;
            massData.Center = center;

            // Inertia tensor relative to the local origin.
            massData.I = density * I;
        }

        public override float ComputeSubmergedArea(Vec2 normal, float offset, XForm xf, out Vec2 c)
        {
            //Transform plane into shape co-ordinates
            Vec2 normalL = Math.MulT(xf.R, normal);
            float offsetL = offset - Vec2.Dot(normal, xf.Position);

            float[] depths = new float[Settings.MaxPolygonVertices];
            int diveCount = 0;
            int intoIndex = -1;
            int outoIndex = -1;

            bool lastSubmerged = false;
            int i;
            for (i = 0; i < VertexCount; i++)
            {
                depths[i] = Vec2.Dot(normalL, Vertices[i]) - offsetL;
                bool isSubmerged = depths[i] < -Settings.FLT_EPSILON;
                if (i > 0)
                {
                    if (isSubmerged)
                    {
                        if (!lastSubmerged)
                        {
                            intoIndex = i - 1;
                            diveCount++;
                        }
                    }
                    else
                    {
                        if (lastSubmerged)
                        {
                            outoIndex = i - 1;
                            diveCount++;
                        }
                    }
                }
                lastSubmerged = isSubmerged;
            }
            switch (diveCount)
            {
                case 0:
                    if (lastSubmerged)
                    {
                        //Completely submerged
                        MassData md;
                        ComputeMass(out md, 1.0f);
                        c = Math.Mul(xf, md.Center);
                        return md.Mass;
                    }
                    else
                    {
                        //Completely dry
                        c = new Vec2();
                        return 0;
                    }
                    break;
                case 1:
                    if (intoIndex == -1)
                    {
                        intoIndex = VertexCount - 1;
                    }
                    else
                    {
                        outoIndex = VertexCount - 1;
                    }
                    break;
            }
            int intoIndex2 = (intoIndex + 1) % VertexCount;
            int outoIndex2 = (outoIndex + 1) % VertexCount;

            float intoLambda = (0 - depths[intoIndex]) / (depths[intoIndex2] - depths[intoIndex]);
            float outoLambda = (0 - depths[outoIndex]) / (depths[outoIndex2] - depths[outoIndex]);

            Vec2 intoVec = new Vec2(Vertices[intoIndex].X * (1 - intoLambda) + Vertices[intoIndex2].X * intoLambda,
                            Vertices[intoIndex].Y * (1 - intoLambda) + Vertices[intoIndex2].Y * intoLambda);
            Vec2 outoVec = new Vec2(Vertices[outoIndex].X * (1 - outoLambda) + Vertices[outoIndex2].X * outoLambda,
                            Vertices[outoIndex].Y * (1 - outoLambda) + Vertices[outoIndex2].Y * outoLambda);

            //Initialize accumulator
            float area = 0;
            Vec2 center = new Vec2(0, 0);
            Vec2 p2 = Vertices[intoIndex2];
            Vec2 p3;

            float k_inv3 = 1.0f / 3.0f;

            //An awkward loop from intoIndex2+1 to outIndex2
            i = intoIndex2;
            while (i != outoIndex2)
            {
                i = (i + 1) % VertexCount;
                if (i == outoIndex2)
                    p3 = outoVec;
                else
                    p3 = Vertices[i];
                //Add the triangle formed by intoVec,p2,p3
                {
                    Vec2 e1 = p2 - intoVec;
                    Vec2 e2 = p3 - intoVec;

                    float D = Vec2.Cross(e1, e2);

                    float triangleArea = 0.5f * D;

                    area += triangleArea;

                    // Area weighted centroid
                    center += triangleArea * k_inv3 * (intoVec + p2 + p3);

                }
                //
                p2 = p3;
            }

            //Normalize and transform centroid
            center *= 1.0f / area;

            c = Math.Mul(xf, center);

            return area;
        }

        public override float ComputeSweepRadius(Vec2 pivot)
        {
            Box2DXDebug.Assert(VertexCount > 0);
            float sr = Vec2.DistanceSquared(Vertices[0], pivot);
            for (int i = 1; i < VertexCount; ++i)
            {
                sr = Math.Max(sr, Vec2.DistanceSquared(Vertices[i], pivot));
            }

            return Math.Sqrt(sr);
        }

        public static Vec2 ComputeCentroid(Vec2[] vs, int count)
        {
            Box2DXDebug.Assert(count >= 3);

            Vec2 c = new Vec2(); c.Set(0.0f, 0.0f);
            float area = 0.0f;

            // pRef is the reference point for forming triangles.
            // It's location doesn't change the result (except for rounding error).
            Vec2 pRef = new Vec2(0.0f, 0.0f);
#if O
			// This code would put the reference point inside the polygon.
			for (int i = 0; i < count; ++i)
			{
				pRef += vs[i];
			}
			pRef *= 1.0f / count;
#endif

            float inv3 = 1.0f / 3.0f;

            for (int i = 0; i < count; ++i)
            {
                // Triangle vertices.
                Vec2 p1 = pRef;
                Vec2 p2 = vs[i];
                Vec2 p3 = i + 1 < count ? vs[i + 1] : vs[0];

                Vec2 e1 = p2 - p1;
                Vec2 e2 = p3 - p1;

                float D = Vec2.Cross(e1, e2);

                float triangleArea = 0.5f * D;
                area += triangleArea;

                // Area weighted centroid
                c += triangleArea * inv3 * (p1 + p2 + p3);
            }

            // Centroid
            Box2DXDebug.Assert(area > Settings.FLT_EPSILON);
            c *= 1.0f / area;
            return c;
        }
    }
}