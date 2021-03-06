using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// This is a manager class used to overlook the running of a simulation.
/// </summary>
public class Simulation : MonoBehaviour {

	public enum State {
		/// <summary>
		/// BotNavSim is not simulating.
		/// </summary>
		inactive,
		
		/// <summary>
		/// Simulation is about to start.
		/// </summary>
		starting,
		
		/// <summary>
		/// Simulation is running.
		/// </summary>
		simulating,
		
		/// <summary>
		/// Simulation is stopped.
		/// </summary>
		stopped,
		
		/// <summary>
		/// Simulation has reached the end of the batch. 
		/// </summary>
		end
	}

	public enum StopCode {
		/// <summary>
		/// Reason for simulation stopping is not given.
		/// </summary>
		Unspecified,
		
		/// <summary>
		/// Simulation stopped because user requested next test.
		/// </summary>
		UserRequestNextTest,
		
		/// <summary>
		/// Simulation stopped because the robot reached the destination.
		/// </summary>
		RobotReachedDestination,
		
		/// <summary>
		/// Simulation stopped because the maximum test time was exceeded.
		/// </summary>
		MaxTimeExceeded,
		
		/// <summary>
		/// Simulation stopped because the robot appears to be stuck.
		/// i.e. the robot position has not changed for some time.
		/// </summary>
		RobotIsStuck
	}
	
	
	
	[System.Serializable]
	public class Settings {
		
		/// <summary>
		/// The title of this simulation.
		/// </summary>
		public string title = "Simulation";
				
		/// <summary>
		/// The number of repeat tests with these parameters.
		/// </summary>
		public int numberOfTests = 1;
		
		/* ### Core Parameters ### */
		
		/// <summary>
		/// The filename of the environment to load.
		/// </summary>
		public string environmentName = "<none>";
		/// <summary>
		/// The filename of the navigation assembly to load.
		/// </summary>
		public string navigationAssemblyName = "<none>";
		/// <summary>
		/// The filename of the robot to load.
		/// </summary>
		public string robotName = "<none>";

				
		/* ### Initial Conditions ### */
		
		/// <summary>
		/// If true, robot starts each test at a random location in simulation bounds.
		/// </summary>
		public bool randomizeOrigin = false;
		/// <summary>
		/// If true, destination starts each test at a random location in simulation bounds.
		/// </summary>
		public bool randomizeDestination = false;
		
		/* ### Termination Conditions ### */
		
		/// <summary>
		/// The maximum test time in seconds.
		/// </summary>
		public int maximumTestTime = 60;
		/// <summary>
		/// If true, test ends when robot reaches the destination.
		/// </summary>
		public bool continueOnNavObjectiveComplete = false;
		/// <summary>
		/// If true, test ends when robot average position over time doesn't change enough.
		/// </summary>
		public bool continueOnRobotIsStuck = false;
		
		
		/// <summary>
		/// Gets a value indicating whether this <see cref="Simulation.Settings"/> is valid for simulating with.
		/// </summary>
		/// <value><c>true</c> if is valid; otherwise, <c>false</c>.</value>
		public bool isValid {
			get {
				bool v = true;
				v &= environmentName != "<none>";
				v &= navigationAssemblyName != "<none>";
				v &= robotName != "<none>";
				v &= numberOfTests > 0;
				return v;
			}
		}
		
		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="Simulation.Settings"/> is active
		/// and determines which properties are editable in UI.
		/// </summary>
		/// <value><c>true</c> if active; otherwise, <c>false</c>.</value>
		public bool active { get; set; }
		
		public string name {
			get {
				return robotName + "_" + navigationAssemblyName + "_" + environmentName;
			}
		}
		public string fileName {
			get {
				return datetime.ToString("yyyyMMdd_HHmmss_") +
					Simulation.settings.title + ".xml";
			}
		}
		public System.DateTime datetime {get; private set;}
		public string date {
			get {
				return datetime.ToShortDateString();
			}
		}
		public string time {
			get {
				return datetime.ToShortTimeString();
			}
		}
		
		public Settings() {
			datetime = System.DateTime.Now;
		}
		
		/// <summary>
		/// Randomly select simulation parameters. 
		/// </summary>
		public void Randomize() {
			title = "Random Settings";
			numberOfTests = 3;
			environmentName = EnvLoader.RandomEnvironmentName();
			robotName = BotLoader.RandomRobotName();
			navigationAssemblyName = NavLoader.RandomPluginName();
			randomizeDestination = true;
			continueOnNavObjectiveComplete = true;
			continueOnRobotIsStuck = true;
		}
	}

	// observer class that defines an area that includes the robot and destination during a simulation
	private class Observer : IObservable {
		public Observer() {}
		
		public string name {
			get { return "Simulation"; }
		}
		
		public Bounds bounds {
			get {
				// encapsulate robot and destination
				if (isRunning) {
					Bounds b = new Bounds();
					b.Encapsulate(robot.position);
					b.Encapsulate(destination.transform.position);
					return b;
				} 
				// use Simulation.Instance.Bounds
				else {
					return Simulation.Instance.bounds;
				}
			}
		}
	}

	/// <summary>
	/// Initializes the <see cref="Simulation"/> class.
	/// </summary>
	static Simulation() {
		testArea = new Observer();
	}

	/// <summary>
	/// Reference to the MonoBebehaviour instance
	/// </summary>
	public static Simulation Instance;
	
	
	/* ### Static  Properties ### */
	
	/// <summary>
	/// Gets the simulation state.
	/// </summary>
	public static State state {get; private set;}
	
	/// <summary>
	/// Exhibition mode will run continually and 
	/// randomly choose camera perspectives and simulation settings.
	/// </summary>
	public static bool exhibitionMode;  
	
	/// <summary>
	/// Gets or sets the settings for the current simulation.
	/// </summary>
	public static Settings settings {
		get { return _settings; }
		set {
			_settings.active = false;
			_settings = value;
			if (_settings == null) _settings = new Settings();	
			_settings.active = true;
		}
	}

	/// <summary>
	/// List of settings to iterate through in batch mode.
	/// </summary>
	public static List<Settings> batch = new List<Settings>();
	
	/// <summary>
	/// Gets the simulation number (index in batch list, 1 to batch.Count).
	/// </summary>
	public static int simulationNumber {
		get; private set;
	}
	
	/// <summary>
	/// Gets the current test number (1 to settings.numberOfTests).
	/// </summary>
	public static int testNumber {
		get; private set;
	}
	
	/// <summary>
	/// Gets reference to the robot in the current simulation.
	/// </summary>
	public static Robot robot {
		get { return _robot; }
		private set {
			if(_robot) _robot.transform.Recycle();
			_robot = value;
			robot.destination = destination.transform;
		}
	}
	
	// Reference to the environment
	/// <summary>
	/// Gets or sets reference to the environment in the current simulation.
	/// </summary>
	public static GameObject environment {
		get {
			return _environment.gameObject;
		}
		set {
			if (isRunning) Halt(0);
			if (_environment) _environment.transform.Recycle();
			_environment = value.GetComponent<Environment>();
			SetBounds();
		}
	}
	
	// Reference to the destination
	/// <summary>
	/// Gets reference to the destination.
	/// </summary>
	public static GameObject destination { 
		get; private set; 
	}
	
	/// <summary>
	/// Gets the test area (Observer object)
	/// </summary>
	public static IObservable testArea {
		get; private set;
	}
	
	// Simulation states
	/// <summary>
	/// Gets a value indicating whether this <see cref="Simulation"/> has not yet started.
	/// </summary>
	/// <value><c>true</c> if pre simulation; otherwise, <c>false</c>.</value>
	public static bool isInactive {
		get { return state == State.inactive; }
	}
	/// <summary>
	/// Gets a value indicating whether this <see cref="Simulation"/> is running.
	/// </summary>
	/// <value><c>true</c> if is running; otherwise, <c>false</c>.</value>
	public static bool isRunning {
		get { return state == State.simulating; }
	}
	/// <summary>
	/// Gets a value indicating whether this <see cref="Simulation"/> is stopped.
	/// </summary>
	/// <value><c>true</c> if is stopped; otherwise, <c>false</c>.</value>
	public static bool isStopped {
		get { return state == State.stopped; }
	}
	/// <summary>
	/// Gets a value indicating whether this <see cref="Simulation"/> is finished.
	/// </summary>
	/// <value><c>true</c> if is finished; otherwise, <c>false</c>.</value>
	public static bool isFinished {
		get { return state == State.end; }
	}

	
	/// <summary>
	/// If true, simulation will be logged to a file via Log class.
	/// </summary>
	public static bool loggingEnabled = true;
	
	// is the simulation paused?
	/// <summary>
	/// Gets or sets a value indicating whether this <see cref="Simulation"/> is paused.
	/// </summary>
	/// <value><c>true</c> if paused; otherwise, <c>false</c>.</value>
	public static bool paused {
		get { return _paused; }
		set {
			_paused = value;
			if (_paused) Time.timeScale = 0f;
			else Time.timeScale = timeScale;
		}
	}

	/// <summary>
	/// Time (in seconds) since robot started searching for destination.
	/// </summary>
	public static float time {
		get {
			if (isRunning) _stopTime = Time.time;
			return _stopTime - _startTime;
		}
	}
	
	/// <summary>
	/// Gets or sets the time scale.
	/// </summary>
	public static float timeScale {
		get { return _timeScale; }
		set {
			_timeScale = value;
			if (!paused) {
				Time.timeScale = value;
			}
		}
	}

	// Time variables used to calculate Simulation.time
	private static float _startTime;
	private static float _stopTime;
	
	private static Settings _settings;
	private static Robot _robot;
	private static Environment _environment;
	private static bool _paused;
	private static float _timeScale = 1f;
	
	/* ### Static Methods ### */
	
	/// <summary>
	/// Enter this instance.
	/// </summary>
	public static void Enter() {
		CamController.AddViewMode(CamController.ViewMode.Birdseye);
		CamController.AddViewMode(CamController.ViewMode.FreeMovement);
		CamController.AddViewMode(CamController.ViewMode.Mounted);
		CamController.AddViewMode(CamController.ViewMode.Orbit);
		CamController.AddAreaOfInterest(testArea);
	}
	
	/// <summary>
	/// Begin simulating.
	/// </summary>
	public static void Begin() {
		Debug.Log("Simulation Begin");
		simulationNumber = 0;
		NextSimulation();
	}
	
	/// <summary>
	/// Halt current simulation.
	/// Load the next simulation in batch, or
	///  change state to State.end if at the end of batch. 
	/// </summary>
	public static void NextSimulation() {
		// stop current simulation
		if (state == State.simulating) {
			Halt(StopCode.Unspecified);
		}
		// next in batch
		simulationNumber++;
		if (simulationNumber > batch.Count) {
			// end of batch
			Halt(StopCode.Unspecified);
			End();
			return;
		}
		Debug.Log("Simulation NextSimulation: " + simulationNumber + " of " + batch.Count);
		// load simulation settings
		settings = batch[simulationNumber-1];
		Log.Settings();
		// load environment
		EnvLoader.SearchForEnvironments();
		environment = EnvLoader.LoadEnvironment(settings.environmentName);
		destination.transform.position = RandomInBounds(Instance.bounds);
		// load robot
		if (robot) CamController.RemoveAreaOfInterest(robot);
		BotLoader.SearchForRobots();
		robot = BotLoader.LoadRobot(settings.robotName);
		robot.navigation = NavLoader.LoadPlugin(settings.navigationAssemblyName);
		// configure camera
		CamController.AddAreaOfInterest(robot);
		CamController.SetViewMode(CamController.ViewMode.Birdseye);
		CamController.SetAreaOfInterest(robot);
		// reset test number
		testNumber = 0;
		NextTest();
	}
	
	/// <summary>
	/// Stops the current test and starts the next test in current simulation.
	/// </summary>
	public static void NextTest() {
		if (testNumber >= settings.numberOfTests) {
			Halt(StopCode.Unspecified);
			NextSimulation();
			return;
		}
		// start test routine
		if (state != State.starting) Instance.StartCoroutine(StartTestRoutine());
	}
	
	/// <summary>
	/// Run the simulation. 
	/// </summary>
	public static void Run() {
		Debug.Log("Simulation run.");
		if (state == State.stopped) {
			Time.timeScale = _timeScale;
			state = State.simulating;
		}
	}
	
	/// <summary>
	/// Pause the simulation. 
	/// </summary>
	public static void Pause() {
		Debug.Log("Simulation Pause.");
		if (state == State.simulating) {
			Time.timeScale = 0f;
			state = State.stopped;
		}
	}
	
	/// <summary>
	/// Halt simulation and write log to file. 
	/// </summary>
	/// <param name="code">Reason for halt.</param>
	public static void Halt(StopCode code) {
		Debug.Log("Simulation Halt! " + code.ToString());
		// stop logging
		if (Log.logging) Log.Stop(code);

		// freeze the robot
		if (robot) {
			robot.rigidbody.velocity = Vector3.zero;
			robot.rigidbody.angularVelocity = Vector3.zero;
			robot.moveEnabled = false;
		}
		// set simulation state
		state = State.stopped;
	}
	
	
	/// <summary>
	/// Stop all simulations.
	/// </summary>
	public static void End() {
		Debug.Log("Simulation End.");
		settings.active = false;
		state = State.end;
		
		// in exhibition mode, run more simulations with random settings
		if (exhibitionMode) {
			if (batch.Count > 10) {
				batch.RemoveAt(0);
			}
			settings = new Settings();
			settings.Randomize();
			batch.Add(settings);
			Begin();
		}
	}
	
	/// <summary>
	/// Exit simulation. 
	/// </summary>
	public static void Exit() {
		Debug.Log("Simulation Exit.");
		Instance.StopAllCoroutines();
		settings = null;
		if (Log.logging) Log.Stop(StopCode.Unspecified);
		if (robot) robot.Recycle();
		if (environment) environment.transform.Recycle();
		CamController.ClearAreaList();
		CamController.ClearViewModeList();
		state = State.inactive;
	}
	
	/// <summary>
	/// Return a random position inside the bounds, but 
	/// not inside any physical objects.
	/// </summary>
	/// <returns>Random position inside bounds.</returns>
	public static Vector3 RandomInBounds(Bounds b) {
		Vector3 v = new Vector3();
		v.x = Random.Range(b.min.x, b.max.x);
		v.y = b.max.y;
		v.z = Random.Range(b.min.z, b.max.z);
		RaycastHit hit;
		if (Physics.Raycast(v, Vector3.down, out hit)) {
			v = hit.point + hit.normal* 0.25f;
			Debug.DrawRay(v, Vector3.down, Color.white, 5f);
		}
		return v;
	}
	
	/// <summary>
	/// Routine for starting a new test
	/// </summary>
	/// <returns>The test routine.</returns>
	private static IEnumerator StartTestRoutine() {
		if (isRunning) Halt(StopCode.Unspecified);
		state = State.starting;
		CamController.Instance.OnTestEnd();
		yield return new WaitForSeconds(1f);
		
		// place the robot
		robot.Reset();
		PlaceRobotInStartArea();
			
		// place the destination
		PlaceDestination();
			
		yield return new WaitForSeconds(1f);
		
		CamController.Instance.OnTestStart();
		destination.SendMessage("ChooseRandomSprite");
		testNumber++;
		Debug.Log("Simulation NextTest: " + testNumber + " of " + settings.numberOfTests);
		
		yield return new WaitForSeconds(1f);
		
		_startTime = Time.time;
		state = State.simulating;
		if (loggingEnabled) Log.Start();
		robot.moveEnabled = true;
		robot.NavigateToDestination();
	}
	
	/// <summary>
	/// Places the robot in start area.
	/// </summary>
	private static void PlaceRobotInStartArea() {
		if (settings.randomizeOrigin) {
			robot.position = RandomInBounds(_environment.originBounds);
		} else {
			robot.position = _environment.originBounds.center;
		}
		robot.transform.rotation = Quaternion.identity;
	}
	
	/// <summary>
	/// Places the destination.
	/// </summary>
	private static void PlaceDestination() {
		if (settings.randomizeDestination) {
			destination.transform.position = RandomInBounds(_environment.destinationBounds);
		} else {
			destination.transform.position = _environment.destinationBounds.center;
		}
	}
	
	// Set the simulation bounds to encapsulate all renderers in scene
	private static void SetBounds() {
		Bounds b = new Bounds();
		foreach(Renderer r in environment.GetComponentsInChildren<Renderer>())
			b.Encapsulate(r.bounds);
			
		Instance.bounds = b;
	}

	/** Instance Methods **/
	
	/// <summary>
	/// The simulation bounds described as a cube. This is the search
	/// space indicated to INavigation.
	/// </summary>
	public Bounds bounds {
		get; private set;
	}
	
	
	// Called on gameobject created
	void Awake() {
		// singleton pattern (can only be one Instance of Simulation)
		if (Instance) {
			Destroy(this.gameObject);
		}
		else {
			Instance = this;
		}
		_settings = new Settings();
	}
	
	/// <summary>
	/// Called on the first frame
	/// </summary>
	void Start() {
		destination = GameObject.Find("Destination");
	}
	
	/// <summary>
	/// Update this instance (called every rendered frame)
	/// </summary>
	void Update() {
		if (isRunning) {
			// check for conditions to end the test
			if (robot.atDestination && settings.continueOnNavObjectiveComplete) {
				Debug.Log("Simulation: nav objective complete!");
				Halt(StopCode.RobotReachedDestination);
				NextTest();
			}
			else if (robot.isStuck && settings.continueOnRobotIsStuck) {
				Debug.LogWarning("Simulation: Robot appears to be stuck! Skipping test.");
				Halt(StopCode.RobotIsStuck);
				NextTest();
			}
			else if (settings.maximumTestTime > 0 && time > settings.maximumTestTime) {
				Debug.LogWarning("Simulation: Max test time exceeded! Skipping test.");
				Halt(StopCode.MaxTimeExceeded);
				NextTest();
			}

		}
	}

	/// <summary>
	/// Raises the draw gizmos event.
	/// </summary>
	void OnDrawGizmos() {
		Gizmos.DrawWireCube(bounds.center, bounds.size);
	}
	
	/// <summary>
	/// called before application shuts down
	/// </summary>
	void OnApplicationQuit() {
		Log.Stop(StopCode.Unspecified);
	}
}
