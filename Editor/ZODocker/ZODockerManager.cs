﻿using System.Collections;
using System.Collections.Generic;
using MS.Shell.Editor;
using UnityEngine;

namespace ZO.Editor{

public class ZODockerManager
{
    public static string dockerLogColor = "#207020";
    public static bool showLogs = true;
    public static bool isRunning = false;

    public static void DockerComposeUp(){
        // Navigate to parent directories where the docker and dockercompose files are located
        var options = new EditorShell.Options(){
            workDirectory = "../../docker/",
            //encoding = System.Text.Encoding.GetEncoding("GBK"),
            environmentVars = new Dictionary<string, string>(){
                //{"PATH", "usr/bin"}
            }
        };
        string command = "docker-compose up";
        // Execute docker command
        var task = EditorShell.Execute(command, options);
        DockerLog($"Starting docker container, please wait...");
        task.onLog += (EditorShell.LogType logType, string log) => {
            DockerLog(log);
        };
        task.onExit += (exitCode) => {
            DockerLog($"Docker compose up exit: {exitCode}", forceDisplay: true);
        };

        isRunning = true;
    }

    public static void DockerComposeDown(){
        // Navigate to parent directories where the docker and dockercompose files are located
        var options = new EditorShell.Options(){
            workDirectory = "../../docker/",
            //encoding = System.Text.Encoding.GetEncoding("GBK"),
            environmentVars = new Dictionary<string, string>(){
                //{"PATH", "usr/bin"}
            }
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