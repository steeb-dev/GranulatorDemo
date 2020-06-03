﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Grain : MonoBehaviour
{
    public bool _IsPlaying = false;
    private int _GrainPos;
    private int _GrainLength;
    public float _GrainPitch;
    private float _GrainPitchRand;
    private float _GrainVol;
    private float _GrainAttack = 0.1f;
    private float _GrainRelease = 0.1f;
    public AudioClip _AudioClip;
    private AudioSource _AudioSource;
    private float[] _Samples;
    private float[] _GrainSamples;
    private int _Channels;
    public int _CurrentIndex = -1;
    private int _AudioBufferSize;
    private int _AudioBufferNum;
    private int _GrainOffset;
    public Granulator _Granulator;


    //---------------------------------------------------------------------
    void Start()
    {
        AudioSettings.GetDSPBufferSize(out _AudioBufferSize, out _AudioBufferNum);
    }

    public void Initialise(AudioClip audioClip)
    {
        _AudioSource = GetComponent<AudioSource>();

        if (_AudioSource == null)
        {
            _AudioSource = this.gameObject.AddComponent<AudioSource>();
        }


        _AudioClip = audioClip;

        _Samples = new float[_AudioClip.samples * _AudioClip.channels];
        _Channels = _AudioClip.channels;
        _AudioClip.GetData(_Samples, 0);
        _GrainOffset = 0;


        Debug.Log("SAMPLES IN AUDIO CLIP: " + _Samples.Length);
    }


    //---------------------------------------------------------------------
    void Update()
    {
        // Updates pitch
        _AudioSource.pitch = _GrainPitch + _GrainPitchRand;

        if (this.transform.position.sqrMagnitude > 10000)
            _IsPlaying = false;
    }



    public void PlayGrain(
    float newGrainPos, int newGrainLength, float newGrainPitch,
    float newGrainPitchRand, float newGrainVol, int offset)
    {
        _GrainPos = (int)((newGrainPos * _Samples.Length)); // Rounding to make sure pos always starts at first channel
        _GrainLength = newGrainLength + offset;
        _GrainPitch = newGrainPitch;
        _GrainPitchRand = newGrainPitchRand;
        _GrainVol = newGrainVol;
        _GrainOffset = offset;

        _IsPlaying = true;

        BuildSampleArray();
    }


    //---------------------------------------------------------------------
    private void BuildSampleArray()
    {
        _GrainSamples = new float[_GrainLength];

        int sourceIndex;


        Debug.Log("----------------------------------------");
        Debug.Log("----------------------------------------");

        // Construct grain sample data (including 0s for timing offset)
        for (int i = 0; i < _GrainSamples.Length - _Channels; i += _Channels)
        {
            // Set source audio sample position for grain, and loop to start if the grain is longer than source-audio
            sourceIndex = _GrainPos + i;
            while (sourceIndex + _Channels > _Samples.Length)
                sourceIndex -= _Samples.Length;
            
            // HACCCCCKKKY SHIT
            if (sourceIndex < 0)
                sourceIndex = 0;

            // Populate 0s for grain timing offset
            if (i < _GrainOffset)
                _GrainSamples[i] = 0;

            // Populate with source audio for remainder and apply windowing
            else
            {
                for (int channel = 0; channel < _Channels; channel++)
                {
                    //_GrainSamples[i + channel] = _Samples[sourceIndex + channel - _GrainOffset]
                    //    * _GrainWindow[Mathf.RoundToInt(Map(i, _GrainOffset, _GrainSamples.Length, 0, _GrainWindow.Length))];

                    _GrainSamples[i + channel] = _Samples[sourceIndex + channel]
                        * Windowing(i - _GrainOffset, _GrainSamples.Length - _GrainOffset);
                }
            }
        }

        _CurrentIndex = -1;
    }

    private float Windowing(int currentSample, int grainLength)
    {
        float outputSample = 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * currentSample / grainLength));

        return outputSample;
    }


    //---------------------------------------------------------------------
    private float GetNextSample(int index, int sample)
    {
        _CurrentIndex++;

        // If at the end of the grain duration, stop playing and reset index 
        if (_CurrentIndex >= _GrainSamples.Length)
        {
            _CurrentIndex = -1;
            _IsPlaying = false;
        }        

        float returnSample;

        if (_IsPlaying)
            returnSample = _GrainSamples[_CurrentIndex];
        else
            returnSample = 0;

        return returnSample;
    }


    //---------------------------------------------------------------------
    void OnAudioFilterRead(float[] data, int channels)
    {
        // For length of audio buffer, populate with grain samples, maintaining index over successive buffers (if required)
        for (int bufferIndex = 0; bufferIndex < data.Length; bufferIndex += channels)
        {
            for (int channel = 0; channel < channels; channel++)
            {
                if (_IsPlaying)
                    data[bufferIndex + channel] = GetNextSample(bufferIndex, channel) * _GrainVol;
                else
                    data[bufferIndex + channel] = 0;
            }
        }


        // If this grain is not playing but it's still in active pool, remove from active pool and place in inactive pool
        //if (!_IsPlaying && _Granulator._ActivePool.Contains(this))
        //{
        //    _Granulator._InactivePool.Add(this);
        //    _Granulator._ActivePool.Remove(this);
        //}
    }


    //---------------------------------------------------------------------
    // Utility func to map a value from one range to another range
    private float Map(float val, float inMin, float inMax, float outMin, float outMax)
    {
        return outMin + ((outMax - outMin) / (inMax - inMin)) * (val - inMin);
    }











    // OLD FUNCTIONS


    public void NewGrain(int newGrainPos, int newGrainLength, float newGrainPitch, float newGrainPitchRand, float newGrainVol, float newGrainAttack, float newGrainRelease, Vector3 pos)
    {
        _GrainPos = (int)((newGrainPos / _Channels)) * _Channels; // rounding to make sure pos always starts at first channel!
        _GrainLength = newGrainLength;
        _GrainPitch = newGrainPitch;
        _GrainPitchRand = newGrainPitchRand;
        _GrainVol = newGrainVol;
        _GrainAttack = newGrainAttack;
        _GrainRelease = newGrainRelease;
        _IsPlaying = true;
        BuildSamplesAR();
    }
    //---------------------------------------------------------------------
    private void BuildSamplesAR()
    {
        _GrainSamples = new float[_GrainLength];

        int sourceIndex = _GrainPos;

        // build ar of samples for this grain:
        for (int i = 0; i < _GrainSamples.Length - _Channels; i += _Channels)
        {
            sourceIndex = _GrainPos + i;

            // loop the file if the grain is longer than the source-audio! (or the grain starts at the very end of the source-audio)
            while (sourceIndex + _Channels > _Samples.Length)
                sourceIndex -= _Samples.Length;

            for (int j = 0; j < _Channels; j++)
            {
                _GrainSamples[i + j] = _Samples[sourceIndex + j];
            }
        }

        // fades for the grain, so it doesn't create clicks on start/stop!
        // control with grain attack and grain release inputs
        for (int sampleIndex = 0; sampleIndex < _GrainSamples.Length; sampleIndex += _Channels)
        {
            for (int channel = 0; channel < _Channels; channel++)
            {
                if (sampleIndex < _GrainSamples.Length * _GrainAttack)
                    _GrainSamples[sampleIndex + channel] *= Map(sampleIndex, 0, _GrainSamples.Length * _GrainAttack, 0f, 1f);
                else if (sampleIndex > _GrainSamples.Length * (1.0f - _GrainRelease))
                    _GrainSamples[sampleIndex + channel] *= Map(sampleIndex, _GrainSamples.Length * (1.0f - _GrainRelease), _GrainSamples.Length, 1f, 0f);
            }

        }
        _CurrentIndex = -1;
    }
}