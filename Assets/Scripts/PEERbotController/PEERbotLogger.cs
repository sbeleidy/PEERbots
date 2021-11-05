using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

using SFB;

public class buttonModification {
    public const string logType = "modifyButton";
    public string palette; 
    public string title;
    public string attributeChanged;
    public string previousValue;
    public string newValue;
    public string time;
    public string date; 

}

public class PEERbotBlinkData {

    public const string logType = "blinkRobot";

    public string time;

    public string date; 

}

public class PEERbotSendModeData {
    public const string logType = "changeConnection";

    public string time;

    public string date; 

    public SendMode lastConnection;

    public SendMode newConnection;
}

public class PEERbotLogger : MonoBehaviour {

    [Header("Connections")]
    public PEERbotController pc;

    [Header("File IO")]
    private static string SLASH;
    public static string logPath;
    
    [Header("Logging")]    
    public List<PEERbotButtonDataFull> log;
    public List<PEERbotPaletteLogData> paletteLog;
    public List<PEERbotButtonQuickSpeechData> quickSpeechLog;
    public List<PEERbotButtonDataFull> buttonLog;
    public List<PEERbotBlinkData> blinkLog = new List<PEERbotBlinkData>();
    public List<PEERbotSendModeData> connectionLog = new List<PEERbotSendModeData>();

    public List<PEERbotButtonDataFull> buttonAuthorLog = new List<PEERbotButtonDataFull>();

    public List<buttonModification> modificationLog = new List<buttonModification>();


    public string sessionID = "";
    private string timezone = "";
    private bool isLogging = false;
    public bool autoStartLogging = true;

    public bool finishedLoadingPalette = false;
    public bool finishedLoadingSubgoal = false;
    private bool fromTitleChange = false;
    public static string lastPalette = null;

    public string prevTitle = "";
    public string prevColor = "";
    public string prevEmotion = "";
    public string prevSpeech = "";
    public string prevGoal = "";
    public string prevSubgoal = "";
    public string prevProficiency = "";


    void Awake() {
        //Get local timezone
        foreach(Match match in Regex.Matches(System.TimeZone.CurrentTimeZone.StandardName, "[A-Z]")) { timezone += match.Value; }
    
        //Set SLASH
        SLASH = (Application.platform == RuntimePlatform.Android ||
                 Application.platform == RuntimePlatform.OSXPlayer ||
                 Application.platform == RuntimePlatform.OSXEditor ||
                 Application.platform == RuntimePlatform.IPhonePlayer ||
                 Application.platform == RuntimePlatform.WindowsEditor ||
                 Application.platform == RuntimePlatform.WindowsPlayer)?"/":"\\";

        //Force set log path        
        if(Application.platform == RuntimePlatform.Android || 
           Application.platform == RuntimePlatform.IPhonePlayer) {
            logPath = Application.persistentDataPath + SLASH + "Logs";
        } else {
            logPath = Application.streamingAssetsPath + SLASH + "Logs";
        }
        System.IO.Directory.CreateDirectory(logPath);

        //Begin Logging
        if(autoStartLogging) { startLogging(); }
    }

    public void SetSessionID(string text) { if(text == null) { text = ""; }
        PEERbotSaveLoad.SanitizeFilename(text);
        sessionID = text;
    }

    ///**************************************************///
    ///***************CSV EXPORT FUNCTIONS***************///
    ///**************************************************///
    public void NativeShare() { 
        string filename = "[LOG] MasterLog.csv";
        string path = logPath + SLASH + filename;
        
        new NativeShare().AddFile(path)
                        .SetSubject("Shared PEERbots File: \"" + filename + "\"")
                        .SetText("Sent \"" + filename + "\" on " + System.DateTime.Now.ToString("yyyy-MM-dd hh:mm:sstt") + ". Enjoy!")
                        .SetCallback( ( result, shareTarget ) => Debug.Log( "Share CSV palette: " + result + ", selected app: " + shareTarget ) )
                        .Share();    
    }

    ///***************************************************///
    ///***************CSV LOGGING FUNCTIONS***************///
    ///***************************************************///
    public void addToMasterLog(string date, string time, string type, string json)
    {
        string path = logPath + SLASH + "[LOG] MasterLog.log";
        StreamWriter writer = new StreamWriter(path, true);

        writer.WriteLine(date + " " + time + " - " + type + " - " + json);
        writer.Close();
    }

    public void logButtonCreation(PEERbotButton button){
        PEERbotButtonDataFull buttonCreateLog = button.data;
        buttonCreateLog.logType = "createButton";
        buttonCreateLog.date = System.DateTime.Now.ToString("yyyy-MM-dd");
        buttonCreateLog.time = System.DateTime.Now.ToString("hh:mm:sstt ") + timezone;
        buttonAuthorLog.Add(buttonCreateLog);
        string json = JsonUtility.ToJson(buttonCreateLog);
        addToMasterLog(buttonCreateLog.date, buttonCreateLog.time, "createButton", json);
    }

    public void logButtonCopy(PEERbotButton button){
        PEERbotButtonDataFull buttonCopyLog = button.data;
        buttonCopyLog.logType = "copyButton";
        buttonCopyLog.date = System.DateTime.Now.ToString("yyyy-MM-dd");
        buttonCopyLog.time = System.DateTime.Now.ToString("hh:mm:sstt ") + timezone;
        buttonAuthorLog.Add(buttonCopyLog);
        string json = JsonUtility.ToJson(buttonCopyLog);
        addToMasterLog(buttonCopyLog.date, buttonCopyLog.time, "copyButton", json);
    }

    public void logButtonDelete(PEERbotButton button){
        PEERbotButtonDataFull buttonDeleteLog = button.data;
        buttonDeleteLog.logType = "deleteButton";
        buttonDeleteLog.date = System.DateTime.Now.ToString("yyyy-MM-dd");
        buttonDeleteLog.time = System.DateTime.Now.ToString("hh:mm:sstt ") + timezone;
        buttonAuthorLog.Add(buttonDeleteLog);
        string json = JsonUtility.ToJson(buttonDeleteLog);
        addToMasterLog(buttonDeleteLog.date, buttonDeleteLog.time, "deleteButton", json);
    }

    public void modifyTitle() {
        fromTitleChange = true;
        logModifyButton(prevTitle, "Title", prevTitle, pc.currentButton.data.title);
    }
    
    public void modifyColor() {
        if (finishedLoadingPalette)
            logModifyButton(pc.currentButton.data.title, "Color", prevColor, pc.currentButton.data.color);
        prevColor = "";
    }

    public void modifyEmotion() {
        if (finishedLoadingPalette)
            logModifyButton(pc.currentButton.data.title, "Emotion", prevEmotion, pc.currentButton.data.emotion);
        prevEmotion = "";
        
    }

    public void modifySpeech() {
        if (finishedLoadingPalette)
            logModifyButton(pc.currentButton.data.title, "Speech", prevSpeech, pc.currentButton.data.speech);
        prevSpeech = "";
    }

    public void modifyGoal() {
        if (finishedLoadingPalette)
            logModifyButton(pc.currentButton.data.title, "Goal", prevGoal, pc.currentButton.data.goal);
        prevGoal = "";
        
    }

    public void modifySubgoal() {
        if (finishedLoadingPalette && finishedLoadingSubgoal)
            logModifyButton(pc.currentButton.data.title, "Subgoal", prevSubgoal, pc.currentButton.data.subgoal);
        prevSubgoal = "";
    }

    public void modifyProficiency() {
        if (finishedLoadingPalette)
            logModifyButton(pc.currentButton.data.title, "Proficiency", prevProficiency, pc.currentButton.data.proficiency);
        prevProficiency = "";
    }

    public void logModifyButton(string title, string attributeChanged, string from, string to){
        if ((from != to) && (title == pc.currentButton.data.title || fromTitleChange)){
            buttonModification mod = new buttonModification();
            mod.palette = pc.currentPalette.title;
            mod.attributeChanged = attributeChanged;
            mod.title = title;
            mod.attributeChanged = attributeChanged;
            mod.previousValue = from;
            mod.newValue = to;
            mod.date = System.DateTime.Now.ToString("yyyy-MM-dd");
            mod.time = System.DateTime.Now.ToString("hh:mm:sstt ") + timezone;
            Debug.Log(JsonUtility.ToJson(mod));
            modificationLog.Add(mod);
            fromTitleChange = false;
            string json = JsonUtility.ToJson(mod);
            addToMasterLog(mod.date, mod.time, "modifyButton", json);
        }
    }

    public void AddToLog() {AddToLog(pc.currentPalette, pc.currentButton.data); }
    public void AddToLog(PEERbotPalette palette, PEERbotButtonDataFull data) { 
        if(!isLogging) { return; }
        if (palette == null || data ==  null) 
        {
            Debug.LogWarning("Button is null! Cannot add to Log!");
            return;
        }

        data.palette = palette.title;
        data.date = System.DateTime.Now.ToString("yyyy-MM-dd");
        data.time = System.DateTime.Now.ToString("hh:mm:sstt ") + timezone;
        
        //Check if quick speech
        if (data.title == "Quick Speech")
        {
            data.logType = "sayQuick";
            log.Add(data);
            PEERbotButtonQuickSpeechData logged = new PEERbotButtonQuickSpeechData();
            logged.speech = data.speech;
            logged.palette = data.palette;
            logged.date = data.date;
            logged.time = data.time;
            quickSpeechLog.Add(logged);
            string json = JsonUtility.ToJson(logged);
            addToMasterLog(logged.date, logged.time, "sayQuick", json);
        }   else {  
            buttonLog.Add(data);
            log.Add(data);
            string json = JsonUtility.ToJson(data);
            addToMasterLog(data.date, data.time, "sayButton", json);
        }
    }


    
    public void AddToPaletteLog() { AddToPaletteLog(pc.currentPalette); }
    public void AddToPaletteLog(PEERbotPalette palette){
        if (palette == null)
        {
            Debug.LogWarning("Palette is null! Cannot add to PaletteLog!");
            return;
        }
        PEERbotPaletteLogData data = new PEERbotPaletteLogData();
        if (lastPalette == null){
            GameObject paletteScript = GameObject.Find("PEERbotPalette");
            PEERbotPalette script = palette.GetComponent<PEERbotPalette>();
            data.lastPalette = script.initialPalette;
        } else {
            data.lastPalette = lastPalette;
        }
        data.newPalette = palette.title;
        data.date = System.DateTime.Now.ToString("yyyy-MM-dd");
        data.time = System.DateTime.Now.ToString("hh:mm:sstt ") + timezone;
        paletteLog.Add(data);
        lastPalette = palette.title;
        string json = JsonUtility.ToJson(data);
        addToMasterLog(data.date, data.time, "Palette Selected", json);
    }

    public void AddtoBlinkLog(){
        PEERbotBlinkData data = new PEERbotBlinkData();
        data.date = System.DateTime.Now.ToString("yyyy-MM-dd");
        data.time = System.DateTime.Now.ToString("hh:mm:sstt ") + timezone;
        blinkLog.Add(data);
        string json = JsonUtility.ToJson(data);
        addToMasterLog(data.date, data.time, "Blink Robot", json);
    }
    public void AddtoConnectionLog(SendMode before, SendMode after){
        PEERbotSendModeData data = new PEERbotSendModeData();
        data.date = System.DateTime.Now.ToString("yyyy-MM-dd");
        data.time = System.DateTime.Now.ToString("hh:mm:sstt ") + timezone;
        data.lastConnection = before;
        data.newConnection = after;
        connectionLog.Add(data);
        string json = JsonUtility.ToJson(data);
        addToMasterLog(data.date, data.time, "Connection Change", json);
    }

    public void SaveLog(string logName) {
        //Check and make sure path and path are not null.
        if(log == null) { Debug.LogWarning("Button Log is null! Cannot save log."); return; }
        if(log.Count == 0) { Debug.LogWarning("Button Log is empty! Cannot save log."); return; }
        //Make sure path and name is not null or empty.
        if(string.IsNullOrEmpty(logPath)) { Debug.LogWarning("logPath is null or empty! Cannot save log."); return; }
        if(string.IsNullOrEmpty(logName)) { Debug.LogWarning("logName is null or empty! Cannot save log."); return; }
        //Save using sinban CSV auto json parser
        Sinbad.CsvUtil.SaveObjects(log, logPath + SLASH + logName, true);
        Debug.Log("Log saved at " + logPath + SLASH + logName);
    }    


    public void SavePaletteLog(string logName) {
        //Check and make sure path and path are not null.
        if(paletteLog == null) { Debug.LogWarning("Palette Log is null! Cannot save log."); return; }
        if(paletteLog.Count == 0) { Debug.LogWarning("Palette Log is empty! Cannot save log."); return; }
        //Make sure path and name is not null or empty.
        if(string.IsNullOrEmpty(logPath)) { Debug.LogWarning("logPath is null or empty! Cannot save log."); return; }
        if(string.IsNullOrEmpty(logName)) { Debug.LogWarning("logName is null or empty! Cannot save log."); return; }
        //Save using sinban CSV auto json parser
        Sinbad.CsvUtil.SaveObjects(paletteLog, logPath + SLASH + logName, true);
        Debug.Log("Log saved at " + logPath + SLASH + logName);
    }    

    public void SaveQuickSpeechLog(string logName) {
        //Check and make sure path and path are not null.
        if(quickSpeechLog == null) { Debug.LogWarning("Quick Speech Log is null! Cannot save log."); return; }
        if(quickSpeechLog.Count == 0) { Debug.LogWarning("Quick Speech Log is empty! Cannot save log."); return; }
        //Make sure path and name is not null or empty.
        if(string.IsNullOrEmpty(logPath)) { Debug.LogWarning("logPath is null or empty! Cannot save log."); return; }
        if(string.IsNullOrEmpty(logName)) { Debug.LogWarning("logName is null or empty! Cannot save log."); return; }
        //Save using sinban CSV auto json parser
        Sinbad.CsvUtil.SaveObjects(quickSpeechLog, logPath + SLASH + logName, true);
        Debug.Log("Log saved at " + logPath + SLASH + logName);
    }

     public void SaveButtonLog(string logName) {
        //Check and make sure path and path are not null.
        if(buttonLog == null) { Debug.LogWarning("Button Log is null! Cannot save log."); return; }
        if(buttonLog.Count == 0) { Debug.LogWarning("Button Log is empty! Cannot save log."); return; }
        //Make sure path and name is not null or empty.
        if(string.IsNullOrEmpty(logPath)) { Debug.LogWarning("logPath is null or empty! Cannot save log."); return; }
        if(string.IsNullOrEmpty(logName)) { Debug.LogWarning("logName is null or empty! Cannot save log."); return; }
        //Save using sinban CSV auto json parser
        Sinbad.CsvUtil.SaveObjects(buttonLog, logPath + SLASH + logName, true);
        Debug.Log("Log saved at " + logPath + SLASH + logName);
    }

    public void SaveBlinkLog(string logName) {
        //Check and make sure path and path are not null.
        if(blinkLog == null) { Debug.LogWarning("Button Log is null! Cannot save log."); return; }
        if(blinkLog.Count == 0) { Debug.LogWarning("Button Log is empty! Cannot save log."); return; }
        //Make sure path and name is not null or empty.
        if(string.IsNullOrEmpty(logPath)) { Debug.LogWarning("logPath is null or empty! Cannot save log."); return; }
        if(string.IsNullOrEmpty(logName)) { Debug.LogWarning("logName is null or empty! Cannot save log."); return; }
        //Save using sinban CSV auto json parser
        Sinbad.CsvUtil.SaveObjects(blinkLog, logPath + SLASH + logName, true);
        Debug.Log("Log saved at " + logPath + SLASH + logName);
    }

    public void SaveConnectionLog(string logName) {
        //Check and make sure path and path are not null.
        if(connectionLog == null) { Debug.LogWarning("Button Log is null! Cannot save log."); return; }
        if(connectionLog.Count == 0) { Debug.LogWarning("Button Log is empty! Cannot save log."); return; }
        //Make sure path and name is not null or empty.
        if(string.IsNullOrEmpty(logPath)) { Debug.LogWarning("logPath is null or empty! Cannot save log."); return; }
        if(string.IsNullOrEmpty(logName)) { Debug.LogWarning("logName is null or empty! Cannot save log."); return; }
        //Save using sinban CSV auto json parser
        Sinbad.CsvUtil.SaveObjects(connectionLog, logPath + SLASH + logName, true);
        Debug.Log("Log saved at " + logPath + SLASH + logName);
    }

    public void SaveButtonAuthorLog(string logName) {
        //Check and make sure path and path are not null.
        if(buttonAuthorLog == null) { Debug.LogWarning("Button Log is null! Cannot save log."); return; }
        if(buttonAuthorLog.Count == 0) { Debug.LogWarning("Button Log is empty! Cannot save log."); return; }
        //Make sure path and name is not null or empty.
        if(string.IsNullOrEmpty(logPath)) { Debug.LogWarning("logPath is null or empty! Cannot save log."); return; }
        if(string.IsNullOrEmpty(logName)) { Debug.LogWarning("logName is null or empty! Cannot save log."); return; }
        //Save using sinban CSV auto json parser
        Sinbad.CsvUtil.SaveObjects(buttonAuthorLog, logPath + SLASH + logName, true);
        Debug.Log("Log saved at " + logPath + SLASH + logName);
    }

    public void SaveModificationLog(string logName) {
        //Check and make sure path and path are not null.
        if(modificationLog == null) { Debug.LogWarning("Button Log is null! Cannot save log."); return; }
        if(modificationLog.Count == 0) { Debug.LogWarning("Button Log is empty! Cannot save log."); return; }
        //Make sure path and name is not null or empty.
        if(string.IsNullOrEmpty(logPath)) { Debug.LogWarning("logPath is null or empty! Cannot save log."); return; }
        if(string.IsNullOrEmpty(logName)) { Debug.LogWarning("logName is null or empty! Cannot save log."); return; }
        //Save using sinban CSV auto json parser
        Debug.Log(modificationLog.Count);
        Sinbad.CsvUtil.SaveObjects(modificationLog, logPath + SLASH + logName, true);
        Debug.Log("Log saved at " + logPath + SLASH + logName);
    }

    public void startLogging() { 
        if(isLogging) { stopLogging(); }
        isLogging = true;
    }
    public void stopLogging() {
        if(isLogging) {
            SaveLog("[LOG] " + sessionID + ((sessionID.Length>0)?" ":"") + System.DateTime.Now.ToString("yyyy-MM-dd hh-mm-sstt") + ".csv");
            SavePaletteLog("[PaletteLOG] " + sessionID + ((sessionID.Length>0)?" ":"") + System.DateTime.Now.ToString("yyyy-MM-dd hh-mm-sstt") + ".csv");
            SaveButtonLog("[ButtonLog] " + sessionID + ((sessionID.Length>0)?" ":"") + System.DateTime.Now.ToString("yyyy-MM-dd hh-mm-sstt") + ".csv");
            SaveQuickSpeechLog("[QuickSpeechLOG] " + sessionID + ((sessionID.Length>0)?" ":"") + System.DateTime.Now.ToString("yyyy-MM-dd hh-mm-sstt") + ".csv");
            SaveBlinkLog("[BlinkLOG] " + sessionID + ((sessionID.Length>0)?" ":"") + System.DateTime.Now.ToString("yyyy-MM-dd hh-mm-sstt") + ".csv");
            SaveConnectionLog("[ConnectionLOG] " + sessionID + ((sessionID.Length>0)?" ":"") + System.DateTime.Now.ToString("yyyy-MM-dd hh-mm-sstt") + ".csv");
            SaveButtonAuthorLog("[ButtonAuthorLOG] " + sessionID + ((sessionID.Length>0)?" ":"") + System.DateTime.Now.ToString("yyyy-MM-dd hh-mm-sstt") + ".csv");
            SaveModificationLog("[ButtonModificationLOG] " + sessionID + ((sessionID.Length>0)?" ":"") + System.DateTime.Now.ToString("yyyy-MM-dd hh-mm-sstt") + ".csv");
            isLogging = false;
        }
    }
    public void OnApplicationQuit() { stopLogging(); }
    void OnDestroy() { stopLogging(); }

    ///**********************************************///
    ///*************PATH HELPER FUNCTIONS************///
    ///**********************************************///
    public void StandaloneFileBrowserSelectFolder() { 
        string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
        foreach(string path in paths) { setLogPath(path); }
    } 

    public void setLogPath(string path) { 
        PlayerPrefs.SetString("LogPath", path);
        logPath = path;
    }

}
