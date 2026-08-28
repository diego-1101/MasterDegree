using System;

[Serializable]
public enum GrazLegSide
{
    Right = 0,
    Left = 1,
    Relaxed = 2
}

[Serializable]
public class GrazAcquisitionTrial
{
    public int trialIndex;
    public string subjectId;
    public string sessionId;
    public string run;
    public string classId;
    public string label;
    public int markerCode;
    public string cueImagePath;
    public string description;
    public float targetAngle;
    public float tolerance;
    public float holdTime;
    public GrazLegSide legSide;
    public int randomSeed;
    public bool shuffled;
}
