﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Granulator : MonoBehaviour
{
    public int maxGrains = 10;
    [Range(0.0f, 1000f)]
    public int grainLength = 100;       // ms
    [Range(0.0f, 1000f)]
    public int grainLengthRand = 0;     // ms
    [Range(0.0f, 1.0f)]
    public float grainPos = 0;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float grainPosRand = 0;      // from 0 > 1
    [Range(0.0f, 1000f)]
    public int grainDist = 10;          // ms
    [Range(0.0f, 1000f)]
    public int grainDistRand = 0;       // ms
    [Range(0.0f, 5f)]
    public float grainPitch = 1;
    [Range(0.0f, 1f)]
    public float grainPitchRand = 0;
    [Range(0.0f, 2.0f)]
    public float grainVol = 1;          // from 0 > 1
    [Range(0.0f, 1.0f)]
    public float grainVolRand = 0;      // from 0 > 1
    [Range(0.0f, 0.5f)]
    public float grainAttack = .3f;     // from 0 > 1
    [Range(0.0f, 0.5f)]
    public float grainRelease = .3f;    // from 0 > 1
    public bool isPlaying = true;       // the on/off button

    public AudioClip audioClip;
    public GameObject grainPrefab;
    private AudioClip lastClip;
    // tmp vars
    private int newGrainPos = 0;
    private float newGrainPitch = 0;
    private float newGrainPitchRand = 0;
    private int newGrainLength = 0;
    private int newGrainDist = 0;
    private float newGrainVol = 0;

    private int channels;
    private int grainTimer = 0;
    private Vector3 pos;

    public bool moveGrains = true;
    public List<Grain> _ActivePool;
    public List<Grain> _InactivePool;


    //---------------------------------------------------------------------
    private void Start()
    {
        this.gameObject.AddComponent<AudioSource>();
    }

    void Awake()
    {
        CreateWindow();
        for (int i = 0; i < maxGrains; i++)
        {
            GameObject tmp = Instantiate(grainPrefab); //, this.transform);
            Grain g = tmp.GetComponent<Grain>();
            g._Granulator = this;
            g.audioClip = audioClip;
            _ActivePool.Add(g);
        }
    }

    // would be good if actually every grain will get freshly randomized values, but idk if that's possible... 
    // updating the random-vals every frame has to suffice for now... :/
    //---------------------------------------------------------------------
    void Update()
    {
        if (lastClip != audioClip)
        {
            lastClip = audioClip;
            CreateWindow();
        }

        // clamp values to reasonable ranges:
        grainPos = Clamp(grainPos, 0, 1);
        grainPosRand = Clamp(grainPosRand, 0, 1);
        grainDist = (int)Clamp(grainDist, 1, 10000);
        grainDistRand = (int)Clamp(grainDistRand, 0, 10000);
        grainLengthRand = (int)Clamp(grainLengthRand, 0, 10000);


        grainPitch = Clamp(grainPitch, 0, 1000);
        grainPitchRand = Clamp(grainPitchRand, 0, 1000);
        grainVol = Clamp(grainVol, 0, 2);
        grainVolRand = Clamp(grainVolRand, 0, 1);

        // calculate randomized values for new grains:
        newGrainPos = (int)((grainPos + Random.Range(0, grainPosRand)) * audioClip.samples);
        newGrainPitch = grainPitch;
        newGrainPitchRand = Random.Range(-grainPitchRand, grainPitchRand);
        newGrainLength = (int)(audioClip.frequency / 1000 * (grainLength + Random.Range(0, grainLengthRand)));
        newGrainDist = audioClip.frequency / 1000 * (grainDist + Random.Range(0, grainDistRand));

        newGrainVol = Clamp(grainVol + Random.Range(-grainVolRand, grainVolRand), 0, 3);
        pos = transform.position;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            SeedAllInactiveGrains();
        }
    }

    void SeedAllInactiveGrains()
    {
        foreach (Grain g in _InactivePool)
        {
            SeedNewGrain(g);
        }
    }


    void SeedNewGrain(Grain g)
    {
        if(g.audioClip != audioClip)
        {
            g.audioClip = audioClip;
            g.UpdateGrain();
        }

        g.NewGrain(newGrainPos, newGrainLength, newGrainPitch, newGrainPitchRand, newGrainVol, window, pos);
    }

    //---------------------------------------------------------------------
    // utility func to clamp a value between min and max
    float Clamp(float val, float min, float max) {
        val = val > min ? val : min;
        val = val < max ? val : max;
        return val;
    }

    void CreateWindow()
    {
        window = new float[audioClip.frequency];
        for (int i = 0; i < window.Length; i++)
        {
            window[i] = (float)0.5 * (1 - Mathf.Cos(2 * Mathf.PI * i / audioClip.frequency));
        }
    }


    private float[] window;
}