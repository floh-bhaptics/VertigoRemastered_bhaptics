using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using MelonLoader;
using HarmonyLib;

using MyBhapticsTactsuit;

using Vertigo2.Weapons;
using Vertigo2.Interaction;
using Vertigo2;
using Vertigo2.Player;
using VertigoUpgrades;
using Valve.VR;



namespace Vertigo_Remastered_bhaptics
{
    public class Vertigo_Remastered_bhaptics : MelonMod
    {
        public static TactsuitVR tactsuitVr;
        // private static int leftHand = ((int)SteamVR_Input_Sources.LeftHand);
        private static int rightHand = ((int)SteamVR_Input_Sources.RightHand);
        private static bool rightFootLast = true;

        public override void OnApplicationStart()
        {
            base.OnApplicationStart();
            tactsuitVr = new TactsuitVR();
            tactsuitVr.PlaybackHaptics("HeartBeat");
        }


        #region General settings

        private static (float, float) getAngleAndShift(VertigoPlayer player, HitInfo hit)
        {
            Vector3 patternOrigin = new Vector3(0f, 0f, 1f);
            // y is "up", z is "forward" in local coordinates
            Vector3 hitPosition = hit.hitPoint - player.position;
            Quaternion PlayerRotation = player.head.rotation;
            Vector3 playerDir = PlayerRotation.eulerAngles;
            // We only want rotation correction in y direction (left-right), top-bottom and yaw we can leave
            Vector3 flattenedHit = new Vector3(hitPosition.x, 0f, hitPosition.z);
            float earlyhitAngle = Vector3.Angle(flattenedHit, patternOrigin);
            Vector3 earlycrossProduct = Vector3.Cross(flattenedHit, patternOrigin);
            if (earlycrossProduct.y > 0f) { earlyhitAngle *= -1f; }
            //tactsuitVr.LOG("EarlyHitAngle: " + earlyhitAngle.ToString());
            float myRotation = earlyhitAngle - playerDir.y;
            myRotation *= -1f;
            if (myRotation < 0f) { myRotation = 360f + myRotation; }

            /*
            Vector3 relativeHitDir = Quaternion.Euler(playerDir) * hitPosition;
            Vector2 xzHitDir = new Vector2(relativeHitDir.x, relativeHitDir.z);
            //Vector2 patternOrigin = new Vector2(0f, 1f);
            float hitAngle = Vector2.SignedAngle(xzHitDir, patternOrigin);
            hitAngle *= -1;
            //hitAngle += 90f;
            if (hitAngle < 0f) { hitAngle = 360f + hitAngle; }
            */
            float hitShift = hitPosition.y;
            if (hitShift > 0.0f) { hitShift = 0.5f; }
            else if (hitShift < -0.5f) { hitShift = -0.5f; }
            else { hitShift = (hitShift + 0.25f) * 2.0f; }

            //tactsuitVr.LOG("Relative x-z-position: " + relativeHitDir.x.ToString() + " "  + relativeHitDir.z.ToString());
            //tactsuitVr.LOG("HitAngle: " + hitAngle.ToString());
            //tactsuitVr.LOG("HitShift: " + hitShift.ToString());
            return (myRotation, hitShift);
        }

        [HarmonyPatch(typeof(VertigoPlayer), "Die")]
        public class bhaptics_PlayerDies
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopThreads();
            }
        }

        [HarmonyPatch(typeof(VertigoPlayer), "EntityUpdate")]
        public class bhaptics_EntityAddHealth
        {
            [HarmonyPostfix]
            public static void Postfix(VertigoEntity __instance)
            {
                //tactsuitVr.StopHeartBeat();
                if (__instance.health > 0.3* __instance.maxHealth) { tactsuitVr.StopHeartBeat(); }
            }
        }

        #endregion

        #region Player gets hit

        [HarmonyPatch(typeof(VertigoPlayer), "Hit")]
        public class bhaptics_PlayerHit
        {
            [HarmonyPostfix]
            public static void Postfix(VertigoPlayer __instance, HitInfo hit, bool crit)
            {
                if (__instance.health < 0.3f * __instance.maxHealth) { tactsuitVr.StartHeartBeat(); }
                if (crit) { tactsuitVr.LOG("Critical hit!"); }
                if ((hit.damageType & DamageType.Fire) == DamageType.Fire)
                {
                    tactsuitVr.PlaybackHaptics("FlameThrower");
                }
                if ((hit.damageType & DamageType.Explosion) == DamageType.Explosion)
                {
                    tactsuitVr.PlaybackHaptics("ExplosionUp");
                }
                if ((hit.damageType & DamageType.Poison) == DamageType.Poison)
                {
                    tactsuitVr.PlaybackHaptics("GasDeath");
                }
                if ((hit.damageType & DamageType.Grenade) == DamageType.Grenade)
                {
                    tactsuitVr.PlaybackHaptics("ExplosionFace");
                }
                if ((hit.damageType & DamageType.Electricity) == DamageType.Electricity)
                {
                    tactsuitVr.PlaybackHaptics("Electrocution");
                }
                if ((hit.damageType & DamageType.Drowning) == DamageType.Drowning)
                {
                    if (!tactsuitVr.IsPlaying("Smoking")) { tactsuitVr.PlaybackHaptics("Smoking"); }
                }
                if ((hit.damageType & DamageType.Radiation) == DamageType.Radiation)
                {
                    if (!tactsuitVr.IsPlaying("Radiation")) { tactsuitVr.PlaybackHaptics("Radiation"); }
                }
                if ((hit.damageType & DamageType.Bullet) == DamageType.Bullet)
                {
                    (float hitAngle, float hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("BulletHit", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Laser) == DamageType.Laser)
                {
                    (float hitAngle, float hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("BulletHit", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Impact) == DamageType.Impact)
                {
                    (float hitAngle, float hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("Impact", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Blade) == DamageType.Blade)
                {
                    (float hitAngle, float hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("BladeHit", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Bite) == DamageType.Bite)
                {
                    (float hitAngle, float hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("BulletHit", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Plasma) == DamageType.Plasma)
                {
                    (float hitAngle, float hitShift) = getAngleAndShift(__instance, hit);
                    if (hitShift >= 0.5f) { tactsuitVr.HeadShot(hitAngle); return; }
                    tactsuitVr.PlayBackHit("BulletHit", hitAngle, hitShift);
                    return;
                }
                if ((hit.damageType & DamageType.Gordle_Blue) == DamageType.Gordle_Blue)
                {
                    tactsuitVr.LOG("Gordle_Blue hitPoint: " + hit.hitPoint.x.ToString() + " " + hit.hitPoint.y.ToString() + " " + hit.hitPoint.z.ToString());
                }
                if ((hit.damageType & DamageType.Generic) == DamageType.Generic)
                {
                    tactsuitVr.LOG("Generic hitPoint: " + hit.hitPoint.x.ToString() + " " + hit.hitPoint.y.ToString() + " " + hit.hitPoint.z.ToString());
                }
                if ((hit.damageType & DamageType.Antimatter) == DamageType.Antimatter)
                {
                    tactsuitVr.LOG("Antimatter hitPoint: " + hit.hitPoint.x.ToString() + " " + hit.hitPoint.y.ToString() + " " + hit.hitPoint.z.ToString());
                }
                if ((hit.damageType & DamageType.Heat) == DamageType.Heat)
                {
                    tactsuitVr.LOG("Heat hitPoint: " + hit.hitPoint.x.ToString() + " " + hit.hitPoint.y.ToString() + " " + hit.hitPoint.z.ToString());
                }
                if ((hit.damageType & DamageType.Cold) == DamageType.Cold)
                {
                    tactsuitVr.LOG("Cold hitPoint: " + hit.hitPoint.x.ToString() + " " + hit.hitPoint.y.ToString() + " " + hit.hitPoint.z.ToString());
                }
                if ((hit.damageType & DamageType.Relativistic) == DamageType.Relativistic)
                {
                    tactsuitVr.LOG("Relativistic hitPoint: " + hit.hitPoint.x.ToString() + " " + hit.hitPoint.y.ToString() + " " + hit.hitPoint.z.ToString());
                }
                if ((hit.damageType & DamageType.Gordle_Orange) == DamageType.Gordle_Orange)
                {
                    tactsuitVr.LOG("Gordle_Orange hitPoint: " + hit.hitPoint.x.ToString() + " " + hit.hitPoint.y.ToString() + " " + hit.hitPoint.z.ToString());
                }

            }
        }

        #endregion

        #region Weapons

        [HarmonyPatch(typeof(Gun), "ShootHaptics")]
        public class bhaptics_GunFeedback
        {
            [HarmonyPostfix]
            public static void Postfix(Gun __instance, float length, float power)
            {
                bool isRightHand = (((int)__instance.inputSource) == rightHand);
                bool twoHanded = (__instance.heldEquippable.otherHandHolding);

                if (__instance.name == "Minigun")
                {
                    if (power < 0.5f) { tactsuitVr.StopMinigun(isRightHand, twoHanded); }
                    else { tactsuitVr.FireMinigun(isRightHand, twoHanded); }
                    return;
                }
                float intensity = Math.Max(power * 2.0f, 1.2f);
                tactsuitVr.GunRecoil(isRightHand, intensity);
            }
        }

        [HarmonyPatch(typeof(PlasmaSword), "OnImpact")]
        public class bhaptics_PlasmaSwordOnOmpact
        {
            [HarmonyPostfix]
            public static void Postfix(PlasmaSword __instance, float normalizedSpeed)
            {
                bool isRightHand = ( ((int)__instance.inputSource) == rightHand );
                float intensity = (1.0f - (1.0f - normalizedSpeed) * 0.5f);
                tactsuitVr.SwordRecoil(isRightHand, intensity);
            }
        }

        [HarmonyPatch(typeof(PlasmaSword), "OnDeflect")]
        public class bhaptics_PlasmaSwordOnDeflect
        {
            [HarmonyPostfix]
            public static void Postfix(PlasmaSword __instance)
            {
                bool isRightHand = (((int)__instance.inputSource) == rightHand);
                tactsuitVr.SwordRecoil(isRightHand, 0.5f);
            }
        }

        [HarmonyPatch(typeof(RocketLauncher), "ShootHaptics")]
        public class bhaptics_ShootRocketHaptics
        {
            [HarmonyPostfix]
            public static void Postfix(VertigoInteractable handle)
            {
                //bool isRightHand = (((int)handle.mainHoldingHand.inputSource) == rightHand);
                //tactsuitVr.LOG("Shoot Rocket: " + isRightHand.ToString());
                // tactsuitVr.GunRecoil(isRightHand, normalizedSpeed);
                tactsuitVr.GunRecoil(true, 1.1f);
                tactsuitVr.GunRecoil(false, 1.1f);
            }
        }

        [HarmonyPatch(typeof(FlingBomb), "Explode")]
        public class bhaptics_FlingBombExplode
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ExplosionUp");
            }
        }


        #endregion

        #region Boss fights

        [HarmonyPatch(typeof(SpiderQueen), "Die")]
        public class bhaptics_SpiderQueenDies
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ExplosionUp");
            }
        }

        [HarmonyPatch(typeof(SpiderQueen), "EyestalkKilled")]
        public class bhaptics_SpiderQueenEyeKilled
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("BellyRumble");
            }
        }


        [HarmonyPatch(typeof(FrankRocket), "Detonate")]
        public class bhaptics_DetonateFrankRocket
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ExplosionUp");
            }
        }

        [HarmonyPatch(typeof(FrankFight), "Roar")]
        public class bhaptics_FrankRoar
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("BellyRumble");
            }
        }

        [HarmonyPatch(typeof(CentipodBoss), "Roar")]
        public class bhaptics_CentipodRoar
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("BellyRumble");
            }
        }

        [HarmonyPatch(typeof(CentipodBoss), "Die")]
        public class bhaptics_CentipodDie
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("BellyRumble");
            }
        }

        [HarmonyPatch(typeof(FrankFight), "Death")]
        public class bhaptics_FrankDeath
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ExplosionUp");
            }
        }

        #endregion

        #region Hand interaction

        [HarmonyPatch(typeof(VertigoHand), "Grab")]
        public class bhaptics_Grab
        {
            [HarmonyPostfix]
            public static void Postfix(VertigoHand __instance)
            {
                //tactsuitVr.LOG("Hand: " + ((int)__instance.inputSource).ToString());
            }
        }

        #endregion

        #region World interaction

        [HarmonyPatch(typeof(ToxicGas), "PlayAudio")]
        public class bhaptics_StartGasDamage
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StartChoking();
            }
        }

        [HarmonyPatch(typeof(ToxicGas), "Update")]
        public class bhaptics_StopGasDamage
        {
            [HarmonyPostfix]
            public static void Postfix(ToxicGas __instance)
            {
                if (!__instance.inGas)
                { tactsuitVr.StopChoking(); }
            }
        }


        [HarmonyPatch(typeof(SpacetimeManipulator), "Teleport")]
        public class bhaptics_Teleport
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopHapticFeedback("CollectJuice_R");
                tactsuitVr.StopHapticFeedback("CollectJuice_L");
                tactsuitVr.PlaybackHaptics("TeleportThrough");
            }
        }

        [HarmonyPatch(typeof(SpacetimeManipulator), "UpdateJuiceCollection")]
        public class bhaptics_JuiceCollection
        {
            [HarmonyPostfix]
            public static void Postfix(SpacetimeManipulator __instance)
            {
                if (!__instance.succing) { return; }
                bool isRightHand = (((int)__instance.inputSource) == rightHand);
                if (isRightHand)
                {
                    if (!tactsuitVr.IsPlaying("CollectJuice_R")) { tactsuitVr.PlaybackHaptics("CollectJuice_R"); }
                } else
                {
                    if (!tactsuitVr.IsPlaying("CollectJuice_L")) { tactsuitVr.PlaybackHaptics("CollectJuice_L"); }
                }
                
            }
        }

        [HarmonyPatch(typeof(PlayerFootstepFX), "Step")]
        public class bhaptics_Footstep
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                if (rightFootLast)
                {
                    rightFootLast = false;
                }
                else
                {
                    rightFootLast = true;
                }
                tactsuitVr.FootStep(rightFootLast);
            }
        }

        [HarmonyPatch(typeof(WaterHead), "EnterWater")]
        public class bhaptics_HeadEnterWater
        {
            [HarmonyPostfix]
            public static void Postfix(WaterBody __instance)
            {
                    tactsuitVr.StartWater();
            }
        }

        [HarmonyPatch(typeof(WaterHead), "ExitWater")]
        public class bhaptics_HeadExitWater
        {
            [HarmonyPostfix]
            public static void Postfix(WaterBody __instance)
            {
                tactsuitVr.StopWater();
            }
        }


        [HarmonyPatch(typeof(Elevator), "Move")]
        public class bhaptics_ElevatorMove
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ElevatorTingle");
            }
        }

        [HarmonyPatch(typeof(DeathScreen), "Start")]
        public class bhaptics_DeathScreenStart
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.StopThreads();
            }
        }
        /*
                [HarmonyPatch(typeof(VertigoCharacterController), "OnGroundHit")]
                public class bhaptics_PlayerHitsGround
                {
                    [HarmonyPostfix]
                    public static void Postfix(VertigoCharacterController __instance)
                    {
                        tactsuitVr.PlaybackHaptics("FallDamage");
                    }
                }
        */
        #endregion

        #region Cutscenes

        [HarmonyPatch(typeof(HallBeastAmbush), "Roar")]
        public class bhaptics_HallBeastRoar
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("BellyRumble");
            }
        }


        [HarmonyPatch(typeof(BridgesExo), "ExtensionEvent")]
        public class bhaptics_ExtendBridges
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("BellyRumble", 0.6f);
            }
        }


        [HarmonyPatch(typeof(ExoElevatorScene), "Crunch")]
        public class bhaptics_CrunchBigBot
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ExplosionUp");
            }
        }

        [HarmonyPatch(typeof(ExoElevatorScene), "TriggerBigBotAttack")]
        public class bhaptics_BigBotAttack
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("BellyRumble");
            }
        }


        [HarmonyPatch(typeof(Dynamite), "Trigger")]
        public class bhaptics_ExplodeDynamite
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ExplosionUp");
            }
        }

        [HarmonyPatch(typeof(HomingMissile), "Explode")]
        public class bhaptics_MissileExplode
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ExplosionUp");
            }
        }

        [HarmonyPatch(typeof(DropshipHeist), "FlightEnd")]
        public class bhaptics_DropshipCrash
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ExplosionUp");
            }
        }


        [HarmonyPatch(typeof(WormholeCutscene), "PickupSoul")]
        public class bhaptics_PickupSoul
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("RipOutSoul");
            }
        }

        [HarmonyPatch(typeof(WormholeCutscene), "DropSoul")]
        public class bhaptics_DropSoul
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("PutBackSoul");
            }
        }

        [HarmonyPatch(typeof(BigBotCatch), "DoCatchCoroutine")]
        public class bhaptics_BigBotCatch
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("ForceGrab");
            }
        }

        [HarmonyPatch(typeof(DNAScanner), "OnTriggerEnter")]
        public class bhaptics_DNAScanner
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("Scanning");
            }
        }

        [HarmonyPatch(typeof(AnomalyController), "TransitionToWormhole")]
        public class bhaptics_DimensionTransition
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("EnterDimension");
            }
        }

        [HarmonyPatch(typeof(WormholeCutscene), "FinishCutscene")]
        public class bhaptics_FinishWormhole
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("EnterDimension");
            }
        }

        [HarmonyPatch(typeof(CabinFireplace), "Light")]
        public class bhaptics_LightFire
        {
            [HarmonyPostfix]
            public static void Postfix()
            {
                tactsuitVr.PlaybackHaptics("LightFire");
            }
        }



        #endregion


    }
}
