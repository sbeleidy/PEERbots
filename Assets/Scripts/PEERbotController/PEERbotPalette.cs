using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PEERbotPalette : MonoBehaviour {


  public string title = "";
  
  public List<PEERbotButton> buttons;

  [System.NonSerialized]
  public int index;

  [System.NonSerialized]
  private PEERbotController wc;


[System.NonSerialized]
 public string initialPalette = null;

  void Awake() { 
    buttons = new List<PEERbotButton>();
    wc = GlobalObjectFinder.FindGameObjectWithTag("PEERbotController").GetComponent<PEERbotController>(); 
  } 

  public void Select() {
    if (initialPalette == null){
      initialPalette = wc.currentPalette.title;
    }
    wc.selectPalette(this);
  }

}

[System.Serializable]
public class PEERbotPaletteData {
  public string title = "";
  public string date;
  public string time;
  public List<PEERbotButtonData> buttons;
}

[System.Serializable]
public class PEERbotPaletteLogData
{
  public const string logType = "changePalette";
  public string newPalette = "";
  public string lastPalette = "";
  public string date;
  public string time;
}