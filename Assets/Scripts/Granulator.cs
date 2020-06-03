using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Granulator : MonoBehaviour
{
    public ParticleManager _ParticleManager;
    private ParticleSystem.Particle _TempParticle;
    private ParticleSystem.Particle[] _Particles;

    public GameObject _GrainObjectHolder;
    public GameObject _GrainPrefab;

    public AudioClip _AudioClip;
    public bool _IsPlaying = true;       // the on/off button

    public enum ParticleMode { Spawning, Static };
    public ParticleMode _ParticleMode = ParticleMode.Spawning;

    public int _MaxGrains = 100;
    [Range(2.0f, 3000f)]
    public int _GrainDuration = 300;       // ms
    [Range(0.0f, 1000f)]
    public int _GrainDurationRandom = 0;     // ms
    [Range(0.0f, 1.0f)]
    public float _GrainPosition = 0;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _GrainPositionRandom = 0;      // from 0 > 1
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

    
    // Temp vars
    private float _NewGrainPosition = 0;
    private float _NewGrainPitch = 0;
    private float _NewGrainPitchRandom = 0;
    private int _NewGrainDurationInSamps = 0;
    private float _NewGrainDurationInMS = 0;
    private int _NewGrainDensity = 0;
    private float _NewGrainVolume = 0;

    private List<int> _GrainTriggerList;
    private int _SamplesSinceLastGrain;
    private int _GrainsLastUpdate = 0;


    private Rigidbody _RigidBody;
    private Vector3 _InheritVelocity;

    public bool _MoveGrains = true;
    public bool _Gravity = false;
    public bool _Collisions = false;

    private bool _GravityPrevious;
    private bool _CollisionsPrevious;

    [Range(0.0f, 30.0f)]
    public float _GrainSpeedOnBirth = 5.0f;
    public float _KeyboardForce = 1;

    private List<GameObject> _GrainObjects;


    private float[] _Window;

    private const int _SampleRate = 44100;


    //---------------------------------------------------------------------
    private void Start()
    {
        this.gameObject.AddComponent<AudioSource>();
        _RigidBody = this.GetComponent<Rigidbody>();
        _GrainObjects = new List<GameObject>();
        _GrainTriggerList = new List<int>();
        _SamplesSinceLastGrain = 0;
    }

    void Awake()
    {
    }


    void Update()
    {
        _InheritVelocity = _RigidBody.velocity * 0.5f;

        if (_Gravity != _GravityPrevious)
        {
            _GravityPrevious = _Gravity;
            _ParticleManager.SetGravity(_Gravity);
        }

        if (_Collisions != _CollisionsPrevious)
        {
            _CollisionsPrevious = _Collisions;
            _ParticleManager.SetCollisions(_Collisions);
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

        GenerateGrainValues();

        int samplesLastUpdate = (int)(Time.deltaTime * _SampleRate);
        int samplesSinceStart = (int)(Time.time * _SampleRate);
        int densityInSamples = _NewGrainDensity * (_SampleRate / 1000);






        _GrainTriggerList.Clear();


        // MOVEMENT GRAIN GENERATION
        // NOTE: Could change this to simply divide the number of grains to be played (using density)
        // this update, based on previous update in samples
        //for (int i = samplesSinceStart; i < samplesSinceStart + samplesLastUpdate; i++)
        //{
        //    if (i >= _SamplesSinceLastGrain + densityInSamples)
        //    {
        //        _GrainTriggerList.Add(i - samplesSinceStart);
        //        _SamplesSinceLastGrain = i;
        //    }
        //}



        Debug.Log("----------------------------------------");

        // Building above idea
        int samplesSinceLastGrain = _SamplesSinceLastGrain - samplesLastUpdate;

        int grainsThisUpdate = 0;
        int firstGrainOffset = 0;


        // Adding the previous update's time in samples AFTER the update is complete should
        // correctly accumulate the samples since the last grain playback
        if (_GrainsLastUpdate == 0)
            _SamplesSinceLastGrain += samplesLastUpdate;
        else
            _SamplesSinceLastGrain = samplesLastUpdate - _SamplesSinceLastGrain;


        //Debug.Log("Samples Since Last Grain: " + _SamplesSinceLastGrain);
        //Debug.Log("Density in samples: " + densityInSamples);


        // If the estimated density of grains in samples minus samples since last grain fits within 
        // the estimated time for the next frame calculate number of grains to play this update
        if (densityInSamples - _SamplesSinceLastGrain < samplesLastUpdate)
        {
            // Should always equal one or more
            grainsThisUpdate = (int)(samplesLastUpdate / densityInSamples) + 1;
            
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
            if (i == grainsThisUpdate - 1)
                _SamplesSinceLastGrain = i;
        }

        // If a grain is going to be played this update, set the samples since last grain
        // counter to the sample offset value of the grain to be played last
        if (_GrainTriggerList.Count > 0)
            _SamplesSinceLastGrain = _GrainTriggerList[_GrainTriggerList.Count - 1];




        //Debug.Log("Samples last update: " + samplesLastUpdate);
        //Debug.Log("Last grain in samples: " + _SamplesSinceLastGrain);
        //Debug.Log("Density in samples: " + densityInSamples);
        //Debug.Log("Approx grains per update: " + grainsThisUpdate);
        //Debug.Log("Next grain sample: " + (_SamplesSinceLastGrain + densityInSamples));
        //Debug.Log("Actual grains this update: " + _GrainTriggerList.Count);


        // COLLISION GRAIN GENERATION



        // Create grain objects
        foreach (int offset in _GrainTriggerList)
            CreateMovementGrain(offset);


        Debug.Log("Total active grains: " + _GrainObjects.Count);


        // GRAIN TEST KEYS
        if (Input.GetKey(KeyCode.W))
            _RigidBody.AddForce(0, 0, _KeyboardForce);
        if (Input.GetKey(KeyCode.A))
            _RigidBody.AddForce(-_KeyboardForce, 0, 0);
        if (Input.GetKey(KeyCode.S))
            _RigidBody.AddForce(0, 0, -_KeyboardForce);
        if (Input.GetKey(KeyCode.D))
            _RigidBody.AddForce(_KeyboardForce, 0, 0);
    }


    void CreateMovementGrain(int offset)
    {
        if (_GrainObjects.Count < _MaxGrains)
        {
            // Generate new particle in particle system and return it here
            _TempParticle = _ParticleManager.SpawnRandomParticle(_InheritVelocity, _GrainSpeedOnBirth, _NewGrainDurationInMS / 1000);

            // Create new grain object
            GameObject tempGrainObject = Instantiate(_GrainPrefab);
            tempGrainObject.transform.parent = _GrainObjectHolder.transform;
            tempGrainObject.transform.position = _TempParticle.position;
            tempGrainObject.GetComponent<Rigidbody>().velocity = _TempParticle.velocity + _InheritVelocity;

            // Initialise, play, and add grain to playing list
            Grain g = tempGrainObject.GetComponent<Grain>();
            g._Granulator = this;
            g.Initialise(_AudioClip);
            g.PlayGrain(_NewGrainPosition, _NewGrainDurationInSamps, _NewGrainPitch, _NewGrainPitchRandom, _NewGrainVolume, offset);
            _GrainObjects.Add(tempGrainObject);
        }
    }

    void CreateCollisionGrain()
    {

    }



    void GenerateGrainValues()
    {
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

        // Calculate randomized values for new grains
        _NewGrainPosition = _GrainPosition + Random.Range(0, _GrainPositionRandom);
        _NewGrainPitch = _GrainPitch;
        _NewGrainPitchRandom = Random.Range(-_GrainPitchRandom, _GrainPitchRandom);
        _NewGrainDurationInMS = (_GrainDuration + Random.Range(0, _GrainDurationRandom));
        _NewGrainDurationInSamps = (int)(44100 / 1000 * _NewGrainDurationInMS);
        _NewGrainDensity = _TimeBetweenGrains + Random.Range(0, _TimeBetweenGrainsRandom);
        _NewGrainVolume = Clamp(_GrainVolume + Random.Range(-_GrainVolumeRandom, _GrainVolumeRandom), 0, 3);
    }



    // Clamp function
    float Clamp(float val, float min, float max) {
        val = val > min ? val : min;
        val = val < max ? val : max;
        return val;
    }

    // Generate windowing function lookup table
    void CreateWindow()
    {
        _Window = new float[_SampleRate];

        for (int i = 0; i < _Window.Length; i++)
        {
            _Window[i] = (float)0.5 * (1 - Mathf.Cos(2 * Mathf.PI * i / _SampleRate));
        }
    }
}




//void CreateGrainObject()
//{
//    _ParticleCount = _ParticleManager.GetParticleCount();

//    if (_GrainObjects.Count < _MaxGrains && _ParticleCount > 0)
//    {
//        GameObject tmp = Instantiate(_GrainPrefab);
//        Rigidbody tmpRB = tmp.GetComponent<Rigidbody>();

//        Grain g = tmp.GetComponent<Grain>();
//        g._Granulator = this;
//        g.Initialise(_AudioClip);
//        _GrainObjects.Add(tmp);

//        // Find random particle, set grain position to particle
//        _Particle = _ParticleManager.GetRandomParticle();
//        tmp.transform.position = _Particle.position;
//        tmpRB.velocity = _Particle.velocity;


//        // Start grain
//        g.PlayGrain(_NewGrainPosition, _NewGrainDurationInSamps, _NewGrainPitch, _NewGrainPitchRandom, _NewGrainVolume, _Window);
//    }
//}