//#define DEBUG_DRAWNEIGHBORS
using UnityEngine;
using UnitySteer;
using UnitySteer.Helpers;


/// <summary>
/// Steering behavior which goes through all SteerForNeighbor behaviors
/// attached to the object and calls their CalculateNeighborContribution
/// method for each neighbor.
/// </summary>
/// <remarks>
/// Sample values to user for flocking boids (angles are in degrees):
/// 
/// public float separationRadius =   5;
/// public float separationAngle  = 135;
/// public float separationWeight =  12;
/// 
/// public float alignmentRadius =    7.5f;
/// public float alignmentAngle  =   45;
/// public float alignmentWeight =    8;
/// 
/// public float cohesionRadius  =    9;
/// public float cohesionAngle   =   99;
/// public float cohesionWeight  =    8;
/// 
/// 
/// This behavior will return a pure direction vector, which is the normalized
/// aggregation of the force vectors of each of the SteerForNeigbhors descendants
/// that are attached to the game object. These *are* affected by the
/// steering's weight in relation to the others, but the final resulting
/// force depends entirely on the weight of the SteerForNeighborGroup 
/// behavior.
/// </remarks>
[AddComponentMenu("UnitySteer/Steer/... for Neighbor Group")]
public class SteerForNeighborGroup : Steering
{
	#region Private properties
	[SerializeField]
	float _minRadius = 3f;
	[SerializeField]
	float _maxRadius = 7.5f;
	[SerializeField]
	float _angleCos = 0.7f;	
	[SerializeField]
	LayerMask _layersChecked;

	SteerForNeighbors[] _behaviors;
	#endregion


	#region Public properties
	/// <summary>
	/// Cosine of the maximum angle
	/// </summary>
	/// <remarks>All boid-like behaviors have an angle that helps limit them.
	/// We store the cosine of the angle for faster calculations</remarks>
	public float AngleCos {
		get {
			return this._angleCos;
		}
		set {
			_angleCos = Mathf.Clamp(value, -1.0f, 1.0f);
		}
	}

	/// <summary>
	/// Degree accessor for the angle
	/// </summary>
	/// <remarks>The cosine is actually used in calculations for performance reasons</remarks>
	public float AngleDegrees
	{
		get
		{
			return OpenSteerUtility.DegreesFromCos(_angleCos);;
		}
		set
		{
			_angleCos = OpenSteerUtility.CosFromDegrees(value);
		}
	}	

	/// <summary>
	/// Indicates the vehicles on which layers are evaluated on this behavior
	/// </summary>	
	public LayerMask LayersChecked {
		get {
			return this._layersChecked;
		}
		set {
			_layersChecked = value;
		}
	}

	/// <summary>
	/// Minimum radius in which another vehicle is definitely considered in the neighborhood
	/// </summary>
	public float MinRadius {
		get {
			return this._minRadius;
		}
		set {
			_minRadius = value;
		}
	}	

	/// <summary>
	/// Maximum neighborhood radius
	/// </summary>
	public float MaxRadius {
		get {
			return this._maxRadius;
		}
		set {
			_maxRadius = value;
		}
	}		
	#endregion	


	#region Methods
	protected override void Awake()
	{
		base.Awake();
		_behaviors = GetComponents<SteerForNeighbors>();
		foreach(var b in _behaviors)
		{
			// Ensure UnitySteer does not call them
			b.enabled = false;
		}
	}

	protected override Vector3 CalculateForce ()
	{
		// steering accumulator and count of neighbors, both initially zero
		Vector3 steering = Vector3.zero;
		Profiler.BeginSample("SteerForNeighborGroup.Looping over neighbors");
		for (int i = 0; i < Vehicle.Radar.Vehicles.Count; i++) {
			var other  = Vehicle.Radar.Vehicles[i];
			if (!other.GameObject.Equals(null) &&
			    (1 << other.GameObject.layer & LayersChecked) != 0 &&
			    Vehicle.IsInNeighborhood(other, MinRadius, MaxRadius, AngleCos)) 
			{
				#if DEBUG_DRAWNEIGHBORS
				Debug.DrawLine(Vehicle.Position, other.Position, Color.magenta);
				#endif
				Profiler.BeginSample("SteerForNeighborGroup.Adding");
				for(int bi = 0; bi < _behaviors.Length; bi++)
				{
					steering += _behaviors[bi].CalculateNeighborContribution(other) * _behaviors[bi].Weight;
				}
				Profiler.EndSample();
			}
		};
		Profiler.EndSample();

		Profiler.BeginSample("Normalizing");
		// Normalize for pure direction
		steering.Normalize();
		Profiler.EndSample();

		return steering;
	}
	#endregion

}
