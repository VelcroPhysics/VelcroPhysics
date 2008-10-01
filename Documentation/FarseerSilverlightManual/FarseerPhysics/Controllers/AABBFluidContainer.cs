using FarseerGames.FarseerPhysics.Collisions;
using FarseerGames.FarseerPhysics.Interfaces;
#if (XNA)
using Microsoft.Xna.Framework;
#else
using FarseerGames.FarseerPhysics.Mathematics;
#endif

namespace FarseerGames.FarseerPhysics.Controllers
{
    /// <summary>
    /// TODO: Create documentation
    /// </summary>
    public class AABBFluidContainer : IFluidContainer
    {
        private AABB _aabb;

        public AABBFluidContainer()
        {
        }

        public AABBFluidContainer(AABB aabb)
        {
            _aabb = aabb;
        }

        public AABB AABB
        {
            get { return _aabb; }
            set { _aabb = value; }
        }

        #region IFluidContainer Members

        public bool Intersect(AABB aabb)
        {
            return AABB.Intersect(aabb, _aabb);
        }

        public bool Contains(ref Vector2 vector)
        {
            return _aabb.Contains(vector);
        }

        #endregion
    }
}