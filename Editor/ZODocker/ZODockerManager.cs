﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MS.Shell.Editor;
using UnityEngine;

namespace ZO.Editor{

public class ZODockerManager
{
    public static string dockerLogColor = "#207020";
    public static bool showLogs = true;
    public static bool isRunning = false;
    public static string composeWorkingDirectory = "../../docker/dev"; // default Compose file for development

    public static Task<bool> IsZODockerRunning(){
        var options = new EditorShell.Options(){
            workDirectory = composeWorkingDirectory,
            environmentVars = new Dictionary<string, string>(){ }
        };

        // Create a task and return it so clients can use async/await
        // Use TaskCompletionSource so that we can manually fulfill the task when 
        // the shell script executes the onExit callback
        TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();

        string command = "if [ $(docker inspect -f '{{.State.Running}}' zosim) = \"true\" ]; then exit 0; else exit 1; fi";
        // Execute docker command
        var shellTask = EditorShell.Execute(command, options);
        DockerLog($"Checking if docker running...");
        shellTask.onLog += (EditorShell.LogType logType, string log) => {
            DockerLog(log);
        };
        shellTask.onExit += (exitCode) => {
            Debug.Log("Check if docker running exit code: " + exitCode);
            isRunning = exitCode == 0;
            taskCompletionSource.SetResult(isRunning);
        };

        return taskCompletionSource.Task;
    }

    public static void DockerComposeUp(){
        // Navigate to parent directories where the docker and dockercompose files are located
        var options = new EditorShell.Options(){
            workDirectory = composeWorkingDirectory,
            environmentVars = new Dictionary<string, string>(){ }
        };
        string command = "docker-compose up";
        // Execute docker command
        var task = EditorShell.Execute(command, options);
        DockerLog($"Starting docker container, please wait...");
        task.onLog += (EditorShell.LogType logType, string log) => {
            DockerLog(log);
        };
        task.onExit += (exitCode) => {
            if(exitCode == 0){
                isRunning = true;
            }
            
            DockerLog($"Docker compose up exit: {exitCode}", forceDisplay: true);
        };

        
    }

    public static void DockerComposeDown(){
        // Navigate to parent directories where the docker and dockercompose files are located
        var options = new EditorShell.Options(){
            workDirectory = composeWorkingDirectory,
            environmentVars = new Dictionary<string, string>(){ }
        };
        string command = "docker-compose down";
        // Execute docker command
        var task = EditorShell.Execute(command, options);
        DockerLog($"Stopping Docker, please wait...", forceDisplay: true);
        task.onLog += (EditorShell.LogType logType, string log) => {
            DockerLog(log);
        };
        task.onExit += (exitCode) => {
            DockerLog($"Docker compose down exit: {exitCode}", forceDisplay: true);
        };

        isRunning = false;
    }

    private static string BuildVolumesString(string[] volumes){

            if(volumes == null) return string.Empty;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            
            foreach(var v in volumes){
                builder.Append(" -v ");
                builder.Append(v);
            }

            return builder.ToString();
    }

    public static void DockerRun(string service, 
                                 string command, 
                                 string[] additionalVolumes = null, 
                                 Action<int> callback = null) {

        // docker-compose -f ./docker/docker-compose.yml run --rm 
        // zosim_tools python ./zo-asset-tools/zo_convex_decomposition/zo_convex_decomposition.py
        var options = new EditorShell.Options(){workDirectory = composeWorkingDirectory,
            environmentVars = new Dictionary<string, string>(){ }
        };

        string volumes = BuildVolumesString(additionalVolumes);

        // Run command in a new container, and delete after execution with --rm
        string dockerCommand = $"docker-compose run --rm{volumes} {service} {command}";
        Debug.Log(dockerCommand);

        // Execute docker command
        var task = EditorShell.Execute(dockerCommand, options);
        DockerLog($"Executing command on {service}...", forceDisplay: true);
        task.onLog += (EditorShell.LogType logType, string log) => {
            DockerLog(log);
        };
        task.onExit += (exitCode) => {
            DockerLog($"Docker compose run exit: {service} ## {command}", forceDisplay: true);
            callback(exitCode);
        };
    }
    
    private string GetCommandPath(string command){
        System.Diagnostics.ProcessStartInfo getDockerPathProcessInfo = new System.Diagnostics.ProcessStartInfo();
        getDockerPathProcessInfo.FileName = "which";
        getDockerPathProcessInfo.Arguments = command;
        getDockerPathProcessInfo.UseShellExecute = false;
        getDockerPathProcessInfo.RedirectStandardError = true;
        getDockerPathProcessInfo.RedirectStandardOutput = true;

        System.Diagnostics.Process process = System.Diagnostics.Process.Start(getDockerPathProcessInfo);
        string output = process.StandardOutput.ReadToEnd();
        return output;
    }

    private static void DockerLog(string message, bool forceDisplay = false)
    {
        if(!showLogs && !forceDisplay) return;

        UnityEngine.Debug.Log($"<color={dockerLogColor}>{message}</color>");
        
    }
}
}