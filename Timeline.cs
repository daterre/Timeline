using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Timeline
{
	public TimelineMode Mode = TimelineMode.Update;

    public MonoBehaviour Script { get; private set;}
	public Coroutine Coroutine { get; private set; }
	public bool IsEnded { get; private set; }
	public float PlayTime { get; private set; }
	public int LoopsCompleted { get; private set; }
	public float Duration { get { return _lastWaitMarker; } }

    List<TimelineEvent> _events;
    Dictionary<string, object> _vars = null;
	float _lastWaitMarker = 0f;
	float _longestSequenceDuration = 0f;
    	
    public Timeline(MonoBehaviour script, TimelineMode mode = TimelineMode.Update)
    {
    	this.Script = script;
		this.IsEnded = false;
		this.PlayTime = 0f;
		this.LoopsCompleted = 0;
		this.Mode = mode;

    	_events = new List<TimelineEvent>();
    }

	#region Vars
	// ..............................................................
	public Timeline Var<T>(string name, T val)
    {
        if (_vars == null)
            _vars = new Dictionary<string, object>();
        _vars[name] = val;
        return this;
    }

    public T Var<T>(string name)
    {
        object val;
        if (_vars == null || !_vars.TryGetValue(name, out val))
            throw new ArgumentException(name + " is not defined in the timeline vars.");

        return (T) val;
    }

    public IEnumerable<string> Vars
    {
        get { if (_vars == null) return null; else return _vars.Keys.AsEnumerable(); }
    }
	// ..............................................................
	#endregion

	#region Event queuing methods
	// ..............................................................

	// TODO: allow naming sequences
	private Timeline Tween(string sequenceName, float duration, Action<TimelineSequence> action)
    {
    	_events.Add(new TimelineSequence(){
			Timeline = this,
			EventIndex = _events.Count,
			TimeCode = _lastWaitMarker,
			Name = sequenceName,
    		Duration = duration,
    		UpdateAction = action
    	});

		if (duration > _longestSequenceDuration)
			_longestSequenceDuration = duration;
    		
    	return this;
    }

	public Timeline Tween(float duration, Action<TimelineSequence> action)
	{
		return Tween(null, duration, action);
	}

	public Timeline Frames(float duration, int frameCount, Action<TimelineSequence, int> action)
	{
		int? prevFrame = -1;
		return Tween(duration, s =>
		{
			int currentFrame = Mathf.FloorToInt(s.Time * frameCount);
			if (currentFrame < frameCount && currentFrame != prevFrame.Value)
			{
				prevFrame = currentFrame;
				action(s, currentFrame);
			}
		});
	}

	public Timeline Frames(Sprite[] frames, float fps, SpriteRenderer renderer)
	{
		return Frames(
			frames.Length / fps,
			frames.Length,
			(s, i) => renderer.sprite = frames[i]
		);
	}

	public Timeline Nest(Action<Timeline> init)
	{
		Timeline nested = this.Script.Timeline(this.Mode);
		
		init(nested);
		Nest(nested);

		return this;
	}

	public Timeline Nest(Timeline nested)
	{
		if (nested == null)
			throw new System.ArgumentNullException("nested");

		//if (nested.Script.gameObject != this.Script.gameObject)
		//	throw new System.ArgumentException("Nested timeline must be created from the same game object as the parent timeline.");

		//if (nested.Mode != this.Mode)
		//	throw new System.ArgumentException("Nested timeline mode must be the same as the parent timeline mode.");

		_events.Add(new TimelineNested()
		{
			Timeline = this,
			InnerTimeline = nested,
			EventIndex = _events.Count,
			TimeCode = _lastWaitMarker
		});

		if (nested.Duration > _longestSequenceDuration)
			_longestSequenceDuration = nested.Duration;

		return this;
	}
    	
    public Timeline Do(Action<TimelineAction> action)
    {
		_events.Add(new TimelineAction()
		{
			Timeline = this,
			EventIndex = _events.Count,
			TimeCode = _lastWaitMarker,
			Action = action
		});

    	return this;
    }
    	
    public Timeline Wait()
    {
		// Don't add redundant / unnecessary waits
		if (_events.Count == 0 || _events[_events.Count - 1] is TimelineWaitMarker)
			return this;

		_lastWaitMarker += _longestSequenceDuration;
		_longestSequenceDuration = 0f;

		_events.Add(new TimelineWaitMarker() {
			Timeline = this,
			EventIndex = _events.Count,
			TimeCode = _lastWaitMarker
		});
    		
    	return this;
    }
    	
    public Timeline Hold(float duration)
    {
		Wait();
		
    	_events.Add(new TimelineSequence(){
			Timeline = this,
    		Duration = duration,
			EventIndex = _events.Count,
			TimeCode = _lastWaitMarker
    	});
    		
		_lastWaitMarker += duration;
		_longestSequenceDuration = 0f;

        Wait();

    	return this;
    }

	public Timeline LoopBegin(int loopCount = -1)
	{
		Wait();

		_events.Add(new TimelineLoopMarker() {
			Timeline = this,
			EventIndex = _events.Count,
			TimeCode = _lastWaitMarker,
			MarkerType = TimelineLoopMarkerType.Begin,
			LoopCount = loopCount
		});

		return this;
	}

	public Timeline LoopEnd()
	{
		Wait();

		_events.Add(new TimelineLoopMarker()
		{
			Timeline = this,
			EventIndex = _events.Count,
			TimeCode = _lastWaitMarker,
			MarkerType = TimelineLoopMarkerType.End
		});

		return this;
	}
	// ..............................................................
	#endregion

	#region Control methods
	// ..............................................................

	public Timeline Play()
    {
		if (this.Coroutine != null)
			throw new System.InvalidOperationException("Timeline is already playing.");

		// Close any open loops
		bool openLoop = false;
		for (int i = 0; i < _events.Count; i++)
		{
			if (_events[i] is TimelineLoopMarker)
			{
				var loopMarker = (TimelineLoopMarker) _events[i];
				openLoop = loopMarker.MarkerType == TimelineLoopMarkerType.Begin;
			}
		}

		if (openLoop)
		{
			// A loop was left open, so close it (will also add a wait)
			this.LoopEnd();
		}
		else
		{
			// Add a wait marker to the end of the sequence list, so that the coroutine will wait for all the timeline to finish
			this.Wait();
		}

		this.Coroutine = this.Script.StartCoroutine(PlayInternal());
    	return this;
    }

    public Timeline Stop()
    {
		for (int i = 0; i < _events.Count; i++) { 
			if (_events[i] is TimelineNested) {
				var nested = (TimelineNested)_events[i];
				nested.InnerTimeline.Stop();
			}
		}

		if (this.Script != null && this.Coroutine != null)
			this.Script.StopCoroutine(this.Coroutine);

		this.IsEnded = true;

		return this;
    }

	System.Collections.IEnumerator PlayInternal()
	{
		this.PlayTime = 0f;
		this.LoopsCompleted = 0;

		TimelineLoopMarker loopStartMarker = null;
		 
		for (int eventIndex = 0; eventIndex < _events.Count; eventIndex++)
		{
			TimelineEvent timelineEvent = _events[eventIndex];

			// ......................
			// TimelineAction
			if (timelineEvent is TimelineAction)
			{
				// Invoke the action immediately
				((TimelineAction)timelineEvent).Invoke();
			}

			// ......................
			// TimelineWaitMarker
			else if (timelineEvent is TimelineWaitMarker)
			{
				var waitMarker = (TimelineWaitMarker) timelineEvent;

				while (true)
				{
					// Wait for all previous sequences to end
					bool alldone = true;
					for (int j = eventIndex - 1; j >= 0; j--)
					{
						var prevEvent = _events[j] as TimelineSpan;
						
						if (prevEvent is TimelineSequence && !prevEvent.IsEnded)
							(prevEvent as TimelineSequence).Advance();

						if (prevEvent != null)
							alldone &= prevEvent.IsEnded;
					}

					//if (this.Script.name == "peggBlock (1)")
					//	Debug.LogFormat("Loop {0} - {1:0.00}", this.LoopsCompleted, this.PlayTime);

					if (alldone)
					{
						break;
					}
					else
					{
						yield return this.Mode == TimelineMode.FixedUpdate ?
							new WaitForFixedUpdate() :
							null;

						if (this.Script == null)
						{
							this.Stop();
							yield break;
						}

						this.PlayTime +=
							this.Mode == TimelineMode.FixedUpdate ? Time.fixedDeltaTime :
							this.Mode == TimelineMode.Realtime ? Time.unscaledDeltaTime :
							Time.deltaTime;
					}
				}
			}

			// ......................
			// TimelineLoopMarker
			else if (timelineEvent is TimelineLoopMarker)
			{
				var loopMarker = (TimelineLoopMarker)timelineEvent;
				if (loopMarker.MarkerType == TimelineLoopMarkerType.Begin)
				{
					// Start a new loop from here
					loopStartMarker = loopMarker;
					this.LoopsCompleted = 0;
				}
				else if (loopMarker.MarkerType == TimelineLoopMarkerType.End)
				{
					// Stop the current loop
					if (loopStartMarker != null)
					{
						this.LoopsCompleted ++;
						if (loopStartMarker.LoopCount > 0 && this.LoopsCompleted == loopStartMarker.LoopCount)
						{
							// End the current loop
							loopStartMarker = null;
						}
						else
						{
							// Restart the loop
							eventIndex = loopStartMarker.EventIndex; // +1 in the for loop will actually advance this to the next event after the loop begin
							this.PlayTime -= loopMarker.TimeCode - loopStartMarker.TimeCode;
						}
					}
					else
						Debug.LogError("LoopEnd found with no matching LoopBegin");
				}
			}

			// ......................
			// TimelineSpan
			else if (timelineEvent is TimelineSpan)
			{
				var sequence = (TimelineSpan)timelineEvent;
				sequence.Play();
			}
		}

		this.IsEnded = true;
	}

	// ..............................................................
	#endregion
}

public static class TimelineExtenstions
{
    public static Timeline Timeline(this MonoBehaviour script, TimelineMode mode = TimelineMode.Update)
    {
		return new Timeline(script, mode);
    }
}

public enum TimelineMode
{
    Update,
	FixedUpdate,
	Realtime
}
