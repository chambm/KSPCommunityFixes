using System;

namespace KSPCommunityFixes.BugFixes
{
    /// <summary>
    /// Make <see cref="ModuleRCS"/> act on the control input of the physics step it fires in.<para/>
    /// Stock reads the vessel's control state (pitch, yaw, roll, translation) in <see cref="ModuleRCS.Update"/>, once per
    /// rendered frame, and fires from it in <see cref="ModuleRCS.FixedUpdate"/>, every physics step. Reaction wheels,
    /// engine gimbals and control surfaces read the same input every physics step, and SAS and autopilots write it every
    /// physics step, so RCS alone answers a command up to a frame old: one whole physics step when there is at most one
    /// per frame, and all the steps of a frame when there are several. Its delay in game time grows with time warp -
    /// 0.24 s with physics warp at 12x (BetterTimeWarp's physics rates), up to about 0.5 s under TimeControl's
    /// hyper-warp at 12x (24 steps a frame) - and an attitude hold that is stable at 1x goes unstable on it: RCS fires
    /// nearly all the time, against itself, and its unbalanced translation raises the orbit by kilometers while the
    /// attitude looks held.<para/>
    /// <see cref="ModuleRCS.Update"/> is now also called at the start of every physics step. It only reads the input, so
    /// the per-frame call Unity still makes changes nothing, and going through the method rather than copying its body
    /// keeps other mods' patches on it working (RP-1's avionics lock zeroes the input around it).
    /// </summary>
    class RCSInputLag : BasePatch
    {
        protected override Version VersionMin => new Version(1, 8, 0);

        protected override void ApplyPatches()
        {
            AddPatch(PatchType.Prefix, typeof(ModuleRCS), nameof(ModuleRCS.FixedUpdate));
        }

        private static void ModuleRCS_FixedUpdate_Prefix(ModuleRCS __instance)
        {
            __instance.Update();
        }
    }
}
