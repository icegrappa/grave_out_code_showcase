using System.Collections;
using UnityEngine;

public class HoleTrigger : MonoBehaviour
{
    [SerializeField] protected LayerMask deformerLayer;
    [SerializeField] public float deepChangeSpeed = 1f;
    [SerializeField] public float howDeepDig = 0.1f;
    [SerializeField] public float minYScaleThreshold = -6f;

    private bool isAdjustingScale = false; // Flag to indicate if the scale is currently being adjusted

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object entering the trigger is on the specified layer
        if ((deformerLayer.value & (1 << other.gameObject.layer)) != 0 && !isAdjustingScale)
        {
            StartCoroutine(AdjustScale(transform));
            Debug.Log($"Triggered by: {other.gameObject.name}");
        }
    }
    private IEnumerator AdjustScale(Transform target)
{
    isAdjustingScale = true; // Set the flag to true as we start adjusting the scale

    // Calculate the target scale with a check to not go below the threshold
    float newTargetYScale = Mathf.Max(target.localScale.y - howDeepDig, minYScaleThreshold);
    Vector3 targetScale = new Vector3(target.localScale.x, newTargetYScale, target.localScale.z);

    // Interpolate scale towards the target scale smoothly
    while (Vector3.Distance(target.localScale, targetScale) > 0.01f)
    {
        target.localScale = Vector3.Lerp(target.localScale, targetScale, Time.deltaTime * deepChangeSpeed);
        yield return null;
    }

    // Set the final scale to make sure it is exactly at the target scale, avoiding floating point imprecision
    target.localScale = targetScale;

    isAdjustingScale = false; // Reset the flag as we have finished adjusting the scale
}

}
