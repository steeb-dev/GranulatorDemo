using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public ParticleSystem _ParticleSystem;
    private ParticleSystem.Particle[] _Particles;

    [System.Obsolete]
    private void Awake()
    {
        _Particles = new ParticleSystem.Particle[_ParticleSystem.main.maxParticles];
    }

    void Start()
    {
        
    }

    void Update()
    {
    }


    public ParticleSystem.Particle[] GetParticles()
    {
        _ParticleSystem.GetParticles(_Particles);
        return _Particles;
    }

    public ParticleSystem.Particle GetRandomParticle()
    {
        // Get all particles, then return a random one based on the current count
        _ParticleSystem.GetParticles(_Particles);
        ParticleSystem.Particle particle = _Particles[(int)(Random.value * _ParticleSystem.particleCount)];

        return particle;
    }

    public int GetMaxParticles()
    {
        return _ParticleSystem.main.maxParticles;
    }

    public int GetParticleCount()
    {
        return _ParticleSystem.particleCount;
    }
}
