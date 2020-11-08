﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using UnityEngine;
using Varjo;
using System.Linq;
using UnityEngine.SceneManagement;

public class DataLogger : MonoBehaviour
{
    private const string vehicleDataFileName = "vehicleData.csv";
    private const string navigationLineFileName = "navigationLine.csv";
    private const string targetDetectionDataFileName = "targetDetectionData.csv";
    private const string generalTargetInfo = "generalTargetInfo.csv";
    private const string generalExperimentInfo = "GeneralExperimentInfo.csv";
    private const string fixationFileName = "fixationData.csv";
    private const string dataFolder = "Data";
    //our car
    public GameObject car;
    private Navigator carNavigator;
    //Current Navigation
    public Transform navigation;
    private NavigationHelper navigationHelper;
    private Vector3[] navigationLine;
    private int indexClosestPoint = 0;//Used for calculating the distance to optimal navigation path (i.e., centre of raod)

    //Experiment manager;
    private ExperimentManager experimentManager;

    private MyGazeLogger myGazeLogger;
   
    //list of items
    private List<VehicleDataPoint> vehicleData;
    private List<TargetAlarm> targetDetectionData;

    private ExperimentInput experimentInput;
    private Transform player;

    private string saveFolder;
    private string steerInputAxis;
    [HideInInspector]
    public bool savedData;
    public bool logging;

    private void OnApplicationQuit() { if (experimentInput.saveData && !savedData) { SaveData(); } }
    private void Awake()
    {
        StartUpFunction();
    }
    void StartUpFunction()
    {
        
        Debug.Log("Started data logger...");
        player = GameObject.FindGameObjectWithTag("Player").transform;
        experimentInput = player.GetComponent<ExperimentInput>();

        experimentManager = GetComponent<ExperimentManager>();
        if (!experimentInput.saveData)
        {
            GetComponent<DataLogger>().enabled = false;
            logging = false;
            return;
        }

        //gameState = experimentManager.gameState;
        myGazeLogger = GetComponent<MyGazeLogger>();
        myGazeLogger.experimentManager = experimentManager;
        myGazeLogger.startAutomatically = false;
        myGazeLogger.useCustomLogPath = true;
        if (experimentManager.camType != MyCameraType.Normal) { myGazeLogger.cam = experimentManager.CameraTransform(); }

        navigationHelper = navigation.GetComponent<NavigationHelper>();
        navigationLine = navigationHelper.GetNavigationLine();

        List<string> inputs = experimentManager.GetCarControlInput();
        steerInputAxis = inputs[0];

        carNavigator = car.GetComponent<Navigator>();

    }
    private void Update()
    {
        if (logging) { AddVehicleData(); }
    }
    public void SaveData()
    {
        if (!logging) { return; }
        Debug.Log($"Saving all data to {saveFolder}...");
        if (experimentManager.camType != MyCameraType.Normal) { SaveFixationData(); myGazeLogger.StopLogging();  }

        SaveVehicleData();
        SaveTargetDetectionData();
        SaveTargetInfoData();
        SaveNavigationData();
        SaveGeneralExperimentInfo();

        logging = false;
        savedData = true;
    }
    private void SaveFixationData()
    {
        Debug.Log("Saving fixation data...");
        //Order of LoggedTag enum
        /*World,
        Target,
        HUDSymbology,
        HUDText,
        ConformalSymbology,
        InsideCar,
        LeftMirror,
        RightMirror,
        RearMirror,*/

        IEnumerable<LoggedTags> loggedTags = EnumUtil.GetValues<LoggedTags>();
        string[] columns = new string[loggedTags.Count()]; int index = 0;
        foreach (LoggedTags tag in loggedTags) { columns[index] = tag.ToString(); index++; }

        string[] logData = new string[columns.Length];
        string filePath = string.Join("/", saveFolder, fixationFileName);

        using (StreamWriter file = new StreamWriter(filePath))
        {
            Log(columns, file);

            logData[0] = myGazeLogger.fixationData.world.ToString();
            logData[1] = myGazeLogger.fixationData.target.ToString();
            logData[2] = myGazeLogger.fixationData.hudSymbology.ToString();
            logData[3] = myGazeLogger.fixationData.hudText.ToString();
            logData[4] = myGazeLogger.fixationData.conformalSymbology.ToString();
            logData[5] = myGazeLogger.fixationData.insideCar.ToString();
            logData[6] = myGazeLogger.fixationData.leftMirror.ToString();
            logData[7] = myGazeLogger.fixationData.rightMirror.ToString();
            logData[8] = myGazeLogger.fixationData.rearMirror.ToString();
            logData[9] = myGazeLogger.fixationData.unknown.ToString();

            Log(logData, file);
            file.Flush();
            file.Close();
        }
    }
    private void SaveVehicleData()
    {
        Debug.Log("Saving vehicle data...");
        string[] columns = { "Time", "Frame", "Speed", "DistanceToOptimalPath", "Position", "Rotation", "SteeringInput", "UpcomingOperation" };
        string[] logData = new string[columns.Length];
        string filePath = string.Join("/", saveFolder, vehicleDataFileName);

        using (StreamWriter file = new StreamWriter(filePath))
        {
            Log(columns, file);

            foreach (VehicleDataPoint dataPoint in vehicleData)
            {
                logData[0] = dataPoint.time.ToString();
                logData[1] = dataPoint.frame.ToString();
                logData[2] = dataPoint.speed.ToString();
                logData[3] = dataPoint.distanceToOptimalPath.ToString();
                logData[4] = dataPoint.position;
                logData[5] = dataPoint.rotation;
                logData[6] = dataPoint.steerInput.ToString();
                logData[7] = dataPoint.upcomingOperation;

                Log(logData, file);
            }
            file.Flush();
            file.Close();
        }
    }
    private void SaveTargetDetectionData()
    {
        Debug.Log("Saving alarms...");
        string[] columns = { "Time", "Frame", "AlarmType", "ReactionTime", "TargetID", "TargetDifficulty"};
        string[] logData = new string[columns.Length];
        string filePath = string.Join("/", saveFolder, targetDetectionDataFileName);
        
        using (StreamWriter file = new StreamWriter(filePath))
        {
            Log(columns, file);
            
            foreach (TargetAlarm alarm in targetDetectionData)
            {
                logData[0] = alarm.time.ToString();
                logData[1] = alarm.frame.ToString();
                logData[2] = alarm.alarmType.ToString();
                logData[3] = alarm.reactionTime.ToString();
                logData[4] = alarm.targetID;
                logData[5] = alarm.targetDifficulty.ToString();

                Log(logData, file);

            }
            file.Flush();
            file.Close();
        }
    }
    private void SaveNavigationData()
    {
        Debug.Log("Saving navigation info...");
        string[] columns = { "NavigationLine" };
        string[] logData = new string[columns.Length];
        string filePath = string.Join("/", saveFolder, navigationLineFileName);

        Vector3[] navigationLine = navigationHelper.GetNavigationLine();

        using (StreamWriter file = new StreamWriter(filePath))
        {
            Log(columns, file);
            
            foreach (Vector3 point in navigationLine)
            {
                logData[0] = point.ToString("F3");
                Log(logData, file);
            }
            file.Flush();
            file.Close();
        }
    }
    private void SaveTargetInfoData()
    {
        Debug.Log("Saving target info...");
        string[] columns = { "ID", "Detected", "ReactionTime", "FixationTime", "Difficulty","Side","AfterTurn", "DifficultPosition","waypointOperation","Position" };
        string[] logData = new string[columns.Length];
        string filePath = string.Join("/", saveFolder, generalTargetInfo);
        
        List<Target> targets = navigationHelper.GetAllTargets();

        using (StreamWriter file = new StreamWriter(filePath))
        {
            Log(columns, file);
            
            foreach(Target target in targets)
            {
                logData[0] = target.GetID();
                logData[1] = target.detected.ToString();
                logData[2] = target.reactionTime.ToString();
                logData[3] = target.totalFixationTime.ToString();
                logData[4] = target.difficulty.ToString();
                logData[5] = target.side.ToString();
                logData[6] = target.afterTurn.ToString();
                logData[7] = target.difficultPosition.ToString();
                logData[8] = target.waypoint.operation.ToString();
                logData[9] = target.transform.position.ToString("F3");
                Log(logData, file);
            }
            file.Flush();
            file.Close();
        }
    }
    void SaveGeneralExperimentInfo()
    {
        Debug.Log("Saving general experiment info...");
        string[] columns = { "Total Targets", "LeftTarget", "RightTargets", TargetDifficulty.easy.ToString(), TargetDifficulty.medium.ToString(), TargetDifficulty.hard.ToString(), "RelativePositionDriverView", "RelativeRotationDriverView",
                              "Navigation", "Condition", "Transparency", "TotalExperimentTime", "LeftTurns","RightTurns"};
        string[] logData = new string[columns.Length];
        string filePath = string.Join("/", saveFolder, generalExperimentInfo);
        TargetCountInfo targetCountInfo = navigationHelper.targetCountInfo;
        List<DifficultyCount> targetDifficulty = navigationHelper.targetDifficultyList;
        
        using (StreamWriter file = new StreamWriter(filePath))
        {
            Log(columns, file);

            //General target info
            logData[0] = targetCountInfo.totalTargets.ToString();
            logData[1] = targetCountInfo.LeftPosition.ToString();
            logData[2] = targetCountInfo.rightPosition.ToString();

            int index = 3;
            foreach (DifficultyCount difficltyCount in targetDifficulty)
            { 
                logData[index] = difficltyCount.count.ToString();
                index++;
            }

            //Info on the driver view
            Vector3 relativePosition = experimentManager.driverView.position - car.transform.position;
            Quaternion relativeRotation = Quaternion.Inverse(car.transform.rotation) * experimentManager.driverView.rotation;
            logData[6] = relativePosition.ToString("F3");
            logData[7] = relativeRotation.eulerAngles.ToString("F3");

            //Experiment inputs
            logData[8] = experimentManager.activeExperiment.navigation.name;
            logData[9] = experimentManager.activeExperiment.navigationType.ToString();
            logData[10] = experimentManager.activeExperiment.transparency.ToString();
            logData[11] = experimentManager.activeExperiment.experimentTime.ToString();

            logData[12] = experimentManager.activeNavigationHelper.leftTurns.ToString();
            logData[13] = experimentManager.activeNavigationHelper.rightTurns.ToString();
            //Log data and close file
            Log(logData, file);
            file.Flush();
            file.Close();
        }
    }
    void Log(string[] values, StreamWriter file)
    {
        string line = "";
        if(values == null) { Debug.LogError("Got null values in Log()..."); return; }
        for (int i = 0; i < values.Length; ++i)
        {
            values[i] = values[i].Replace("\r", "").Replace("\n", ""); // Remove new lines so they don't break csv
            line += values[i] + (i == (values.Length - 1) ? "" : ";"); // Do not add semicolon to last data string
        }
        file.WriteLine(line);
    }
    public void StartNewMeasurement()
    {

        Debug.Log($"Starting new measurement...");
        logging = true;
        savedData = false;
        saveFolder = SaveFolder();

        vehicleData = new List<VehicleDataPoint>();
        targetDetectionData = new List<TargetAlarm>();

        if (experimentManager.camType != MyCameraType.Normal)
        {
            myGazeLogger.customLogPath = saveFolder + "/";
            myGazeLogger.fixationData = new Fixation();
            if (myGazeLogger.IsLogging()) { myGazeLogger.RestartLogging(); }
            else { myGazeLogger.StartLogging(); }
        }
    }
    private string SaveFolder()
    {
        //Save folder will be .../unityproject/Data/subjectName-date/subjectName/navigationName

        /*string[] assetsFolderArray = Application.dataPath.Split('/'); //Gives .../unityproject/assest

        //emmit unityfolder/assets and keep root folder

        string[] baseFolderArray = new string[assetsFolderArray.Length - 2];
        for (int i = 0; i < (assetsFolderArray.Length - 2); i++) { baseFolderArray[i] = assetsFolderArray[i]; }

        string baseFolder = string.Join("/", baseFolderArray);*/
        string saveFolder = string.Join("/", experimentInput.subjectDataFolder, SceneManager.GetActiveScene().name + DateTime.Now.ToString("_HH-mm-ss"));
        Debug.Log($"Creaiting savefolder: {saveFolder}...");
        Directory.CreateDirectory(saveFolder);
        

        return saveFolder;
    }
    private void AddVehicleData()
    {
        if (vehicleData == null)
        {
            Debug.Log("ERROR: Vehicle data was corrupted... Re-started measurement...");
            StartNewMeasurement();
        }
        VehicleDataPoint dataPoint = new VehicleDataPoint();
        dataPoint.time = experimentManager.activeExperiment.experimentTime;
        dataPoint.frame = Time.frameCount;

        dataPoint.distanceToOptimalPath = GetDistanceToOptimalPath(car.transform.position);
        dataPoint.position = car.gameObject.transform.position.ToString("F3");
        dataPoint.rotation = car.gameObject.transform.rotation.eulerAngles.ToString("F3");
        dataPoint.steerInput = Input.GetAxis(steerInputAxis);
        dataPoint.speed = car.GetComponent<Rigidbody>().velocity.magnitude;
        dataPoint.upcomingOperation = carNavigator.target.operation.ToString();
        vehicleData.Add(dataPoint);
    }
    private float GetDistanceToOptimalPath(Vector3 car_position)
    {
        
        //(1) Checks if current line segment is still the closest one 
        //(2)Then calculates the distance to the line they span
        
        float current_distance,next_distance, former_distance;

        current_distance = DistanceClosestPointOnLineSegment(navigationLine[indexClosestPoint], navigationLine[indexClosestPoint + 1], car_position);

        while (true)
        {
            //check forward
            if (indexClosestPoint + 2 >= navigationLine.Length) { break; }

            next_distance = DistanceClosestPointOnLineSegment(navigationLine[indexClosestPoint + 1], navigationLine[indexClosestPoint + 2], car_position);
            
            if (next_distance < current_distance)
            {
                //If closer we  update distance and index and redo the loop
                current_distance = next_distance;
                indexClosestPoint++;
                continue;
            }

            //check backward
            if (indexClosestPoint - 2 <= 0) { break; }

            
            former_distance = DistanceClosestPointOnLineSegment(navigationLine[indexClosestPoint - 2], navigationLine[indexClosestPoint  - 1], car_position);
           
            if (former_distance < current_distance) { 
                current_distance = former_distance;
                indexClosestPoint--;
            }

            if (former_distance > current_distance && next_distance > current_distance) { break; }
        }

        return current_distance;
    }
    float DistanceClosestPointOnLineSegment(Vector3 segmentStart, Vector3 segmentEnd, Vector3 point)
    {
        // Shift the problem to the origin to simplify the math.    
        var wander = point - segmentStart;
        var span = segmentEnd - segmentStart;

        // Compute how far along the line is the closest approach to our point.
        float t = Vector3.Dot(wander, span) / span.sqrMagnitude;

        // Restrict this point to within the line segment from start to end.
        t = Mathf.Clamp01(t);

        // Return this point.
        return Vector3.Distance(segmentStart + t * span, point);
    }
    public void AddFalseAlarm()
    {
        if (!logging) { return; }
        TargetAlarm alarm = new TargetAlarm();
        alarm.time = experimentManager.activeExperiment.experimentTime;
        alarm.frame = Time.frameCount;
        alarm.alarmType = false;

        targetDetectionData.Add(alarm);
        Debug.Log("Added false alarm...");
    }
    public void AddTrueAlarm(Target target)
    {
        if (!logging) { return; }
        TargetAlarm alarm = new TargetAlarm();

        alarm.time = experimentManager.activeExperiment.experimentTime;
        alarm.frame = Time.frameCount;
        alarm.alarmType = true;
        alarm.targetID = target.GetID();
        alarm.reactionTime = alarm.time - target.startTimeVisible;

        //The difficulty names always end with a number ranging from 1-6
        alarm.targetDifficulty = target.GetTargetDifficulty().ToString();
        targetDetectionData.Add(alarm);

        Debug.Log($"Added true alarm for {target.GetID()}, reaction time: {Math.Round(alarm.reactionTime, 2)}s ...");
    }
   
}
public class VehicleDataPoint
{
    public float time;
    public int frame;

    public float speed;
    public float distanceToOptimalPath;
    public string position;
    public string rotation;
    public float steerInput;

    public string upcomingOperation;
}

public class TargetAlarm
{
    public float time;
    public int frame;

    public bool alarmType;
    public float reactionTime;
    public string targetID;
    public string targetDifficulty;

    public TargetAlarm()
    {
        targetID = "-";
        reactionTime = 0f;
        targetDifficulty = "-";
    }
}


public class NavigationLine
{
    public Vector3[] navigationLine;
}
