using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FIMSpace.Generating
{
    public class LayerChecker : PGGGeneratorBase
    {
        public delegate void NotifyReadyForGeneration(GameObject obj);

        public NotifyReadyForGeneration OnReadyForGeneration;
        public LayerMask raycastLayerMaskIgnore; // Layer mask for raycasting
        public LayerMask raycastLayerMaskCollide; // Layer mask for raycasting
        public float raycastLength = 10.0f; // Raycast length
        public Vector3 offsetRaycastOrigin = Vector3.up * 0.5f; // Offset for raycast origin
        public Vector3 raycastDirection = Vector3.up; // Raycast direction
        public Vector3 cachedPosition;
        private Ray ray1;
        private Ray ray2;

        public QueryTriggerInteraction ignoreQuery = QueryTriggerInteraction.Ignore;
        public QueryTriggerInteraction collideQuery = QueryTriggerInteraction.Collide;

        [Space(3)] public List<CellPreset> cellPresets = new();

        public void SetGuides(List<SpawnInstruction> guides)
        {
            this.guides = guides;
        }

        private List<SpawnInstruction> guides;

        public override FGenGraph<FieldCell, FGenPoint> PGG_Grid => null;
        public override FieldSetup PGG_Setup => null;

        private RaycastHit hit1;
        private RaycastHit hit2;

        public bool _FieldSpawned;

        private void Awake()
        {
            CellManager.Instance.AddChecker(this);
        }

        protected override void Start()
        {
            base.Start();
            cachedPosition = transform.position;
            ray1 = new Ray(cachedPosition + offsetRaycastOrigin, raycastDirection);
            ray2 = new Ray(cachedPosition + new Vector3(0, -1, 0), Vector3.up);
        }

        public bool RaycastWithParameters(Ray ray, out RaycastHit hit, float maxDistance, LayerMask layerMask,
            QueryTriggerInteraction queryTriggerInteraction)
        {
            return Physics.Raycast(ray, out hit, maxDistance, layerMask, queryTriggerInteraction);
        }

        public IEnumerator RaycastCoroutine()
        {
            var hitSomething1 =
                RaycastWithParameters(ray1, out hit1, raycastLength, raycastLayerMaskIgnore, ignoreQuery);

            // Early return if the first raycast doesn't hit anything
            if (!hitSomething1) yield break;


            var hitSomething2 =
                RaycastWithParameters(ray2, out hit2, raycastLength, raycastLayerMaskCollide, collideQuery);


            // Early return if the second raycast doesn't hit anything
            if (!hitSomething2) yield break;
        }


        public void UpdateDeformationSettingsWorldOffset(CellPreset cellPreset)
        {
            if (cellPreset == null || !cellPreset.provideDeformation || cellPreset.deformationSettings == null)
            {
                Debug.LogWarning("Invalid or non-deformable cellPreset provided.");
                return;
            }

            // Get the child's local position within its parent
            var localSpacePosition = transform.localPosition;

            // Calculate the world position based on the parent's world position and local position
            var worldPosition = transform.parent.TransformPoint(localSpacePosition);

            // Update the WorldOffset of the deformationSettings
            cellPreset.deformationSettings.WorldOffset = worldPosition;
        }


        public RaycastHit GetIgnoreHit()
        {
            return hit1;
        }

        public RaycastHit GetColideHit()
        {
            return hit2;
        }

        public Ray RetriveWaterRay()
        {
            return ray2;
        }


        public void GenerateObjects(FieldSetup fieldSetup, CellPreset cellPreset)
        {
            if (fieldSetup == null)
            {
                Debug.LogError("FieldSetup is null. Can't generate objects without FieldSetup!");
                return;
            }

            if (cellPreset == null)
            {
                Debug.LogError("DistancePreset is null. Can't generate objects without DistancePreset!");
                return;
            }

            //Prepare(); // Prepare seed
            //FGenerators.SetSeed(Seed);
            ClearGenerated(); // Cleaning previous generated objects for re-generating

            var origin = Vector3Int.zero;
            var fieldSizeInCells = cellPreset.FieldSizeInCells;

            if (cellPreset.CenterOrigin) origin = new Vector3Int(-fieldSizeInCells.x / 2, 0, -fieldSizeInCells.z / 2);

            Generated.Add(IGeneration.GenerateFieldObjectsRectangleGrid(fieldSetup, fieldSizeInCells, Seed, transform,
                true, guides, true, origin));


            base.GenerateObjects(); // Triggering event if assigned
        }

        public virtual void MarkGenerationProcess()
        {
            // Notify CellContainer that generation is completed for this GameObject
            OnReadyForGeneration?.Invoke(gameObject);
        }
    }

    #region Editor Inspector Window

#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(LayerChecker))]
    public class LayerCheckerEditor : PGGGeneratorBaseEditor
    {
        protected override void DrawGUIBeforeDefaultInspector()
        {
            GUILayout.Space(3);
            base.DrawGUIBeforeDefaultInspector();
        }
    }
#endif

    #endregion
}