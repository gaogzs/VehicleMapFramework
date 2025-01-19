﻿using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace VehicleInteriors
{
    public static class JobAcrossMapsUtility
    {
        public static void StartGotoDestMapJob(Pawn pawn, TargetInfo? exitSpot = null, TargetInfo? enterSpot = null)
        {
            var nextJob = pawn.CurJob.Clone();
            var driver = nextJob.GetCachedDriver(pawn);
            curToilIndex(driver) = pawn.jobs.curDriver.CurToilIndex - 1;
            pawn.jobs.curDriver.globalFinishActions.Clear(); //Jobはまだ終わっちゃいねえためFinishActionはさせない。TryDropThingなどをしていることもあるし
            pawn.jobs.StartJob(JobAcrossMapsUtility.GotoDestMapJob(pawn, exitSpot, enterSpot, nextJob), JobCondition.InterruptForced, keepCarryingThingOverride: true);
        }

        private static AccessTools.FieldRef<JobDriver, int> curToilIndex = AccessTools.FieldRefAccess<JobDriver, int>("curToilIndex");

        public static Job GotoDestMapJob(Pawn pawn, TargetInfo? exitSpot = null, TargetInfo? enterSpot = null, Job nextJob = null)
        {
            if ((enterSpot.HasValue && enterSpot.Value.Map != null) || (exitSpot.HasValue && exitSpot.Value.Map != null))
            {
                return JobMaker.MakeJob(VMF_DefOf.VMF_GotoDestMap).SetSpotsAndNextJob(pawn, exitSpot, enterSpot, nextJob: nextJob);
            }
            return nextJob;
        }

        public static void TryTakeGotoDestMapJob(Pawn pawn, TargetInfo? exitSpot = null, TargetInfo? enterSpot = null)
        {
            pawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VMF_DefOf.VMF_GotoAcrossMaps).SetSpotsToJobAcrossMaps(pawn, exitSpot, enterSpot), new JobTag?(JobTag.Misc), false);
        }

        public static Job SetSpotsToJobAcrossMaps(this Job job, Pawn pawn, TargetInfo? exitSpot1 = null, TargetInfo? enterSpot1 = null, TargetInfo? exitSpot2 = null, TargetInfo? enterSpot2 = null)
        {
            var driver = job.GetCachedDriver(pawn) as JobDriverAcrossMaps;
            driver.SetSpots(exitSpot1, enterSpot1, exitSpot2, enterSpot2);
            return job;
        }

        public static Job SetSpotsAndNextJob(this Job job, Pawn pawn, TargetInfo? exitSpot1 = null, TargetInfo? enterSpot1 = null, TargetInfo? exitSpot2 = null, TargetInfo? enterSpot2 = null, Job nextJob = null)
        {
            var driver = job.GetCachedDriver(pawn) as JobDriver_GotoDestMap;
            driver.SetSpots(exitSpot1, enterSpot1, exitSpot2, enterSpot2);
            driver.nextJob = nextJob;
            return job;
        }

        public static bool PawnDeterminingJob(this Pawn pawn)
        {
            return pawn.jobs.DeterminingNextJob || FloatMenuMakerMap.makingFor == pawn || Find.Selector.SingleSelectedObject == pawn;
        }
    }
}
