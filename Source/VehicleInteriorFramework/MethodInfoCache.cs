﻿using HarmonyLib;
using RimWorld.Planet;
using SmashTools;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using VehicleInteriors.VIF_HarmonyPatches;
using Vehicles;
using Verse;

namespace VehicleInteriors
{
    public class MethodInfoCache
    {
        public static readonly MethodInfo g_FocusedVehicle = AccessTools.PropertyGetter(typeof(Command_FocusVehicleMap), nameof(Command_FocusVehicleMap.FocusedVehicle));

        public static readonly MethodInfo g_Find_CurrentMap = AccessTools.PropertyGetter(typeof(Find), nameof(Find.CurrentMap));

        public static readonly MethodInfo g_VehicleMapUtility_CurrentMap = AccessTools.PropertyGetter(typeof(VehicleMapUtility), nameof(VehicleMapUtility.CurrentMap));

        public static readonly MethodInfo m_IsVehicleMapOf = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.IsVehicleMapOf));

        public static readonly MethodInfo m_IsOnVehicleMapOf = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.IsOnVehicleMapOf));

        public static readonly MethodInfo m_OrigToVehicleMap1 = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.OrigToVehicleMap), new Type[] { typeof(Vector3) });

        public static readonly MethodInfo m_OrigToVehicleMap2 = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.OrigToVehicleMap), new Type[] { typeof(Vector3), typeof(VehiclePawnWithInterior) });

        public static readonly MethodInfo m_VehicleMapToOrig1 = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.VehicleMapToOrig), new Type[] { typeof(Vector3) });

        public static readonly MethodInfo m_VehicleMapToOrig2 = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.VehicleMapToOrig), new Type[] { typeof(Vector3), typeof(VehiclePawnWithInterior) });

        public static readonly MethodInfo g_Thing_Map = AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Map));

        public static readonly MethodInfo m_BaseMap_Thing = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.BaseMap), new Type[] { typeof(Thing) });

        public static readonly MethodInfo g_Zone_Map = AccessTools.PropertyGetter(typeof(Zone), nameof(Zone.Map));

        public static readonly MethodInfo m_BaseMap_Zone = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.BaseMap), new Type[] { typeof(Zone) });

        public static readonly MethodInfo g_Thing_MapHeld = AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.MapHeld));

        public static readonly MethodInfo m_MapHeldBaseMap = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.MapHeldBaseMap));

        public static readonly MethodInfo g_Thing_Position = AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Position));

        public static readonly MethodInfo m_PositionOnBaseMap = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.PositionOnBaseMap), new Type[] { typeof(Thing) });

        public static readonly MethodInfo g_LocalTargetInfo_Cell = AccessTools.PropertyGetter(typeof(LocalTargetInfo), nameof(LocalTargetInfo.Cell));

        public static readonly MethodInfo m_CellOnBaseMap = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.CellOnBaseMap));

        public static readonly MethodInfo m_OccupiedRect = AccessTools.Method(typeof(GenAdj), nameof(GenAdj.OccupiedRect), new Type[] { typeof(Thing) });

        public static readonly MethodInfo m_MovedOccupiedRect = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.MovedOccupiedRect));

        public static readonly MethodInfo m_ToTargetInfo = AccessTools.Method(typeof(LocalTargetInfo), nameof(LocalTargetInfo.ToTargetInfo));

        public static readonly MethodInfo m_ToBaseMapTargetInfo = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.ToBaseMapTargetInfo));

        public static readonly MethodInfo g_FullRotation = AccessTools.PropertyGetter(typeof(VehiclePawn), nameof(VehiclePawn.FullRotation));

        public static readonly MethodInfo m_BaseFullRotationOfThing = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.BaseFullRotationOfThing));

        public static readonly MethodInfo g_Angle = AccessTools.PropertyGetter(typeof(VehiclePawn), nameof(VehiclePawn.Angle));

        public static readonly MethodInfo g_AsAngleRot8 = AccessTools.PropertyGetter(typeof(Rot8), nameof(Rot8.AsAngle));

        public static readonly MethodInfo m_BaseMap_Map = AccessTools.Method(typeof(VehicleMapUtility), nameof(VehicleMapUtility.BaseMap), new Type[] { typeof(Map) });

        public static readonly MethodInfo m_RotatePoint = AccessTools.Method(typeof(Ext_Math), nameof(Ext_Math.RotatePoint));

        public static readonly MethodInfo g_Thing_Spawned = AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Spawned));

        public static readonly MethodInfo m_Rot8_AsQuat = AccessTools.Method(typeof(Rot8Utility), nameof(Rot8Utility.AsQuat));

        public static readonly MethodInfo o_Quaternion_Multiply = AccessTools.Method(typeof(Quaternion), "op_Multiply", new Type[] { typeof(Quaternion), typeof(Quaternion) });

        public static readonly MethodInfo m_GenDraw_DrawFieldEdges = AccessTools.Method(typeof(GenDraw), nameof(GenDraw.DrawFieldEdges), new Type[] { typeof(List<IntVec3>) });

        public static readonly MethodInfo m_GenDrawOnVehicle_DrawFieldEdges = AccessTools.Method(typeof(GenDrawOnVehicle), nameof(GenDrawOnVehicle.DrawFieldEdges), new Type[] { typeof(List<IntVec3>), typeof(Map) });

        public static readonly MethodInfo g_Designator_Map = AccessTools.PropertyGetter(typeof(Designator), nameof(Designator.Map));

        public static readonly MethodInfo g_Thing_Rotation = AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.Rotation));

        public static readonly MethodInfo m_Thing_RotationOrig = AccessTools.Method(typeof(Patch_Thing_Rotation), nameof(Patch_Thing_Rotation.Rotation));
    }
}
