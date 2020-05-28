using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Granulator : MonoBehaviour
{
    public ParticleManager _ParticleManager;
    private ParticleSystem.Particle _Particle;

    public int _MaxGrains = 100;
    [Range(0.0f, 3000f)]
    public int _GrainDuration = 100;       // ms
    [Range(0.0f, 1000f)]
    public int _GrainDurationRandom = 0;     // ms
    [Range(0.0f, 1.0f)]
    public float _GrainPosition = 0;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _GrainPositionRandom = 0;      // from 0 > 1
    [Range(0.0f, 1000f)]
    public int _TimeBetweenGrains = 20;          // ms
    [Range(0.0f, 1000f)]
    public int _TimeBetweenGrainsRandom = 0;       // ms
    [Range(0.0f, 5f)]
    public float _GrainPitch = 1;
    [Range(0.0f, 1f)]
    public float _GrainPitchRandom = 0;
    [Range(0.0f, 2.0f)]
    public float _GrainVolume = 1;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _GrainVolumeRandom = 0;      // from 0 > 1

    public bool _IsPlaying = true;       // the on/off button

    public AudioClip _AudioClip;
    public GameObject _GrainPrefab;
    private AudioClip _LastClip;
    
    // Temp vars
    private int _NewGrainPosition = 0;
    private float _NewGrainPitch = 0;
    private float _NewGrainPitchRandom = 0;
    private int _NewGrainDuration = 0;
    private int _NewGrainDensity = 0;
    private float _NewGrainVolume = 0;

    private int _Channels;
    private int _GrainTimer = 0;
    private int _ParticleCount;
    private float _UpdateTime;
    private float _GrainsPerUpdate;
    private int _LastGrainStart;
    private List<int> _UpdateGrainOffsetList;
    private int _TimeSinceStart;


    private Rigidbody _RigidBody;


    public bool _MoveGrains = true;
    public float _KeyboardForce = 1;

    public List<GameObject> _GrainObjects;


    private float[] _Window;


    //---------------------------------------------------------------------
    private void Start()
    {
        this.gameObject.AddComponent<AudioSource>();
        _UpdateGrainOffsetList = new List<int>();
        _LastGrainStart = 0;
    }

    void Awake()
    {
        _RigidBody = this.GetComponent<Rigidbody>();
        CreateWindow();
    }


    // would be good if actually every grain will get freshly randomized values, but idk if that's possible... 
    // updating the random-vals every frame has to suffice for now... :/
    //---------------------------------------------------------------------
    void FixedUpdate()
    {
        if (_LastClip != _AudioClip)
        {
            _LastClip = _AudioClip;
            CreateWindow();
        }

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
        _TimeBetweenGrains = (int)Clamp(_TimeBetweenGrains, 1, 10000);
        _TimeBetweenGrainsRandom = (int)Clamp(_TimeBetweenGrainsRandom, 0, 10000);
        _GrainDurationRandom = (int)Clamp(_GrainDurationRandom, 0, 10000);

        _GrainPitch = Clamp(_GrainPitch, 0, 1000);
        _GrainPitchRandom = Clamp(_GrainPitchRandom, 0, 1000);
        _GrainVolume = Clamp(_GrainVolume, 0, 2);
        _GrainVolumeRandom = Clamp(_GrainVolumeRandom, 0, 1);

        // Calculate randomized values for new grains
        _NewGrainPosition = (int)((_GrainPosition + Random.Range(0, _GrainPositionRandom)) * _AudioClip.samples);
        _NewGrainPitch = _GrainPitch;
        _NewGrainPitchRandom = Random.Range(-_GrainPitchRandom, _GrainPitchRandom);
        _NewGrainDuration = (int)(_AudioClip.frequency / 1000 * (_GrainDuration + Random.Range(0, _GrainDurationRandom)));
        _NewGrainDensity = _TimeBetweenGrains + Random.Range(0, _TimeBetweenGrainsRandom);

        _NewGrainVolume = Clamp(_GrainVolume + Random.Range(-_GrainVolumeRandom, _GrainVolumeRandom), 0, 3);





        // Get time between last frame for grain density triggering
        _UpdateTime = Time.fixedDeltaTime;
        Debug.Log("----------------------------");
        Debug.Log("Update time: " + _UpdateTime * 1000);
        Debug.Log("Grain density: " + _NewGrainDensity);

        // Calculate how many grains should be created this update
        _GrainsPerUpdate = (_UpdateTime * 1000) / (float)_NewGrainDensity;
        Debug.Log("Grains per update " + _GrainsPerUpdate);


        _UpdateGrainOffsetList.Clear();

        _TimeSinceStart = (int) ((Time.time) * 1000);
        
        Debug.Log("Time since start: " + _TimeSinceStart);
        Debug.Log("Time of last grain: " + _LastGrainStart);
        Debug.Log("Next grain due: " + (_LastGrainStart + _NewGrainDensity));


        for (int i = _TimeSinceStart; i < _TimeSinceStart + _UpdateTime; i++)
        {
            // If (another) grain is to be played this update
            if (i >= _LastGrainStart + _NewGrainDensity)
            {
                // Add grain to playback offset list
                _UpdateGrainOffsetList.Add(i * _AudioClip.frequency / 1000);
                // And set _LastGrain start to that grain time offset
                _LastGrainStart = i + _NewGrainDensity;
            }
        }

        Debug.Log("Grains to play this update: " + _UpdateGrainOffsetList.Count);


        foreach (int offset in _UpdateGrainOffsetList)
        {
            CreateGrainObject(offset);
        }



        // GRAIN TEST KEY TRIGGERS

        if (Input.GetKeyDown(KeyCode.I))
        {
            InvokeRepeating("CreateGrainObject", 0, 1.0f / _TimeBetweenGrains);
            //CreateGrainObject();
        }

        if (Input.GetKeyUp(KeyCode.I))
        {
            CancelInvoke();
        }

        //CreateGrainObject();

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
            g.PlayGrain(_NewGrainPosition, _NewGrainDuration, _NewGrainPitch, _NewGrainPitchRandom, _NewGrainVolume, _Window);
        }
    }


    void CreateGrainObject(int offset)
    {
        // Redundant?
        _ParticleCount = _ParticleManager.GetParticleCount();

        if (_GrainObjects.Count < _MaxGrains)
        {
            // Create new grain object
            GameObject tmp = Instantiate(_GrainPrefab);
            tmp.transform.parent = this.transform;

            // Initialise the particle
            Grain g = tmp.GetComponent<Grain>();
            g._Granulator = this;
            g.Initialise(_AudioClip);
            _GrainObjects.Add(tmp);

            // Generate new particle in particle system and return it here
            _Particle = _ParticleManager.SpawnRandomParticle(_NewGrainDuration);

            // Provide grain object with generated particle transforms
            Rigidbody tmpRB = tmp.GetComponent<Rigidbody>();
            tmp.transform.position = _Particle.position;
            tmpRB.velocity = _Particle.velocity;

            // Start grain
            g.PlayGrain(_NewGrainPosition, _NewGrainDuration, _NewGrainPitch, _NewGrainPitchRandom, _NewGrainVolume, _Window, offset);
        }
    }


    //void SeedAllInactiveGrains()
    //{
    //    for (int i = _InactivePool.Count - 1; i >= 0; i--)
    //    {
    //        // Seed new grain
    //        SeedNewGrain(_InactivePool[i]);

    //        // Move grain from inactive to active pool
    //        _ActivePool.Add(_InactivePool[i]);
    //        _InactivePool.RemoveAt(i);
    //    }
    //}


    //void SeedNewGrain(Grain g)
    //{
    //    // Refresh grain if audio clip has changed
    //    if (g._AudioClip != _AudioClip)
    //    {
    //        g._AudioClip = _AudioClip;
    //        g.UpdateGrain();
    //    }

    //    _Particle = _ParticleManager.GetRandomParticle();

    //    // Start new grain
    //    g.PlayGrain(_NewGrainPos, _NewGrainLength, _NewGrainPitch, _NewGrainPitchRand, _NewGrainVol, _Window, _Particle);
    //}


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
        _Window = new float[_AudioClip.frequency];
        for (int i = 0; i < _Window.Length; i++)
        {
            _Window[i] = (float)0.5 * (1 - Mathf.Cos(2 * Mathf.PI * i / _AudioClip.frequency));
        }
    }
}