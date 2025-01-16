﻿using RimWorld;
using SmashTools;
using System.Linq;
using UnityEngine;
using Vehicles;
using Verse;

namespace VehicleInteriors
{
    public class VehicleStatPart_HumanPower : VehicleStatPart
    {

        public override bool Disabled(VehiclePawn vehicle)
        {
            return !vehicle.VehicleDef.HasModExtension<VehicleHumanPowered>();
        }

        protected float? Modifier(VehiclePawn vehicle)
        {
            var handlers = vehicle.handlers?.Where(h => h.Isnt<VehicleHandlerBuildable>() && h.RequiredForMovement);
            if (!handlers.NullOrEmpty())
            {
                return handlers.Average(h =>
                {
                    var statValue = 0f;
                    foreach (var pawn in h.handlers)
                    {
                        if (!h.CanOperateRole(pawn)) continue;
                        var statFactor = 1f;
                        statFactor *= pawn.GetStatValue(StatDefOf.MoveSpeed) / StatDefOf.MoveSpeed.defaultBaseValue;
                        statFactor *= Mathf.LerpUnclamped(1f, (1f / pawn.GetStatValue(StatDefOf.IncomingDamageFactor) / StatDefOf.IncomingDamageFactor.defaultBaseValue), 0.5f);
                        statFactor *= Mathf.LerpUnclamped(1f, pawn.GetStatValue(StatDefOf.WorkSpeedGlobal) / StatDefOf.WorkSpeedGlobal.defaultBaseValue, 0.25f);
                        statFactor *= Mathf.LerpUnclamped(1f, pawn.BodySize, 1.25f);
                        var skillFactor = Mathf.Min(pawn.skills.GetSkill(SkillDefOf.Melee).Level + pawn.skills.GetSkill(SkillDefOf.Mining).Level, 20f) / 10f;
                        statFactor *= Mathf.LerpUnclamped(1f, skillFactor, 0.25f);
                        statValue += statFactor;
                    }
                    return statValue / h.role.Slots;
                });
            }
            return 0f;
        }

        public override float TransformValue(VehiclePawn vehicle, float value)
        {
            return value * this.Modifier(vehicle).GetValueOrDefault();
        }

        public override string ExplanationPart(VehiclePawn vehicle)
        {
            return "VMF_StatsReport_HumanPowerAverage".Translate(this.Modifier(vehicle).GetValueOrDefault().ToStringByStyle(ToStringStyle.FloatMaxTwo, ToStringNumberSense.Factor));
        }
    }
}
