using System.Collections.Generic;
using UnityEngine;

namespace CourierRunner
{
    /// <summary>How much of a segment is simulated, by distance ahead of the courier (GAME_DESIGN.md §4).</summary>
    public enum TrackZone
    {
        /// <summary>Not placed yet or already recycled.</summary>
        Hidden,
        /// <summary>Far ahead: only decoration is shown, gameplay objects are inactive.</summary>
        Far,
        /// <summary>Visible gameplay objects, colliders off.</summary>
        Mid,
        /// <summary>Fully active: colliders, towers and animations.</summary>
        Near,
    }

    /// <summary>
    /// One reusable piece of road. The ground is built once; decoration and gameplay objects
    /// come from pools and are handed back when the segment is recycled further ahead.
    /// </summary>
    public sealed class TrackSegment : MonoBehaviour
    {
        public int Index { get; private set; }
        public float StartZ { get; private set; }
        public float Length { get; private set; }
        public float EndZ => StartZ + Length;
        public SegmentTheme Theme { get; private set; }
        public ThemeProfile Profile { get; private set; }
        public TrackZone Zone { get; private set; } = TrackZone.Hidden;

        /// <summary>Scenery and other objects that ignore the activation zones.</summary>
        public Transform Decor { get; private set; }
        /// <summary>Coins, obstacles and pickups; toggled by <see cref="SetZone"/>.</summary>
        public Transform Gameplay { get; private set; }

        private readonly List<Collider> colliders = new();

        public static TrackSegment Create(Transform parent, float length, Material grass, Material path)
        {
            var root = new GameObject("Segment");
            root.transform.SetParent(parent, false);
            TrackSegment segment = root.AddComponent<TrackSegment>();
            segment.Length = length;
            segment.BuildGround(grass, path);
            segment.Decor = new GameObject("Decor").transform;
            segment.Decor.SetParent(root.transform, false);
            segment.Gameplay = new GameObject("Gameplay").transform;
            segment.Gameplay.SetParent(root.transform, false);
            root.SetActive(false);
            return segment;
        }

        private void BuildGround(Material grass, Material path)
        {
            // Both cubes span exactly [0, Length] in local Z so neighbouring segments meet edge to edge.
            CreateGroundPiece("Forest Grass", new Vector3(0f, -0.34f, Length * 0.5f), new Vector3(80f, 0.55f, Length), grass);
            CreateGroundPiece("Forest Path", new Vector3(0f, -0.255f, Length * 0.5f), new Vector3(10.2f, 0.5f, Length), path);
        }

        private void CreateGroundPiece(string pieceName, Vector3 position, Vector3 scale, Material material)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = pieceName;
            piece.transform.SetParent(transform, false);
            piece.transform.localPosition = position;
            piece.transform.localScale = scale;
            if (material != null) piece.GetComponent<Renderer>().sharedMaterial = material;
            // The courier runs on the ground collider; keep it, but it must never be a trigger.
            piece.GetComponent<Collider>().isTrigger = false;
        }

        public void Place(int index, float startZ, SegmentTheme theme)
        {
            Index = index;
            StartZ = startZ;
            Theme = theme;
            Profile = ThemeProfile.For(theme);
            name = $"Segment {index} ({theme})";
            transform.localPosition = new Vector3(0f, 0f, startZ);
            gameObject.SetActive(true);
            Zone = TrackZone.Hidden;
        }

        /// <summary>Returns every pooled child to its pool and destroys one-off objects.</summary>
        public void Clear()
        {
            ReleaseChildren(Decor);
            ReleaseChildren(Gameplay);
            Gameplay.gameObject.SetActive(true);
            Zone = TrackZone.Hidden;
            gameObject.SetActive(false);
        }

        private static void ReleaseChildren(Transform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Transform child = container.GetChild(i);
                PooledObject pooled = child.GetComponent<PooledObject>();
                if (pooled != null)
                {
                    pooled.Release();
                }
                else
                {
                    child.SetParent(null, false);
                    Destroy(child.gameObject);
                }
            }
        }

        /// <summary>Converts a lane (-1, 0, 1) and a distance into the segment into a local position.</summary>
        public Vector3 LanePoint(int lane, float z, float y = 0f) =>
            new(lane * GameConfig.Instance.laneWidth, y, z);

        public void SetZone(TrackZone zone, bool force = false)
        {
            if (!force && zone == Zone) return;
            Zone = zone;
            bool visible = zone == TrackZone.Mid || zone == TrackZone.Near;
            if (Gameplay.gameObject.activeSelf != visible) Gameplay.gameObject.SetActive(visible);
            if (!visible) return;
            Gameplay.GetComponentsInChildren(true, colliders);
            bool solid = zone == TrackZone.Near;
            foreach (Collider item in colliders) item.enabled = solid;
        }
    }
}
