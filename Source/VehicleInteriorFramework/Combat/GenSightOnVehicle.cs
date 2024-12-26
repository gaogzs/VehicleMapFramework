﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace VehicleInteriors
{
    public static class GenSightOnVehicle
    {
        public static bool LineOfSight(IntVec3 start, IntVec3 end, Map map, bool skipFirstCell = false, Func<IntVec3, bool> validator = null, int halfXOffset = 0, int halfZOffset = 0)
        {
            bool flag;
            if (map.IsVehicleMapOf(out var vehicle))
            {
                start = start.OrigToVehicleMap(vehicle);
                end  = end.OrigToVehicleMap(vehicle);
                map = vehicle.Map;
            }
            if (start.x == end.x)   
            {
                flag = (start.z < end.z);
            }
            else
            {
                flag = (start.x < end.x);
            }
            int num = Mathf.Abs(end.x - start.x);
            int num2 = Mathf.Abs(end.z - start.z);
            int num3 = start.x;
            int num4 = start.z;
            int i = 1 + num + num2;
            int num5 = (end.x > start.x) ? 1 : -1;
            int num6 = (end.z > start.z) ? 1 : -1;
            num *= 4;
            num2 *= 4;
            num += halfXOffset * 2;
            num2 += halfZOffset * 2;
            int num7 = num / 2 - num2 / 2;
            IntVec3 intVec = default;
            while (i > 1)
            {
                intVec.x = num3;
                intVec.z = num4;
                if (!skipFirstCell || !(intVec == start))
                {
                    if (!intVec.CanBeSeenOverOnVehicle(map))
                    {
                        return false;
                    }
                    if (validator != null && !validator(intVec))
                    {
                        return false;
                    }
                }
                if (num7 > 0 || (num7 == 0 && flag))
                {
                    num3 += num5;
                    num7 -= num2;
                }
                else
                {
                    num4 += num6;
                    num7 += num;
                }
                i--;
            }
            return true;
        }

        public static bool CanBeSeenOverOnVehicle(this IntVec3 c, Map map)
        {
            if (!c.InBounds(map)) return true;

            var flag = true;
            if (c.ToVector3Shifted().TryGetVehiclePawnWithMap(map, out var vehicle))
            {
                var c2 = c.VehicleMapToOrig(vehicle);
                flag = !c2.InBounds(vehicle.interiorMap);
                if (!flag)
                {
                    Building edifice = c2.GetEdifice(vehicle.interiorMap);
                    flag = edifice == null || edifice.CanBeSeenOver();
                }
            }
            Building edifice2 = c.GetEdifice(map);
            return flag && (edifice2 == null || edifice2.CanBeSeenOver());
        }

        public static bool LineOfSightThingToTarget(Thing thing, LocalTargetInfo target, bool skipFirstCell = false, Func<IntVec3, bool> validator = null)
        {
            return GenSightOnVehicle.LineOfSight(thing.PositionOnBaseMap(), target.CellOnBaseMap(), thing.BaseMap(), skipFirstCell, validator);
        }

        public static bool LineOfSightThingToThing(Thing start, Thing end, bool skipFirstCell = false, Func<IntVec3, bool> validator = null)
        {
            return GenSightOnVehicle.LineOfSight(start.PositionOnBaseMap(), end.PositionOnBaseMap(), start.BaseMap(), skipFirstCell, validator);
        }

        public static bool LineOfSightToThing(IntVec3 start, Thing t, Map map, bool skipFirstCell = false, Func<IntVec3, bool> validator = null)
        {
            if (t.def.size == IntVec2.One)
            {
                return GenSightOnVehicle.LineOfSight(start, t.PositionOnBaseMap(), map, skipFirstCell, validator);
            }
            var flag = t.IsOnNonFocusedVehicleMapOf(out var vehicle);
            foreach (IntVec3 end in t.OccupiedRect())
            {
                var end2 = flag ? end.OrigToVehicleMap(vehicle) : end;
                if (GenSightOnVehicle.LineOfSight(start, end2, map, skipFirstCell, validator))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool LineOfSight(IntVec3 start, IntVec3 end, Map map)
        {
            return GenSightOnVehicle.LineOfSight(start, end, map, CellRect.SingleCell(start), CellRect.SingleCell(end), null);
        }

        public static bool LineOfSight(IntVec3 start, IntVec3 end, Map map, CellRect startRect, CellRect endRect, Func<IntVec3, bool> validator = null)
        {
            bool flag;
            if (map.IsVehicleMapOf(out var vehicle))
            {
                start = start.OrigToVehicleMap(vehicle);
                map = vehicle.Map;
            }
            if (start.x == end.x)
            {
                flag = (start.z < end.z);
            }
            else
            {
                flag = (start.x < end.x);
            }
            int num = Mathf.Abs(end.x - start.x);
            int num2 = Mathf.Abs(end.z - start.z);
            int num3 = start.x;
            int num4 = start.z;
            int i = 1 + num + num2;
            int num5 = (end.x > start.x) ? 1 : -1;
            int num6 = (end.z > start.z) ? 1 : -1;
            int num7 = num - num2;
            num *= 2;
            num2 *= 2;
            IntVec3 intVec = default;
            while (i > 1)
            {
                intVec.x = num3;
                intVec.z = num4;
                if (endRect.Contains(intVec))
                {
                    return true;
                }
                if (!startRect.Contains(intVec))
                {
                    if (!intVec.CanBeSeenOverOnVehicle(map))
                    {
                        return false;
                    }
                    if (validator != null && !validator(intVec))
                    {
                        return false;
                    }
                }
                if (num7 > 0 || (num7 == 0 && flag))
                {
                    num3 += num5;
                    num7 -= num2;
                }
                else
                {
                    num4 += num6;
                    num7 += num;
                }
                i--;
            }
            return true;
        }

        public static bool LineOfSightToEdges(IntVec3 start, IntVec3 end, Map map, bool skipFirstCell = false, Func<IntVec3, bool> validator = null)
        {
            if (GenSightOnVehicle.LineOfSight(start, end, map, skipFirstCell, validator, 0, 0))
            {
                return true;
            }
            int num = (start * 2).DistanceToSquared(end * 2);
            for (int i = 0; i < 4; i++)
            {
                if ((start * 2).DistanceToSquared(end * 2 + GenAdj.CardinalDirections[i]) <= num && GenSightOnVehicle.LineOfSight(start, end, map, skipFirstCell, validator, GenAdj.CardinalDirections[i].x, GenAdj.CardinalDirections[i].z))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
