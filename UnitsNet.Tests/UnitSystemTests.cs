﻿// Copyright(c) 2007 Andreas Gullberg Larsen
// https://github.com/anjdreas/UnitsNet
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using UnitsNet.Units;
using Xunit.Abstractions;
#if WINDOWS_UWP
using Culture=System.String;
#else
using System.Globalization;
using Culture=System.IFormatProvider;
#endif

namespace UnitsNet.Tests
{
    // Avoid accessing static prop DefaultToString in parallel from multiple tests:
    // UnitSystemTests.DefaultToStringFormatting()
    // LengthTests.ToStringReturnsCorrectNumberAndUnitWithCentimeterAsDefualtUnit()
    [Collection("DefaultToString")]
    public class UnitSystemTests
    {
        private readonly ITestOutputHelper _output;

        public UnitSystemTests(ITestOutputHelper output)
        {
            _output = output;
        }

        // The default, parameterless ToString() method uses 2 sigifnificant digits after the radix point.
        [Theory]
        [InlineData(0, "0 m")]
        [InlineData(0.1, "0.1 m")]
        [InlineData(0.11, "0.11 m")]
        [InlineData(0.111234, "0.11 m")]
        [InlineData(0.115, "0.12 m")]
        public void DefaultToStringFormatting(double value, string expected)
        {
            string actual = Length.FromMeters(value).ToString();
            Assert.Equal(expected, actual);
        }

        private enum CustomUnit
        {
            // ReSharper disable UnusedMember.Local
            Undefined = 0,
            Unit1,
            Unit2
            // ReSharper restore UnusedMember.Local
        }

#if !WINDOWS_UWP
        private UnitSystem GetCachedUnitSystem()
        {
            Culture culture = GetCulture("en-US");
            UnitSystem unitSystem = UnitSystem.GetCached(culture);
            return unitSystem;
        }
#endif

        private static IEnumerable<object> GetUnitTypesWithMissingAbbreviations<TUnitType>(string cultureName,
            IEnumerable<TUnitType> unitValues)
            where TUnitType : /*Enum constraint hack*/ struct, IComparable, IFormattable
        {
            UnitSystem unitSystem = UnitSystem.GetCached(GetCulture(cultureName));

            var unitsMissingAbbreviations = new List<TUnitType>();
            foreach (TUnitType unit in unitValues)
            {
                try
                {
#if WINDOWS_UWP
                    unitSystem.GetDefaultAbbreviation(unit.GetType(), Convert.ToInt32(unit));
#else
                    unitSystem.GetDefaultAbbreviation(unit);
#endif
                }
                catch
                {
                    unitsMissingAbbreviations.Add(unit);
                }
            }

            return unitsMissingAbbreviations.Cast<object>();
        }

        // These cultures all use a comma for the radix point
        [Theory]
        [InlineData("de-DE")]
        [InlineData("da-DK")]
        [InlineData("es-AR")]
        [InlineData("es-ES")]
        [InlineData("it-IT")]
        public void CommaRadixPointCultureFormatting(string culture)
        {
            Assert.Equal("0,12 m", Length.FromMeters(0.12).ToString(LengthUnit.Meter, GetCulture(culture)));
        }

        // These cultures all use a decimal point for the radix point
        [Theory]
        [InlineData("en-CA")]
        [InlineData("en-US")]
        [InlineData("ar-EG")]
        [InlineData("en-GB")]
        [InlineData("es-MX")]
        public void DecimalRadixPointCultureFormatting(string culture)
        {
            Assert.Equal("0.12 m", Length.FromMeters(0.12).ToString(LengthUnit.Meter, GetCulture(culture)));
        }

        // These cultures all use a comma in digit grouping
        [Theory]
        [InlineData("en-CA")]
        [InlineData("en-US")]
        [InlineData("ar-EG")]
        [InlineData("en-GB")]
        [InlineData("es-MX")]
        public void CommaDigitGroupingCultureFormatting(string cultureName)
        {
            Culture culture = GetCulture(cultureName);
            Assert.Equal("1,111 m", Length.FromMeters(1111).ToString(LengthUnit.Meter, culture));

            // Feet/Inch and Stone/Pound combinations are only used (customarily) in the US, UK and maybe Ireland - all English speaking countries.
            // FeetInches returns a whole number of feet, with the remainder expressed (rounded) in inches. Same for SonePounds.
            Assert.Equal("2,222 ft 3 in",
                Length.FromFeetInches(2222, 3).FeetInches.ToString(culture));
            Assert.Equal("3,333 st 7 lb",
                Mass.FromStonePounds(3333, 7).StonePounds.ToString(culture));
        }

        // These cultures use a thin space in digit grouping
        [Theory]
        [InlineData("nn-NO")]
        [InlineData("fr-FR")]
        public void SpaceDigitGroupingCultureFormatting(string culture)
        {
            // Note: the space used in digit groupings is actually a "thin space" Unicode character U+2009
            Assert.Equal("1 111 m", Length.FromMeters(1111).ToString(LengthUnit.Meter, GetCulture(culture)));
        }

        // Switzerland uses an apostrophe for digit grouping
//        [Ignore("Fails on Win 8.1 and Win10 due to a bug in .NET framework.")]
//        [InlineData("fr-CH")]
//        public void ApostropheDigitGroupingCultureFormatting(string culture)
//        {
//            Assert.Equal("1'111 m", Length.FromMeters(1111).ToString(LengthUnit.Meter, new CultureInfo(culture)));
//        }

        // These cultures all use a decimal point in digit grouping
        [Theory]
        [InlineData("de-DE")]
        [InlineData("da-DK")]
        [InlineData("es-AR")]
        [InlineData("es-ES")]
        [InlineData("it-IT")]
        public void DecimalPointDigitGroupingCultureFormatting(string culture)
        {
            Assert.Equal("1.111 m", Length.FromMeters(1111).ToString(LengthUnit.Meter, GetCulture(culture)));
        }

#if !WINDOWS_UWP
        [Theory]
        [InlineData("m^2", AreaUnit.SquareMeter)]
        [InlineData("cm^2", AreaUnit.Undefined)]
        public void Parse_ReturnsUnitMappedByCustomAbbreviationOrUndefined(string unitAbbreviationToParse, AreaUnit expected)
        {
            UnitSystem unitSystem = GetCachedUnitSystem();
            unitSystem.MapUnitToAbbreviation(AreaUnit.SquareMeter, "m^2");
            var actual = unitSystem.Parse<AreaUnit>(unitAbbreviationToParse);
            Assert.Equal(expected, actual);
        }
#endif

        [Theory]
        [InlineData(1, "1.1 m")]
        [InlineData(2, "1.12 m")]
        [InlineData(3, "1.123 m")]
        [InlineData(4, "1.1235 m")]
        [InlineData(5, "1.12346 m")]
        [InlineData(6, "1.123457 m")]
        public void CustomNumberOfSignificantDigitsAfterRadixFormatting(int significantDigitsAfterRadix, string expected)
        {
            string actual = Length.FromMeters(1.123456789).ToString(LengthUnit.Meter, null, significantDigitsAfterRadix);
            Assert.Equal(expected, actual);
        }

        // Due to rounding, the values will result in the same string representation regardless of the number of significant digits (up to a certain point)
        [Theory]
        [InlineData(0.819999999999, 2, "0.82 m")]
        [InlineData(0.819999999999, 4, "0.82 m")]
        [InlineData(0.00299999999, 2, "0.003 m")]
        [InlineData(0.00299999999, 4, "0.003 m")]
        [InlineData(0.0003000001, 2, "3e-04 m")]
        [InlineData(0.0003000001, 4, "3e-04 m")]
        public void RoundingErrorsWithSignificantDigitsAfterRadixFormatting(double value,
            int maxSignificantDigitsAfterRadix, string expected)
        {
            string actual = Length.FromMeters(value).ToString(LengthUnit.Meter, null, maxSignificantDigitsAfterRadix);
            Assert.Equal(expected, actual);
        }

        // Any value in the interval (-inf ≤ x < 1e-03] is formatted in scientific notation
        [Theory]
        [InlineData(double.MinValue, "-1.8e+308 m")]
        [InlineData(1.23e-120, "1.23e-120 m")]
        [InlineData(0.0000111, "1.11e-05 m")]
        [InlineData(1.99e-4, "1.99e-04 m")]
        public void ScientificNotationLowerInterval(double value, string expected)
        {
            string actual = Length.FromMeters(value).ToString();
            Assert.Equal(expected, actual);
        }

        // Any value in the interval [1e-03 ≤ x < 1e+03] is formatted in fixed point notation.
        [Theory]
        [InlineData(1e-3, "0.001 m")]
        [InlineData(1.1, "1.1 m")]
        [InlineData(999.99, "999.99 m")]
        public void FixedPointNotationIntervalFormatting(double value, string expected)
        {
            string actual = Length.FromMeters(value).ToString();
            Assert.Equal(expected, actual);
        }

        // Any value in the interval [1e+03 ≤ x < 1e+06] is formatted in fixed point notation with digit grouping.
        [Theory]
        [InlineData(1000, "1,000 m")]
        [InlineData(11000, "11,000 m")]
        [InlineData(111000, "111,000 m")]
        [InlineData(999999.99, "999,999.99 m")]
        public void FixedPointNotationWithDigitGroupingIntervalFormatting(double value, string expected)
        {
            string actual = Length.FromMeters(value).ToString();
            Assert.Equal(expected, actual);
        }

        // Any value in the interval [1e+06 ≤ x ≤ +inf) is formatted in scientific notation.
        [Theory]
        [InlineData(1e6, "1e+06 m")]
        [InlineData(11100000, "1.11e+07 m")]
        [InlineData(double.MaxValue, "1.8e+308 m")]
        public void ScientificNotationUpperIntervalFormatting(double value, string expected)
        {
            string actual = Length.FromMeters(value).ToString();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldUseCorrectMicroSign()
        {
            // "\u00b5" = Micro sign
            Assert.Equal(AccelerationUnit.MicrometerPerSecondSquared, Acceleration.ParseUnit("\u00b5m/s²"));
            Assert.Equal(AmplitudeRatioUnit.DecibelMicrovolt, AmplitudeRatio.ParseUnit("dB\u00b5V"));
            Assert.Equal(AngleUnit.Microdegree, Angle.ParseUnit("\u00b5°"));
            Assert.Equal(AngleUnit.Microradian, Angle.ParseUnit("\u00b5rad"));
            Assert.Equal(AreaUnit.SquareMicrometer, Area.ParseUnit("\u00b5m²"));
            Assert.Equal(DurationUnit.Microsecond, Duration.ParseUnit("\u00b5s"));
            Assert.Equal(ElectricCurrentUnit.Microampere, ElectricCurrent.ParseUnit("\u00b5A"));
            Assert.Equal(ElectricPotentialUnit.Microvolt, ElectricPotential.ParseUnit("\u00b5V"));
            Assert.Equal(FlowUnit.MicrolitersPerMinute, Flow.ParseUnit("\u00b5LPM"));
            Assert.Equal(ForceChangeRateUnit.MicronewtonPerSecond, ForceChangeRate.ParseUnit("\u00b5N/s"));
            Assert.Equal(ForcePerLengthUnit.MicronewtonPerMeter, ForcePerLength.ParseUnit("\u00b5N/m"));
            Assert.Equal(KinematicViscosityUnit.Microstokes, KinematicViscosity.ParseUnit("\u00b5St"));
            Assert.Equal(LengthUnit.Microinch, Length.ParseUnit("\u00b5in"));
            Assert.Equal(LengthUnit.Micrometer, Length.ParseUnit("\u00b5m"));
            Assert.Equal(MassFlowUnit.MicrogramPerSecond, MassFlow.ParseUnit("\u00b5g/S"));
            Assert.Equal(MassUnit.Microgram, Mass.ParseUnit("\u00b5g"));
            Assert.Equal(PowerUnit.Microwatt, Power.ParseUnit("\u00b5W"));
            Assert.Equal(PressureUnit.Micropascal, Pressure.ParseUnit("\u00b5Pa"));
            Assert.Equal(RotationalSpeedUnit.MicrodegreePerSecond, RotationalSpeed.ParseUnit("\u00b5°/s"));
            Assert.Equal(RotationalSpeedUnit.MicroradianPerSecond, RotationalSpeed.ParseUnit("\u00b5rad/s"));
            Assert.Equal(SpeedUnit.MicrometerPerMinute, Speed.ParseUnit("\u00b5m/min"));
            Assert.Equal(SpeedUnit.MicrometerPerSecond, Speed.ParseUnit("\u00b5m/s"));
            Assert.Equal(TemperatureChangeRateUnit.MicrodegreeCelsiusPerSecond, TemperatureChangeRate.ParseUnit("\u00b5°C/s"));
            Assert.Equal(VolumeUnit.Microliter, Volume.ParseUnit("\u00b5l"));
            Assert.Equal(VolumeUnit.CubicMicrometer, Volume.ParseUnit("\u00b5m³"));

            // "\u03bc" = Lower case greek letter 'Mu' 
            Assert.Throws<UnitsNetException>(() => Acceleration.ParseUnit("\u03bcm/s²"));
            Assert.Throws<UnitsNetException>(() => AmplitudeRatio.ParseUnit("dB\u03bcV"));
            Assert.Throws<UnitsNetException>(() => Angle.ParseUnit("\u03bc°"));
            Assert.Throws<UnitsNetException>(() => Angle.ParseUnit("\u03bcrad"));
            Assert.Throws<UnitsNetException>(() => Area.ParseUnit("\u03bcm²"));
            Assert.Throws<UnitsNetException>(() => Duration.ParseUnit("\u03bcs"));
            Assert.Throws<UnitsNetException>(() => ElectricCurrent.ParseUnit("\u03bcA"));
            Assert.Throws<UnitsNetException>(() => ElectricPotential.ParseUnit("\u03bcV"));
            Assert.Throws<UnitsNetException>(() => Flow.ParseUnit("\u03bcLPM"));
            Assert.Throws<UnitsNetException>(() => ForceChangeRate.ParseUnit("\u03bcN/s"));
            Assert.Throws<UnitsNetException>(() => ForcePerLength.ParseUnit("\u03bcN/m"));
            Assert.Throws<UnitsNetException>(() => KinematicViscosity.ParseUnit("\u03bcSt"));
            Assert.Throws<UnitsNetException>(() => Length.ParseUnit("\u03bcin"));
            Assert.Throws<UnitsNetException>(() => Length.ParseUnit("\u03bcm"));
            Assert.Throws<UnitsNetException>(() => MassFlow.ParseUnit("\u03bcg/S"));
            Assert.Throws<UnitsNetException>(() => Mass.ParseUnit("\u03bcg"));
            Assert.Throws<UnitsNetException>(() => Power.ParseUnit("\u03bcW"));
            Assert.Throws<UnitsNetException>(() => Pressure.ParseUnit("\u03bcPa"));
            Assert.Throws<UnitsNetException>(() => RotationalSpeed.ParseUnit("\u03bc°/s"));
            Assert.Throws<UnitsNetException>(() => RotationalSpeed.ParseUnit("\u03bcrad/s"));
            Assert.Throws<UnitsNetException>(() => Speed.ParseUnit("\u03bcm/min"));
            Assert.Throws<UnitsNetException>(() => Speed.ParseUnit("\u03bcm/s"));
            Assert.Throws<UnitsNetException>(() => TemperatureChangeRate.ParseUnit("\u03bc°C/s"));
            Assert.Throws<UnitsNetException>(() => Volume.ParseUnit("\u03bcl"));
            Assert.Throws<UnitsNetException>(() => Volume.ParseUnit("\u03bcm³"));
        }

        [Theory]
        [InlineData("en-US")]
        [InlineData("nb-NO")]
        [InlineData("ru-RU")]
        public void AllUnitAbbreviationsImplemented(string cultureName)
        {
            List<object> unitValuesMissingAbbreviations = new List<object>()
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName, EnumUtils.GetEnumValues<AngleUnit>()))
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName, EnumUtils.GetEnumValues<AreaUnit>()))
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName, EnumUtils.GetEnumValues<DurationUnit>()))
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName,
                    EnumUtils.GetEnumValues<ElectricPotentialUnit>()))
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName, EnumUtils.GetEnumValues<FlowUnit>()))
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName, EnumUtils.GetEnumValues<ForceUnit>()))
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName, EnumUtils.GetEnumValues<LengthUnit>()))
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName, EnumUtils.GetEnumValues<MassUnit>()))
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName, EnumUtils.GetEnumValues<PressureUnit>()))
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName, EnumUtils.GetEnumValues<RotationalSpeedUnit>()))
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName, EnumUtils.GetEnumValues<SpeedUnit>()))
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName, EnumUtils.GetEnumValues<TemperatureUnit>()))
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName, EnumUtils.GetEnumValues<TorqueUnit>()))
                .Concat(GetUnitTypesWithMissingAbbreviations(cultureName, EnumUtils.GetEnumValues<VolumeUnit>()))
                .ToList();

            // We want to flag if any localizations are missing, but not break the build
            // or flag an error for pull requests. For now they are not considered 
            // critical and it is cumbersome to have a third person review the pull request
            // and add in any translations before merging it in.
            if (unitValuesMissingAbbreviations.Any())
            {
                string unitsWithNoAbbrev = string.Join(", ",
                    unitValuesMissingAbbreviations.Select(unitValue => unitValue.GetType().Name + "." + unitValue).ToArray());

                string message = "Units missing abbreviations: " + unitsWithNoAbbrev;
                _output.WriteLine(message);
            }
            Assert.Empty(unitValuesMissingAbbreviations);
        }

        [Fact]
        public void AllUnitsImplementToStringForInvariantCulture()
        {
            Assert.Equal("1 °", Angle.FromDegrees(1).ToString());
            Assert.Equal("1 m²", Area.FromSquareMeters(1).ToString());
            Assert.Equal("1 V", ElectricPotential.FromVolts(1).ToString());
            Assert.Equal("1 m³/s", Flow.FromCubicMetersPerSecond(1).ToString());
            Assert.Equal("1 N", Force.FromNewtons(1).ToString());
            Assert.Equal("1 m", Length.FromMeters(1).ToString());
            Assert.Equal("1 kg", Mass.FromKilograms(1).ToString());
            Assert.Equal("1 Pa", Pressure.FromPascals(1).ToString());
            Assert.Equal("1 rad/s", RotationalSpeed.FromRadiansPerSecond(1).ToString());
            Assert.Equal("1 K", Temperature.FromKelvins(1).ToString());
            Assert.Equal("1 N·m", Torque.FromNewtonMeters(1).ToString());
            Assert.Equal("1 m³", Volume.FromCubicMeters(1).ToString());

            Assert.Equal("2 ft 3 in", Length.FromFeetInches(2, 3).FeetInches.ToString());
            Assert.Equal("3 st 7 lb", Mass.FromStonePounds(3, 7).StonePounds.ToString());
        }

        [Fact]
        public void ToString_WithNorwegianCulture()
        {
#if WINDOWS_UWP
            Culture norwegian = "nb-NO";
#else
            Culture norwegian = new CultureInfo("nb-NO");
#endif
            Assert.Equal("1 °", Angle.FromDegrees(1).ToString(AngleUnit.Degree, norwegian));
            Assert.Equal("1 m²", Area.FromSquareMeters(1).ToString(AreaUnit.SquareMeter, norwegian));
            Assert.Equal("1 V", ElectricPotential.FromVolts(1).ToString(ElectricPotentialUnit.Volt, norwegian));
            Assert.Equal("1 m³/s", Flow.FromCubicMetersPerSecond(1).ToString(FlowUnit.CubicMeterPerSecond, norwegian));
            Assert.Equal("1 N", Force.FromNewtons(1).ToString(ForceUnit.Newton, norwegian));
            Assert.Equal("1 m", Length.FromMeters(1).ToString(LengthUnit.Meter, norwegian));
            Assert.Equal("1 kg", Mass.FromKilograms(1).ToString(MassUnit.Kilogram, norwegian));
            Assert.Equal("1 Pa", Pressure.FromPascals(1).ToString(PressureUnit.Pascal, norwegian));
            Assert.Equal("1 rad/s", RotationalSpeed.FromRadiansPerSecond(1).ToString(RotationalSpeedUnit.RadianPerSecond, norwegian));
            Assert.Equal("1 K", Temperature.FromKelvins(1).ToString(TemperatureUnit.Kelvin, norwegian));
            Assert.Equal("1 N·m", Torque.FromNewtonMeters(1).ToString(TorqueUnit.NewtonMeter, norwegian));
            Assert.Equal("1 m³", Volume.FromCubicMeters(1).ToString(VolumeUnit.CubicMeter, norwegian));
        }

        [Fact]
        public void ToString_WithRussianCulture()
        {
#if WINDOWS_UWP
            Culture russian = "ru-RU";
#else
            Culture russian = new CultureInfo( "ru-RU");
#endif
            Assert.Equal("1 °", Angle.FromDegrees(1).ToString(AngleUnit.Degree, russian));
            Assert.Equal("1 м²", Area.FromSquareMeters(1).ToString(AreaUnit.SquareMeter, russian));
            Assert.Equal("1 В", ElectricPotential.FromVolts(1).ToString(ElectricPotentialUnit.Volt, russian));
            Assert.Equal("1 м³/с", Flow.FromCubicMetersPerSecond(1).ToString(FlowUnit.CubicMeterPerSecond, russian));
            Assert.Equal("1 Н", Force.FromNewtons(1).ToString(ForceUnit.Newton, russian));
            Assert.Equal("1 м", Length.FromMeters(1).ToString(LengthUnit.Meter, russian));
            Assert.Equal("1 кг", Mass.FromKilograms(1).ToString(MassUnit.Kilogram, russian));
            Assert.Equal("1 Па", Pressure.FromPascals(1).ToString(PressureUnit.Pascal, russian));
            Assert.Equal("1 рад/с", RotationalSpeed.FromRadiansPerSecond(1).ToString(RotationalSpeedUnit.RadianPerSecond, russian));
            Assert.Equal("1 K", Temperature.FromKelvins(1).ToString(TemperatureUnit.Kelvin, russian));
            Assert.Equal("1 Н·м", Torque.FromNewtonMeters(1).ToString(TorqueUnit.NewtonMeter, russian));
            Assert.Equal("1 м³", Volume.FromCubicMeters(1).ToString(VolumeUnit.CubicMeter, russian));
        }

        [Fact]
        public void GetDefaultAbbreviationFallsBackToDefaultStringIfNotSpecified()
        {
            UnitSystem usUnits = UnitSystem.GetCached(GetCulture("en-US"));

#if WINDOWS_UWP
            string abbreviation = usUnits.GetDefaultAbbreviation(typeof(CustomUnit), (int)CustomUnit.Unit1);
            Assert.Equal("(no abbreviation for CustomUnit with numeric value 1)", abbreviation);
#else
            string abbreviation = usUnits.GetDefaultAbbreviation(CustomUnit.Unit1);
            Assert.Equal("(no abbreviation for CustomUnit.Unit1)", abbreviation);
#endif
        }

#if !WINDOWS_UWP
        [Fact]
        public void GetDefaultAbbreviationFallsBackToUsEnglishCulture()
        {
            // CurrentCulture affects number formatting, such as comma or dot as decimal separator.
            // CurrentUICulture affects localization, in this case the abbreviation.
            // Zulu (South Africa)
            CultureInfo zuluCulture = new CultureInfo("zu-ZA");
            UnitSystem zuluUnits = UnitSystem.GetCached(zuluCulture);
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = zuluCulture;

            UnitSystem usUnits = UnitSystem.GetCached(new CultureInfo("en-US"));
            usUnits.MapUnitToAbbreviation(CustomUnit.Unit1, "US english abbreviation for Unit1");

            // Act
            string abbreviation = zuluUnits.GetDefaultAbbreviation(CustomUnit.Unit1);

            // Assert
            Assert.Equal("US english abbreviation for Unit1", abbreviation);
        }
#endif

#if !WINDOWS_UWP
        [Fact]
        public void MapUnitToAbbreviation_AddCustomUnit_DoesNotOverrideDefaultAbbreviationForAlreadyMappedUnits()
        {
            CultureInfo cultureInfo = new CultureInfo("en-US");
            UnitSystem unitSystem = UnitSystem.GetCached(cultureInfo);
            unitSystem.MapUnitToAbbreviation(AreaUnit.SquareMeter, "m^2");

            Assert.Equal("m²", unitSystem.GetDefaultAbbreviation(AreaUnit.SquareMeter));
        }
#endif

        [Fact]
        public void NegativeInfinityFormatting()
        {
            Assert.Equal("-∞ m", Length.FromMeters(double.NegativeInfinity).ToString());
        }

        [Fact]
        public void NotANumberFormatting()
        {
            Assert.Equal("NaN m", Length.FromMeters(double.NaN).ToString());
        }

#if !WINDOWS_UWP
        [Fact]
        public void Parse_AmbiguousUnitsThrowsException()
        {
            UnitSystem unitSystem = GetCachedUnitSystem();

            // Act 1
            Assert.Throws<AmbiguousUnitParseException>(() => unitSystem.Parse<VolumeUnit>("tsp"));

            // Act 2
            Assert.Throws<AmbiguousUnitParseException>(() => Volume.Parse("1 tsp"));
        }
#endif

        [Fact]
        public void Parse_UnambiguousUnitsDoesNotThrow()
        {
            Volume unit = Volume.Parse("1 l");

            Assert.Equal(Volume.FromLiters(1), unit);
        }

        [Fact]
        public void PositiveInfinityFormatting()
        {
            Assert.Equal("∞ m", Length.FromMeters(double.PositiveInfinity).ToString());
        }

        /// <summary>
        ///     Convenience method to use the proper culture parameter type.
        ///     The UWP lib uses culture name string instead of CultureInfo.
        /// </summary>
        private static Culture GetCulture(string cultureName)
        {
#if WINDOWS_UWP
            return cultureName;
#else
            return new CultureInfo(cultureName);
#endif
        }
    }
}