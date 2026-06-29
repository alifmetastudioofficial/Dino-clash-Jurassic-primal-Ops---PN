using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class watereffects : MonoBehaviour
{
    [Tooltip("Particle effect to use for water splashes.")]
    public GameObject waterSplash;
    [Tooltip("Particles emitted around player treading water.")]
    public ParticleSystem rippleEffect;
    [Tooltip("Particles emitted underwater for ambient bubbles/particles.")]
    public ParticleSystem bubblesEffect;
    [Tooltip("Particles to emit when player is swimming on water surface.")]
    public ParticleSystem splashTrail;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
