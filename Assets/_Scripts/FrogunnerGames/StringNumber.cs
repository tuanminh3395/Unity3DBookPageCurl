namespace FrogunnerGames
{
    public static class StringNumber
    {
        private static readonly string[] NumberString1Digit;
        private static readonly string[] NumberString2Digit;
        private static readonly string[] NumberStringPercentage;

        static StringNumber()
        {
            NumberString1Digit = new string[99999];
            for (int i = 0; i < NumberString1Digit.Length; ++i)
            {
                NumberString1Digit[i] = $"{i}";
            }

            NumberString2Digit = new string[60];
            for (int i = 0; i < NumberString2Digit.Length; ++i)
            {
                NumberString2Digit[i] = $"{i:00}";
            }

            NumberStringPercentage = new string[1001];
            for (int i = 0; i < NumberStringPercentage.Length; ++i)
            {
                NumberStringPercentage[i] = $"{i * 0.1f:0.0}";
            }
        }

        public static string PercentageToText(float percentage)
        {
            percentage = (float) System.Math.Round(percentage, 1);
            int index = (int) (percentage * 10);

            if (index < 0) return NumberStringPercentage[0];
            if (index >= NumberStringPercentage.Length) return NumberStringPercentage[NumberStringPercentage.Length - 1];

            return NumberStringPercentage[index];
        }

        public static string IntToText(int number)
        {
            if (number < 0) return NumberString1Digit[0];

            if (number >= NumberString1Digit.Length) return NumberString1Digit[NumberString1Digit.Length - 1];

            return NumberString1Digit[number];
        }

        public static string IntToTimeText(int number)
        {
            if (number < 0) return NumberString2Digit[0];

            if (number >= NumberString2Digit.Length) return NumberString2Digit[NumberString2Digit.Length - 1];

            return NumberString2Digit[number];
        }

        public static string SecondsToMinuteText(int secs)
        {
            int minute = secs / 60;
            int sec = secs - minute * 60;
            return minute + "m " + sec + "s";
        }
    }
}