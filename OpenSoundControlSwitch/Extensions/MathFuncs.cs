namespace OpenSoundControlSwitch
{
    public class MathFuncs
    {
        // Pass a 0.0 to X.X (default 1.0) float value, and turn it into a 0 to X int value
        public int FtoI(float Value, int MaxI, float? MaxF = 1.0f)
        {
            return (int)(Value >= MaxF ? MaxI : Value * (MaxI + 1));
        }

        // Pass a value, then the minimum and maximum range for it,
        // and the function will spit out a percentage from 0 to 100 (int)
        public int ItoRi(int Value, int MinI, int MaxI)
        {
            return (Value - MinI) * 100 / (MaxI - MinI);
        }

        // Pass a value, then the minimum and maximum range for it,
        // and the function will spit out a percentage from 0.0 to 1.0 (float)
        public float ItoRf(int Value, float MinF, float MaxF)
        {
            return (Value - MinF) * 1.0f / (MaxF - MinF);
        }

        // Linear interpolation, useful when you want to get a 0.0 to 1.0 float value and translate it
        // to a specific range of your choosing (e.g. 0.0 - 1.0 to 1700.0 to 6500.0, for the light temperature,
        // which you will then round by casting it to an int; (int)6500.0 -> 6500K)
        public float Lerp(float Value, float MinF, float MaxF)
        {
            return MinF * (1 - Value) + MaxF * Value;
        }

        // Round an integer number to an even number
        // (Why isn't this built into the Math classas one function already?)
        public decimal RoundNum(int Value)
        {
            return Math.Round((decimal)Value / 2, MidpointRounding.AwayFromZero) * 2;
        }
    }
}