using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace AncientBarkPieces
{
    // Ancient bark versions of the vanilla wood building pieces: clones with a dark, mossy bark texture
    // (world-mapped, vanilla pixel size) that cost Ancient Bark instead of wood.
    [BepInPlugin(Guid, "Ancient Bark Pieces", "1.1.0")]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("blacks7ar.CoreWoodPieces", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("shudnal.MoreGatesExtended", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "jellydotexe.ancientbarkpieces";
        public const string Prefix = "ancientbark_";
        const float TriplanarScale = 0.25f;
        static ManualLogSource Log;

        // Vanilla prefabs to copy. Names that don't exist in this game version are skipped with a warning.
        static readonly string[] Bases =
        {
            "wood_floor", "wood_floor_1x1",
            "woodwall", "wood_wall_half", "wood_wall_quarter",
            "wood_wall_roof", "wood_wall_roof_45", "wood_wall_roof_top", "wood_wall_roof_top_45",
            "wood_wall_roof_upsidedown", "wood_wall_roof_45_upsidedown",
            "wood_pole", "wood_pole2", "wood_beam", "wood_beam_1", "wood_beam_26", "wood_beam_45",
            "wood_stair", "wood_stepladder", "wood_door", "wood_gate",
            "stake_wall", "wood_fence",
            // plain bark versions of the long darkwood shapes
            "darkwood_arch", "darkwood_pole4", "darkwood_beam4x4",
            // log roofs from blacks7ar's CoreWoodPieces, copied at runtime when that mod is installed
            "BCW_CoreWood_Roof26", "BCW_CoreWood_Roof45", "BCW_CoreWood_RoofTop26", "BCW_CoreWood_RoofTop45",
            "BCW_CoreWood_RoofOCorner26", "BCW_CoreWood_RoofICorner26", "BCW_CoreWood_RoofOCorner45", "BCW_CoreWood_RoofICorner45",
            // log gates and doors from the same mod
            "BCW_CoreWood_Gate", "BCW_CoreWood_FenceGate", "BCW_CoreWood_TrapDoor",
            "BCW_CoreWood_DrawBridgeNarrow", "BCW_CoreWood_DrawBridgeNarrowTall", "BCW_CoreWood_DrawBridgeWide", "BCW_CoreWood_DrawBridgeWideTall",
            // log gables, walls, floor, stair, windows and fences from the same mod
            "BCW_CoreWood_WallRoof26", "BCW_CoreWood_WallRoof45", "BCW_CoreWood_WallRoof_UpsideDown26", "BCW_CoreWood_WallRoof_UpsideDown45",
            "BCW_CoreWood_WallRoofTop26", "BCW_CoreWood_WallRoofTop45",
            "BCW_CoreWoodWall", "BCW_CoreWoodWall1", "BCW_CoreWood_HalfWall", "BCW_CoreWood_Floor",
            "BCW_CoreWood_Stair", "BCW_CoreWood_Window1", "BCW_CoreWood_WindowHalf",
            "BCW_CoreWood_Fence1", "BCW_CoreWood_Fence2", "BCW_CoreWood_FencePillar", "BCW_CoreWood_PalisadeFence", "BCW_CoreWood_PalisadeFenceTall",
            "wood_pole_log", "wood_pole_log_4", "wood_wall_log", "wood_wall_log_4x0.5", "wood_log_26", "wood_log_45",
            // framed glass windows from shudnal's More Gates Extended, copied at runtime when that mod is installed
            "h_window_01", "h_window_02", "h_window_03", "h_window_04", "h_window_05",
            "h_window_06", "h_window_07", "h_window_08", "h_window_09",
        };
        static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            { "BCW_CoreWood_Roof26", "Ancient bark log roof 26°" }, { "BCW_CoreWood_Roof45", "Ancient bark log roof 45°" },
            { "BCW_CoreWood_RoofTop26", "Ancient bark log roof ridge 26°" }, { "BCW_CoreWood_RoofTop45", "Ancient bark log roof ridge 45°" },
            { "BCW_CoreWood_RoofOCorner26", "Ancient bark log roof o-corner 26°" }, { "BCW_CoreWood_RoofICorner26", "Ancient bark log roof i-corner 26°" },
            { "BCW_CoreWood_RoofOCorner45", "Ancient bark log roof o-corner 45°" }, { "BCW_CoreWood_RoofICorner45", "Ancient bark log roof i-corner 45°" },
            { "BCW_CoreWood_Gate", "Ancient bark log gate" }, { "BCW_CoreWood_FenceGate", "Ancient bark log fence gate" },
            { "BCW_CoreWood_TrapDoor", "Ancient bark log trapdoor" },
            { "BCW_CoreWood_DrawBridgeNarrow", "Ancient bark drawbridge (narrow)" }, { "BCW_CoreWood_DrawBridgeNarrowTall", "Ancient bark drawbridge (narrow, tall)" },
            { "BCW_CoreWood_DrawBridgeWide", "Ancient bark drawbridge (wide)" }, { "BCW_CoreWood_DrawBridgeWideTall", "Ancient bark drawbridge (wide, tall)" },
            { "BCW_CoreWood_WallRoof26", "Ancient bark log gable 26°" }, { "BCW_CoreWood_WallRoof45", "Ancient bark log gable 45°" },
            { "BCW_CoreWood_WallRoof_UpsideDown26", "Ancient bark log gable 26° (inverted)" }, { "BCW_CoreWood_WallRoof_UpsideDown45", "Ancient bark log gable 45° (inverted)" },
            { "BCW_CoreWood_WallRoofTop26", "Ancient bark log roof cross 26°" }, { "BCW_CoreWood_WallRoofTop45", "Ancient bark log roof cross 45°" },
            { "BCW_CoreWoodWall", "Ancient bark log wall" }, { "BCW_CoreWoodWall1", "Ancient bark log wall" },
            { "BCW_CoreWood_HalfWall", "Ancient bark log half wall" }, { "BCW_CoreWood_Floor", "Ancient bark log floor" },
            { "BCW_CoreWood_Stair", "Ancient bark log stair" }, { "BCW_CoreWood_Window1", "Ancient bark log window" }, { "BCW_CoreWood_WindowHalf", "Ancient bark log half window" },
            { "BCW_CoreWood_Fence1", "Ancient bark log fence" }, { "BCW_CoreWood_Fence2", "Ancient bark log fence 2" }, { "BCW_CoreWood_FencePillar", "Ancient bark log fence pillar" },
            { "BCW_CoreWood_PalisadeFence", "Ancient bark palisade" }, { "BCW_CoreWood_PalisadeFenceTall", "Ancient bark palisade (tall)" },
            { "darkwood_arch", "Ancient bark arch" }, { "darkwood_pole4", "Ancient bark pole 4m" }, { "darkwood_beam4x4", "Ancient bark beam 4m" },
            { "stake_wall", "Ancient bark stakewall" }, { "wood_fence", "Ancient bark roundpole fence" },
            { "h_window_01", "Ancient bark small window 1" }, { "h_window_02", "Ancient bark small window 2" }, { "h_window_03", "Ancient bark small window 3" },
            { "h_window_04", "Ancient bark small window 4" }, { "h_window_05", "Ancient bark small window 5" },
            { "h_window_06", "Ancient bark big window 1" }, { "h_window_07", "Ancient bark big window 2" },
            { "h_window_08", "Ancient bark big window 3" }, { "h_window_09", "Ancient bark big window 4" },
        };
        // CoreWoodPieces' own amounts (core wood + wood), paid in bark
        static readonly Dictionary<string, int> BcwCost = new Dictionary<string, int>
        {
            { "BCW_CoreWood_Gate", 8 }, { "BCW_CoreWood_FenceGate", 5 }, { "BCW_CoreWood_TrapDoor", 6 },
            { "BCW_CoreWood_WallRoof26", 3 }, { "BCW_CoreWood_WallRoof45", 3 }, { "BCW_CoreWood_WallRoof_UpsideDown26", 3 }, { "BCW_CoreWood_WallRoof_UpsideDown45", 3 },
            { "BCW_CoreWood_WallRoofTop26", 2 }, { "BCW_CoreWood_WallRoofTop45", 2 },
            { "BCW_CoreWoodWall", 3 }, { "BCW_CoreWoodWall1", 3 }, { "BCW_CoreWood_HalfWall", 2 }, { "BCW_CoreWood_Floor", 3 },
            { "BCW_CoreWood_Stair", 3 }, { "BCW_CoreWood_Window1", 6 }, { "BCW_CoreWood_WindowHalf", 4 },
            { "BCW_CoreWood_Fence1", 4 }, { "BCW_CoreWood_Fence2", 4 }, { "BCW_CoreWood_FencePillar", 1 }, { "BCW_CoreWood_PalisadeFence", 8 }, { "BCW_CoreWood_PalisadeFenceTall", 10 },
            { "BCW_CoreWood_DrawBridgeNarrow", 9 }, { "BCW_CoreWood_DrawBridgeNarrowTall", 11 }, { "BCW_CoreWood_DrawBridgeWide", 18 }, { "BCW_CoreWood_DrawBridgeWideTall", 21 },
        };
        // Everything is world-mapped, logs included: with the per-log atlas, logs placed side by side all showed the same moss.
        static readonly string[] PlankMats = { "woodwall", "woodpole", "door_wood", "logwall", "stake", "DarkWood_" };
        // Plains material on the darkwood originals; these pieces are meant to be buildable from the swamp.
        static readonly HashSet<string> Dropped = new HashSet<string> { "Tar" };
        static readonly HashSet<string> WoodItems = new HashSet<string> { "Wood", "RoundLog", "FineWood" };

        readonly Dictionary<Material, Material> swapped = new Dictionary<Material, Material>();
        Texture2D bark;
        Texture2D roofSlope, roofRidge;

        void Awake()
        {
            Log = Logger;
            PrefabManager.OnVanillaPrefabsAvailable += AddPieces;
        }

        void AddPieces()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddPieces;
            try
            {
                bark = LoadPng("bark.png");
                roofSlope = LoadPng("roofslope.png"); roofRidge = LoadPng("roofridge.png");
                int n = 0;
                foreach (var b in Bases) if (AddPiece(b)) n++;
                if (AddPiece("wood_pole_log", ShortPole, "Ancient bark log pole 1m", HalfHeight)) n++;
                Log.LogInfo("Registered " + n + " of " + (Bases.Length + 1) + " ancient bark pieces");
            }
            catch (Exception e) { Log.LogError("Ancient bark pieces failed: " + e); }
        }

        static string ItemName(Piece.Requirement r) => r.m_resItem.name.Replace("JVLmock_", "");
        static bool IsMoreGates(string baseName) => baseName.StartsWith("h_window_");

        // 1 m log pole: the 2 m log pole at half height. Snap points and collider scale with the root,
        // and the bark is world-mapped, so it keeps the same texel size.
        const string ShortPole = "wood_pole_log_1";
        static void HalfHeight(CustomPiece c)
        {
            var t = c.PiecePrefab.transform; var s = t.localScale; t.localScale = new Vector3(s.x, s.y * 0.5f, s.z);
            foreach (var r in c.PiecePrefab.GetComponent<Piece>().m_resources) r.m_amount = Math.Max(1, (r.m_amount + 1) / 2);
        }

        bool AddPiece(string baseName, string newName = null, string displayName = null, Action<CustomPiece> tweak = null)
        {
            newName = newName ?? baseName;
            var basePrefab = PrefabManager.Instance.GetPrefab(baseName);
            var basePiece = basePrefab ? basePrefab.GetComponent<Piece>() : null;
            if (basePiece == null)
            {
                if (baseName.StartsWith("BCW_")) Log.LogInfo(baseName + " not found (CoreWoodPieces not installed), skipped");
                else if (IsMoreGates(baseName)) Log.LogInfo(baseName + " not found (More Gates Extended not installed), skipped");
                else Log.LogWarning(baseName + " not found, skipped");
                return false;
            }

            var config = new PieceConfig
            {
                Name = displayName ?? (Names.TryGetValue(baseName, out var nm) ? nm : "Ancient bark " + basePiece.m_name), // the $token inside is still translated
                PieceTable = PieceTables.Hammer,
                // Pieces from Jotunn mods (More Gates) still hold Jotunn's "JVLmock_" placeholder items when we copy them.
                Requirements = basePiece.m_resources.Where(r => r.m_resItem != null && !Dropped.Contains(ItemName(r))).Select(r => new RequirementConfig(
                    WoodItems.Contains(ItemName(r)) ? "ElderBark" : ItemName(r), r.m_amount, 0, r.m_recover)).ToArray(),
            };
            if (baseName.StartsWith("BCW_"))
            {
                // CoreWoodPieces fills in cost, station and tab after we copy, so set them here: its own amounts
                // (core wood + wood) paid in bark, on the vanilla building tab.
                config.Requirements = new[] { new RequirementConfig("ElderBark", BcwCost.TryGetValue(baseName, out var cost) ? cost : baseName.Contains("Corner") ? 5 : 3, 0, true) };
                config.CraftingStation = CraftingStations.Workbench;
                config.Category = PieceCategories.Building;
            }
            var custom = new CustomPiece(Prefix + newName, baseName, config);
            var left = new List<string>();
            int count = Retexture(custom.PiecePrefab, left);
            if (count == 0)
            {
                Log.LogWarning(baseName + ": no wood material found (" + string.Join(", ", left) + "), skipped");
                PrefabManager.Instance.DestroyPrefab(Prefix + newName);
                return false;
            }
            MapRoofLogs(custom.PiecePrefab); // only touches pieces built from the "VerticalLog" mesh
            if (baseName.StartsWith("BCW_CoreWoodWall"))
            {
                var lg = custom.PiecePrefab.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(m => m.sharedMesh != null && m.sharedMesh.name.StartsWith("ablog_"));
                bool upright = lg != null && !lg.sharedMesh.name.StartsWith("ablog_r");
                custom.Piece.m_name = "Ancient bark log wall (" + (upright ? "vertical" : "horizontal") + ")";
            }
            tweak?.Invoke(custom);
            PieceManager.Instance.AddPiece(custom);
            var piece = custom.Piece;
            RenderManager.Instance.EnqueueRender(new RenderManager.RenderRequest(custom.PiecePrefab)
            {
                Rotation = RenderManager.IsometricRotation, Width = 64, Height = 64,
            }, sprite => { if (sprite != null && piece != null) piece.m_icon = sprite; });
            Log.LogInfo(Prefix + newName + ": " + string.Join(" + ", custom.Piece.m_resources.Select(r => r.m_amount + " " + (r.m_resItem ? ItemName(r) : "?")))
                + (left.Count > 0 ? "  (kept: " + string.Join(", ", left.Distinct()) + ")" : ""));
            return true;
        }

        int Retexture(GameObject prefab, List<string> left)
        {
            int count = 0;
            foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is MeshRenderer)) continue;
                var mats = r.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i]; if (m == null || m.shader == null) continue;
                    // More Gates' windows use Unity's Standard shader for their plank frames: give those the bark
                    // material made from vanilla's own wood wall, so they match the bark walls next to them.
                    if (m.shader.name == "Standard" && m.name.StartsWith("woodwall", StringComparison.OrdinalIgnoreCase)) m = VanillaWoodwall() ?? m;
                    if (m.shader.name != "Custom/Piece") continue;
                    if (!swapped.TryGetValue(m, out var nm)) swapped[m] = nm = MakeMaterial(m);
                    if (nm == null) { left.Add(m.name); continue; }
                    mats[i] = nm; changed = true; count++;
                }
                if (changed) r.sharedMaterials = mats;
            }
            return count;
        }

        Material vanillaWoodwall;
        Material VanillaWoodwall()
        {
            if (vanillaWoodwall == null)
            {
                var p = PrefabManager.Instance.GetPrefab("woodwall");
                vanillaWoodwall = p ? p.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials)
                    .FirstOrDefault(x => x != null && x.shader != null && x.shader.name == "Custom/Piece" && x.name.StartsWith("woodwall")) : null;
            }
            return vanillaWoodwall;
        }

        Material MakeMaterial(Material m)
        {
            bool plank = PlankMats.Any(x => m.name.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!plank) return null;
            var nm = new Material(m) { name = m.name + "_ancientbark" };
            // Planks: map the bark by world position so it runs across plank edges instead of repeating per plank.
            nm.SetTexture("_MainTex", bark);
            nm.SetFloat("_TriplanarMap", 1f); nm.SetFloat("_TriplanarScale", TriplanarScale);
            // The door leaf uses the same world mapping: its planks are stretched cubes, so mapping by local position distorts the bark.
            foreach (var kw in nm.shader.keywordSpace.keywordNames) if (kw.ToUpperInvariant().Contains("TRIPLANAR")) nm.EnableKeyword(kw);
            return nm;
        }

        // CoreWood's roof logs get their own wrap-around mapping instead of the world mapping, which smears on slopes.
        // Same bark, same pixel size (4 m per texture repeat). Slope logs read a texture whose moss thickens toward
        // the eave end; level (ridge) logs read a mossier one. Each log uses a different strip of the texture, and
        // RoofLogVariety re-picks the strips per placed piece so neighbouring pieces differ too.
        const int Variants = 8;
        const float Density = 0.25f;
        void MapRoofLogs(GameObject prefab)
        {
            int n = 0;
            foreach (var mf in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh = mf.sharedMesh; if (mesh == null || mesh.name != "VerticalLog" || !mesh.isReadable) continue;
                var r = mf.GetComponent<MeshRenderer>(); if (r == null) continue;
                var t = mf.transform;
                Vector3 dir = prefab.transform.InverseTransformDirection(t.TransformDirection(Vector3.forward));
                bool level = Mathf.Abs(dir.y) < 0.15f;
                // seam of the wrap goes on the side of the log that faces down
                Vector3 down = t.InverseTransformDirection(prefab.transform.TransformDirection(Vector3.down));
                int seam = Mathf.RoundToInt(Mathf.Atan2(down.y, down.x) * 8f / Mathf.PI);
                // vanilla's roundpole fence has its own, thinner mesh that is also called "VerticalLog": keep them apart
                if (!sourceIds.TryGetValue(mesh, out int sid)) sourceIds[mesh] = sid = sourceIds.Count;
                string key = "ablog_" + (level ? "r" : dir.y > 0f ? "u" : "d") + seam + "m" + sid;
                if (!RoofLogVariety.Table.TryGetValue(key, out var set))
                {
                    set = new Mesh[Variants];
                    for (int k = 0; k < Variants; k++) set[k] = WrapLog(mesh, key + "_" + k, level, dir.y > 0f, seam * Mathf.PI / 8f, k);
                    RoofLogVariety.Table[key] = set;
                }
                mf.sharedMesh = set[n++ % Variants];
                r.sharedMaterial = LogMaterial(r.sharedMaterial, level);
            }
            if (n > 0 && prefab.GetComponent<RoofLogVariety>() == null) prefab.AddComponent<RoofLogVariety>();
        }

        readonly Dictionary<Mesh, int> sourceIds = new Dictionary<Mesh, int>();
        readonly Dictionary<string, Material> logMats = new Dictionary<string, Material>();
        Material LogMaterial(Material current, bool level)
        {
            string key = current.name + (level ? "|r" : "|s");
            if (logMats.TryGetValue(key, out var nm)) return nm;
            nm = new Material(current) { name = current.name + (level ? "_ridge" : "_slope") };
            nm.SetTexture("_MainTex", level ? roofRidge : roofSlope);
            nm.SetTextureScale("_MainTex", Vector2.one); nm.SetTextureOffset("_MainTex", Vector2.zero);
            nm.SetFloat("_TriplanarMap", 0f);
            foreach (var kw in nm.shader.keywordSpace.keywordNames) if (kw.ToUpperInvariant().Contains("TRIPLANAR")) nm.DisableKeyword(kw);
            // the log relief map belongs to the old layout
            nm.SetTexture("_BumpMap", null); nm.DisableKeyword("_NORMALMAP");
            logMats[key] = nm; return nm;
        }

        static Mesh WrapLog(Mesh src, string name, bool level, bool lowIsMinZ, float seamAngle, int k)
        {
            var v = src.vertices; var nrm = src.normals; var tri = src.triangles; var b = src.bounds;
            var rnd = new System.Random(name.GetHashCode() * 31 + k);
            float u0 = (k + (float)rnd.NextDouble() * 0.6f) / Variants;
            float v0 = level ? (float)rnd.NextDouble() : 0.02f + (float)rnd.NextDouble() * 0.03f;
            float radius = (b.size.x + b.size.y) * 0.25f;
            var nv = new Vector3[tri.Length]; var nn = new Vector3[tri.Length]; var uv = new Vector2[tri.Length]; var nt = new int[tri.Length];
            var ang = new float[3];
            for (int i = 0; i < tri.Length; i += 3)
            {
                Vector3 a = v[tri[i]], bb = v[tri[i + 1]], c = v[tri[i + 2]];
                Vector3 fn = Vector3.Cross(bb - a, c - a).normalized;
                bool cap = Mathf.Abs(fn.z) > 0.9f;
                for (int j = 0; j < 3; j++)
                {
                    Vector3 p = v[tri[i + j]];
                    ang[j] = Mathf.Repeat(Mathf.Atan2(p.y - b.center.y, p.x - b.center.x) - seamAngle, 2f * Mathf.PI);
                }
                if (!cap && Mathf.Max(ang[0], Mathf.Max(ang[1], ang[2])) - Mathf.Min(ang[0], Mathf.Min(ang[1], ang[2])) > Mathf.PI)
                    for (int j = 0; j < 3; j++) if (ang[j] < Mathf.PI) ang[j] += 2f * Mathf.PI;
                for (int j = 0; j < 3; j++)
                {
                    int o = tri[i + j]; Vector3 p = v[o];
                    float along = lowIsMinZ ? p.z - b.min.z : b.max.z - p.z;
                    Vector2 t = cap
                        ? new Vector2(u0 + (p.x - b.center.x) * Density, v0 + (level ? 0.3f : 0.02f) + (p.y - b.min.y) * Density)
                        : new Vector2(u0 + ang[j] * radius * Density, v0 + along * Density);
                    nv[i + j] = p; nn[i + j] = nrm.Length == v.Length ? nrm[o] : fn; uv[i + j] = t; nt[i + j] = i + j;
                }
            }
            var m = new Mesh { name = name };
            m.vertices = nv; m.normals = nn; m.uv = uv; m.triangles = nt; m.RecalculateBounds(); m.RecalculateTangents();
            return m;
        }

        static Texture2D LoadPng(string resource)
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            using (var ms = new MemoryStream())
            {
                s.CopyTo(ms);
                var t = new Texture2D(2, 2, TextureFormat.RGBA32, true) { name = "ancientbark_" + Path.GetFileNameWithoutExtension(resource) };
                t.LoadImage(ms.ToArray()); t.wrapMode = TextureWrapMode.Repeat; t.anisoLevel = 8; t.filterMode = FilterMode.Bilinear; t.Apply(true, true);
                return t;
            }
        }
    }

    // On every placed roof piece, give each log a texture strip chosen from where the piece stands,
    // so two pieces next to each other do not show the same logs.
    public class RoofLogVariety : MonoBehaviour
    {
        public static readonly Dictionary<string, Mesh[]> Table = new Dictionary<string, Mesh[]>();
        void Awake()
        {
            Vector3 p = transform.position;
            int h = Mathf.RoundToInt(p.x * 2f) * 73856093 ^ Mathf.RoundToInt(p.y * 2f) * 19349663 ^ Mathf.RoundToInt(p.z * 2f) * 83492791;
            int i = 0;
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
            {
                var m = mf.sharedMesh; if (m == null || !m.name.StartsWith("ablog_")) continue;
                string key = m.name.Substring(0, m.name.LastIndexOf('_'));
                // "New" and "Worn" hold the same logs in the same order; restart the count so they match
                if (mf.transform.GetSiblingIndex() == 0) i = 0;
                if (Table.TryGetValue(key, out var set)) mf.sharedMesh = set[(int)((uint)(h + i * 3) % (uint)set.Length)];
                i++;
            }
        }
    }
}
