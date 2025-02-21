﻿using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace VehicleInteriors.VMF_HarmonyPatches
{
    [HarmonyPatch(typeof(Thing), nameof(Thing.Rotation), MethodType.Getter)]
    public static class Patch_Thing_Rotation
    {
        [HarmonyPatch(MethodType.Setter)]
        public static void Prefix(Thing __instance, ref Rot4 value)
        {
            if (__instance is Pawn pawn && pawn.IsOnNonFocusedVehicleMapOf(out var vehicle))
            {
                if (pawn.pather?.Moving ?? false)
                {
                    var angle = (pawn.pather.nextCell - pawn.Position).AngleFlat;
                    value = Rot8.FromAngle(Ext_Math.RotateAngle(angle, vehicle.FullRotation.AsAngle));
                }
                else if (!pawn.Drafted)
                {
                    value.AsInt += vehicle.Rotation.AsInt;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Building_Door), "StuckOpen", MethodType.Getter)]
    public static class Patch_Building_Door_StuckOpen
    {
        public static void Postfix(Building_Door __instance, ref bool __result)
        {
            __result = __result && !(__instance is Building_VehicleRamp);
        }
    }

    [HarmonyPatch(typeof(Building_Door), "DrawMovers")]
    public static class Patch_Building_Door_DrawMovers
    {
        public static void Prefix(ref float altitude, Building_Door __instance)
        {
            if (__instance.IsOnNonFocusedVehicleMapOf(out var vehicle))
            {
                altitude += vehicle.DrawPos.y;
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var m_MatAt = AccessTools.Method(typeof(Graphic), nameof(Graphic.MatAt));
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Callvirt && c.OperandIs(m_MatAt));
            codes.Insert(pos - 1, CodeInstruction.Call(typeof(VehicleMapUtility), nameof(VehicleMapUtility.RotForVehicleDraw)));
            return codes.MethodReplacer(MethodInfoCache.g_Thing_Rotation, AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.BaseFullRotationDoor)))
                .MethodReplacer(MethodInfoCache.g_Rot4_AsQuat, MethodInfoCache.m_Rot8_AsQuatRef)
                .MethodReplacer(MethodInfoCache.m_Rot4_Rotate, MethodInfoCache.m_Rot8_Rotate);
        }
    }

    [HarmonyPatch(typeof(Building_MultiTileDoor), "DrawAt")]
    public static class Patch_Building_MultiTileDoor_DrawAt
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var f_Vector3_y = AccessTools.Field(typeof(Vector3), nameof(Vector3.y));
            var num = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Stfld && instruction.OperandIs(f_Vector3_y))
                {
                    yield return new CodeInstruction(OpCodes.Ldc_R4, 7.3076926f);
                    yield return new CodeInstruction(OpCodes.Add);
                }
                if (instruction.opcode == OpCodes.Call && instruction.OperandIs(MethodInfoCache.g_Thing_Rotation) && num < 2)
                {
                    yield return new CodeInstruction(OpCodes.Call, MethodInfoCache.m_BaseFullRotation_Thing);
                    yield return CodeInstruction.Call(typeof(VehicleMapUtility), nameof(VehicleMapUtility.RotForVehicleDraw));
                    num++;
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }

    [HarmonyPatch(typeof(CompInteractable), nameof(CompInteractable.CanInteract))]
    public static class Patch_CompInteractable_CanInteract
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(MethodInfoCache.g_Thing_PositionHeld, MethodInfoCache.m_PositionHeldOnBaseMap)
                .MethodReplacer(MethodInfoCache.m_ReachabilityUtility_CanReach, MethodInfoCache.m_ReachabilityUtilityOnVehicle_CanReach);
        }
    }

    //ソーラーパネルはGraphic_Singleで見た目上回転しないのでFullRotationがHorizontalだったら回転しない
    [HarmonyPatch(typeof(CompPowerPlantSolar), nameof(CompPowerPlantSolar.PostDraw))]
    public static class Patch_CompPowerPlantSolar_PostDraw
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.MethodReplacer(MethodInfoCache.g_Thing_Rotation, MethodInfoCache.m_BaseFullRotation_Thing)
                .MethodReplacer(MethodInfoCache.m_Rot4_Rotate, MethodInfoCache.m_Rot8_Rotate).ToList();

            var label = generator.DefineLabel();
            var label2 = generator.DefineLabel();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Call && c.OperandIs(MethodInfoCache.m_Rot8_Rotate)) - 1;
            codes[pos].labels.Add(label);
            codes[pos + 2].labels.Add(label2);
            codes.InsertRange(pos, new[]
            {
                new CodeInstruction(OpCodes.Dup),
                new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Rot8), nameof(Rot8.IsHorizontal))),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Br_S, label2)
            });
            return codes;
        }
    }

    [HarmonyPatch(typeof(CompPowerPlantWind), nameof(CompPowerPlantWind.PostDraw))]
    public static class Patch_CompPowerPlantWind_PostDraw
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(MethodInfoCache.g_Thing_Rotation, MethodInfoCache.m_BaseFullRotation_Thing)
                .MethodReplacer(MethodInfoCache.g_Rot4_FacingCell, MethodInfoCache.g_Rot8_FacingCell)
                .MethodReplacer(MethodInfoCache.g_Rot4_RighthandCell, MethodInfoCache.m_Rot8Utility_RighthandCell)
                .MethodReplacer(MethodInfoCache.m_Rot4_Rotate, MethodInfoCache.m_Rot8_Rotate)
                .MethodReplacer(MethodInfoCache.g_Rot4_AsQuat, MethodInfoCache.m_Rot8_AsQuatRef)
                .MethodReplacer(MethodInfoCache.m_IntVec3_ToVector3, MethodInfoCache.m_Rot8Utility_ToFundVector3);
        }
    }

    [HarmonyPatch(typeof(CompPowerPlantWind), nameof(CompPowerPlantWind.CompTick))]
    public static class Patch_CompPowerPlantWind_CompTick
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(MethodInfoCache.g_Thing_Map, MethodInfoCache.m_BaseMap_Thing);
        }
    }

    [HarmonyPatch(typeof(CompPowerPlantWind), "RecalculateBlockages")]
    public static class Patch_CompPowerPlantWind_RecalculateBlockages
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Stloc_0);
            codes.InsertRange(pos, new[]
            {
                CodeInstruction.LoadArgument(0),
                CodeInstruction.LoadField(typeof(CompPowerPlantWind), nameof(CompPowerPlantWind.parent)),
                new CodeInstruction(OpCodes.Call, MethodInfoCache.m_BaseMap_Thing),
                CodeInstruction.Call(typeof(Patch_CompPowerPlantWind_RecalculateBlockages), nameof(Patch_CompPowerPlantWind_RecalculateBlockages.Restrict))
            });

            return codes.MethodReplacer(MethodInfoCache.g_Thing_Position, MethodInfoCache.m_PositionOnBaseMap)
                .MethodReplacer(MethodInfoCache.g_Thing_Rotation, MethodInfoCache.m_BaseRotation)
                .MethodReplacer(MethodInfoCache.g_Thing_Map, MethodInfoCache.m_BaseMap_Thing);
        }

        private static IEnumerable<IntVec3> Restrict(IEnumerable<IntVec3> enumerable, Map map)
        {
            return enumerable.Where(c => c.InBounds(map));
        }
    }

    [HarmonyPatch(typeof(Building_Battery), "DrawAt")]
    public static class Patch_Building_Battery_DrawAt
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var ldcr4 = instructions.FirstOrDefault(c => c.opcode == OpCodes.Ldc_R4 && c.OperandIs(0.1f));
            if (ldcr4 != null) ldcr4.operand = 0.75f;
            return instructions.MethodReplacer(MethodInfoCache.g_Thing_Rotation, MethodInfoCache.m_BaseFullRotation_Thing)
                .MethodReplacer(MethodInfoCache.m_Rot4_Rotate, MethodInfoCache.m_Rot8_Rotate);
        }
    }

    [HarmonyPatch(typeof(PlaceWorker_WindTurbine), nameof(PlaceWorker_WindTurbine.DrawGhost))]
    public static class Patch_PlaceWorker_WindTurbine_DrawGhost
    {
        public static void Prefix(ref IntVec3 center, ref Rot4 rot, Thing thing)
        {
            if (Command_FocusVehicleMap.FocusedVehicle != null)
            {
                center = center.ToBaseMapCoord(Command_FocusVehicleMap.FocusedVehicle);
                rot.AsInt += Command_FocusVehicleMap.FocusedVehicle.Rotation.AsInt;
            }
            if (thing.IsOnNonFocusedVehicleMapOf(out var vehicle))
            {
                center = center.ToBaseMapCoord(vehicle);
                rot.AsInt += vehicle.Rotation.AsInt;
            }
        }
    }

    [HarmonyPatch(typeof(CompRefuelable), nameof(CompRefuelable.PostDraw))]
    public static class Patch_CompRefuelable_PostDraw
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return instructions.MethodReplacer(MethodInfoCache.g_Thing_Rotation, MethodInfoCache.m_BaseFullRotation_Thing)
                .MethodReplacer(MethodInfoCache.m_Rot4_Rotate, MethodInfoCache.m_Rot8_Rotate);
        }
    }

    [HarmonyPatch(typeof(PlaceWorker_FuelingPort), nameof(PlaceWorker_FuelingPort.DrawFuelingPortCell))]
    public static class Patch_PlaceWorker_FuelingPort_DrawFuelingPortCell
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Stloc_0);
            codes.InsertRange(pos, new[]
            {
                CodeInstruction.LoadArgument(0),
                new CodeInstruction(OpCodes.Call, MethodInfoCache.m_FocusedDrawPosOffset)
            });
            return codes;
        }
    }

    //Vehicleは移動するからTickごとにTileを取得し直す
    [HarmonyPatch(typeof(TravelingTransportPods), nameof(TravelingTransportPods.Tick))]
    public static class Patch_TravelingTransportPods_Tick
    {
        public static void Postfix(TravelingTransportPods __instance)
        {
            if (__instance.arrivalAction is TransportPodsArrivalAction_LandInVehicleMap arrivalAction && arrivalAction.mapParent is MapParent_Vehicle mapParent)
            {
                __instance.destinationTile = mapParent.Tile;
            }
        }
    }

    //ワイヤーの行き先オフセットとFillableBarの回転
    [HarmonyPatch(typeof(Building_MechCharger), "DrawAt")]
    public static class Patch_Building_MechCharger_DrawAt
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var rot = generator.DeclareLocal(typeof(Rot4));
            var f_rotation = AccessTools.Field(typeof(GenDraw.FillableBarRequest), nameof(GenDraw.FillableBarRequest.rotation));
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Stfld && c.OperandIs(f_rotation));
            //一度Rot4に格納しないとエラー出したので
            codes.InsertRange(pos, new[]
            {
                new CodeInstruction(OpCodes.Stloc_S, rot),
                new CodeInstruction(OpCodes.Ldloc_S, rot),
            });

            var pos2 = codes.FindIndex(c => c.opcode == OpCodes.Call && c.OperandIs(MethodInfoCache.m_IntVec3_ToVector3Shifted)) + 1;
            var vehicle = generator.DeclareLocal(typeof(VehiclePawnWithMap));
            var label = generator.DefineLabel();

            codes[pos2].labels.Add(label);
            codes.InsertRange(pos2, new[]
            {
                CodeInstruction.LoadArgument(0),
                new CodeInstruction(OpCodes.Ldloca_S, vehicle),
                new CodeInstruction(OpCodes.Call, MethodInfoCache.m_IsOnNonFocusedVehicleMapOf),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Ldloc_S, vehicle),
                new CodeInstruction(OpCodes.Call, MethodInfoCache.m_ToBaseMapCoord2)
            });
            return codes.MethodReplacer(MethodInfoCache.g_Thing_Rotation, MethodInfoCache.m_BaseFullRotation_Thing);
        }
    }

    //ThingがあればThing.Map、なければFocusedVehicle.VehicleMap、それもなければFind.CurrentMapを参照するようにする
    [HarmonyPatch(typeof(PlaceWorker_WatchArea), nameof(PlaceWorker_WatchArea.DrawGhost))]
    public static class Patch_PlaceWorker_WatchArea_DrawGhost
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var pos = codes.FindIndex(c => c.opcode == OpCodes.Stloc_0);
            var label = generator.DefineLabel();

            codes[pos].labels.Add(label);
            codes.InsertRange(pos, new[]
            {
                CodeInstruction.LoadArgument(5),
                new CodeInstruction(OpCodes.Brfalse_S, label),
                new CodeInstruction(OpCodes.Pop),
                CodeInstruction.LoadArgument(5),
                new CodeInstruction(OpCodes.Callvirt, MethodInfoCache.g_Thing_Map)
            });
            pos = codes.FindIndex(pos, c => c.opcode == OpCodes.Call && c.OperandIs(MethodInfoCache.m_GenDraw_DrawFieldEdges));
            codes.Insert(pos, CodeInstruction.LoadLocal(0));
            return codes.MethodReplacer(MethodInfoCache.g_Find_CurrentMap, MethodInfoCache.g_VehicleMapUtility_CurrentMap)
                .MethodReplacer(MethodInfoCache.m_GenDraw_DrawFieldEdges, MethodInfoCache.m_GenDrawOnVehicle_DrawFieldEdges);
        }
    }

    //マップ外からPawnFlyerが飛んでくることが起こりうるので(MeleeAnimationのLassoなど)領域外の時はPositionのセットをスキップする
    [HarmonyPatch(typeof(PawnFlyer), "RecomputePosition")]
    public static class Patch_PawnFlyer_RecomputePosition
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();
            var s_Position = AccessTools.PropertySetter(typeof(Thing), nameof(Thing.Position));
            var pos = codes.FindLastIndex(c => c.opcode == OpCodes.Call && c.OperandIs(s_Position));

            var label = generator.DefineLabel();
            var m_InBounds = AccessTools.Method(typeof(GenGrid), nameof(GenGrid.InBounds), new Type[] { typeof(IntVec3), typeof(Map) });

            codes[pos].labels.Add(label);
            codes.InsertRange(pos, new[]
            {
                new CodeInstruction(OpCodes.Dup),
                CodeInstruction.LoadArgument(0),
                new CodeInstruction(OpCodes.Callvirt, MethodInfoCache.g_Thing_Map),
                new CodeInstruction(OpCodes.Call, m_InBounds),
                new CodeInstruction(OpCodes.Brtrue_S, label),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ret)
            });
            return codes;
        }
    }

    [HarmonyPatch(typeof(PawnFlyer), nameof(PawnFlyer.DestinationPos), MethodType.Getter)]
    public static class Patch_PawnFlyer_DestinationPos
    {
        public static void Postfix(PawnFlyer __instance, ref Vector3 __result)
        {
            if (__instance.Map.IsNonFocusedVehicleMapOf(out var vehicle))
            {
                __result = __result.ToBaseMapCoord(vehicle);
            }
        }
    }

    [HarmonyPatch(typeof(GenSpawn), nameof(GenSpawn.Spawn), typeof(Thing), typeof(IntVec3), typeof(Map), typeof(Rot4), typeof(WipeMode), typeof(bool), typeof(bool))]
    public static class Patch_GenSpawn_Spawn
    {
        public static void Prefix(Thing newThing, ref Map map, IntVec3 loc)
        {
            if (newThing is Projectile)
            {
                map = map.BaseMap();
            }
            else if (newThing is Mote && !loc.InBounds(map))
            {
                map = map.BaseMap();
            }
        }
    }

    //[HarmonyPatch(typeof(Pawn_RotationTracker), nameof(Pawn_RotationTracker.FaceTarget))]
    //public static class Patch_Pawn_RotationTracker_FaceTarget
    //{
    //    public static bool Prefix(Pawn_RotationTracker __instance, Pawn ___pawn, LocalTargetInfo target)
    //    {
    //        Log.Message($"{___pawn.def.size}");
    //        if (___pawn is VehiclePawn vehicle) Log.Message(vehicle.VehicleDef.Size);
    //        if (___pawn is VehiclePawn vehicle2 && vehicle2.VehicleDef.size.x * vehicle2.VehicleDef.size.z > 200)
    //        {
    //            FaceTarget_Threaded(__instance, ___pawn, target);
    //            return false;
    //        }
    //        return true;
    //    }

    //    private static void FaceTarget_Threaded(Pawn_RotationTracker __instance, Pawn ___pawn, LocalTargetInfo target)
    //    {
    //        if (!target.IsValid)
    //        {
    //            return;
    //        }

    //        if (target.HasThing)
    //        {
    //            Thing thing = (target.Thing.Spawned ? target.Thing : ThingOwnerUtility.GetFirstSpawnedParentThing(target.Thing));
    //            if (thing == null || !thing.Spawned)
    //            {
    //                return;
    //            }

    //            bool flag = false;
    //            IntVec3 c = default(IntVec3);
    //            CellRect cellRect = thing.OccupiedRect();
    //            var flag2 = false;
    //            Parallel.ForEach(cellRect, c2 =>
    //            {
    //                if (!flag2 && ___pawn.Position == c2)
    //                {
    //                    __instance.Face(thing.DrawPos);
    //                    flag2 = true;
    //                }
    //            });
    //            if (flag2) return;

    //            var flag3 = false;
    //            Parallel.ForEach(cellRect, c2 =>
    //            {
    //                if (!flag3 && c.AdjacentToCardinal(___pawn.Position))
    //                {
    //                    FaceAdjacentCell(___pawn, c2);
    //                    flag3 = true;
    //                }

    //                if (!flag3 && c2.AdjacentTo8Way(___pawn.Position))
    //                {
    //                    flag = true;
    //                    c = c2;
    //                }
    //            });
    //            if (flag3) return;

    //            if (flag)
    //            {
    //                if (DebugViewSettings.drawPawnRotatorTarget)
    //                {
    //                    ___pawn.Map.debugDrawer.FlashCell(___pawn.Position, 0.6f, "jbthing");
    //                    GenDraw.DrawLineBetween(___pawn.Position.ToVector3Shifted(), c.ToVector3Shifted());
    //                }

    //                FaceAdjacentCell(___pawn, c);
    //            }
    //            else
    //            {
    //                __instance.Face(thing.DrawPos);
    //            }
    //        }
    //        else if (___pawn.Position.AdjacentTo8Way(target.Cell))
    //        {
    //            if (DebugViewSettings.drawPawnRotatorTarget)
    //            {
    //                ___pawn.Map.debugDrawer.FlashCell(___pawn.Position, 0.2f, "jbloc");
    //                GenDraw.DrawLineBetween(___pawn.Position.ToVector3Shifted(), target.Cell.ToVector3Shifted());
    //            }

    //            FaceAdjacentCell(___pawn, target.Cell);
    //        }
    //        else if (target.Cell.IsValid && target.Cell != ___pawn.Position)
    //        {
    //            __instance.Face(target.Cell.ToVector3());
    //        }
    //    }

    //    private static void FaceAdjacentCell(Pawn pawn, IntVec3 c)
    //    {
    //        if (c == pawn.Position)
    //        {
    //            return;
    //        }
    //        IntVec3 intVec = c - pawn.Position;
    //        if (intVec.x > 0)
    //        {
    //            pawn.Rotation = Rot4.East;
    //            return;
    //        }
    //        if (intVec.x < 0)
    //        {
    //            pawn.Rotation = Rot4.West;
    //            return;
    //        }
    //        if (intVec.z > 0)
    //        {
    //            pawn.Rotation = Rot4.North;
    //            return;
    //        }
    //        pawn.Rotation = Rot4.South;
    //    }
    //}
}

