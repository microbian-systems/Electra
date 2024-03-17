using System;

namespace Microbians.Common.Extensions
{
    public static class RoundingExtensions
    {
        public static double RoundToTwoDecimalPlaces(this double number)
        {
            var rounded = Math.Round(number, 2);

            return rounded;
        }

        public static double RoundUpOrDown(this double number)
        {
            var roundedDoubleNumber = RoundedDivision(number);

            return roundedDoubleNumber;
        }

        private static double RoundedDivision(double number)
        {
            double divisor = 1;
            var div = number / divisor;
            var floor = Math.Floor(div);
            var celing = Math.Ceiling(div);
            var difference = (div - floor);

            return difference < 0.5 ? floor : celing;
        }

        public static double RoundToOneDecimalPlaces(this double number)
        {
            var rounded = Math.Round(number, 2);

            return rounded;
        }

        public static decimal RoundToTwoDecimalPlaces(this decimal number)
        {
            var rounded = Math.Round(number, 2);

            return rounded;
        }
    }
}
