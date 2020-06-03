using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    public ParticleSystem _DummyParticleSystem;
    public ParticleSystem _MovementParticleSystem;
    public ParticleSystem _CollisionParticleSystem;
    private ParticleSystem.Particle[] _Particles;
    private ParticleSystem.Particle[] _TempParticle;

    private Granulator _Granulator;

    private List<ParticleCollisionEvent> _CollisionEvents;

    public enum ParticleGroup { Trigger, Movement, Collision };

    private bool _Gravity;
    private bool _Collisions;

    //[System.Obsolete]
    private void Awake()
    {
        _Particles = new ParticleSystem.Particle[_MovementParticleSystem.main.maxParticles];
        _TempParticle = new ParticleSystem.Particle[1];
    }

    void Start()
    {
    }

    void Update()
    {
    }

    public void Initialise(Granulator granulator)
    {
        _Granulator = granulator;
    }

    // NOT IMPLEMENTED YET
    public void SetGravity (bool gravity)
    {
        _Gravity = gravity;
    }

    // NOT IMPLEMENTED YET
    public void SetCollisions(bool collisions)
    {
        _Collisions = collisions;
    }

    // Main collide passthrough function for Granulator
    public void Collide(GameObject other, List<ParticleCollisionEvent> collisions)
    {
        _Granulator.TriggerCollision(collisions, other);
        foreach (ParticleCollisionEvent collision in collisions)
        {
            SpawnCollisionParticle(collision.intersection, collision.velocity, _Granulator._GrainDuration);
        } 
    }

    // Particle spawning for movement system, which is more random
    public ParticleSystem.Particle SpawnMovementParticle(Vector3 inheritVelocity, float startSpeed, float life)
    {
        ParticleSystem.MainModule main = _DummyParticleSystem.main;
        // Emit particle in the dummy system, get its values, then kill the particle
        main.startSpeed = startSpeed;
        _DummyParticleSystem.Emit(1);
        _DummyParticleSystem.GetParticles(_TempParticle);
        _DummyParticleSystem.Clear();

        // Generate new emit params based on dummy particle and life passed into the function
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.position = _TempParticle[0].position;
        emitParams.velocity = _TempParticle[0].velocity + inheritVelocity;
        emitParams.startLifetime = life;

        // Emit particle in main particle system based on those values
        _MovementParticleSystem.Emit(emitParams, 1);

        // Return the particle to granulator function
        return _TempParticle[0];
    }

    // Particle spawning for collision system, which is not random
    // TO DO: NEED TO FIX COLLISION NORMAL DIRECTION!!!
    public void SpawnCollisionParticle(Vector3 position, Vector3 velocity, float life)
    {
        // Generate new emit params based on dummy particle and life passed into the function
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();
        emitParams.position = position;
        emitParams.velocity = velocity;
        emitParams.startLifetime = life;

        // Emit particle in main particle system based on those values
        _CollisionParticleSystem.Emit(emitParams, 1);
    }

    // NOT IMPLEMENTED
    public ParticleSystem.Particle[] GetParticles(ParticleGroup particleGroup)
    {
        ParticleSystem.Particle[] returnParticleSystem = null;

        if (particleGroup == ParticleGroup.Collision)
        {
            _CollisionParticleSystem.GetParticles(returnParticleSystem);
        }
        else if (particleGroup == ParticleGroup.Movement)
        {
            _MovementParticleSystem.GetParticles(returnParticleSystem);
        }
        else if (particleGroup == ParticleGroup.Trigger)
        {
            _DummyParticleSystem.GetParticles(returnParticleSystem);
        }

        return returnParticleSystem;
    }

    public ParticleSystem.Particle GetRandomMovementParticle()
    {
        // Get all particles, then return a random one based on the current count
        _MovementParticleSystem.GetParticles(_Particles);
        ParticleSystem.Particle particle = _Particles[(int)(Random.value * _MovementParticleSystem.particleCount)];

        return particle;
    }

    public int GetMaxParticles()
    {
        return _MovementParticleSystem.main.maxParticles;
    }

    public int GetParticleCount()
    {
        return _MovementParticleSystem.particleCount;
    }
}
