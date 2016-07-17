using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public sealed class TimelineSequence : TimelineSpan
{
    public Action<TimelineSequence> UpdateAction;
   
	public string Name { get; internal set; }
	public float Duration { get; internal set; }
	public float Time { get; private set; }

	IEnumerator _enumerator = null;

    internal TimelineSequence()
    {
    	Time = 0f;
    }

	public override bool IsEnded {
		get { return this.Time >= 1f; }
	}
    
    public T Var<T>(string name)
    {
        return this.Timeline.Var<T>(name);
    }

	internal override void Play()
	{
		this.Time = 0f;

		if (Duration <= 0f)
		{
			// Sync
			if (UpdateAction != null)
				UpdateAction(this);
			this.Time = 1f;
		}
		else
		{
			// Async
			_enumerator = PlayAsync();
		}
	}

	internal bool Advance()
	{
		if (_enumerator == null)
			throw new InvalidOperationException("Can't advance the sequence - it hasn't been started.");

		return _enumerator.MoveNext();
	}

	internal IEnumerator PlayAsync()
    {
		bool done = false;

    	while(true)
    	{
			this.Time = Mathf.Clamp01((this.Timeline.PlayTime - this.TimeCode)/Duration);
			
			//if (this.Timeline.Script.name == "peggBlock (1)")
			//	Debug.LogFormat("{0} - {1:P}", this.Timeline.Script.name, this.Time);

			if (Time >= 1f)
			{
				done = true;
				Time = 1f;
			}

    		if (UpdateAction != null)
    			UpdateAction(this);

			if (done)
                break;

			yield return null;
    	}
    }
}
