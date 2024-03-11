using System;
using UnityEngine;

[Serializable]
public class DeformationSettings
{
    [Header("World Space Settings")]
    [Tooltip("The world space position offset for where deformation will occur.")]
    public Vector3 WorldOffset;

    [Tooltip("The center point of deformation in world space.")]
    public Vector3 DeformationCenterPoint;

    [Tooltip("The center point of deformation in local space relative to the deformed object.")]
    public Vector3 LocalDeformationPoint;

    [Header("Deformation Parameters")]
    [Tooltip("The height impact of the deformation.")] [Range(-100f, 100f)]
    public float HeightImpact;

    [Tooltip("The width impact of the deformation.")] [Range(0f, 100f)]
    public float WidthImpact;

    [Tooltip("The radius of the flat top of the deformation.")] [Range(0f, 100f)]
    public float FlatTopRadius;

    [Tooltip("The degree of steepness for the deformation.")] [Range(0f, 100f)]
    public float DegreeStepness;

    [Tooltip("A factor to control the smoothing of the deformation.")] [Range(0f, 1f)]
    public float SmoothingFactor;

    [Tooltip("Enable Hermite smoothing for the deformation.")]
    public bool UseHermiteSmoothing;

    [Tooltip("Enable the use of degree steepness for the deformation.")]
    public bool UseDegreeStepness;

    [Header("Mesh Collider Settings")]
    [Tooltip("The offset for the MeshCollider.")]
    public Vector3 MeshColliderOffset;

    [Tooltip("Set the MeshCollider as convex.")]
    public bool SetConvex;
}