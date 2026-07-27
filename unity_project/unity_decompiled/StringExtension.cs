using System;

public static class StringExtension
{
	public static DateTime ToDateTime(this string value)
	{
		string[] array = value.Split("-");
		return new DateTime(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));
	}
}
