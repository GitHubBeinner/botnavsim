﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// IToolbar class for simulation control. 
/// Provides controls for pausing, stopping or skipping the current test
/// Provides a toggle for exhibition mode.
/// Provides a slider for simulation timescale.
/// </summary>
public class UI_SimulationControl : IToolbar {

	public UI_SimulationControl() {
		_editSettings = new UI_SimulationSettings(Simulation.settings);
		hidden = true;
	}

	public bool contextual {
		get {
			return BotNavSim.isSimulating;
		}
	}

	public bool hidden {
		get; set; 
	}

	public string windowTitle {
		get {
			return "Simulation: " + Simulation.state.ToString();
		}
	}

	public Rect windowRect {
		get; set; 
	}
	
	public GUI.WindowFunction windowFunction {
		get {
			return MainWindow;
		}
	}
	
	
	private bool _liveEditSettings;
	private UI_SimulationSettings _editSettings;
	
	
	/// <summary>
	/// Simulation settings window function called by UI_Toolbar.
	/// </summary>
	void MainWindow (int windowID) {
		float lw = 200f;
				
		// simulation information
		GUILayout.BeginHorizontal(GUILayout.Width(UI_Toolbar.I.innerWidth));
		GUILayout.Label(Simulation.settings.title + "(" + Simulation.simulationNumber + "/" +
		                Simulation.batch.Count + ") ", GUILayout.Width(lw));
		GUILayout.Label("Test " + Simulation.testNumber + "/" + Simulation.settings.numberOfTests);
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal(GUILayout.Width(UI_Toolbar.I.innerWidth));
		GUILayout.Label("Time (s): " + Simulation.time.ToString("G3") + "/" + 
			Simulation.settings.maximumTestTime, GUILayout.Width(lw));
		GUILayout.EndHorizontal();

		
		// exhbition mode tickbox
		GUILayout.BeginHorizontal(GUILayout.Width(UI_Toolbar.I.innerWidth));
		GUILayout.Label("Exhibition Mode: ", GUILayout.Width(lw));
		Simulation.exhibitionMode = GUILayout.Toggle(Simulation.exhibitionMode, "");
		GUILayout.EndHorizontal();
		
		// timescale slider 
		GUILayout.BeginHorizontal(GUILayout.Width(UI_Toolbar.I.innerWidth));
		GUILayout.Label("Simulation Timescale: ", GUILayout.Width(lw));
		Simulation.timeScale = GUILayout.HorizontalSlider(
			Simulation.timeScale,
			0.5f, 4f);
		GUILayout.EndHorizontal();
		
		// contextual control buttons
		if (Simulation.isRunning) {
			GUILayout.BeginHorizontal();
			if (Simulation.paused) {
				if (GUILayout.Button("Play"))
					Simulation.paused = false;
			}
			else {
				if (GUILayout.Button("Pause"))
					Simulation.paused = true;
			}
			if (GUILayout.Button("Stop")) {
				Simulation.exhibitionMode = false;
				Simulation.End();
			}
			GUILayout.EndHorizontal();
			if (GUILayout.Button("Next Test")) {
				Simulation.NextTest();
			}
			
		}
		if (Simulation.isFinished) {
			if  (GUILayout.Button("Start Again")) {
				Simulation.Begin();
			}
			if (GUILayout.Button("New Simulation...")) {
				Simulation.End();
			}
		}
		
		// show/hide button for edit settings window
		if (_liveEditSettings) {
			if (GUILayout.Button("Hide Settings")) {
				_liveEditSettings = false;
				UI_Toolbar.I.additionalWindows.Remove((IWindowFunction)_editSettings);
			}
		}
		else {
			if (GUILayout.Button("Show Settings")) {
				_editSettings = new UI_SimulationSettings(Simulation.settings);
				_liveEditSettings = true;
				UI_Toolbar.I.additionalWindows.Add((IWindowFunction)_editSettings);
			}
		}
		
		// update controls when _editSettings is completed
		if (_liveEditSettings) {
			if (_editSettings.windowFunction == null) {
				_liveEditSettings = false;
			}
		}

	}
	

}
