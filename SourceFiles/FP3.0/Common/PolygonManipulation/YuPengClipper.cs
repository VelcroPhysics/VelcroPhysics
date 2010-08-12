﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace FarseerPhysics.Common.PolygonManipulation
{
    public enum PolyClipType
    {
        Intersection, Union, Difference
    }

    public enum PolyClipError
    {
        None, DegeneratedOutput, NonSimpleInput, BrokenResult
    }

    public static class YuPengClipper
    {
        private const float ClipperEpsilon = 1.192092896e-07f;

        /// <summary>Implements "A new algorithm for Boolean operations on general polygons" 
        /// available here: http://liama.ia.ac.cn/wiki/_media/user:dong:dong_cg_05.pdf
        /// Merges two polygons, a subject and a clip with the specified operation. Polygons may not be 
        /// self-intersecting.
        /// 
        /// Warning: May yield incorrect results or even crash if polygons contain colinear points.</summary>
        /// <param name="subject">The subject polygon.</param>
        /// <param name="clip">The clip polygon, which is added, 
        /// substracted or intersected with the subject</param>
        /// <param name="clipType">The operation to be performed. Either
        /// Union, Difference or Intersection.</param>
        /// <returns>A list of closed polygons, which make up the result of the clipping operation.
        /// Outer contours are ordered counter clockwise, holes are ordered clockwise.</returns>
        public static List<Vertices> Execute(Vertices subject, Vertices clip,
                                             PolyClipType clipType, out PolyClipError error)
        {
            error = PolyClipError.None;

            if (!subject.IsSimple() || !clip.IsSimple()) {
                error = PolyClipError.NonSimpleInput;
                Debug.WriteLine("Input polygons must be simple (cannot intersect themselves).");
                return new List<Vertices>();
            }

            // Copy polygons
            Vertices slicedSubject = new Vertices(subject);
            Vertices slicedClip = new Vertices(clip);
            // Calculate the intersection and touch points between
            // subject and clip and add them to both
            CalculateIntersections(subject, clip, out slicedSubject, out slicedClip);

            // Translate polygons into upper right quadrant
            // as the algorithm depends on it
            Vector2 minimum = Vector2.One;
            for (int i = 0; i < subject.Count; ++i) {
                if (subject[i].X < minimum.X) { minimum.X = subject[i].X; }
                if (subject[i].Y < minimum.Y) { minimum.Y = subject[i].Y; }
            }
            for (int i = 0; i < clip.Count; ++i) {
                if (clip[i].X < minimum.X) { minimum.X = clip[i].X; }
                if (clip[i].Y < minimum.Y) { minimum.Y = clip[i].Y; }
            }
            Vector2 translate = Vector2.One - minimum;
            if (translate != Vector2.Zero) {
                slicedSubject.Translate(ref translate);
                slicedClip.Translate(ref translate);
            }

            // Enforce counterclockwise contours
            slicedSubject.ForceCounterClockWise();
            slicedClip.ForceCounterClockWise();

            List<Edge> subjectSimplices;
            List<float> subjectCoeff;
            List<Edge> clipSimplices;
            List<float> clipCoeff;
            // Build simplical chains from the polygons and calculate the
            // the corresponding coefficients
            CalculateSimplicalChain(slicedSubject, out subjectCoeff, out subjectSimplices);
            CalculateSimplicalChain(slicedClip, out clipCoeff, out clipSimplices);

            List<float> subjectCharacter;
            List<float> clipCharacter;
            // Determine the characteristics function for all non-original edges
            // in subject and clip simplical chain
            CalculateEdgeCharacter(subjectCoeff, subjectSimplices, clipCoeff, clipSimplices,
                                   out subjectCharacter, out clipCharacter);

            List<Edge> resultSimplices;
            // Combine the edges contributing to the result, depending on the clipType
            CalculateResultChain(subjectSimplices, subjectCharacter, clipSimplices, clipCharacter, clipType,
                                 out resultSimplices);

            List<Vertices> Result;
            // Convert result chain back to polygon(s)
            error = BuildPolygonsFromChain(resultSimplices, out Result);

            // Reverse the polygon translation from the beginning
            translate *= -1f;
            for (int i = 0; i < Result.Count; ++i) {
                Result[i].Translate(ref translate);
            }
            return Result;
        }

        /// <summary>Calculates all intersections between two polygons.</summary>
        /// <param name="polygon1">The first polygon.</param>
        /// <param name="polygon2">The second polygon.</param>
        /// <param name="slicedPoly1">Returns the first polygon with added intersection points.</param>
        /// <param name="slicedPoly1">Returns the second polygon with added intersection points.</param>
        private static void CalculateIntersections(Vertices polygon1, Vertices polygon2,
                                                   out Vertices slicedPoly1, out Vertices slicedPoly2)
        {
            slicedPoly1 = new Vertices(polygon1);
            slicedPoly2 = new Vertices(polygon2);

            // Iterate through polygon1's edges
            for (int i = 0; i < polygon1.Count; i++) {
                // Get edge vertices
                Vector2 a = polygon1[i];
                Vector2 b = polygon1[polygon1.NextIndex(i)];

                // Get intersections between this edge and polygon2
                for (int j = 0; j < polygon2.Count; j++) {
                    Vector2 c = polygon2[j];
                    Vector2 d = polygon2[polygon2.NextIndex(j)];

                    Vector2 intersectionPoint;
                    // Check if the edges intersect
                    if (LineTools.LineIntersect(a, b, c, d, out intersectionPoint)) {
                        // calculate alpha values for sorting multiple intersections points on a edge
                        float alpha;
                        // Insert intersection point into first polygon
                        alpha = GetAlpha(a, b, intersectionPoint);
                        if (alpha > 0f && alpha < 1f) {
                            int index = slicedPoly1.IndexOf(a) + 1;
                            while (index < slicedPoly1.Count &&
                                   GetAlpha(a, b, slicedPoly1[index]) <= alpha) { ++index; }
                            slicedPoly1.Insert(index, intersectionPoint);
                        }
                        // Insert intersection point into second polygon
                        alpha = GetAlpha(c, d, intersectionPoint);
                        if (alpha > 0f && alpha < 1f) {
                            int index = slicedPoly2.IndexOf(c) + 1;
                            while (index < slicedPoly2.Count &&
                                   GetAlpha(c, d, slicedPoly2[index]) <= alpha) { ++index; }
                            slicedPoly2.Insert(index, intersectionPoint);
                        }
                    }
                }
            }
            // Check for very small edges
            float minDistanceSquared = ClipperEpsilon;
            for (int i = 0; i < slicedPoly1.Count; ++i) {
                int iNext = slicedPoly1.NextIndex(i);
                //If they are closer than the distance remove vertex
                if ((slicedPoly1[iNext] - slicedPoly1[i]).LengthSquared() <= minDistanceSquared) {
                    slicedPoly1.RemoveAt(i);
                    --i;
                }
            }
            for (int i = 0; i < slicedPoly2.Count; ++i) {
                int iNext = slicedPoly2.NextIndex(i);
                //If they are closer than the distance remove vertex
                if ((slicedPoly2[iNext] - slicedPoly2[i]).LengthSquared() <= minDistanceSquared) {
                    slicedPoly2.RemoveAt(i);
                    --i;
                }
            }
        }

        /// <summary>Calculates the simplical chain corresponding to the input polygon.</summary>
        /// <remarks>Used by method <c>Execute()</c>.</remarks>
        private static void CalculateSimplicalChain(Vertices poly, out List<float> coeff,
                                                    out List<Edge> simplicies)
        {
            simplicies = new List<Edge>();
            coeff = new List<float>();
            for (int i = 0; i < poly.Count; ++i) {
                simplicies.Add(new Edge(poly[i], poly[poly.NextIndex(i)]));
                coeff.Add(CalculateSimplexCoefficient(Vector2.Zero, poly[i], poly[poly.NextIndex(i)]));
            }
        }

        /// <summary>Calculates the characteristics function for all edges of
        /// a given simplical chain.</summary>
        /// <remarks>Used by method <c>Execute()</c>.</remarks>
        private static void CalculateEdgeCharacter(List<float> poly1Coeff, List<Edge> poly1Simplicies,
                                                   List<float> poly2Coeff, List<Edge> poly2Simplicies,
                                                   out List<float> poly1Char, out List<float> poly2Char)
        {
            poly1Char = new List<float>();
            poly2Char = new List<float>();
            for (int i = 0; i < poly1Simplicies.Count; ++i) {
                poly1Char.Add(0f);
                if (poly2Simplicies.Contains(poly1Simplicies[i])) {
                    poly1Char[i] = 1f;
                }
                else {
                    for (int j = 0; j < poly2Simplicies.Count; ++j) {
                        if (!poly2Simplicies.Contains(-poly1Simplicies[i])) {
                            poly1Char[i] += CalculateBeta(poly1Simplicies[i].GetCenter(),
                                                          poly2Simplicies[j], poly2Coeff[j]);
                        }
                    }
                }
            }
            for (int i = 0; i < poly2Simplicies.Count; ++i) {
                poly2Char.Add(0f);
                for (int j = 0; j < poly1Simplicies.Count; ++j) {
                    if (!poly1Simplicies.Contains(poly2Simplicies[i]) &&
                        !poly1Simplicies.Contains(-poly2Simplicies[i])) {
                        poly2Char[i] += CalculateBeta(poly2Simplicies[i].GetCenter(),
                                                      poly1Simplicies[j], poly1Coeff[j]);
                    }
                }
            }
        }

        /// <summary>Calculates the result between the subject and clip simplical chains,
        /// based on the provided operation.</summary>
        /// <remarks>Used by method <c>Execute()</c>.</remarks>
        private static void CalculateResultChain(List<Edge> poly1Simplicies, List<float> poly1Char,
                                                 List<Edge> poly2Simplicies, List<float> poly2Char,
                                                 PolyClipType clipType, out List<Edge> resultSimplices)
        {
            resultSimplices = new List<Edge>();

            for (int i = 0; i < poly1Simplicies.Count; ++i) {
                if (clipType == PolyClipType.Intersection) {
                    if (poly1Char[i] == 1f) { resultSimplices.Add(poly1Simplicies[i]); }
                }
                else {
                    if (poly1Char[i] == 0f) { resultSimplices.Add(poly1Simplicies[i]); }
                }
            }
            for (int i = 0; i < poly2Simplicies.Count; ++i) {
                if (clipType == PolyClipType.Intersection || clipType == PolyClipType.Difference) {
                    if (poly2Char[i] == 1f) { resultSimplices.Add(-poly2Simplicies[i]); }
                }
                else {
                    if (poly2Char[i] == 0f) {
                        resultSimplices.Add(poly2Simplicies[i]);
                    }
                }
            }
        }

        /// <summary>Calculates the polygon(s) from the result simplical chain.</summary>
        /// <remarks>Used by method <c>Execute()</c>.</remarks>
        private static PolyClipError BuildPolygonsFromChain(List<Edge> simplicies, out List<Vertices> result)
        {
            result = new List<Vertices>();
            PolyClipError errVal = PolyClipError.None;

            while (simplicies.Count > 0) {
                Vertices output = new Vertices();
                output.Add(simplicies[0].EdgeStart);
                output.Add(simplicies[0].EdgeEnd);
                simplicies.RemoveAt(0);
                bool closed = false;
                int index = 0;
                int count = simplicies.Count; // Needed to catch infinite loops
                while (!closed && simplicies.Count > 0) {
                    if (VectorEqual(output[output.Count - 1], simplicies[index].EdgeStart)) {
                        if (VectorEqual(simplicies[index].EdgeEnd, output[0])) {
                            closed = true;
                        }
                        else {
                            output.Add(simplicies[index].EdgeEnd);
                        }
                        simplicies.RemoveAt(index);
                        --index;
                    }
                    else if (VectorEqual(output[output.Count - 1], simplicies[index].EdgeEnd)) {
                        if (VectorEqual(simplicies[index].EdgeStart, output[0])) {
                            closed = true;
                        }
                        else {
                            output.Add(simplicies[index].EdgeStart);
                        }
                        simplicies.RemoveAt(index);
                        --index;
                    }
                    if (!closed) {
                        if (++index == simplicies.Count) {
                            if (count == simplicies.Count) {
                                result = new List<Vertices>();
                                Debug.WriteLine("Undefined error while building result polygon(s).");
                                return PolyClipError.BrokenResult;
                            }
                            index = 0;
                            count = simplicies.Count;
                        }
                    }
                }
                if(output.Count < 3){
                    errVal = PolyClipError.DegeneratedOutput;
                    Debug.WriteLine("Degenerated output polygon produced (vertices < 3).");
                }
                result.Add(output);
            }
            return errVal;
        }

        /// <summary>Needed to calculate the characteristics function of a simplex.</summary>
        /// <remarks>Used by method <c>CalculateEdgeCharacter()</c>.</remarks>
        private static float CalculateBeta(Vector2 point, Edge e, float coefficient)
        {
            float result = 0f;
            if (PointInSimplex(point, e)) { result = coefficient; }
            if (PoinOnLineSegment(Vector2.Zero, e.EdgeStart, point) ||
                PoinOnLineSegment(Vector2.Zero, e.EdgeEnd, point)) { result = .5f * coefficient; }
            return result;
        }

        /// <summary>Needed for sorting multiple intersections points on the same edge.</summary>
        /// <remarks>Used by method <c>CalculateIntersections()</c>.</remarks>
        private static float GetAlpha(Vector2 start, Vector2 end, Vector2 point)
        {
            return (point - start).LengthSquared() / (end - start).LengthSquared();
        }

        /// <summary>Returns a positive number if c is to the left of the line going from a to b.</summary>
        /// <remarks>Used by method <c>PointInPoly()</c> and <c>SignedAreaSimplex</c>.</remarks>
        /// <returns>Positive number if points arc left, negative if points arc right, 
        /// and 0 if points are collinear.</returns>
        private static float IsLeft(Vector2 a, Vector2 b, Vector2 c)
        {
            //cross product
            return a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y);
        }

        /// <summary>Returns the coefficient of a simplex.</summary>
        /// <remarks>Used by method <c>CalculateSimplicalChain()</c>.</remarks>
        private static float CalculateSimplexCoefficient(Vector2 a, Vector2 b, Vector2 c)
        {
            float isLeft = IsLeft(a, b, c);
            if (isLeft < 0f) { return -1f; }
            else if (isLeft > 0f) { return 1f; }
            else { return 0f; }
        }

        /// <summary>Winding number test for a point in a polygon.</summary>
        /// <param name="point">The point to be tested.</param>
        /// <param name="polygon">The polygon that the point is tested against.</param>
        /// <returns>False if the winding number is even and the point is outside
        /// the polygon and True otherwise.</returns>
        public static bool PointInPolygon(Vector2 point, Vertices polygon)
        {
            // Winding number
            int wn = 0;

            // Iterate through polygon's edges
            for (int i = 0; i < polygon.Count; i++) {
                // Get points
                Vector2 p1 = polygon[i];
                Vector2 p2 = polygon[polygon.NextIndex(i)];
                // Test edge for intersection with ray from point
                if (p1.Y <= point.Y) {
                    if (p2.Y > point.Y && IsLeft(p1, p2, point) > 0f) { ++wn; }
                }
                else {
                    if (p2.Y <= point.Y && IsLeft(p1, p2, point) < 0f) { --wn; }
                }
            }
            return (wn % 2 != 0);
        }

        /// <summary>Winding number test for a point in a simplex.</summary>
        /// <param name="point">The point to be tested.</param>
        /// <param name="polygon">The simplex that the point is tested against.</param>
        /// <returns>False if the winding number is even and the point is outside
        /// the simplex and True otherwise.</returns>
        private static bool PointInSimplex(Vector2 point, Edge e)
        {
            Vertices polygon = new Vertices();
            polygon.Add(Vector2.Zero);
            polygon.Add(e.EdgeStart);
            polygon.Add(e.EdgeEnd);
            return PointInPolygon(point, polygon);
        }

        /// <summary>Tests if a point lies on a line segment.</summary>
        /// <remarks>Used by method <c>CalculateBeta()</c>.</remarks>
        private static bool PoinOnLineSegment(Vector2 start, Vector2 end, Vector2 point)
        {
            Vector2 segment = end - start;
            return IsLeft(start, end, point) == 0f &&
                   Vector2.Dot(point - start, segment) >= 0f &&
                   Vector2.Dot(point - end, segment) <= 0f;
        }

        private static bool VectorEqual(Vector2 vec1, Vector2 vec2)
        {
            return (vec2 - vec1).LengthSquared() <= ClipperEpsilon;
        }

        #region Nested type: Edge
        /// <summary>Specifies an Edge. Edges are used to represent simplicies in simplical chains</summary>
        private class Edge
        {
            public Vector2 EdgeStart { get; private set; }
            public Vector2 EdgeEnd { get; private set; }

            public Edge(Vector2 edgeStart, Vector2 edgeEnd)
            {
                EdgeStart = edgeStart;
                EdgeEnd = edgeEnd;
            }

            public Vector2 GetCenter()
            {
                return (EdgeStart + EdgeEnd) / 2f;
            }

            public static Edge operator -(Edge e)
            {
                return new Edge(e.EdgeEnd, e.EdgeStart);
            }

            public override bool Equals(System.Object obj)
            {
                // If parameter is null return false.
                if (obj == null) { return false; }

                // If parameter cannot be cast to Point return false.
                Edge e = obj as Edge;
                if ((System.Object)e == null) { return false; }

                // Return true if the fields match
                return (EdgeStart == e.EdgeStart) && (EdgeEnd == e.EdgeEnd);
            }

            public bool Equals(Edge e)
            {
                // If parameter is null return false:
                if ((object)e == null) { return false; }

                // Return true if the fields match
                return (EdgeStart == e.EdgeStart) && (EdgeEnd == e.EdgeEnd);
            }

            public override int GetHashCode()
            {
                return EdgeStart.GetHashCode() ^ EdgeEnd.GetHashCode();
            }
        }
        #endregion
    }
}
