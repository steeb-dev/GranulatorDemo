﻿using System.Collections;
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


    [Range(0.1f, 5f)]
    public float _GrainPitch = 1;
    [Range(0.0f, 1f)]
    public float _GrainPitchRandom = 0;
    [Range(0.0f, 2.0f)]
    public float _GrainVolume = 1;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _GrainVolumeRandom = 0;      // from 0 > 1


    public AudioClip _EmitterClip;
    [Range(1.0f, 1000f)]
    public int _TimeBetweenGrains = 20;          // ms
    [Range(0.0f, 1000f)]
    public int _TimeBetweenGrainsRandom = 0;       // ms
    [Range(0.0f, 1.0f)]
    public float _GrainPosition = 0;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _GrainPositionRandom = 0;      // from 0 > 1
    [Range(2.0f, 1000f)]
    public int _GrainDuration = 300;       // ms
    [Range(0.0f, 1000f)]
    public int _GrainDurationRandom = 0;     // ms

    public AudioClip _CollisionClip;
    public int _CollisionGrainBurst = 5;
    [Range(1.0f, 1000f)]
    public int _CollisionDensity = 40;                  // ms
    [Range(0.0f, 1.0f)]
    public float _CollisionGrainPosition = 0;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _CollisionPositionRandom = 0;      // from 0 > 1
    [Range(2.0f, 1000f)]
    public int _CollisionDuration = 300;       // ms
    [Range(0.0f, 1000f)]
    public int _CollisionDurationRandom = 0;     // ms

    public enum ParticleMode { Spawning, Static };
    public ParticleMode _ParticleMode = ParticleMode.Spawning;

    
    // Temp vars
    private float _NewGrainPosition = 0;
    private float _NewCollisionGrainPosition = 0;
    private float _NewGrainPitch = 0;
    private float _NewGrainPitchRandom = 0;
    private float _NewGrainDuration = 0;
    private int _NewCollisionDuration;
    private int _NewGrainDensity = 0;
    private float _NewGrainVolume = 0;

    private int _SamplesSinceLastGrain;
    private int _EmitterGrainsLastUpdate = 0;
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

    private List<GrainData> _GrainQueue;

    //public List<int> _GrainTriggerList;

    public List<GameObject> _GrainsPlaying;
    public List<GameObject> _GrainsFinished;


    private const int _SampleRate = 44100;


    //---------------------------------------------------------------------
    private void Start()
    {
        _ParticleManager.Initialise(this);

        this.gameObject.AddComponent<AudioSource>();
        _RigidBody = this.GetComponent<Rigidbody>();

        _GrainsPlaying = new List<GameObject>();
        _GrainsFinished = new List<GameObject>();

        for (int i = 0; i < _MaxGrains; i++)
        {
            GameObject tempGrainObject = Instantiate(_GrainPrefab);
            tempGrainObject.SetActive(true);
            tempGrainObject.GetComponent<Grain>()._Granulator = this;
            tempGrainObject.transform.parent = _GrainObjectHolder.transform;
            _GrainsFinished.Add(tempGrainObject);
        }
        
        _GrainQueue = new List<GrainData>();

        _SamplesSinceLastGrain = 0;

        // Initialise some grain values
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

        //Move finished grains to inactive pool
        for (int i = _GrainsPlaying.Count - 1; i >= 0; i--)
        {
            Grain tempGrain = _GrainsPlaying[i].GetComponent<Grain>();

            if (!tempGrain._IsPlaying)
            {
                //tempGrain.gameObject.SetActive(false);
                _GrainsFinished.Add(_GrainsPlaying[i]);
                _GrainsPlaying.RemoveAt(i);
            }
        }




        //---------------------------------------------------------------------
        // EMITTER GRAIN TIMING GENERATION
        //---------------------------------------------------------------------
        // Emitter grains are those which play back constantly throughout each
        // update, as opposed to being trigged from single events.
        // This function creates the timing of emitter grains to be played
        // over the next update.

        int emitterGrainsToPlay = 0;
        int firstGrainOffset = 0;
        int densityInSamples = _NewGrainDensity * (_SampleRate / 1000);

        // If no sample was played last update, adding the previous update's samples count,
        // AFTER the update is complete, should correctly accumulate the samples since the
        // last grain playback. Otherwise, if a sample WAS played last update, the sample
        // offset of that grain is subtracted from the total samples of the previous update.
        // This provides the correct number of samples since the most recent grain was started.
        if (_EmitterGrainsLastUpdate == 0)
            _SamplesSinceLastGrain += _SamplesLastUpdate;
        else
            _SamplesSinceLastGrain = _SamplesLastUpdate - _SamplesSinceLastGrain;

        // If the density of grains minus samples since last grain fits within the
        // estimated time for the this update, calculate number of grains to play this update
        if (densityInSamples - _SamplesSinceLastGrain < _SamplesLastUpdate)
        {
            // Should always equal one or more
            // Not sure if the + 1 is correct here. Potentially introducing rounding errors?
            // Need to check
            emitterGrainsToPlay = (int)(_SamplesLastUpdate / densityInSamples) + 1;
            
            // Create initial grain offset for this update
            firstGrainOffset = densityInSamples - _SamplesSinceLastGrain;
            
            // Hacky check to avoid offsets lower than 0 (if this occurs, something
            // isn't handled correctly. This is a precaution. Haven't properly checked this yet.
            if (firstGrainOffset < 0)
                firstGrainOffset = 0;
        }

        _EmitterGrainsLastUpdate = emitterGrainsToPlay;


        //---------------------------------------------------------------------
        // CREATE EMITTER GRAINS
        //---------------------------------------------------------------------
        // Populate grain queue with emitter grains
        //
        for (int i = 0; i < emitterGrainsToPlay; i++)
        {
            //_GrainTriggerList.Add(firstGrainOffset + i * densityInSamples);

            // Create new random values for each grain
            GenerateGrainValues();

            // Generate new particle in trigger particle system and return it here
            ParticleSystem.Particle tempParticle = _ParticleManager.SpawnEmitterParticle(_RigidBody.velocity, _GrainSpeedOnBirth, (float)_GrainDuration / 1000);

            // Calculate timing offset for grain
            int offset = firstGrainOffset + i * densityInSamples;

            // Create temporary grain data object and add it to the playback queue
            GrainData tempGrainData = new GrainData(_TempParticle.position, _GrainObjectHolder.transform, tempParticle.velocity + _ParticleSynthVelocity, _Gravity, _Mass,
                _EmitterClip, _NewGrainDuration, offset, _NewGrainPosition, _NewGrainPitch, _NewGrainVolume);

            _GrainQueue.Add(tempGrainData);
        }


        // If a grain is going to be played this update, set the samples since last grain
        // counter to the sample offset value of the final grain
        if (_GrainQueue.Count > 0)
            _SamplesSinceLastGrain = _GrainQueue[_GrainQueue.Count - 1].offset;

        // NOTE: If no grains are played, the time that the current update takes
        // (determined next update) will be added to this "samples since last grain" counter instead.
        // This provides the correct distribution of grains per x samples. Go to top of "emitter grain generation"
        // for more information




        //---------------------------------------------------------------------
        // CREATE COLLISION GRAINS
        //---------------------------------------------------------------------
        // Populate grain queue with collision grains
        //




        //---------------------------------------------------------------------
        // ASSIGN GRAIN QUEUE TO GRAIN OBJECTS
        //---------------------------------------------------------------------
        foreach (GrainData grain in _GrainQueue)
        {
            if (_GrainsFinished.Count > 0)
            {
                //_GrainsFinished[0].SetActive(true);
                _GrainsFinished[0].GetComponent<Grain>().Initialise(grain);
                _GrainsPlaying.Add(_GrainsFinished[0]);
                _GrainsFinished.RemoveAt(0);
            }
        }

        _GrainQueue.Clear();



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




    ////---------------------------------------------------------------------
    //// Collision grain creation
    ////---------------------------------------------------------------------

    //// TO DO: Move over to _GrainsToPlay list AND merge with above function (CreateEmitterGrain)
    //void CreateCollisionGrain(ParticleCollisionEvent collision, int offset)
    //{
    //    if (_GrainsFinished.Count > 0 && _Collisions)
    //    {
    //        // Create new random values for each grain
    //        GenerateGrainValues();

    //        // Spawn particle (does not interact with the grain at all)
    //        _ParticleManager.SpawnCollisionParticle(collision, (float)_CollisionDuration / 1000);

    //        // Create new grain object
    //        _GrainsFinished[0] = Instantiate(_GrainPrefab);
    //        _GrainsFinished[0].transform.parent = _GrainObjectHolder.transform;
    //        _GrainsFinished[0].transform.position = collision.intersection;
    //        _GrainsFinished[0].transform.rotation = Quaternion.LookRotation(collision.normal);

    //        // TO DO: Fix collision grain velocity on spawn??

    //        // Initialise, start, and add grain to currently playing list
    //        Grain grainScript = _GrainsFinished[0].GetComponent<Grain>();
    //        grainScript._Granulator = this;
    //        //grainScript.Initialise(_CollisionAudioClip);
    //        grainScript.PlayGrain(_CollisionClip, _NewCollisionGrainPosition, _NewCollisionDuration, _NewGrainPitch, _NewGrainVolume, offset);

    //        _GrainsPlaying.Add(_GrainsFinished[0]);
    //        _GrainsFinished.RemoveAt(0);
    //    }
    //}



    //---------------------------------------------------------------------
    // creates a burst of new grains on collision events
    //---------------------------------------------------------------------
    public void TriggerCollision(List<ParticleCollisionEvent> collisions, GameObject other)
    {
        for (int i = 0; i < collisions.Count; i++)
        {
            for (int j = 0; j < _CollisionGrainBurst; j++)
            {
                GenerateGrainValues();

                // Calculate timing offset for grain
                int offset = j * _CollisionDensity * (_SampleRate / 1000);

                // Create temporary grain data object and add it to the playback queue
                GrainData tempGrainData = new GrainData(collisions[i].intersection, _GrainObjectHolder.transform, Vector3.zero, _Gravity, _Mass,
                    _CollisionClip, _NewCollisionDuration, offset, _NewCollisionGrainPosition, _NewGrainPitch, _NewGrainVolume);

                _GrainQueue.Add(tempGrainData);
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

        _NewGrainDuration = _GrainDuration + Random.Range(0, _GrainDurationRandom);
        _NewCollisionDuration = _CollisionDuration + Random.Range(0, _CollisionDurationRandom);

        _NewGrainVolume = Clamp(_GrainVolume + Random.Range(-_GrainVolumeRandom, _GrainVolumeRandom), 0, 3);
    }


    public void GrainNotPlaying(GameObject grain)
    {
        //grain.SetActive(false);
        _GrainsFinished.Add(grain);
        _GrainsPlaying.Remove(grain);
    }


    public class GrainData
    {
        public Vector3 objectPosition;
        public Transform objectParent;
        public Vector3 objectVelocity;
        public bool objectGravity;
        public float objectMass;
        public AudioClip audioClip;

        public int offset;
        public float grainDuration;
        public float grainPos;
        public float grainPitch;
        public float grainVolume;

        public GrainData() { }
        public GrainData(Vector3 position, Transform parent, Vector3 velocity, bool gravity, float mass,
            AudioClip grainAudioClip, float durationInMS, int grainOffsetInSamples, float playheadPosition, float pitch, float volume)
        {
            objectPosition = position;
            objectParent = parent;
            objectVelocity = velocity;
            objectGravity = gravity;
            objectMass = mass;
            audioClip = grainAudioClip;
            offset = grainOffsetInSamples;
            grainDuration = durationInMS;
            grainPos = playheadPosition;
            grainPitch = pitch;
            grainVolume = volume;
        }
    }



    //---------------------------------------------------------------------
    float Clamp(float val, float min, float max)
    {
        val = val > min ? val : min;
        val = val < max ? val : max;
        return val;
    }
}