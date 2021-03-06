﻿using UnityEngine;
using System.Collections;

/// <summary>
/// BotNavSim high-level manager class. Holds BotNavSim.State and controls state transition behaviour.
/// </summary>
public class BotNavSim  {
	
	static BotNavSim() {
		_state = State.Idle;
	}
	
	public enum State {
		/// <summary>
		/// Idle state - no particular functionality in operation. 
		/// </summary>
		Idle,
		
		/// <summary>
		/// Program is running a simulation. Functionality managed by Simulation class.
		/// </summary>
		Simulating,
		
		/// <summary>
		/// Program is displaying BotPath data loaded from CSV. Functionality is managed by DataPlayback class.
		/// </summary>
		ViewingData,
		
		/// <summary>
		/// Program is editing a robot. Functionality is managed by RobotCreator class.
		/// </summary>
		EditingRobot,
		
		/// <summary>
		/// Program is editing an environment. Functionality is managed by EnvironmentEditor class. 
		/// </summary>
		EditingEnvironment
	}
	

	
	/// <summary>
	/// Gets or sets the state.
	/// </summary>
	public static State state {
		get {
			return _state;
		}
		set {
			ChangeState(value);
		}
	}
	
	/// <summary>
	/// Gets a value indicating whether <see cref="BotNavSim"/> is idle.
	/// </summary>
	/// <value><c>true</c> if is idle; otherwise, <c>false</c>.</value>
	public static bool isIdle {
		get {
			return _state == State.Idle;
		}
	}
	
	/// <summary>
	/// Gets a value indicating whether <see cref="BotNavSim"/> is simulating.
	/// </summary>
	/// <value><c>true</c> if is simulating; otherwise, <c>false</c>.</value>
	public static bool isSimulating {
		get {
			return _state == State.Simulating;
		}
	}
	
	/// <summary>
	/// Gets a value indicating whether <see cref="BotNavSim"/> is viewing data.
	/// </summary>
	/// <value><c>true</c> if is viewing data; otherwise, <c>false</c>.</value>
	public static bool isViewingData {
		get {
			return _state == State.ViewingData;
		}
	}
	
	/// <summary>
	/// Gets a value indicating whether this <see cref="BotNavSim"/> is editing robot.
	/// </summary>
	/// <value><c>true</c> if is editing robot; otherwise, <c>false</c>.</value>
	public static bool isEditingRobot {
		get {
			return _state == State.EditingRobot;
		}
	}
	
	/// <summary>
	/// Gets a value indicating whether this <see cref="BotNavSim"/> is editing environment.
	/// </summary>
	/// <value><c>true</c> if is editing environment; otherwise, <c>false</c>.</value>
	public static bool isEditingEnvironment {
		get {
			return _state == State.EditingEnvironment;
		}
	}
	
	private static State _state;
	
	private static void ChangeState(State newState) {
		// exit old state behaviour
		switch (_state) {
		case State.Idle:
			break;
		case State.Simulating:
			Simulation.Exit();
			break;
		case State.ViewingData:
			LogLoader.Exit();
			break;
		case State.EditingRobot:
			break;
		case State.EditingEnvironment:
			break;
		}
		
		_state = newState;
		// enter new state behaviour
		switch (newState) {
		case State.Idle:
			break;
		case State.Simulating:
			Simulation.Enter();
			break;
		case State.ViewingData:
			LogLoader.Enter();
			break;
		case State.EditingRobot:
			break;
		case State.EditingEnvironment:
			break;
		}
		
		Debug.Log("BotNavSim: " + state.ToString());
	}
	
}

