using System.Collections.Generic;
using UnityEngine;

// Taken from here:
// https://gist.github.com/davidfoster/48acce6c13e5f7f247dc5d5909dce349
/// <summary>A Kalman filter implementation for <c>Vector3</c> values.</summary>
public class KalmanFilterVector3 {

	//-----------------------------------------------------------------------------------------
	// Constants:
	//-----------------------------------------------------------------------------------------

	public const float DEFAULT_Q = 0.000001f;
	public const float DEFAULT_R = 0.01f;

	public const float DEFAULT_P = 1;

	//-----------------------------------------------------------------------------------------
	// Private Fields:
	//-----------------------------------------------------------------------------------------

	private float q;
	private float r;
	private float p = DEFAULT_P;
	private Vector3 x;
	private float k;

	//-----------------------------------------------------------------------------------------
	// Constructors:
	//-----------------------------------------------------------------------------------------

	// N.B. passing in DEFAULT_Q is necessary, even though we have the same value (as an optional parameter), because this
	// defines a parameterless constructor, allowing us to be new()'d in generics contexts.
	public KalmanFilterVector3() : this(DEFAULT_Q) { }

	public KalmanFilterVector3(float aQ = DEFAULT_Q, float aR = DEFAULT_R) {
		q = aQ;
		r = aR;
	}

	//-----------------------------------------------------------------------------------------
	// Public Methods:
	//-----------------------------------------------------------------------------------------

	public Vector3 Update(Vector3 measurement, float? newQ = null, float? newR = null) {

		// update values if supplied.
		if (newQ != null && q != newQ) {
			q = (float)newQ;
		}
		if (newR != null && r != newR) {
			r = (float)newR;
		}
		
		// update measurement.
		{
			k = (p + q) / (p + q + r);
			p = r * (p + q) / (r + p + q);
		}

		// filter result back into calculation.
		Vector3 result = x + (measurement - x) * k;
		x = result;
		return result;
	}

	public Vector3 Update(List<Vector3> measurements, bool areMeasurementsNewestFirst = false, float? newQ = null, float? newR = null) {

		Vector3 result = Vector3.zero;
		int i = (areMeasurementsNewestFirst) ? measurements.Count - 1 : 0;

		while (i < measurements.Count && i >= 0) {

			// decrement or increment the counter.
			if (areMeasurementsNewestFirst) {
				--i;
			}
			else {
				++i;
			}

			result = Update(measurements[i], newQ, newR);
		}

		return result;
	}

	public void Reset() {
		p = 1;
		x = Vector3.zero;
		k = 0;
	}
}