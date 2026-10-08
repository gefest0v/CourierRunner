using UnityEngine;

namespace CourierRunner
{
    /// <summary>
    /// Puts Crystal Maiden's staff in her hand. In the exported model the staff is a static mesh under the
    /// wrist, while the idle animation moves the dedicated staff bone (staff2). The mesh is re-parented to
    /// that bone in the bind pose, and after each animation update it is slid so its shaft runs through
    /// her left hand.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class CrystalMaidenPose : MonoBehaviour
    {
        private Transform staffMesh;
        private Transform hand;
        private Vector3 localAxis;
        private Vector3 localCenter;
        private bool bound;

        private void Awake() => Bind();

        /// <summary>Must run while the model is still in its bind pose (before the first animation update).</summary>
        public void Bind()
        {
            if (bound) return;
            Transform staffBone = null;
            foreach (Transform item in GetComponentsInChildren<Transform>(true))
            {
                if (item.name == "staff2") staffBone = item;
                else if (item.name == "wrist_L") hand = item;
                else if (item.name.Contains("crystal_maiden_staff") && item.GetComponent<MeshRenderer>() != null) staffMesh = item;
            }
            if (staffMesh == null || staffBone == null || hand == null) return;

            // The shaft is the mesh's longest dimension.
            Bounds mesh = staffMesh.GetComponent<MeshFilter>().sharedMesh.bounds;
            Vector3 size = mesh.size;
            localAxis = size.x >= size.y && size.x >= size.z ? Vector3.right : size.y >= size.z ? Vector3.up : Vector3.forward;
            localCenter = mesh.center;
            staffMesh.SetParent(staffBone, true);
            bound = true;
        }

        private void LateUpdate() => Apply();

        /// <summary>Moves the staff sideways (not along its length) so the shaft passes through the hand.</summary>
        public void Apply()
        {
            if (!bound) return;
            Vector3 axis = staffMesh.TransformDirection(localAxis).normalized;
            Vector3 center = staffMesh.TransformPoint(localCenter);
            Vector3 grip = hand.position;
            Vector3 closest = center + axis * Vector3.Dot(grip - center, axis);
            staffMesh.position += grip - closest;
        }
    }
}
