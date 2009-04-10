#region MIT License
/*
 * Copyright (c) 2005-2008 by Jonathan Mark Porter. http://physics2d.googlepages.com/
 * Edited (2009) by Ian Qvist to be included in Farseer Physics. http://codeplex.com/FarseerPhysics
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to deal 
 * in the Software without restriction, including without limitation the rights to 
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of 
 * the Software, and to permit persons to whom the Software is furnished to do so, 
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be 
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE 
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FarseerGames.FarseerPhysics.Dynamics;
using FarseerGames.FarseerPhysics.Interfaces;

namespace FarseerGames.FarseerPhysics.Collisions
{
    /// <summary>
    /// Spartial hashing stores all the geometries that can collide in a list.
    /// Using this algorithm, you can quickly tell what objects might collide in a certain area.
    /// </summary>
    public class SpartialHashCollider : IBroadPhaseCollider
    {
        private PhysicsSimulator _physicsSimulator;
        private Dictionary<long, List<Geom>> _hash;
        private Dictionary<long, object> _filter;
        private float _cellSize;
        private float _cellSizeInv;
        public bool AutoAdjustCellSize = true;

        public SpartialHashCollider(PhysicsSimulator physicsSimulator)
            : this(physicsSimulator, 50, 2048)
        {
            _physicsSimulator = physicsSimulator;
        }

        public SpartialHashCollider(PhysicsSimulator physicsSimulator, float cellSize, int hashCapacity)
        {
            _physicsSimulator = physicsSimulator;
            _hash = new Dictionary<long, List<Geom>>(hashCapacity);
            _filter = new Dictionary<long, object>();
            _cellSize = cellSize;
            _cellSizeInv = 1 / cellSize;
        }

        #region IBroadPhaseCollider Members

        /// <summary>
        /// Fires when a broad phase collision occurs
        /// </summary>
        public event BroadPhaseCollisionHandler OnBroadPhaseCollision;

        ///<summary>
        /// Not required by collider
        ///</summary>
        public void ProcessRemovedGeoms()
        {
        }

        ///<summary>
        /// Not required by collider
        ///</summary>
        public void ProcessDisposedGeoms()
        {
        }

        ///<summary>
        /// Not required by collider
        ///</summary>
        public void Add(Geom geom)
        {
        }

        /// <summary>
        /// Updates this instance.
        /// </summary>
        public void Update()
        {
            if (_physicsSimulator.geomList.Count == 0)
            {
                return;
            }

            //Calculate hash map
            FillHash();

            //Iterate the hash map
            RunHash();
        }

        #endregion

        public float CellSize
        {
            get { return _cellSize; }
            set
            {
                _cellSize = value;
                _cellSizeInv = 1 / value;
            }
        }

        private void FillHash()
        {
            //Average used to optimize cell size if AutoAdjustCellSize = true.
            float average = 0;

            for (int i = 0; i < _physicsSimulator.geomList.Count; i++)
            {
                Geom geom = _physicsSimulator.geomList[i];

                //Note: Could do some checking here for geometries that should not be included in the hashmap

                AABB aabb = geom.AABB;

                if (AutoAdjustCellSize)
                    average += Math.Max(aabb.Max.X - aabb.Min.X, aabb.Max.Y - aabb.Min.Y);

                int minX = (int)(aabb.Min.X * _cellSizeInv);
                int maxX = (int)(aabb.Max.X * _cellSizeInv) + 1;
                int minY = (int)(aabb.Min.Y * _cellSizeInv);
                int maxY = (int)(aabb.Max.Y * _cellSizeInv) + 1;

                for (int x = minX; x < maxX; x++)
                {
                    for (int y = minY; y < maxY; y++)
                    {
                        long key = PairID.GetHash(x, y);
                        List<Geom> list;
                        if (!_hash.TryGetValue(key, out list))
                        {
                            list = new List<Geom>();
                            _hash.Add(key, list);
                        }
                        list.Add(geom);
                    }
                }
            }

            if (AutoAdjustCellSize)
            {
                CellSize = 2 * average / (_physicsSimulator.geomList.Count);
            }
        }

        private void RunHash()
        {
            List<long> keysToRemove = new List<long>(_hash.Count);
            foreach (KeyValuePair<long, List<Geom>> pair in _hash)
            {
                // If there are no geometries in the list. Remove it.
                // If there are any geometries in the list, process them.
                List<Geom> list = pair.Value;
                if (list.Count == 0)
                {
                    keysToRemove.Add(pair.Key);
                }
                else
                {
                    for (int i = 0; i < list.Count - 1; i++)
                    {
                        Geom geometryA = list[i];
                        for (int j = i + 1; j < list.Count; j++)
                        {
                            Geom geometryB = list[j];

                            if (!geometryA.body.Enabled || !geometryB.body.Enabled)
                                continue;

                            if ((geometryA.CollisionGroup == geometryB.CollisionGroup) &&
                                geometryA.CollisionGroup != 0 && geometryB.CollisionGroup != 0)
                                continue;

                            if (!geometryA.CollisionEnabled || !geometryB.CollisionEnabled)
                                continue;

                            if (geometryA.body.isStatic && geometryB.body.isStatic)
                                continue;

                            if (geometryA.body == geometryB.body)
                                continue;

                            if (((geometryA.CollisionCategories & geometryB.CollidesWith) == CollisionCategory.None) &
                                ((geometryB.CollisionCategories & geometryA.CollidesWith) == CollisionCategory.None))
                                continue;

                            //Check if there is intersection
                            bool intersection = AABB.Intersect(geometryA.AABB, geometryB.AABB);

                            long key = PairID.GetId(geometryA.Id, geometryB.Id);
                            if (!_filter.ContainsKey(key))
                            {
                                _filter.Add(key, null);

                                //User can cancel collision
                                if (OnBroadPhaseCollision != null)
                                    intersection = OnBroadPhaseCollision(geometryA, geometryB);

                                if (!intersection)
                                    continue;

                                Arbiter arbiter = _physicsSimulator.arbiterPool.Fetch();
                                arbiter.ConstructArbiter(geometryA, geometryB, _physicsSimulator);

                                if (!_physicsSimulator.arbiterList.Contains(arbiter))
                                    _physicsSimulator.arbiterList.Add(arbiter);
                                else
                                    _physicsSimulator.arbiterPool.Insert(arbiter);
                            }
                        }
                    }
                    list.Clear();
                }
            }
            _filter.Clear();

            //Remove all the empty lists from the hash
            for (int index = 0; index < keysToRemove.Count; ++index)
            {
                _hash.Remove(keysToRemove[index]);
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PairID
    {
        public static long GetId(int id1, int id2)
        {
            PairID result;
            result.ID = 0;
            if (id1 > id2)
            {
                result.lowID = id2;
                result.highID = id1;
            }
            else
            {
                result.lowID = id1;
                result.highID = id2;
            }
            return result.ID;
        }
        public static long GetHash(int value1, int value2)
        {
            PairID result;
            result.ID = 0;
            result.lowID = value1;
            result.highID = value2;
            return result.ID;
        }
        public static void GetIds(long id, out int id1, out  int id2)
        {
            PairID result;
            result.lowID = 0;
            result.highID = 0;
            result.ID = id;
            id1 = result.lowID;
            id2 = result.highID;
        }
        [FieldOffset(0)]
        long ID;
        [FieldOffset(0)]
        int lowID;
        [FieldOffset(sizeof(int))]
        int highID;
    }
}