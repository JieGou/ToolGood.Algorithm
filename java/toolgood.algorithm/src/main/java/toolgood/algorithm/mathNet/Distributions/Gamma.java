package toolgood.algorithm.mathNet.Distributions;

import toolgood.algorithm.mathNet.SpecialFunctions;

public class Gamma {
    public static double CDF(double shape, double rate, double x)
    {
        //if (shape < 0.0 || rate < 0.0) {
        //    throw new ArgumentException(Resources.InvalidDistributionParameters);
        //}

        if (Double.isInfinite(rate)) {
            return x >= shape ? 1.0 : 0.0;
        }

        if (shape == 0.0 && rate == 0.0) {
            return 0.0;
        }

        return SpecialFunctions.GammaLowerRegularized(shape, x * rate);
    }

    public static double PDF(double shape, double rate, double x)
    {
        //if (shape < 0.0 || rate < 0.0) {
        //    throw new ArgumentException(Resources.InvalidDistributionParameters);
        //}

        if (Double.isInfinite(rate)) {
            return x == shape ? Double.POSITIVE_INFINITY : 0.0;
        }

        if (shape == 0.0 && rate == 0.0) {
            return 0.0;
        }

        if (shape == 1.0) {
            return rate * Math.exp(-rate * x);
        }

        if (shape > 160.0) {
            return Math.exp(PDFLn(shape, rate, x));
        }

        return Math.pow(rate, shape) * Math.pow(x, shape - 1.0) * Math.exp(-rate * x) / SpecialFunctions.Gamma(shape);
    }
    public static double PDFLn(double shape, double rate, double x)
    {
        //if (shape < 0.0 || rate < 0.0) {
        //    throw new ArgumentException(Resources.InvalidDistributionParameters);
        //}

        if (Double.isInfinite(rate)) {
            return x == shape ? Double.POSITIVE_INFINITY : Double.NEGATIVE_INFINITY;
        }

        if (shape == 0.0 && rate == 0.0) {
            return Double.NEGATIVE_INFINITY;
        }

        if (shape == 1.0) {
            return Math.log(rate) - (rate * x);
        }

        return (shape * Math.log(rate)) + ((shape - 1.0) * Math.log(x)) - (rate * x) - SpecialFunctions.GammaLn(shape);
    }

    public static double InvCDF(double shape, double rate, double p)
    {
        //if (shape < 0.0 || rate < 0.0) {
        //    throw new ArgumentException(Resources.InvalidDistributionParameters);
        //}
        //if (a < 0 || a.AlmostEqual(0.0)) {
        //    throw new ArgumentOutOfRangeException("a");
        //}

        //if (y0 < 0 || y0 > 1) {
        //    throw new ArgumentOutOfRangeException("y0");
        //}

        return SpecialFunctions.GammaLowerRegularizedInv(shape, p) / rate;
    }
}