﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public ParticleSystem _DummyParticleSystem;
    public ParticleSystem _ParticleSystem;
    private ParticleSystem.Particle[] _Particles;
    private ParticleSystem.Particle[] _DummyParticle;

    [System.Obsolete]
    private void Awake()
    {
        _Particles = new ParticleSystem.Particle[_ParticleSystem.main.maxParticles];
        _DummyParticle = new ParticleSystem.Particle[1];
    }

    void Start()
    {
        
    }

    void Update()
    {
    }

    public void SpawnParticle(Vector3 position, Vector3 velocity)
    {
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();

        emitParams.position = position;
        emitParams.velocity = velocity;

        _ParticleSystem.Emit(emitParams, 1);
    }
    public ParticleSystem.Particle SpawnRandomParticle(float life)
    {
        // Emit particle in the dummy system, get its values, then kill the particle
        _DummyParticleSystem.Emit(1);
        _DummyParticleSystem.GetParticles(_DummyParticle);
        _DummyParticleSystem.Clear();

        // Get the dummy particle's position and velocity
        Vector3 position = _DummyParticle[0].position;
        Vector3 velocity = _DummyParticle[0].velocity;

        // Generate new emit params based on those values and life passed into the function
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.position = position;
        emitParams.velocity = velocity;
        //emitParams.startLifetime = life / 1000;
        emitParams.startLifetime = 1;

        // Emit particle in main particle system based on those values
        _ParticleSystem.Emit(emitParams, 1);

        // Return the particle to granulator function
        return _DummyParticle[0];
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
