﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace YardiksValheimMod
{
    [BepInPlugin("yardik.SnapPointsMadeEasy", "Snap Points Made Easy", "1.2.0")]
    public class SnapMod : BaseUnityPlugin
    {
        private static SnapMod context;
        public static ConfigEntry<bool> modEnabled;

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            ValheimSnapMod.Settings.Init(Config);
            if (!modEnabled.Value)
            {
                return;
            }

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Player))]
        public class HookPieceRayTest
        {
            [HarmonyReversePatch]
            [HarmonyPatch(typeof(Player), "PieceRayTest")]
            public static bool call_PieceRayTest(object instance, out Vector3 point, out Vector3 normal,
                out Piece piece, out Heightmap heightmap, out Collider waterSurface, bool water) =>
                throw new NotImplementedException();
        }

        [HarmonyPatch(typeof(Player), "UpdatePlacementGhost")]
        private static class UpdatePlacementGhost_Patch
        {
            private static bool modifiedPlacementToggled = false;
            private static int currentSourceSnap = 0;
            private static int currentDestinationSnap = 0;
            private static Transform currentDestinationParent;
            private static Transform currentSourceParent;

            private static void Postfix(Player __instance,
                GameObject ___m_placementGhost)
            {
                if (Input.GetKeyDown(ValheimSnapMod.Settings.SnapSettings.EnableModKey.Value))
                {
                    modifiedPlacementToggled = !modifiedPlacementToggled;
                }

                if (___m_placementGhost == null)
                {
                    return;
                }

                var sourcePiece = ___m_placementGhost.GetComponent<Piece>();
                if (sourcePiece == null)
                {
                    return;
                }

                Piece targetPiece = RayTest(__instance, ___m_placementGhost);
                if (!targetPiece)
                {
                    return;
                } 

                if (modifiedPlacementToggled)
                {
                    if (currentDestinationParent != targetPiece.transform)
                    {
                        if (ValheimSnapMod.Settings.SnapSettings.ResetSnapsOnChangePiece.Value || currentDestinationSnap < 0)
                        {
                            currentDestinationSnap = 0;
                        }

                        currentDestinationParent = targetPiece.transform;
                    }

                    if (currentSourceParent != sourcePiece.transform)
                    {
                        if (ValheimSnapMod.Settings.SnapSettings.ResetSnapsOnChangePiece.Value || currentSourceSnap < 0)
                        {
                            currentSourceSnap = 0;
                        }

                        currentSourceParent = sourcePiece.transform;
                    }

                    if (Input.GetKeyDown(ValheimSnapMod.Settings.SnapSettings.IterateSourceSnapPoints.Value))
                    {
                        currentSourceSnap++;
                    }

                    if (Input.GetKeyDown(ValheimSnapMod.Settings.SnapSettings.IterateDestinationSnapPoints.Value))
                    {
                        currentDestinationSnap++;
                    }

                    var sourceSnapPoints = GetSnapPoints(sourcePiece.transform);
                    var destSnapPoints = GetSnapPoints(currentDestinationParent);

                    if (currentSourceSnap >= sourceSnapPoints.Count)
                    {
                        currentSourceSnap = 0;
                    }

                    if (currentDestinationSnap >= destSnapPoints.Count)
                    {
                        currentDestinationSnap = 0;
                    }

                    var a = sourceSnapPoints[currentSourceSnap];
                    var b = destSnapPoints[currentDestinationSnap];
                    
                    var p = b.position - (a.position - ___m_placementGhost.transform.position);
                    ___m_placementGhost.transform.position = p;
                }
            }

            private static Piece RayTest(Player player, GameObject placementGhost)
            {
                var component1 = placementGhost.GetComponent<Piece>();
                var water = component1.m_waterPiece || component1.m_noInWater;
                HookPieceRayTest.call_PieceRayTest(player, out Vector3 point, out Vector3 normal, out Piece piece, out Heightmap heightmap, out Collider waterSurface, water);
                return piece;
            }

            public static List<Transform> GetSnapPoints(Transform piece)
            {
                List<Transform> points = new List<Transform>();
                if (piece == null)
                {
                    return points;
                }

                for (var index = 0; index < piece.childCount; ++index)
                {
                    var child = piece.GetChild(index);
                    if (child.CompareTag("snappoint"))
                    {
                        points.Add(child);
                    }
                }

                points.Add(piece.transform);
                return points;
            }
        }
    }
}