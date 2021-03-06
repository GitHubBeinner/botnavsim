﻿using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Reflection;
using System.Collections.Generic;


/// <summary>
/// Plugin factory utility class instantates interfaces from a DLL assembly file
/// Adapted from: http://stackoverflow.com/questions/5751844/how-to-reference-a-dll-on-runtime
/// </summary>
public class PluginFactory<T> {

	/// <summary>
	/// Find and instantiate a type from a specified assembly file (accepts only DLL files)
	/// </summary>
	/// <returns>The plugin.</returns>
	/// <param name="file">File.</param>
	public T CreatePlugin(string file) {
		if (!file.EndsWith(".dll")) {
			file += ".dll";
		}
		if (!File.Exists(file)) {
			Debug.LogError("File not found (" + file + ")");
			return default(T);
		}
		Type[] assemblyTypes =  Assembly.LoadFrom(file).GetTypes();
		foreach(Type assemblyType in assemblyTypes) {
			Type interfaceType = assemblyType.GetInterface(typeof(T).FullName);
			// if our interface is found, instantiate and return it!
			if (interfaceType != null) {
				return (T)Activator.CreateInstance(assemblyType);
			}
		}
		Debug.LogError("Interface " + typeof(T).FullName + " not found in file: " + file);
		return default(T);
	}
	
	/// <summary>
	/// Lists assembly (DLL) file names that implement the type T.
	/// </summary>
	/// <returns>The plugins.</returns>
	/// <param name="path">Path.</param>
	public List<string> ListPlugins(string path) {
		List<string> list = new List<string>();
		// find .dll files
		foreach (string file in Directory.GetFiles(path, "*.dll")) {
			Assembly assembly;
			try {
				assembly = Assembly.LoadFrom(file);
			}
			catch(Exception e) {
				Debug.LogError(file + ": " + e + " " + e.Message + ")");
				continue;
			}
			Type[] types;
			try {
				types = assembly.GetTypes();
			}
			catch(Exception e) {
				Debug.LogError(file + ": " + e + " " + e.Message + ")");
				continue;
			}
			foreach (Type assemblyType in types) {
				Type interfaceType = assemblyType.GetInterface(typeof(T).FullName);
				if (interfaceType != null) {
					list.Add(Path.GetFileNameWithoutExtension(file));
				}
			}
		}
		
		return list;
	}
	
}
