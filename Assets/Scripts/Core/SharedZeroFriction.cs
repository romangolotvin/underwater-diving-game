using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Shared zero-friction PhysicsMaterial for diver capsule + ocean floor.
    /// Prevents MeshCollider default friction from cancelling shore WASD.
    /// </summary>
    public static class SharedZeroFriction
    {
        static PhysicsMaterial cached;

        public static PhysicsMaterial Material
        {
            get
            {
                if (cached == null)
                {
                    cached = new PhysicsMaterial("DiveZeroFriction")
                    {
                        dynamicFriction = 0f,
                        staticFriction = 0f,
                        frictionCombine = PhysicsMaterialCombine.Minimum,
                        bounceCombine = PhysicsMaterialCombine.Minimum,
                        bounciness = 0f
                    };
                }
                return cached;
            }
        }
    }
}
