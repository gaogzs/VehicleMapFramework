﻿using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VehicleInteriors
{
    public static class HaulAIAcrossMapsUtility
    {
        public static bool PawnCanAutomaticallyHaulFast(Pawn p, Thing t, bool forced, out TargetInfo exitSpot, out TargetInfo enterSpot)
        {
            Building building;
            exitSpot = TargetInfo.Invalid;
            enterSpot = TargetInfo.Invalid;
            if (t is UnfinishedThing unfinishedThing && unfinishedThing.BoundBill != null && ((building = unfinishedThing.BoundBill.billStack.billGiver as Building) == null || (building.Spawned && building.OccupiedRect().ExpandedBy(1).Contains(unfinishedThing.Position))))
            {
                return false;
            }
            if (!p.CanReach(t, PathEndMode.ClosestTouch, p.NormalMaxDanger(), false, false, TraverseMode.ByPawn, t.Map, out exitSpot, out enterSpot))
            {
                return false;
            }
            if (!p.CanReserve(t, t.MapHeld, 1, -1, null, forced))
            {
                return false;
            }
            if (!p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return false;
            }
            if (t.def.IsNutritionGivingIngestible && t.def.ingestible.HumanEdible && !t.IsSociallyProper(p, false, true))
            {
                JobFailReason.Is("ReservedForPrisoners".Translate(), null);
                return false;
            }
            if (t.IsBurning())
            {
                JobFailReason.Is("BurningLower".Translate(), null);
                return false;
            }
            return true;
        }

        public static Job HaulToStorageJob(Pawn p, Thing t, TargetInfo exitSpot, TargetInfo enterSpot)
        {
            StoragePriority currentPriority = StoreUtility.CurrentStoragePriorityOf(t);
            if (!StoreAcrossMapsUtility.TryFindBestBetterStorageFor(t, p, t.Map, currentPriority, p.Faction, out IntVec3 storeCell, out IHaulDestination haulDestination, true, out var exitSpot2, out var enterSpot2))
            {
                JobFailReason.Is(HaulAIUtility.NoEmptyPlaceLowerTrans, null);
                return null;
            }
            if (haulDestination is ISlotGroupParent)
            {
                return HaulAIAcrossMapsUtility.HaulToCellStorageJob(p, t, storeCell, false, exitSpot, enterSpot, exitSpot2, enterSpot2);
            }
            if (haulDestination is Thing thing && thing.TryGetInnerInteractableThingOwner() != null)
            {
                return HaulAIAcrossMapsUtility.HaulToContainerJob(p, t, thing, exitSpot, enterSpot, exitSpot2, enterSpot2);
            }
            Log.Error("Don't know how to handle HaulToStorageJob for storage " + haulDestination.ToStringSafe<IHaulDestination>() + ". thing=" + t.ToStringSafe<Thing>());
            return null;
        }

        public static Job HaulToCellStorageJob(Pawn p, Thing t, IntVec3 storeCell, bool fitInStoreCell, TargetInfo exitSpot, TargetInfo enterSpot, TargetInfo exitSpot2, TargetInfo enterSpot2)
        {
            Job job = JobMaker.MakeJob(VIF_DefOf.VIF_HaulToCellAcrossMaps, t, storeCell);
            Map destMap = enterSpot2.IsValid ? enterSpot2.Map : exitSpot2.IsValid ? exitSpot2.Map.BaseMap() : enterSpot.IsValid ? enterSpot.Map : exitSpot.IsValid ? exitSpot.Map.BaseMap() : p.Map;
            ISlotGroup slotGroup = destMap.haulDestinationManager.SlotGroupAt(storeCell);
            ISlotGroup storageGroup = slotGroup.StorageGroup;
            ISlotGroup slotGroup2 = storageGroup ?? slotGroup;
            if (destMap.thingGrid.ThingAt(storeCell, t.def) != null)
            {
                if (fitInStoreCell)
                {
                    job.count = storeCell.GetItemStackSpaceLeftFor(p.Map, t.def);
                }
                else
                {
                    job.count = t.def.stackLimit;
                }
            }
            else
            {
                job.count = 99999;
            }
            int num = 0;
            float statValue = p.GetStatValue(StatDefOf.CarryingCapacity, true, -1);
            List<IntVec3> cellsList = slotGroup2.CellsList;
            for (int i = 0; i < cellsList.Count; i++)
            {
                if (StoreAcrossMapsUtility.IsGoodStoreCell(cellsList[i], destMap, t, p, p.Faction, out _, out var _))
                {
                    num += cellsList[i].GetItemStackSpaceLeftFor(destMap, t.def);
                    if (num >= job.count || (float)num >= statValue)
                    {
                        break;
                    }
                }
            }
            job.count = Mathf.Min(job.count, num);
            job.haulOpportunisticDuplicates = true;
            job.haulMode = HaulMode.ToCellStorage;
            var driver = job.GetCachedDriver(p) as JobDriver_HaulToCellAcrossMaps;
            driver.SetSpots(exitSpot, enterSpot, exitSpot2, enterSpot2);
            return job;
        }

        public static Job HaulToContainerJob(Pawn p, Thing t, Thing container, TargetInfo exitSpot, TargetInfo enterSpot, TargetInfo exitSpot2, TargetInfo enterSpot2)
        {
            ThingOwner thingOwner = container.TryGetInnerInteractableThingOwner();
            if (thingOwner == null)
            {
                Log.Error(container.ToStringSafe<Thing>() + " gave null ThingOwner.");
                return null;
            }
            Job job = JobMaker.MakeJob(VIF_DefOf.VIF_HaulToContainerAcrossMaps, t, container);
            job.count = Mathf.Min(t.stackCount, thingOwner.GetCountCanAccept(t, true));
            job.haulMode = HaulMode.ToContainer;
            var driver = job.GetCachedDriver(p) as JobDriver_HaulToContainerAcrossMaps;
            driver.SetSpots(exitSpot, enterSpot, exitSpot2, enterSpot2);
            return job;
        }
    }
}
