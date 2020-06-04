using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Grain : MonoBehaviour
{
    public bool _IsPlaying = false;
    private int _GrainPos;
    private int _GrainLength;
    public float _GrainPitch;
    private float _GrainVol;
    public AudioClip _AudioClip;
    private AudioSource _AudioSource;
    private float[] _Samples;
    private float[] _GrainSamples;
    private int _Channels;
    public int _CurrentIndex = -1;
    private int _GrainOffset;
    public Granulator _Granulator;


    //---------------------------------------------------------------------
    void Start()
    {
    }


    //---------------------------------------------------------------------
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
    }


    //---------------------------------------------------------------------
    void Update()
    {
        if (this.transform.position.sqrMagnitude > 10000)
            _IsPlaying = false;
    }


    //---------------------------------------------------------------------
    public void PlayGrain(
    float newGrainPos, int newGrainLength, float newGrainPitch, float newGrainVol, int offset)
    {
        _GrainPos = (int)((newGrainPos * _Samples.Length)); // Rounding to make sure pos always starts at first channel
        _GrainLength = newGrainLength + offset;
        _AudioSource.pitch = newGrainPitch;
        _GrainVol = newGrainVol;
        _GrainOffset = offset;

        _IsPlaying = true;

        BuildSampleArray();
    }


    //---------------------------------------------------------------------
    private void BuildSampleArray()
    {
        // Grain array to pull samples into
        _GrainSamples = new float[_GrainLength];

        int sourceIndex;

        // Construct grain sample data (including 0s for timing offset)
        for (int i = 0; i < _GrainSamples.Length - _Channels; i += _Channels)
        {
            // Set source audio sample position for grain
            sourceIndex = _GrainPos + i;

            // Loop to start if the grain is longer than source audio
            while (sourceIndex + _Channels > _Samples.Length)
                sourceIndex -= _Samples.Length;
            
            // HACCCCCKKKY SHIT - was getting values in the negative somehow
            // Couldn't be bothered debugging just yet
            if (sourceIndex < 0)
                sourceIndex = 0;

            // Populate 0s for grain timing offset
            if (i < _GrainOffset)
                _GrainSamples[i] = 0;

            // Populate remaining samples with source audio and apply windowing
            else
            {
                for (int channel = 0; channel < _Channels; channel++)
                {
                    _GrainSamples[i + channel] = _Samples[sourceIndex + channel]
                        * Windowing(i - _GrainOffset, _GrainSamples.Length - _GrainOffset);
                }
            }
        }

        _CurrentIndex = -1;
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
    }


    //---------------------------------------------------------------------
    private float Windowing(int currentSample, int grainLength)
    {
        float outputSample = 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * currentSample / grainLength));

        return outputSample;
    }


    //---------------------------------------------------------------------
    // Utility func to map a value from one range to another range
    //---------------------------------------------------------------------
    private float Map(float val, float inMin, float inMax, float outMin, float outMax)
    {
        return outMin + ((outMax - outMin) / (inMax - inMin)) * (val - inMin);
    }
}