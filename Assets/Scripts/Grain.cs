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
    private int _CurrentIndex = -1;
    public Granulator _Granulator;
    public float[] _GrainWindow;


    //---------------------------------------------------------------------
    void Start()
    {
        UpdateGrain();

        _AudioSource = GetComponent<AudioSource>();
        if (_AudioSource == null)
        {
            _AudioSource = this.gameObject.AddComponent<AudioSource>();
        }
    }

    //---------------------------------------------------------------------
    public void UpdateGrain()
    {
        _Samples = new float[_AudioClip.samples * _AudioClip.channels];
        _Channels = _AudioClip.channels;
        _AudioClip.GetData(_Samples, 0);
    }

    //---------------------------------------------------------------------
    void Update()
    {
        _AudioSource.pitch = _GrainPitch + _GrainPitchRand;
    }


    //---------------------------------------------------------------------
    public void PlayGrain(
        int newGrainPos, int newGrainLength, float newGrainPitch,
        float newGrainPitchRand, float newGrainVol, float[] window, Vector3 pos)
    {
        _GrainPos = (int)((newGrainPos / _Channels)) * _Channels; // Rounding to make sure pos always starts at first channel
        _GrainLength = newGrainLength;
        _GrainPitch = newGrainPitch;
        _GrainPitchRand = newGrainPitchRand;
        _GrainVol = newGrainVol;
        _GrainWindow = window;
        _IsPlaying = true;

        BuildSampleArray();
    }


    //---------------------------------------------------------------------
    private void BuildSampleArray()
    {
        _GrainSamples = new float[_GrainLength];

        // Start position of grain
        int sourceIndex = _GrainPos;

        // Build array of samples for this grain
        for (int sampleIndex = 0; sampleIndex < _GrainSamples.Length - _Channels; sampleIndex += _Channels)
        {
            sourceIndex = _GrainPos + sampleIndex;

            // Loop the file if the grain is longer than the source-audio
            while (sourceIndex + _Channels > _Samples.Length)
                sourceIndex -= _Samples.Length;

            // For each channel, populate buffer with sample from audio clip and apply windowing from lookup array
            for (int channel = 0; channel < _Channels; channel++)
                _GrainSamples[sampleIndex + channel] = _Samples[sourceIndex + channel]
                    * _GrainWindow[Mathf.RoundToInt(Map(sampleIndex, 0, _GrainSamples.Length, 0f, _GrainWindow.Length))];
        }

        _CurrentIndex = -1;
    }


    //---------------------------------------------------------------------
    private float GetNextSample(int index, int sample)
    {
        // If at the end of the grain duration, stop playing and reset index 
        if (_CurrentIndex + 1 >= _GrainSamples.Length)
        {
            _CurrentIndex = -1;
            _IsPlaying = false;
        }

        // Increase index and return sample from grain array
        _CurrentIndex++;
        return _GrainSamples[_CurrentIndex];
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
        if (!_IsPlaying && _Granulator._ActivePool.Contains(this))
        {
            _Granulator._InactivePool.Add(this);
            _Granulator._ActivePool.Remove(this);
        }
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