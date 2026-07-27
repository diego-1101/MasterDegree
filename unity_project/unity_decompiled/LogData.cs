public class LogData
{
	public string TempoEEG { get; set; }

	public string TempoUNITY { get; set; }

	public string Classifier { get; set; }

	public string AngleLegR { get; set; }

	public string AngleLegL { get; set; }

	public string xBall { get; set; }

	public string yBall { get; set; }

	public string zBall { get; set; }

	public string Marcadornum { get; set; }

	public string Marcador { get; set; }

	public string Acertos { get; set; }

	public string Classe { get; set; }

	public string Tentativa { get; set; }

	public string Target { get; set; }

	public LogData(string eegTime, string unityTime, string classifier, string rightAngle, string leftAngle, string xPos, string yPos, string zPos, string num, string marcador, string acertos, string classe, string tentativa, string target)
	{
		TempoEEG = eegTime;
		TempoUNITY = unityTime;
		Classifier = classifier;
		AngleLegR = rightAngle;
		AngleLegL = leftAngle;
		xBall = xPos;
		yBall = yPos;
		zBall = zPos;
		Marcadornum = num;
		Marcador = marcador;
		Acertos = acertos;
		Classe = classe;
		Tentativa = tentativa;
	}

	public static implicit operator string(LogData log)
	{
		return log.TempoEEG + "|" + log.TempoUNITY + "|" + log.Classifier + "|" + log.AngleLegR + "|" + log.AngleLegL + "|" + log.xBall + "|" + log.yBall + "|" + log.zBall + "|" + log.Marcadornum + "|" + log.Marcador + "|" + log.Acertos + "|" + log.Classe + "|" + log.Tentativa + "|" + log.Target;
	}

	public static implicit operator LogData(string line)
	{
		string[] array = line.Split("|");
		return new LogData(array[0], array[1], array[2], array[3], array[4], array[5], array[6], array[7], array[8], array[9], array[10], array[11], array[12], array[13]);
	}
}
