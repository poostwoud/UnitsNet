<#
.SYNOPSIS
Generates the C# source code for logarithmic arithmetic operators.

.PARAMETER className
The name of the unit.

.PARAMETER baseUnitFieldName
The name of the backing field used to store the unit's value.

.PARAMETER baseType
The data type of the backing field used to store the unit's value.

.PARAMETER scalingFactor
The scaling factor used in logarithmic calculations. In most cases this is equal to 1.
#>
function GenerateLogarithmicArithmeticOperators([string]$className, [string]$baseUnitFieldName, [string]$baseType, [int]$scalingFactor)
{
    # Most logarithmic operators need a simple scaling factor of 10. However, certain units such as voltage ratio need to
    # use 20 instead of 10.
    $x = 10 * $scalingFactor;

@"

        #region Logarithmic Arithmetic Operators

        // Windows Runtime Component does not allow operator overloads: https://msdn.microsoft.com/en-us/library/br230301.aspx
#if !WINDOWS_UWP
        public static $className operator -($className right)
        {
            return new $className(-right.$baseUnitFieldName);
        }

        public static $className operator +($className left, $className right)
        {
            // Logarithmic addition
            // Formula: $x*log10(10^(x/$x) + 10^(y/$x))
            return new $className($x*Math.Log10(Math.Pow(10, left.$baseUnitFieldName/$x) + Math.Pow(10, right.$baseUnitFieldName/$x)));
        }

        public static $className operator -($className left, $className right)
        {
            // Logarithmic subtraction
            // Formula: $x*log10(10^(x/$x) - 10^(y/$x))
            return new $className($x*Math.Log10(Math.Pow(10, left.$baseUnitFieldName/$x) - Math.Pow(10, right.$baseUnitFieldName/$x)));
        }

        public static $className operator *($baseType left, $className right)
        {
            // Logarithmic multiplication = addition
            return new $className(left + right.$baseUnitFieldName);
        }

        public static $className operator *($className left, double right)
        {
            // Logarithmic multiplication = addition
            return new $className(left.$baseUnitFieldName + ($baseType)right);
        }

        public static $className operator /($className left, double right)
        {
            // Logarithmic division = subtraction
            return new $className(left.$baseUnitFieldName - ($baseType)right);
        }

        public static double operator /($className left, $className right)
        {
            // Logarithmic division = subtraction
            return Convert.ToDouble(left.$baseUnitFieldName - right.$baseUnitFieldName);
        }
#endif

        #endregion
"@;
}
