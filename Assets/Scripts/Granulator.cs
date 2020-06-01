using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Granulator : MonoBehaviour
{
    public ParticleManager _ParticleManager;
    private ParticleSystem.Particle _Particle;

    public GameObject _GrainObjectHolder;
    public GameObject _GrainPrefab;

    public AudioClip _AudioClip;
    public bool _IsPlaying = true;       // the on/off button


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
    
    private AudioClip _LastClip;
    
    // Temp vars
    private int _NewGrainPosition = 0;
    private float _NewGrainPitch = 0;
    private float _NewGrainPitchRandom = 0;
    private int _NewGrainDurationInSamps = 0;
    private float _NewGrainDurationInMS = 0;
    private int _NewGrainDensity = 0;
    private float _NewGrainVolume = 0;

    private int _Channels;
    private int _GrainTimer = 0;
    private int _ParticleCount;
    private List<int> _GrainTriggerOffsetList;
    private int _LastGrainStartInSamples;


    private Rigidbody _RigidBody;


    public bool _MoveGrains = true;
    [Range(0.0f, 30.0f)]
    public float _GrainSpeedOnBirth = 5.0f;
    public float _KeyboardForce = 1;

    public List<GameObject> _GrainObjects;


    private float[] _Window;

    private const int _SampleRate = 44100;


    //---------------------------------------------------------------------
    private void Start()
    {
        this.gameObject.AddComponent<AudioSource>();
        _GrainTriggerOffsetList = new List<int>();
        _LastGrainStartInSamples = 0;
    }

    void Awake()
    {
        _RigidBody = this.GetComponent<Rigidbody>();
        CreateWindow();
    }


    // would be good if actually every grain will get freshly randomized values, but idk if that's possible... 
    // updating the random-vals every frame has to suffice for now... :/
    //---------------------------------------------------------------------
    void Update()
    {
        if (_LastClip != _AudioClip)
        {
            _LastClip = _AudioClip;
            CreateWindow();
        }

        // Remove finished grains
        for (int i = _GrainObjects.Count - 1; i >= 0; i--)
        {
            if (_GrainObjects[i] != null && !_GrainObjects[i].GetComponent<Grain>()._IsPlaying)
            {
                Destroy(_GrainObjects[i]);
                _GrainObjects.RemoveAt(i);
            }
        }

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
        _NewGrainPosition = (int)((_GrainPosition + Random.Range(0, _GrainPositionRandom)) * _AudioClip.samples);
        _NewGrainPitch = _GrainPitch;
        _NewGrainPitchRandom = Random.Range(-_GrainPitchRandom, _GrainPitchRandom);
        _NewGrainDurationInMS = (_GrainDuration + Random.Range(0, _GrainDurationRandom));
        _NewGrainDurationInSamps = (int)(44100 / 1000 * _NewGrainDurationInMS);
        
        _NewGrainDensity = _TimeBetweenGrains + Random.Range(0, _TimeBetweenGrainsRandom);

        _NewGrainVolume = Clamp(_GrainVolume + Random.Range(-_GrainVolumeRandom, _GrainVolumeRandom), 0, 3);



        int samplesPerUpdate = (int)(Time.deltaTime * _SampleRate);
        int samplesSinceStart = (int)(Time.time * _SampleRate);
        int densityInSamples = _NewGrainDensity * (_SampleRate / 1000);

        Debug.Log("----------------------------------------");
        Debug.Log("Samples per update: " + samplesPerUpdate);
        Debug.Log("Last grain in samples: " + _LastGrainStartInSamples);
        Debug.Log("Density in samples: " + densityInSamples);
        Debug.Log("Approx grains per update: " + (samplesPerUpdate / densityInSamples));
        Debug.Log("Next grain sample: " + (_LastGrainStartInSamples + densityInSamples));


        for (int i = samplesSinceStart; i < samplesSinceStart + samplesPerUpdate; i++)
        {
            if (i >= _LastGrainStartInSamples + densityInSamples)
            {
                _GrainTriggerOffsetList.Add(i - samplesSinceStart);
                _LastGrainStartInSamples = i;
            }
        }

        Debug.Log("Actual grains this update: " + _GrainTriggerOffsetList.Count);


        // Create grain objects
        foreach (int offset in _GrainTriggerOffsetList)
        {
            CreateGrainObject(offset);
        }

        _GrainTriggerOffsetList.Clear();


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

    void CreateGrainObject()
    {
        _ParticleCount = _ParticleManager.GetParticleCount();

        if (_GrainObjects.Count < _MaxGrains && _ParticleCount > 0)
        {
            GameObject tmp = Instantiate(_GrainPrefab);
            Rigidbody tmpRB = tmp.GetComponent<Rigidbody>();

            Grain g = tmp.GetComponent<Grain>();
            g._Granulator = this;
            g.Initialise(_AudioClip);
            _GrainObjects.Add(tmp);

            // Find random particle, set grain position to particle
            _Particle = _ParticleManager.GetRandomParticle();
            tmp.transform.position = _Particle.position;
            tmpRB.velocity = _Particle.velocity;


            // Start grain
            g.PlayGrain(_NewGrainPosition, _NewGrainDurationInSamps, _NewGrainPitch, _NewGrainPitchRandom, _NewGrainVolume, _Window);
        }
    }


    void CreateGrainObject(int offset)
    {
        if (_GrainObjects.Count < _MaxGrains)
        {
            // Create new grain object
            GameObject tmp = Instantiate(_GrainPrefab);
            tmp.transform.parent = _GrainObjectHolder.transform;

            Vector3 inheritVelocity = _RigidBody.velocity * 0.5f;

            // Initialise the grain
            Grain g = tmp.GetComponent<Grain>();
            g._Granulator = this;
            g.Initialise(_AudioClip);
            _GrainObjects.Add(tmp);

            // Generate new particle in particle system and return it here
            _Particle = _ParticleManager.SpawnRandomParticle(inheritVelocity, _GrainSpeedOnBirth, _NewGrainDurationInMS / 1000);

            // Provide grain object with particle transform for spatialisation and movement
            Rigidbody tmpRB = tmp.GetComponent<Rigidbody>();
            tmp.transform.position = _Particle.position;
            tmpRB.velocity = _Particle.velocity + inheritVelocity;

            // Start grain using current update's information
            g.PlayGrain(_NewGrainPosition, _NewGrainDurationInSamps, _NewGrainPitch, _NewGrainPitchRandom, _NewGrainVolume, _Window, offset);
        }
    }




    //---------------------------------------------------------------------
    // Utility func to clamp a value between min and max
    float Clamp(float val, float min, float max) {
        val = val > min ? val : min;
        val = val < max ? val : max;
        return val;
    }

    // Generate windowing function lookup table
    void CreateWindow()
    {
        _Window = new float[44100];

        for (int i = 0; i < _Window.Length; i++)
        {
            _Window[i] = (float)0.5 * (1 - Mathf.Cos(2 * Mathf.PI * i / 44100));
        }
    }
}


public class ReadOnlyAttribute { }
