using System;
using HarmonyLib;
using UnityEngine;

namespace KSPCommunityFixes.BugFixes
{
    /// <summary>
    /// Make the orientation of Unity's world axes a function of the clock alone.<para/>
    /// KSP keeps two angles per body: in an inertial scene the body turns in a fixed world
    /// (<c>directRotAngle = rotationAngle - Planetarium.InverseRotAngle</c>); in the rotating frame of a flight below
    /// the inverse-rotation altitude the body sits at <c>directRotAngle</c> and the world turns
    /// (<c>Planetarium.InverseRotAngle = rotationAngle - directRotAngle</c>). Each scene freezes one and moves the other,
    /// and neither is in the save (<see cref="CelestialBody.CBUpdate"/>).<para/>
    /// That is consistent while the clock runs on, but a clock jump that lands in an inertial pass - the first frames of
    /// a flight scene loaded after "Revert to VAB/SPH" (the clock goes back over the flight), or after a mod reverted
    /// from its own time - recomputes <c>directRotAngle</c> from an <c>InverseRotAngle</c> the clock no longer agrees with.
    /// The planet's azimuth in world coordinates then shifts by the planet's rotation over the jump, cumulatively with
    /// every such cycle. Anything judged in world axes - the axis-aligned rectangle test in
    /// <see cref="OcclusionData.Update"/> that stock's convection, solar and body occlusion between parts rests on -
    /// changes with what was flown before: the same vessel on the same launch, same clock, gets different part
    /// shading and heating from one launch to the next.<para/>
    /// <see cref="PSystemSetup.OnSceneChange"/> runs on every scene load request and clears the rotating flags; after it
    /// the pair is re-established from the clock, so the incoming scene starts from the state a freshly started game
    /// has. The outgoing scene is unaffected, the pair still summing to <c>rotationAngle</c>.
    /// </summary>
    class WorldFrameDrift : BasePatch
    {
        protected override Version VersionMin => new Version(1, 8, 0);

        protected override void ApplyPatches()
        {
            AddPatch(PatchType.Postfix, typeof(PSystemSetup), nameof(PSystemSetup.OnSceneChange));
        }

        private static void PSystemSetup_OnSceneChange_Postfix(GameScenes scene)
        {
            if (FlightGlobals.Bodies == null || Planetarium.fetch.IsNullOrDestroyed())
                return;

            double ut = Planetarium.GetUniversalTime();
            for (int i = FlightGlobals.Bodies.Count; i-- > 0;)
            {
                CelestialBody body = FlightGlobals.Bodies[i];
                if (body.IsNullOrDestroyed() || !body.rotates || body.rotationPeriod == 0.0)
                    continue;

                body.directRotAngle = (body.initialRotation + 360.0 * ut / body.rotationPeriod) % 360.0;
            }

            Planetarium.InverseRotAngle = 0.0;
        }
    }
}
