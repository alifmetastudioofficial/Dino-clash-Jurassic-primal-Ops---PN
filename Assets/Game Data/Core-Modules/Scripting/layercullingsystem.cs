using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//[ExecuteInEditMode]
public class layercullingsystem : MonoBehaviour
{
    public static layercullingsystem instance = new layercullingsystem();

    public bool useUpdate = true;

    [Space(10)]
    public float[] distances = new float[32];

    public Camera mainCamera;

    // The Layers that have zero values doesn't do any cull
    void Start()
    {
        SetCamera(mainCamera);
       // mainCamera.farClipPlane = 1000;
        // mainCamera = Camera.main;
        // mainCamera.layerCullDistances = distances;
        instance = this;
    }

    // Prevent to update de camera cull distances every frame!!!
    void Update()
    {

        if (!useUpdate) { return; }
        mainCamera.layerCullDistances = distances;
    }

    public void SetCamera(Camera Cam)
    {
        mainCamera = Cam;
        mainCamera.layerCullDistances = distances;
    }

}