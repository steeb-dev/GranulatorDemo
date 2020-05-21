using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Granulator : MonoBehaviour
{
    public ParticleManager _ParticleManager;
    private ParticleSystem.Particle _Particle;

    public int _MaxGrains = 10;
    [Range(0.0f, 1000f)]
    public int _GrainLength = 100;       // ms
    [Range(0.0f, 3000f)]
    public int _GrainLengthRand = 0;     // ms
    [Range(0.0f, 1.0f)]
    public float _GrainPos = 0;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _GrainPosRand = 0;      // from 0 > 1
    [Range(0.0f, 1000f)]
    public int _GrainDist = 10;          // ms
    [Range(0.0f, 1000f)]
    public int _GrainDistRand = 0;       // ms
    [Range(0.0f, 5f)]
    public float _GrainPitch = 1;
    [Range(0.0f, 1f)]
    public float _GrainPitchRand = 0;
    [Range(0.0f, 2.0f)]
    public float _GrainVol = 1;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float _GrainVolRand = 0;      // from 0 > 1
    [Range(0.0f, 0.5f)]
    public float _GrainAttack = .3f;     // from 0 > 1
    [Range(0.0f, 0.5f)]
    public float _GrainRelease = .3f;    // from 0 > 1
    public bool _IsPlaying = true;       // the on/off button

    public AudioClip _AudioClip;
    public GameObject _GrainPrefab;
    private AudioClip _LastClip;
    
    // Temp vars
    private int _NewGrainPos = 0;
    private float _NewGrainPitch = 0;
    private float _NewGrainPitchRand = 0;
    private int _NewGrainLength = 0;
    private int _NewGrainDist = 0;
    private float _NewGrainVol = 0;

    private int _Channels;
    private int _GrainTimer = 0;
    private int _ParticleCount;

    private Rigidbody _RigidBody;


    public bool _MoveGrains = true;
    public float _KeyboardForce = 1;

    public List<GameObject> _GrainObjects;

    //public List<Grain> _ActivePool;
    //public List<Grain> _InactivePool;

    private float[] _Window;


    //---------------------------------------------------------------------
    private void Start()
    {
        this.gameObject.AddComponent<AudioSource>();
    }

    void Awake()
    {
        _RigidBody = this.GetComponent<Rigidbody>();

        CreateWindow();

        //for (int i = 0; i < _MaxGrains; i++)
        //{
        //    GameObject tmp = Instantiate(_GrainPrefab); //, this.transform);
        //    Grain g = tmp.GetComponent<Grain>();
        //    g._Granulator = this;
        //    g._AudioClip = _AudioClip;
        //    _ActivePool.Add(g);
        //}
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

        for (int i = _GrainObjects.Count - 1; i >= 0; i--)
        {
            if (_GrainObjects[i] != null && !_GrainObjects[i].GetComponent<Grain>()._IsPlaying)
            {
                Destroy(_GrainObjects[i]);
                _GrainObjects.RemoveAt(i);
            }
        }

        // Clamp values to reasonable ranges
        _GrainPos = Clamp(_GrainPos, 0, 1);
        _GrainPosRand = Clamp(_GrainPosRand, 0, 1);
        _GrainDist = (int)Clamp(_GrainDist, 1, 10000);
        _GrainDistRand = (int)Clamp(_GrainDistRand, 0, 10000);
        _GrainLengthRand = (int)Clamp(_GrainLengthRand, 0, 10000);

        _GrainPitch = Clamp(_GrainPitch, 0, 1000);
        _GrainPitchRand = Clamp(_GrainPitchRand, 0, 1000);
        _GrainVol = Clamp(_GrainVol, 0, 2);
        _GrainVolRand = Clamp(_GrainVolRand, 0, 1);

        // Calculate randomized values for new grains
        _NewGrainPos = (int)((_GrainPos + Random.Range(0, _GrainPosRand)) * _AudioClip.samples);
        _NewGrainPitch = _GrainPitch;
        _NewGrainPitchRand = Random.Range(-_GrainPitchRand, _GrainPitchRand);
        _NewGrainLength = (int)(_AudioClip.frequency / 1000 * (_GrainLength + Random.Range(0, _GrainLengthRand)));
        _NewGrainDist = _AudioClip.frequency / 1000 * (_GrainDist + Random.Range(0, _GrainDistRand));

        _NewGrainVol = Clamp(_GrainVol + Random.Range(-_GrainVolRand, _GrainVolRand), 0, 3);



        // GRAIN TEST KEY TRIGGERS
        //if (Input.GetKeyDown(KeyCode.O))
        //{
        //    SeedAllInactiveGrains();
        //}

        //if (Input.GetKey(KeyCode.S))
        //{
        //    if (_InactivePool.Count > 0)
        //    {
        //        SeedNewGrain(_InactivePool[0]);
        //        _ActivePool.Add(_InactivePool[0]);
        //        _InactivePool.RemoveAt(0);
        //    }
        //}

        if (Input.GetKey(KeyCode.I))
        {
            CreateGrainObject();
        }

        CreateGrainObject();

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
            g.PlayGrain(_NewGrainPos, _NewGrainLength, _NewGrainPitch, _NewGrainPitchRand, _NewGrainVol, _Window);
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