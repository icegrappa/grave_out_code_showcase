using Unity.Burst;
using UnityEngine;

namespace MeshDeformation {
	public static class DeformerUtilities { [BurstCompile]
		public static float CalculateDisplacement(Vector3 position, float time, float speed, float amplitude) {
			var distance = 6f - Vector3.Distance(position, Vector3.zero);
			return Mathf.Sin(time * speed + distance) * amplitude;
		}

		[BurstCompile]
	public static float ProvideDeformImpactCalculations(Vector3 localPosition, Vector3 deformationCenterPoint,
    float deformHeightImpact, float deformWidthImpact, float deformFlatTopRadius, 
    float deformSmoothingFactor, bool useDegreeSteepness = false, 
    float? deformDegreeSteepness = null, bool useHermiteSmoothing = false) 
{
    float distanceToCenter = Vector3.Distance(localPosition, deformationCenterPoint);

    // Check if within the flat top radius
    if (distanceToCenter <= deformFlatTopRadius) {
        return deformationCenterPoint.y + deformHeightImpact;
    }
    // Check if within the deformable width impact
    else if (distanceToCenter <= deformWidthImpact) {
        float slopeFactor = (distanceToCenter - deformFlatTopRadius) / (deformWidthImpact - deformFlatTopRadius);
        slopeFactor = 1f - slopeFactor;

        // Apply Hermite smoothing if enabled
        slopeFactor = useHermiteSmoothing ? SmoothStep(slopeFactor) : Mathf.Pow(slopeFactor, deformSmoothingFactor);

        float smoothedHeight = Mathf.Lerp(localPosition.y, deformationCenterPoint.y + deformHeightImpact, slopeFactor);

        // Apply maximum steepness constraint if enabled
        if (useDegreeSteepness && deformDegreeSteepness.HasValue) {
            float maxSteepness = Mathf.Tan(deformDegreeSteepness.Value * Mathf.Deg2Rad);
            float slope = smoothedHeight / distanceToCenter;
            if (slope < -maxSteepness) {
                smoothedHeight = -maxSteepness * distanceToCenter;
            }
        }

        return smoothedHeight;
    }
     return localPosition.y;
}


		private static float SmoothStep(float x) {
			return x * x * (3 - 2 * x); // Hermite interpolation (smoothstep) for x in [0, 1]
		}

	}
}