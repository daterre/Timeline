using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class Timeline: TimelineSpan
{
	public readonly TimelineFrameType FrameType = TimelineFrameType.Update;

	public TimelineState State { get; private set; }
	public MonoBehaviour Script { get; private set; }
	public Coroutine Coroutine { get; private set; }
	public int LoopsCompleted { get; private set; }

	List<TimelineEvent> _events;
	Dictionary<string, object> _vars = null;
	float _lastWaitMarker = 0f;
	float _longestSequenceDuration = 0f;
	float _currentTimecode = 0f;

	public Timeline(MonoBehaviour script, TimelineFrameType mode = TimelineFrameType.Update)
	{
		this.State = TimelineState.Editable;
		this.Script = script;
		this.LoopsCompleted = 0;
		this.FrameType = mode;

		_events = new List<TimelineEvent>();
	}

	void AssertWritable()
	{
		if (this.State != TimelineState.Editable)
			throw new InvalidOperationException("The timeline is readonly once it has been started.");
	}

	void AssertRoot()
	{
		if (this.Timeline != null)
			throw new InvalidOperationException("A nested timeline can't be controlled directly - use Play, Stop and JumpTo on the root timeline only.");
	}

	public float CurrentTimecode
	{
		get
		{
			return this.Timeline == null ?
				_currentTimecode :
				this.Timeline.CurrentTimecode - this.EventTimecode;
		}
	}

	public override float Duration
	{
		get
		{
			return _lastWaitMarker;
		}
	}	

	public bool IsEnded
	{
		get { return CurrentTimecode >= Duration; }
	}

	public bool IsPlaying
	{
		get { return this.Coroutine != null; }
	}

	public float LastFrameDuration
	{
		get
		{
			return this.FrameType == TimelineFrameType.FixedUpdate ? Time.fixedDeltaTime :
				this.FrameType == TimelineFrameType.Realtime ? Time.unscaledDeltaTime :
				Time.deltaTime;
		}
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

		return (T)val;
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
		AssertWritable();

		_events.Add(new TimelineSequence()
		{
			Timeline = this,
			EventIndex = _events.Count,
			EventTimecode = _lastWaitMarker,
			Name = sequenceName,
			DurationInternal = duration,
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
			int currentFrame = Mathf.FloorToInt(s.NormalizedTime * frameCount);
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
		AssertWritable();

		Timeline nested = this.Script.Timeline(this.FrameType);
		nested.Timeline = this;

		init(nested);
		Nest(nested);

		return this;
	}

	public Timeline Nest(Timeline nested)
	{
		AssertWritable();

		if (nested == null)
			throw new System.ArgumentNullException("nested");

		if (nested.State != TimelineState.Editable)
			throw new InvalidOperationException("Can't nest a timeline that has already been started.");

		if (nested.Timeline != null && nested.Timeline != this)
			throw new InvalidOperationException("The timeline is already nested in a different parent timeline.");

		if (nested._events.Any(ev => ev is TimelineLoopMarker))
			throw new InvalidOperationException("A nested timeline can't contain any loops. Use Repeat() instead.");

		if (nested.Script.gameObject != this.Script.gameObject)
			throw new System.ArgumentException("Nested timeline must be created from the same game object as the parent timeline.");

		if (nested.FrameType != this.FrameType)
			throw new System.ArgumentException("Nested timeline frame type must be the same as the parent frame type.");

		nested.Timeline = this;
		nested.EventIndex = _events.Count;
		nested.EventTimecode = _lastWaitMarker;
		nested.Prepare();

		if (nested.Duration > _longestSequenceDuration)
			_longestSequenceDuration = nested.Duration;

		_events.Add(nested);

		return this;
	}

	public Timeline Do(Action<TimelineAction> action)
	{
		AssertWritable();

		_events.Add(new TimelineAction()
		{
			Timeline = this,
			EventIndex = _events.Count,
			EventTimecode = _lastWaitMarker,
			Action = action
		});

		return this;
	}

	public Timeline Wait()
	{
		AssertWritable();

		// Don't add redundant / unnecessary waits
		if (_events.Count == 0 || _events[_events.Count - 1] is TimelineWaitMarker)
			return this;

		_lastWaitMarker += _longestSequenceDuration;
		_longestSequenceDuration = 0f;

		_events.Add(new TimelineWaitMarker()
		{
			Timeline = this,
			EventIndex = _events.Count,
			EventTimecode = _lastWaitMarker
		});

		return this;
	}

	public Timeline Hold(float duration)
	{
		AssertWritable();

		Wait();

		_events.Add(new TimelineSequence()
		{
			Timeline = this,
			DurationInternal = duration,
			EventIndex = _events.Count,
			EventTimecode = _lastWaitMarker
		});

		_lastWaitMarker += duration;
		_longestSequenceDuration = 0f;

		Wait();

		return this;
	}

	public Timeline LoopBegin(int loopCount = -1)
	{
		AssertWritable();

		Wait();

		_events.Add(new TimelineLoopMarker()
		{
			Timeline = this,
			EventIndex = _events.Count,
			EventTimecode = _lastWaitMarker,
			MarkerType = TimelineLoopMarkerType.Begin,
			LoopCount = loopCount
		});

		return this;
	}

	public Timeline LoopEnd()
	{
		AssertWritable();

		Wait();

		_events.Add(new TimelineLoopMarker()
		{
			Timeline = this,
			EventIndex = _events.Count,
			EventTimecode = _lastWaitMarker,
			MarkerType = TimelineLoopMarkerType.End
		});

		return this;
	}
	// ..............................................................
	#endregion

	#region Control methods
	// ..............................................................

	void Prepare()
	{
		if (this.State == TimelineState.Started)
			throw new InvalidOperationException("Timeline has already been prepared and started.");

		// Close any open loops
		bool openLoop = false;
		for (int i = 0; i < _events.Count; i++)
		{
			if (_events[i] is TimelineLoopMarker)
			{
				var loopMarker = (TimelineLoopMarker)_events[i];
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

		this.State = TimelineState.Started;
	}

	internal override void Update(float timecode)
	{
		ScrubTo(timecode-this.EventTimecode);
	}

	public Timeline ScrubTo(float timecode)
	{
		if (this.State != TimelineState.Started)
			Prepare();

		if (this.IsPlaying)
			throw new InvalidOperationException("Can't scrub timeline while it is playing. Call Pause() first.");

		using (var currentEvents = GetEventsAtTime(timecode))
		{
			while (currentEvents.MoveNext())
			{
				TimelineEvent ev = currentEvents.Current;

				if (ev is TimelineAction)
				{
					// Invoke the action immediately
					var action = (TimelineAction)ev;
					action.Invoke();
				}
				else if (ev is TimelineSpan)
				{
					var span = (TimelineSpan)ev;
					span.Update(timecode);
				}
			}
		}

		_currentTimecode = timecode;

		return this;
	}

	public Timeline Play()
	{
		AssertRoot();

		if (this.IsPlaying)
			throw new System.InvalidOperationException("Timeline is already playing.");

		if (this.State != TimelineState.Started)
			Prepare();

		// Create the enumerator that will run the timeline
		this.Coroutine = this.Script.StartCoroutine(PlayInternal());
		return this;
	}

	public Timeline Pause()
	{
		if (this.Script != null && this.Coroutine != null)
			this.Script.StopCoroutine(this.Coroutine);

		return this;
	}

	bool Looping(int i, int max, bool reverse)
	{
		return reverse ? i >= 0 : i < max;
	}

	IEnumerator<TimelineEvent> GetEventsAtTime(float timecode, TimelineTraversal behavior = TimelineTraversal.Scrub)
	{
		bool reverse = this.CurrentTimecode > timecode;
		int start = reverse ? _events.Count - 1 : 0;
		float lastFrameDeltaTime = this.LastFrameDuration;

		for (int i = start; Looping(i, _events.Count, reverse); i = reverse ? i-1 : i+1)
		{
			TimelineEvent ev = _events[i];
			if (ev is TimelineSpan)
			{
				// Add the frame delta to the timespan check so that span can always perform a 100% update
				var span = (TimelineSpan)ev;
				if (span.EventTimecode <= timecode && span.EventTimecode + span.Duration > timecode - lastFrameDeltaTime)
					yield return span;
			}
			else
			{
				if (behavior == TimelineTraversal.Scrub)
				{
					if (ev.EventTimecode == timecode)
						yield return ev;
				}
				else if (behavior == TimelineTraversal.Skip)
				{
					if (
						(!reverse && ev.EventTimecode > this.CurrentTimecode && ev.EventTimecode <= timecode) ||
						(reverse && ev.EventTimecode < this.CurrentTimecode && ev.EventTimecode >= timecode)
						)
						yield return ev;
				}
			}
		}
	}

	System.Collections.IEnumerator PlayInternal()
	{
		bool ended = false;
		float timecode = this.CurrentTimecode;
		TimelineLoopMarker loopStartMarker = null;

		while (timecode < Duration || !ended)
		{
			// If it is ended, play one more frame at the final position
			if (timecode >= Duration)
			{
				timecode = Duration;
				ended = true;
			}

			var currentEvents = GetEventsAtTime(timecode);
			try
			{
				while (currentEvents.MoveNext())
				{
					TimelineEvent ev = currentEvents.Current;

					if (ev is TimelineAction)
					{
						// Invoke the action immediately
						var action = (TimelineAction)ev;
						action.Invoke();
					}
					else if (ev is TimelineSpan)
					{
						var span = (TimelineSpan)ev;
						span.Update(timecode);
					}
					else if (ev is TimelineLoopMarker)
					{
						var loopMarker = (TimelineLoopMarker)ev;
						if (loopMarker.MarkerType == TimelineLoopMarkerType.Begin && loopMarker != loopStartMarker)
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
								this.LoopsCompleted++;
								if (loopStartMarker.LoopCount > 0 && this.LoopsCompleted == loopStartMarker.LoopCount)
								{
									// End the current loop
									loopStartMarker = null;
								}
								else
								{
									// Jump to the loop start time, and re-retrieve the events at the new time
									_currentTimecode = loopStartMarker.EventTimecode;
									timecode -= loopMarker.EventTimecode - loopStartMarker.EventTimecode;
									ended = false;

									currentEvents.Dispose();
									currentEvents = GetEventsAtTime(timecode);
								}
							}
							else
								Debug.LogError("LoopEnd found with no matching LoopBegin");
						}
					}
				}
			}
			finally
			{
				currentEvents.Dispose();
			}

			_currentTimecode = timecode;

			// Wait for a frame
			yield return this.FrameType == TimelineFrameType.FixedUpdate ?
				new WaitForFixedUpdate() :
				null;

			// Advance the time
			timecode += LastFrameDuration;
		}

	}

	/*
	System.Collections.IEnumerator PlayInternalOld()
	{
		this.CurrentTimeCode = 0f;
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

					if (alldone)
					{
						break;
					}
					else
					{
						yield return this.FrameType == TimelineFrameType.FixedUpdate ?
							new WaitForFixedUpdate() :
							null;

						// Maybe this component or gameObject has been destroyed - in that case, stop immediately
						if (this.Script == null)
						{
							this.Stop();
							yield break;
						}

						this.CurrentTimeCode +=
							this.FrameType == TimelineFrameType.FixedUpdate ? Time.fixedDeltaTime :
							this.FrameType == TimelineFrameType.Realtime ? Time.unscaledDeltaTime :
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
							this.CurrentTimeCode -= loopMarker.TimeCode - loopStartMarker.TimeCode;
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
				var span = (TimelineSpan)timelineEvent;
				span.Play();
			}
		}
	}*/

	// ..............................................................
	#endregion
}

public static class TimelineExtenstions
{
	public static Timeline Timeline(this MonoBehaviour script, TimelineFrameType mode = TimelineFrameType.Update)
	{
		return new Timeline(script, mode);
	}
}

public enum TimelineFrameType
{
	Update,
	FixedUpdate,
	Realtime
}

public enum TimelineState
{
	Editable,
	Started
}

public enum TimelineTraversal
{
	Scrub,
	Skip
}