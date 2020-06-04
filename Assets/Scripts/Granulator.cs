using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Granulator : MonoBehaviour
{
    public bool _IsPlaying = true;       // the on/off button

    public ParticleManager _ParticleManager;
    private ParticleSystem.Particle _TempParticle;
    private ParticleSystem.Particle[] _Particles;

    public GameObject _GrainObjectHolder;
    public GameObject _GrainPrefab;


    public int _MaxGrains = 100;

    [Range(1.0f, 1000f)]
    public int _TimeBetweenGrains = 20;          // ms
    [Range(0.0f, 1000f)]
    public int _TimeBetweenGrainsRandom = 0;       // ms
    [Range(0.1f, 5f)]
    public float _GrainPitch = 1;
    [Range(0.0f, 1f)]
    public float _GrainPitchRandom = 0;
    [Range(0.0f, 2.0f)]
    public float _GrainVolume = 1;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _GrainVolumeRandom = 0;      // from 0 > 1


    public AudioClip _MovementAudioClip;
    [Range(0.0f, 1.0f)]
    public float _GrainPosition = 0;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _GrainPositionRandom = 0;      // from 0 > 1
    [Range(2.0f, 3000f)]
    public int _GrainDuration = 300;       // ms
    [Range(0.0f, 1000f)]
    public int _GrainDurationRandom = 0;     // ms

    public AudioClip _CollisionAudioClip;
    [Range(0.0f, 1.0f)]
    public float _CollisionGrainPosition = 0;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _CollisionPositionRandom = 0;      // from 0 > 1
    [Range(2.0f, 3000f)]
    public int _CollisionDuration = 300;       // ms
    [Range(0.0f, 1000f)]
    public int _CollisionDurationRandom = 0;     // ms

    public int _CollisionGrainBurst = 5;

    public enum ParticleMode { Spawning, Static };
    public ParticleMode _ParticleMode = ParticleMode.Spawning;

    
    // Temp vars
    private float _NewGrainPosition = 0;
    private float _NewCollisionGrainPosition = 0;
    private float _NewGrainPitch = 0;
    private float _NewGrainPitchRandom = 0;
    private int _NewGrainDuration = 0;
    private int _NewCollisionDuration;
    private int _NewGrainDensity = 0;
    private float _NewGrainVolume = 0;

    private List<int> _GrainTriggerList;
    private int _SamplesSinceLastGrain;
    private int _GrainsLastUpdate = 0;
    private int _SamplesLastUpdate = 0;

    private Rigidbody _RigidBody;
    private Vector3 _ParticleSynthVelocity;

    public bool _MoveGrains = true;
    public bool _Gravity = false;
    [Range(0.0f, 5f)]
    public float _Mass = 1;
    [Range(0.0f, 30.0f)]
    public float _GrainSpeedOnBirth = 5.0f;
    public bool _Collisions = false;

    private bool _GravityPrevious;
    private float _MassPrevious;
    private bool _CollisionsPrevious;

    public float _KeyboardForce = 1;

    private List<GameObject> _GrainObjects;


    private const int _SampleRate = 44100;


    //---------------------------------------------------------------------
    private void Start()
    {
        _ParticleManager.Initialise(this);

        this.gameObject.AddComponent<AudioSource>();
        _RigidBody = this.GetComponent<Rigidbody>();
        _GrainObjects = new List<GameObject>();
        _GrainTriggerList = new List<int>();
        _SamplesSinceLastGrain = 0;
        GenerateGrainValues();
    }

    //---------------------------------------------------------------------
    void Awake()
    {
    }


    void Update()
    {
        //---------------------------------------------------------------------
        // UPDATE MAINTAINANCE
        //---------------------------------------------------------------------

        _SamplesLastUpdate = (int)(Time.deltaTime * _SampleRate);

        // Clamp values to reasonable ranges
        _GrainPosition = Clamp(_GrainPosition, 0, 1);
        _GrainPositionRandom = Clamp(_GrainPositionRandom, 0, 1);
        _TimeBetweenGrains = (int)Clamp(_TimeBetweenGrains, 1, 1000);
        _TimeBetweenGrainsRandom = (int)Clamp(_TimeBetweenGrainsRandom, 0, 1000);
        _GrainDurationRandom = (int)Clamp(_GrainDurationRandom, 0, 1000);

        _GrainPitch = Clamp(_GrainPitch, 0, 1000);
        _GrainPitchRandom = Clamp(_GrainPitchRandom, 0, 1000);
        _GrainVolume = Clamp(_GrainVolume, 0, 2);
        _GrainVolumeRandom = Clamp(_GrainVolumeRandom, 0, 1);

        _NewGrainDensity = _TimeBetweenGrains + Random.Range(0, _TimeBetweenGrainsRandom);
        _ParticleSynthVelocity = _RigidBody.velocity * 0.5f;


        // Check for updates from UI
        if (_Gravity != _GravityPrevious || _Mass != _MassPrevious)
        {
            _GravityPrevious = _Gravity;
            _MassPrevious = _Mass;
            _ParticleManager.SetGravity(_Gravity, _Mass);
        }


        // Clear finished grains
        if (_GrainObjects != null)
        {
            for (int i = _GrainObjects.Count - 1; i >= 0; i--)
            {
                if (_GrainObjects[i] != null && !_GrainObjects[i].GetComponent<Grain>()._IsPlaying)
                {
                    Destroy(_GrainObjects[i]);
                    _GrainObjects.RemoveAt(i);
                }
            }
        }


        //---------------------------------------------------------------------
        // EMITTER GRAIN GENERATION
        //---------------------------------------------------------------------
        _GrainTriggerList.Clear();

        int grainsThisUpdate = 0;
        int firstGrainOffset = 0;
        int densityInSamples = _NewGrainDensity * (_SampleRate / 1000);

        // If no sample was played last update, adding the previous update's time in samples
        // AFTER the update is complete should correctly accumulate the samples since the
        // last grain playback. Otherwise, if a sample WAS played last update, the sample
        // offset of that grain is subtracted from the total samples of the previous update.

        if (_GrainsLastUpdate == 0)
            _SamplesSinceLastGrain += _SamplesLastUpdate;
        else
            _SamplesSinceLastGrain = _SamplesLastUpdate - _SamplesSinceLastGrain;

        // If the density of grains minus samples since last grain fits within the
        // estimated time for the this update, calculate number of grains to play this update
        if (densityInSamples - _SamplesSinceLastGrain < _SamplesLastUpdate)
        {
            // Should always equal one or more
            grainsThisUpdate = (int)(_SamplesLastUpdate / densityInSamples) + 1;
            
            // Create initial grain offset for this update
            firstGrainOffset = densityInSamples - _SamplesSinceLastGrain;
            
            if (firstGrainOffset < 0)
                firstGrainOffset = 0;
        }

        _GrainsLastUpdate = grainsThisUpdate;

        // Iterate through grains for this update, and create grain triggers using their
        // calculated sample offset value
        for (int i = 0; i < grainsThisUpdate; i++)
        {
            _GrainTriggerList.Add(firstGrainOffset + i * densityInSamples);
        }

        // If a grain is going to be played this update, set the samples since last grain
        // counter to the sample offset value of the grain to be played last
        if (_GrainTriggerList.Count > 0)
            _SamplesSinceLastGrain = _GrainTriggerList[_GrainTriggerList.Count - 1];

        // NOTE: If no grains are played, the time that the current update takes
        // (determined next update) will be added to this counter.



        //---------------------------------------------------------------------
        // CREATE EMITTER GRAINS
        //---------------------------------------------------------------------
        foreach (int offset in _GrainTriggerList)
            CreateEmitterGrain(offset);


        //---------------------------------------------------------------------
        // INTERACTION KEYS
        //---------------------------------------------------------------------
        if (Input.GetKey(KeyCode.W))
            _RigidBody.AddForce(0, 0, _KeyboardForce);
        if (Input.GetKey(KeyCode.A))
            _RigidBody.AddForce(-_KeyboardForce, 0, 0);
        if (Input.GetKey(KeyCode.S))
            _RigidBody.AddForce(0, 0, -_KeyboardForce);
        if (Input.GetKey(KeyCode.D))
            _RigidBody.AddForce(_KeyboardForce, 0, 0);
    }


    //---------------------------------------------------------------------
    // Emitter grain creation
    //---------------------------------------------------------------------
    void CreateEmitterGrain(int offset)
    {
        if (_GrainObjects.Count < _MaxGrains)
        {
            // Create new random values for each grain
            GenerateGrainValues();

            // Generate new particle in trigger particle system and return it here
            ParticleSystem.Particle tempParticle = _ParticleManager.SpawnEmitterParticle(_RigidBody.velocity, _GrainSpeedOnBirth, (float)_GrainDuration / 1000);

            // Create new grain object
            GameObject tempGrainObject = Instantiate(_GrainPrefab);
            tempGrainObject.transform.parent = _GrainObjectHolder.transform;
            tempGrainObject.transform.position = tempParticle.position;

            // Apply physics params to new grain gameobject
            Rigidbody rb = tempGrainObject.GetComponent<Rigidbody>();
            rb.velocity = tempParticle.velocity + _ParticleSynthVelocity;
            rb.useGravity = _Gravity;
            rb.mass = _Mass;

            // TO DO: MASS CURRENTLY NOT BEING APPLIED CORRECTLY TO GAMEOBJECTS
            
            // Initialise, start, and add grain to currently playing list
            Grain grainScript = tempGrainObject.GetComponent<Grain>();
            grainScript._Granulator = this;
            grainScript.Initialise(_MovementAudioClip);
            grainScript.PlayGrain(_NewGrainPosition, _NewGrainDuration, _NewGrainPitch, _NewGrainVolume, offset);
            _GrainObjects.Add(tempGrainObject);
        }
    }

    //---------------------------------------------------------------------
    // Collision grain creation
    //---------------------------------------------------------------------
    void CreateCollisionGrain(ParticleCollisionEvent collision, int offset)
    {
        if (_GrainObjects.Count < _MaxGrains && _Collisions)
        {
            // Create new random values for each grain
            GenerateGrainValues();

            // Spawn particle (does not interact with the grain at all)
            _ParticleManager.SpawnCollisionParticle(collision, (float)_CollisionDuration / 1000);

            // Create new grain object
            GameObject tempGrainObject = Instantiate(_GrainPrefab);
            tempGrainObject.transform.parent = _GrainObjectHolder.transform;
            tempGrainObject.transform.position = collision.intersection;
            tempGrainObject.transform.rotation = Quaternion.LookRotation(collision.normal);

            // TO DO: Fix collision grain velocity on spawn??

            // Initialise, start, and add grain to currently playing list
            Grain grainScript = tempGrainObject.GetComponent<Grain>();
            grainScript._Granulator = this;
            grainScript.Initialise(_CollisionAudioClip);
            grainScript.PlayGrain(_NewCollisionGrainPosition, _NewCollisionDuration, _NewGrainPitch, _NewGrainVolume, offset);
            _GrainObjects.Add(tempGrainObject);
        }
    }

    //---------------------------------------------------------------------
    // Creates a burst of new grains on collision events
    //---------------------------------------------------------------------
    public void TriggerCollision(List <ParticleCollisionEvent> collisions, GameObject other)
    {
        for (int i = 0; i < collisions.Count; i++)
        {
            for (int j = 0; j < _CollisionGrainBurst; j++)
            {
                int currentCollisionSampleOffset = (int) (((float)j / (float)(_CollisionGrainBurst)) * _SamplesLastUpdate * 10);
                CreateCollisionGrain(collisions[i], currentCollisionSampleOffset);
            }
        }
    }

    //---------------------------------------------------------------------
    // Updates random grain values for new grains
    //---------------------------------------------------------------------
    void GenerateGrainValues()
    {
        _NewGrainPosition = _GrainPosition + Random.Range(0, _GrainPositionRandom);
        _NewCollisionGrainPosition = _CollisionGrainPosition + Random.Range(0, _CollisionPositionRandom);

        _NewGrainPitch = _GrainPitch + Random.Range(-_GrainPitchRandom, _GrainPitchRandom);

        _NewGrainDuration = (int)(44100 / 1000 * (_GrainDuration + Random.Range(0, _GrainDurationRandom)));
        _NewCollisionDuration = (int)(44100 / 1000 * (_CollisionDuration + Random.Range(0, _CollisionDurationRandom)));

        _NewGrainVolume = Clamp(_GrainVolume + Random.Range(-_GrainVolumeRandom, _GrainVolumeRandom), 0, 3);
    }

    //---------------------------------------------------------------------
    // Clamp function
    //---------------------------------------------------------------------
    float Clamp(float val, float min, float max) {
        val = val > min ? val : min;
        val = val < max ? val : max;
        return val;
    }


    // Generate windowing function lookup table
    // NOT IN USE ANYMORE
    //void CreateWindow()
    //{
    //    _Window = new float[_SampleRate];

    //    for (int i = 0; i < _Window.Length; i++)
    //    {
    //        _Window[i] = 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * i / _SampleRate));
    //    }
    //}
}